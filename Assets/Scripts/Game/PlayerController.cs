/*
 * Author: Lance
 */
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    public delegate void MakeAMoveDelegate(Vector2 cellIndex, GameManager.ShapeType shapeTypeID);
    public MakeAMoveDelegate MakeAMove;
    public delegate void AutomatedMakeAMoveDelegate(GameManager.ShapeType shapeTypeID);
    public AutomatedMakeAMoveDelegate AutomatedMakeAMove;
    public NetworkVariable<GameManager.ShapeType> shapeTypeID = new NetworkVariable<GameManager.ShapeType>();
    public bool isAI = false;
    private bool isReady = false;
    public bool IsReady => isReady;
    public delegate void ToggleIsReadyDelegate(bool value);
    public ToggleIsReadyDelegate ToggleIsReady;
    [SerializeField]
    private NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString64Bytes> PlayerName => playerName;
    private static List<PlayerController> playerControllers = new List<PlayerController>();
    public List<PlayerController> PlayerControllers => playerControllers;

    [ServerRpc]
    public void ToggleIsReadyServerRpc()
    {
        isReady = (isReady) ? false : true;
        ToggleIsReady?.Invoke(isReady);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        playerControllers.Add(this);

        if (IsOwner)
            playerName.Value = AuthenticationService.Instance.PlayerName;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        playerControllers.Remove(this);
    }

    public void Update()
    {
        // If this is an actual Player we wait for the player to make a move.
        if (!isAI)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                Isle isle;
                if (Physics.Raycast(ray, out hit))
                {
                    isle = hit.transform.GetComponent<Isle>();
                    if (isle != null)
                    {
                        MakeAMove?.Invoke(isle.CellIndex.Value, shapeTypeID.Value);
                    }
                }
            }
        }
        else
            AutomatedMakeAMove?.Invoke(shapeTypeID.Value);
    }
}