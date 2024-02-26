/*
 * Author: Lance
 */
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class JoinLobbyPanel : AbstractUIPanel
{
    public UnityEngine.UI.Button homeButton;
    public LobbySelectable lobbySelectablePrefab;
    public ObjectPool<LobbySelectable> lobbySelectablePool;
    public Transform lobbySelectableParent;
    public ScrollRect scrollRect;
    public TextMeshProUGUI lobbyCountText;

    public override void Awake()
    {
        base.Awake();
        lobbySelectablePool = new ObjectPool<LobbySelectable>(() =>
        {
            return Instantiate(lobbySelectablePrefab);
        },
        lobbySelectable => lobbySelectable.gameObject.SetActive(true),
        lobbySelectable => lobbySelectable.gameObject.SetActive(false),
        null,
        false,
        GlobalLobbyConstants.MAX_LOBBIES_PER_LIST,
        GlobalLobbyConstants.MAX_LOBBIES_PER_LIST);
    }
}
