/*
 * Author: Lance
 */
using LobbyRelaySample;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using WebSocketSharp;

public partial class LobbyManager : NetworkBehaviour
{
    protected Lobby currentLobby;
    protected float heartbeatInterval, lobbyUpdateInterval;
    [SerializeField]
    protected Assets assets;
    [SerializeField]
    protected States states;
    protected bool IsLobbyHost => currentLobby.HostId == AuthenticationService.Instance.PlayerId;
    LobbyEventCallbacks m_LobbyEventCallbacks = new LobbyEventCallbacks();

    private void OnValidate()
    {
        assets.mainPanel = GetComponentInChildren<MainPanel>(); 
        assets.createLobbyPanel = GetComponentInChildren<CreateLobbyPanel>();
        assets.joinLobbyPanel = GetComponentInChildren<JoinLobbyPanel>();
        assets.currentLobbyPanel = GetComponentInChildren<CurrentLobbyPanel>();
        assets.loginUIPanel = GetComponentInChildren<LoginUIPanel>();
    }

    private void Awake()
    {
        InitializeButtons();
    }

    // Start is called before the first frame update
    private async void Start()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += (ulong id) => LeaveLobby();

        assets.loginUIPanel.SetStatus(true, true);
        await UnityServices.InitializeAsync();
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyPollForUpdates();
    }

    /// <summary>
    /// Initialize all buttons within the Lobby panels.
    /// </summary>
    private void InitializeButtons()
    {
        assets.mainPanel.goToCreateLobbyButton.onClick.AddListener(() => { assets.createLobbyPanel.SetStatus(true, true); });
        assets.mainPanel.goToJoinLobbyButton.onClick.AddListener(() => { assets.joinLobbyPanel.SetStatus(true, true); ListLobbies(); });
        assets.createLobbyPanel.createButton.onClick.AddListener(() => { CreateLobby(assets.createLobbyPanel.lobbyNameInputField.text); });
        assets.createLobbyPanel.homeButton.onClick.AddListener(() => { assets.mainPanel.SetStatus(true, true); });
        assets.joinLobbyPanel.homeButton.onClick.AddListener(() => { assets.mainPanel.SetStatus(true, true); });
        assets.currentLobbyPanel.homeButton.onClick.AddListener(() => { LeaveLobby(); });
        assets.loginUIPanel.StartButton.onClick.AddListener(() =>
        {
            if (assets.loginUIPanel.inputField.text.IsNullOrEmpty() || string.IsNullOrWhiteSpace(assets.loginUIPanel.inputField.text))
            {
                assets.loginUIPanel.warningText.gameObject.SetActive(true);
            }
            else
            {
                assets.loginUIPanel.warningText.gameObject.SetActive(false);
                Authenticate(assets.loginUIPanel.inputField.text);
            }
        });
        assets.currentLobbyPanel.readyButton.onClick.AddListener(() =>
        {
            states.isReady = (states.isReady) ? false : true;
            UpdatePlayerStatus(states.isReady ? PlayerStatus.Ready : PlayerStatus.None);
        }
        );
        assets.mainPanel.startSinglePlayerButton.onClick.AddListener(StartSinglePlayer);
        assets.currentLobbyPanel.startMatchButton.onClick.AddListener(() =>
        {
            assets.currentLobbyPanel.startMatchButton.interactable = false;
            StartMultiplayerMatch();
        });
    }

    private async void HandleLobbyHeartbeat()
    {
        if (currentLobby != null && (IsLobbyHost))
        {
            heartbeatInterval -= Time.deltaTime;
            if (heartbeatInterval < 0)
            {
                float heartbeatTimerMax = 15;
                heartbeatInterval = heartbeatTimerMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            }
        }
    }

    bool inRelay = false;
    private async void HandleLobbyPollForUpdates()
    {
        if (currentLobby != null)
        {
            lobbyUpdateInterval -= Time.deltaTime;
            if (lobbyUpdateInterval < 0)
            {
                float lobbyUpdateIntervalMax = 1.1f;
                lobbyUpdateInterval = lobbyUpdateIntervalMax;
                currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                ListPlayers();
            }

            if (!inRelay)
                if (currentLobby != null)
                    if (currentLobby.Data[GlobalLobbyConstants.KEY_START_GAME].Value != "0")
                        if (!IsLobbyHost)
                        {
                            inRelay = true;
                            await JoinRelay(currentLobby.Data[GlobalLobbyConstants.KEY_START_GAME].Value);
                        }
        }
    }

    /// <summary>
    /// Start the multiplayer TicTacToe match.
    /// </summary>
    private async void StartMultiplayerMatch()
    {
        await StartMultiPlayer();
        while (NetworkManager.ConnectedClients.Count != 2)
            await Task.Delay(100);

        MultiplayerGameManager gameManager = Instantiate(assets.multiplayerGameManagerPrefab.gameObject, Vector3.zero, Quaternion.identity, transform).GetComponent<MultiplayerGameManager>();
        gameManager.NetworkObject.Spawn();
        gameManager.StartGame((int)assets.createLobbyPanel.boardSizeSlider.value);
        gameManager.OnGameEnd += OnGameEndClientRpc;
        SetCurrentLobbyUIPanelStatusClientRpc(false);
    }

    private async Task<string> CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            RelayServerData relayServerData = new RelayServerData(allocation, "udp");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartHost();

            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
        return null;
    }

    private async Task JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log("Joining Relay with " + joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            RelayServerData relayServerData = new RelayServerData(joinAllocation, "udp");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void CreateLobby(string lobbyName)
    {
        try
        {
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, AuthenticationService.Instance.PlayerName) },
                        { GlobalLobbyConstants.KEY_USERSTATUS, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, PlayerStatus.None.ToString()) }
                    }
                },
                Data = new Dictionary<string, DataObject>
                {
                    { GlobalLobbyConstants.KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "0") }

                }
            };
            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, GlobalLobbyConstants.MAX_PLAYERS, createLobbyOptions);
            SubscribeToLobbyEvents();
            ListPlayers();
            Debug.Log("Created Lobby! " + currentLobby.Name + " " + currentLobby.MaxPlayers + " " + currentLobby.Id + " " + currentLobby.LobbyCode);
            assets.currentLobbyPanel.SetStatus(true, true);
           // await StartMultiPlayer();            
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void JoinLobby(string id, int playerCount)
    {
        try
        {
            //Debug.Log(id + " | " + playerCount);
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();

            JoinLobbyByIdOptions joinLobbyByIdOptions = new JoinLobbyByIdOptions
            {
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, AuthenticationService.Instance.PlayerName) },
                        { GlobalLobbyConstants.KEY_USERSTATUS, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, PlayerStatus.None.ToString()) }
                    }
                }
            };

            currentLobby = await Lobbies.Instance.JoinLobbyByIdAsync(id, joinLobbyByIdOptions);
            SubscribeToLobbyEvents();
            ListPlayers();

            assets.currentLobbyPanel.SetStatus(true, true);
            //await JoinRelay(currentLobby.Data[GlobalLobbyConstants.KEY_START_GAME].Value);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void LeaveLobby()
    {
        await LeaveLobbyAsync();
    }

    private async Task LeaveLobbyAsync()
    {
        try
        {
            if (currentLobby != null)
                await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
            currentLobby = null;
            inRelay = false;
            NetworkManager.Singleton.Shutdown();
            assets.mainPanel.SetStatus(true, true);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void ListLobbies()
    {
        try
        {
            foreach(LobbySelectable lobbySelectable in assets.joinLobbyPanel.lobbySelectableParent.GetComponentsInChildren<LobbySelectable>())
            {
                assets.joinLobbyPanel.lobbySelectablePool.Release(lobbySelectable);
            }

            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>
                {
                    // Only list lobbies that has more than 0 available slots.
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };

            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);
            Debug.Log("Lobbies found: " + queryResponse.Results.Count);
            foreach (Lobby lobby in queryResponse.Results)
            {
                LobbySelectable lobbySelectable = assets.joinLobbyPanel.lobbySelectablePool.Get();
                lobbySelectable.transform.SetParent(assets.joinLobbyPanel.lobbySelectableParent);
                lobbySelectable.transform.localScale = Vector3.one;
                lobbySelectable.lobbyNameText.text = lobby.Name;
                lobbySelectable.playerCountText.text = lobby.Players.Count + " / " + lobby.MaxPlayers;
                lobbySelectable.joinButton.onClick.AddListener(() => { JoinLobby(lobby.Id, lobby.MaxPlayers); });
                lobbySelectable.OnDisableAction += () => assets.joinLobbyPanel.lobbySelectablePool.Release(lobbySelectable);
                Debug.Log(lobby.Name + " " + lobby.Id + " " + lobby.MaxPlayers);
            }

            assets.joinLobbyPanel.scrollRect.verticalNormalizedPosition = 1;
            assets.joinLobbyPanel.lobbyCountText.text = "Found " + queryResponse.Results.Count + " Lobby(s).";
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    
    private void ListPlayers()
    {
        foreach (PlayerSelectable playerSelectable in assets.currentLobbyPanel.playerSelectableParent.GetComponentsInChildren<PlayerSelectable>())
            assets.currentLobbyPanel.playerSelectablePool.Release(playerSelectable); //.gameObject.SetActive(false);

        bool isReadyToStartMatch = true;
        if (currentLobby != null)
            foreach (Player player in currentLobby.Players)
            {
                PlayerSelectable playerSelectable = assets.currentLobbyPanel.playerSelectablePool.Get().GetComponent<PlayerSelectable>();
                playerSelectable.transform.SetParent(assets.currentLobbyPanel.playerSelectableParent);
                playerSelectable.transform.localScale = Vector3.one;
                bool isReady = (player.Data[GlobalLobbyConstants.KEY_USERSTATUS].Value == PlayerStatus.Ready.ToString()) ? true : false;
                playerSelectable.UpdateSelectable(player.Data["PlayerName"].Value, isReady);
                if (!isReady)
                    isReadyToStartMatch = false;
            }
        if (currentLobby.Players.Count > 1)
            if (currentLobby.HostId == AuthenticationService.Instance.PlayerId)
                assets.currentLobbyPanel.startMatchButton.gameObject.SetActive(isReadyToStartMatch);
    }

    private async void UpdatePlayerStatus(PlayerStatus status)
    {
        UpdatePlayerOptions updatePlayerOptions = new UpdatePlayerOptions
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { GlobalLobbyConstants.KEY_USERSTATUS, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, status.ToString()) }
            }
        };

        await Lobbies.Instance.UpdatePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId, updatePlayerOptions);
    }

    private async void Authenticate(string playerName)
    {
        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(playerName);

        await UnityServices.InitializeAsync(initializationOptions);

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Successfully logged in as " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);

        assets.mainPanel.SetStatus(true, true);
    }

    private void StartSinglePlayer()
    {
        GameManager gameManager = Instantiate(assets.singlePlayerGameManagerPrefab, Vector3.zero, Quaternion.identity, transform);
        gameManager.StartGame((int)assets.mainPanel.boardSizeSlider.value);
        gameManager.OnGameEnd = OnGameEnd;
        assets.mainPanel.SetStatus(false, true);
    }

    private async Task StartMultiPlayer()
    {
        if (IsLobbyHost)
        {
            try
            {
                string relayCode = await CreateRelay();

                Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { GlobalLobbyConstants.KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                    }
                });
            } 
            catch (Exception ex) 
            {
                Debug.Log(ex);
            }
        }
    }
    private async void SubscribeToLobbyEvents()
    {
        m_LobbyEventCallbacks.LobbyDeleted += async () =>
        {
            await LeaveLobbyAsync();
            ListPlayers();
        };

        m_LobbyEventCallbacks.LobbyChanged += (ILobbyChanges obj) =>
        {
            ListLobbies();
        };

        await LobbyService.Instance.SubscribeToLobbyEventsAsync(currentLobby.Id, m_LobbyEventCallbacks);
    }

    private void OnGameEnd()
    {
        assets.mainPanel.SetStatus(true, true);
    }

    [System.Serializable]
    public class Assets
    {
        public MainPanel mainPanel;
        public CreateLobbyPanel createLobbyPanel;
        public JoinLobbyPanel joinLobbyPanel;
        public CurrentLobbyPanel currentLobbyPanel;
        public LoginUIPanel loginUIPanel;
        public GameManager singlePlayerGameManagerPrefab, multiplayerGameManagerPrefab;
    }

    [System.Serializable]
    public class States
    {
        public int localPlayerIndex = 0;
        public bool isReady = false;
        public Dictionary<int, LocalPlayer> localPlayers = new Dictionary<int, LocalPlayer>();
    }
}
