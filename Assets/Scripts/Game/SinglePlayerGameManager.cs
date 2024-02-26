/*
 * Author: Lance
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SinglePlayerGameManager : GameManager
{
    protected override void Start()
    {
        OnEndMatchCoroutine += DisplayGameResult;
    }

    /// <summary>
    /// Initialize players, set the second player as A.I for automated MakeAMove.
    /// </summary>
    protected override void InitializePlayers()
    {
        PlayerController freshPlayer;
        for (int playerIndex = 0; playerIndex < 2; playerIndex++)
        {
            freshPlayer = Instantiate(assets.playerPrefab, Vector3.zero, Quaternion.identity, transform).GetComponent<PlayerController>();
            freshPlayer.shapeTypeID.Value = (ShapeType)playerIndex;
            // If there is only 1 player, we set the last player as A.I
            if (playerIndex == 1 && settings.PlayersCount == 1)
            {
                //freshPlayer = assets.playerSpawnableData.Spawn(transform, Vector3.zero, Quaternion.identity).GetComponent<Player>();
                freshPlayer.isAI = true;
                freshPlayer.AutomatedMakeAMove = AutomatedMakeAMove;
                //freshPlayer.shapeTypeID = playerIndex;
            }
            else
                freshPlayer.MakeAMove = MakeAMove;
        }

        // Randomly pick which player goes first.
        currentShape.Value = Random.Range(0, 2) == 0 ? ShapeType.Cross : ShapeType.Circle;
    }

    /// <summary>
    /// Manually make a move based of specified coordinates.
    /// </summary>
    /// <param name="cellIndex"></param>
    /// <param name="shapeID"></param>
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
            assets.isles[cellIndex].isle.occupant = freshOccupant;
            CompleteTurn(shapeID, cellIndex);
        }
    }

    /// <summary>
    /// Pick a random cell.
    /// </summary>
    /// <param name="shapeTypeID"></param>
    private void AutomatedMakeAMove(ShapeType shapeTypeID)
    {
        if (state.status != State.Status.Playing)
            return;

        if (shapeTypeID != currentShape.Value)
            return;

        List<Isle> isles = new List<Isle>();
        foreach (KeyValuePair<Vector2, IsleData> isleData in assets.isles)
            if (isleData.Value.isle.occupant == null)
                isles.Add(isleData.Value.isle);

        int random = Random.Range(0, isles.Count);

        Isle targetIsle = isles[random];
        Occupant freshOccupant = SpawnOccupant(shapeTypeID, targetIsle.transform.position);
        assets.isles[targetIsle.CellIndex.Value].isle.occupant = freshOccupant;
        CompleteTurn(shapeTypeID, targetIsle.CellIndex.Value);
    }

    /// <summary>
    /// Spawn an isle.
    /// </summary>
    /// <param name="xCellIndex"></param>
    /// <param name="yCellIndex"></param>
    /// <returns></returns>
    protected override Isle SpawnIsle(int xCellIndex, int yCellIndex)
    {
        return Instantiate(assets.islePrefab, new Vector3(xCellIndex * 10, 0, yCellIndex * 10), Quaternion.identity, transform).GetComponent<Isle>();
    }

    /// <summary>
    /// Display the result of the match at the end.
    /// </summary>
    /// <param name="shapeType"></param>
    private void DisplayGameResult(ShapeType shapeType)
    {
        StartCoroutine(DisplayGameResultCoroutine(shapeType));
    }

    private IEnumerator DisplayGameResultCoroutine(ShapeType shapeType)
    {
        assets.gameResultText.text = shapeType.ToString() + " wins!";
        assets.gameResultText.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(2);
        assets.gameResultText.gameObject.SetActive(false);
        yield return new WaitForSecondsRealtime(0.2F);
        OnGameEnd?.Invoke();
        Destroy(gameObject);
    }
}
