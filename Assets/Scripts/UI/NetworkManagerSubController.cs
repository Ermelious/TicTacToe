/*
 * Author: Lance
 */
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkManagerSubController : MonoBehaviour
{
    [SerializeField]
    private NetworkManagerPanel panel;

    private void Awake()
    {
        panel.serverButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
        });

        panel.hostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
        });

        panel.clientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
        });
    }
}
