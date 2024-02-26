/*
 * Author: Lance
 */
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using static GameManager;

public abstract class GameManager : NetworkBehaviour
{
    public enum ShapeType { None = -1, Cross, Circle }

    [SerializeField]
    protected Assets assets;
    [SerializeField]
    protected State state;
    [SerializeField]
    protected Settings settings;
    public delegate void OnGameEndDelegate();
    public OnGameEndDelegate OnGameEnd;
    public delegate void OnEndMatchCoroutineDelegate(ShapeType shapeType);
    public OnEndMatchCoroutineDelegate OnEndMatchCoroutine;
    //    public ShapeType currentShape = ShapeType.None;
    public NetworkVariable<ShapeType> currentShape = new NetworkVariable<ShapeType>(ShapeType.None);

    protected virtual void Awake()
    {
        assets.gameResultText.gameObject.SetActive(false);
        state.moveDirection.Add(State.MoveDirection.Left, new Vector2(-1, 0));
        state.moveDirection.Add(State.MoveDirection.Right, new Vector2(1, 0));
        state.moveDirection.Add(State.MoveDirection.Up, new Vector2(0, 1));
        state.moveDirection.Add(State.MoveDirection.Down, new Vector2(0, -1));
        state.moveDirection.Add(State.MoveDirection.TopLeft, new Vector2(-1, 1));
        state.moveDirection.Add(State.MoveDirection.TopRight, new Vector2(1, 1));
        state.moveDirection.Add(State.MoveDirection.BottomLeft, new Vector2(-1, -1));
        state.moveDirection.Add(State.MoveDirection.BottomRight, new Vector2(1, -1));
    }

    // Start is called before the first frame update
    protected abstract void Start();

    public virtual void StartGame(int boardSize)
    {
        state.boardSize= boardSize;
        GenerateLevel();
        InitializePlayers();
    }

    /// <summary>
    /// Generate the level in the scene.
    /// </summary>
    protected void GenerateLevel()
    {
        Isle freshIsle = null;
        Vector2 cellIndex;

        for (int xCellIndex = 0; xCellIndex < state.boardSize; xCellIndex++)
            for (int yCellIndex = 0; yCellIndex < state.boardSize; yCellIndex++)
            {
                cellIndex = new Vector2(xCellIndex, yCellIndex);
                freshIsle = SpawnIsle(xCellIndex, yCellIndex);
                assets.isles.Add(cellIndex, new IsleData { isle = freshIsle, shape = ShapeType.None });
                freshIsle.Init(cellIndex);
            }

        Camera.main.transform.position = GetCenterWorldPositionForCamera();
    }

    /// <summary>
    /// Spawn a specified shapeType occupant at the specified position. Returns the spawned occupant.
    /// </summary>
    /// <param name="shapeType"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    protected Occupant SpawnOccupant(ShapeType shapeType, Vector3 position)
    {
        switch (shapeType)
        {
            case ShapeType.Cross:
                return Instantiate(assets.crossShapePrefab, position, assets.crossShapePrefab.transform.rotation, transform).GetComponent<Occupant>();
                
            case ShapeType.Circle:
                return Instantiate(assets.circleShapePrefab, position, assets.circleShapePrefab.transform.rotation, transform).GetComponent<Occupant>();
        }
        return null;
    }

    protected Vector3 GetCenterWorldPositionForCamera()
    {
        Vector3 position = assets.isles[new Vector2(state.boardSize / 2, state.boardSize / 2)].isle.transform.position;
        return new Vector3(position.x, state.boardSize * 12, position.z);
    }

    /// <summary>
    /// Spawn an isle based off the specified coordinates.
    /// </summary>
    /// <param name="xCellIndex"></param>
    /// <param name="yCellIndex"></param>
    /// <returns></returns>
    protected abstract Isle SpawnIsle(int xCellIndex, int yCellIndex);

    /// <summary>
    /// Initialize local players
    /// </summary>
    protected abstract void InitializePlayers();

    /// <summary>
    /// Make a move based off the specified coordinates.
    /// </summary>
    /// <param name="cellIndex"></param>
    /// <param name="shapeID"></param>
    protected abstract void MakeAMove(Vector2 cellIndex, ShapeType shapeID);

    /// <summary>
    /// Process the previous move data and pass the turn over to the next player.
    /// </summary>
    /// <param name="shapeID"></param>
    /// <param name="cellIndex"></param>
    protected void CompleteTurn(ShapeType shapeID, Vector2 cellIndex)
    {
        CheckForMatchComplete(shapeID, cellIndex);
        if (state.status == State.Status.GameOver)
            return;
        // Completes the turn of the current Player and allows the next player to make a move.
        currentShape.Value = (shapeID == ShapeType.Circle) ? ShapeType.Cross : ShapeType.Circle;
    }

    /// <summary>
    /// Process the current MakeAMove action, check the level for consecutive shapes of at least 3. If found, end the game.
    /// </summary>
    /// <param name="shapeType"></param>
    /// <param name="cellIndex"></param>
    private void CheckForMatchComplete(ShapeType shapeType, Vector2 cellIndex)
    {
        //Debug.Log("____________________ CHECKING FOR MATCH COMPLETE _________________________ " + shapeType.ToString());
        foreach (KeyValuePair<State.MoveDirection, Vector2> moveDirection in state.moveDirection) 
        {
            Vector2 currentCell = cellIndex;
            // Get to the end of the board based off moveDirection.
            while (assets.isles.ContainsKey(currentCell))
                currentCell += moveDirection.Value;

            int consecutiveShapesCount = 0;
            // Once we're at the end of the board we invert the direction and scan for consecutive shapes.
            while (assets.isles.ContainsKey(currentCell + (moveDirection.Value) * -1))
            {
                currentCell += (moveDirection.Value) * -1;
                if (assets.isles[currentCell].isle.occupant)
                    if (assets.isles[currentCell].isle.occupant.Shape == shapeType)
                    {
                        consecutiveShapesCount++;
                        //Debug.Log(moveDirection.Key.ToString() + " | " + consecutiveShapesCount + " | " + shapeType);
                        if (consecutiveShapesCount == 3)
                        {
                            Debug.Log("Match Complete, " + shapeType.ToString() + " is the winner.");
                            state.status = State.Status.GameOver;
                            OnEndMatchCoroutine(shapeType);
                            return;
                        }
                    }
                    else
                        consecutiveShapesCount = 0;
            }
        }
    }

    [System.Serializable]
    public class Assets
    {
        public GameObject islePrefab = null;
        public Dictionary<Vector2, IsleData> isles = new Dictionary<Vector2, IsleData>();
        public PlayerController playerPrefab = null;
        public Occupant crossShapePrefab, circleShapePrefab = null;
        public TextMeshProUGUI gameResultText = null;
    }

    [System.Serializable]
    public class IsleData
    {
        public ShapeType shape;
        public Isle isle;
    }

    [System.Serializable]
    public class State
    {
        public enum MoveDirection { Left, Right, Up, Down, TopLeft, TopRight, BottomLeft, BottomRight }
        public enum Status { Waiting = -1, Playing, GameOver }
        public Dictionary<MoveDirection, Vector2> moveDirection = new Dictionary<MoveDirection, Vector2>();
        public Status status = Status.Playing;
        [SerializeField]
        public int boardSize = 3;
    }

    [System.Serializable]
    public class Settings
    {
        [SerializeField]
        private int playerCount = 1;
        public int PlayersCount => playerCount;
    }
}