/*
 * Author: Lance
 */
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbySelectable : MonoBehaviour
{
    public TextMeshProUGUI lobbyNameText, playerCountText;
    public Button joinButton;
    public delegate void OnDisableDelegate();
    public OnDisableDelegate OnDisableAction;

    private void OnDisable()
    {
        OnDisableAction?.Invoke();
        OnDisableAction = null;
        joinButton.onClick.RemoveAllListeners();
    }
}