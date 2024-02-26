/*
 * Author: Lance
 */
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

public class CurrentLobbyPanel : AbstractUIPanel
{
    public Button homeButton, readyButton, startMatchButton;
    public ObjectPool<PlayerSelectable> playerSelectablePool;
    public Transform playerSelectableParent;
    public PlayerSelectable playerSelectablePrefab;

    private void OnEnable()
    {
        startMatchButton.gameObject.SetActive(false);
        startMatchButton.interactable = true;
    }

    private void OnDisable()
    {
        startMatchButton.interactable = true;
        startMatchButton.gameObject.SetActive(false);
    }

    public override void Awake()
    {
        base.Awake();
        playerSelectablePool = new ObjectPool<PlayerSelectable>(() =>
        {
            PlayerSelectable playerSelectable = Instantiate(playerSelectablePrefab);
            return playerSelectable;
        },
        playerSelectable => { playerSelectable.gameObject.SetActive(true); }
,
        playerSelectable => { playerSelectable.gameObject.SetActive(false); },
        null,
        false,
        GlobalLobbyConstants.MAX_PLAYERS,
        GlobalLobbyConstants.MAX_PLAYERS);
    }
}
