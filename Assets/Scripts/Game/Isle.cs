/*
 * Author: Lance
 */
using Unity.Netcode;
using UnityEngine;

public class Isle : NetworkBehaviour
{
    [SerializeField]
    private NetworkVariable<Vector2> cellIndex = new NetworkVariable<Vector2>();
    public NetworkVariable<Vector2> CellIndex => cellIndex;
    public Occupant occupant;

    public void Init(Vector2 cellIndex)
    {
        this.cellIndex.Value = cellIndex;
    }
}
