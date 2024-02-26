/*
 * Author: Lance
 */
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public partial class LobbyManager : NetworkBehaviour
{
    [ClientRpc]
    protected void SetCurrentLobbyUIPanelStatusClientRpc(bool value)
    {
        assets.currentLobbyPanel.SetStatus(value, true);
    }

    [ClientRpc]
    private void OnGameEndClientRpc()
    {
        LeaveLobby();
    }
}
