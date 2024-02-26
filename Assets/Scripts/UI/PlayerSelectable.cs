/*
 * Author: Lance
 */
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;

public class PlayerSelectable : MonoBehaviour
{
    public TextMeshProUGUI playerNameText;
    public GameObject readyIcon;

    public void UpdateSelectable(string playerName, bool isReady)
    {
        readyIcon.SetActive(isReady);
        playerNameText.text = playerName;
    }
}