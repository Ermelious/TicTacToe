/*
 * Author: Lance
 */
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerGameManager : GameManager
{
    [SerializeField]
    private MultiplayerAssets multiplayerAssets = new MultiplayerAssets();
    private PlayerController localPlayerController;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
    }

    [ClientRpc]
    private void AdjustCameraPositionClientRpc(Vector3 position) 
    {
        Camera.main.transform.position = position;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        StartCoroutine(WaitForPlayers());
        currentShape.OnValueChanged += (ShapeType previous, ShapeType current) => { DisplayMatchDetails(); };
    }

    /// <summary>
    /// Wait for network players to load in the network.
    /// </summary>
    /// <returns></returns>
    private IEnumerator WaitForPlayers()
    {
        while (NetworkObject.NetworkManager.LocalClient.PlayerObject == null)
            yield return null;
        
        localPlayerController = NetworkObject.NetworkManager.LocalClient.PlayerObject.GetComponent<PlayerController>();

        while (localPlayerController.PlayerControllers.Count != 2)
        {
            yield return new WaitForSeconds(0.1f);
        }

        //Debug.Log("Initilizing player");
        InitializePlayers();

        if (NetworkObject.NetworkManager.IsHost)
        {
            yield return new WaitForSeconds(1F);
            // Wait for Host to receive name from clients.
            foreach (PlayerController player in localPlayerController.PlayerControllers)
                while (string.IsNullOrEmpty(player.PlayerName.Value.ToString()))
                    yield return null;

            //Debug.Log("Sorting players");
            SortPlayers();
            AdjustCameraPositionClientRpc(GetCenterWorldPositionForCamera());
        }
    }

    public override void StartGame(int boardSize)
    {
        state.boardSize = boardSize;
        GenerateLevel();
        OnEndMatchCoroutine += DisplayGameResultClientRpc;
    }

    /// <summary>
    /// Sort Players into their respective shape type.
    /// </summary>
    public void SortPlayers()
    {
        // Randomly assign a shape to each player.
        localPlayerController.PlayerControllers[0].shapeTypeID.Value = (ShapeType)Random.Range(0, 1);
        localPlayerController.PlayerControllers[1].shapeTypeID.Value = localPlayerController.PlayerControllers[0].shapeTypeID.Value == ShapeType.Cross ? ShapeType.Circle : ShapeType.Cross;

        // Randomly pick which player makes the first move.
        currentShape.Value = Random.Range(0, 1) == 0 ? ShapeType.Cross : ShapeType.Circle;
    }

    /// <summary>
    /// Display match details on each turn based off which player is making a move.
    /// </summary>
    private void DisplayMatchDetails()
    {
        foreach (PlayerController playerController in localPlayerController.PlayerControllers)
        {
            if (playerController.IsLocalPlayer)
            {
                if (currentShape.Value == playerController.shapeTypeID.Value)
                {
                    multiplayerAssets.currentInstructionMessage.text = "Please make a move " + playerController.PlayerName.Value;
                    break;
                }
            }
            if (!playerController.IsLocalPlayer)
            {
                if (currentShape.Value == playerController.shapeTypeID.Value)
                {
                    multiplayerAssets.currentInstructionMessage.text = "Waiting for " + playerController.PlayerName.Value + " to make a move.";
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Spawn the level objects
    /// </summary>
    /// <param name="xCellIndex"></param>
    /// <param name="yCellIndex"></param>
    /// <returns></returns>
    protected override Isle SpawnIsle(int xCellIndex, int yCellIndex)
    {
        Isle freshIsle = Instantiate(assets.islePrefab, new Vector3(xCellIndex * 10, 0, yCellIndex * 10), Quaternion.identity, transform).GetComponent<Isle>();
        freshIsle.NetworkObject.Spawn();
        return freshIsle;
    }

    protected override void InitializePlayers()
    {
        localPlayerController.MakeAMove = MakeAMoveServerRpc;
    }

    /// <summary>
    /// All clients will send cellIndex and shapeID to the server and server will execute the function.
    /// </summary>
    /// <param name="cellIndex"></param>
    /// <param name="shapeID"></param>
    [ServerRpc(RequireOwnership = false)]
    public void MakeAMoveServerRpc(Vector2 cellIndex, ShapeType shapeID)
    {
        //Debug.Log("CellIndex: " + cellIndex + " Shape: " + shapeID);
        MakeAMove(cellIndex, shapeID);
    }

    protected override void MakeAMove(Vector2 cellIndex, ShapeType shapeID)
    {
        if (state.status != State.Status.Playing)
            return;

        if (assets.isles[cellIndex].isle.occupant != null)
            return;

        // Check if the Move is made by the current Player
        if (shapeID == currentShape.Value)
        {
            Occupant freshOccupant = SpawnOccupant(shapeID, assets.isles[cellIndex].isle.transform.position);
            freshOccupant.NetworkObject.Spawn();
            assets.isles[cellIndex].isle.occupant = freshOccupant;
            CompleteTurn(shapeID, cellIndex);
        }
    }

    /// <summary>
    /// Server will send the shapeType to all clients and clients will execute and display game result based of the Winner's shapeType.
    /// </summary>
    /// <param name="shapeType"></param>
    [ClientRpc]
    private void DisplayGameResultClientRpc(ShapeType shapeType)
    {
        StartCoroutine(DisplayGameResultCoroutine(shapeType));
    }

    private IEnumerator DisplayGameResultCoroutine(ShapeType shapeType)
    {
        // Get the name of the winner.
        FixedString64Bytes winner = (localPlayerController.PlayerControllers[0].shapeTypeID.Value == shapeType) ? localPlayerController.PlayerControllers[0].PlayerName.Value : localPlayerController.PlayerControllers[1].PlayerName.Value;

        assets.gameResultText.text = winner + " wins!";
        assets.gameResultText.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(2);
        assets.gameResultText.gameObject.SetActive(false);
        yield return new WaitForSecondsRealtime(0.2F);
        OnGameEnd?.Invoke();
    }

    [System.Serializable]
    public class MultiplayerAssets
    {
        public TextMeshProUGUI currentInstructionMessage;
    }
}
