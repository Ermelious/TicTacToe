/*
 * Author: Lance
 */
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainPanel : AbstractUIPanel
{
    public Button startSinglePlayerButton;
    public Button goToCreateLobbyButton;
    public Button goToJoinLobbyButton;
    public Slider boardSizeSlider;
    [SerializeField]
    private TextMeshProUGUI boardSizeText;

    public override void Awake()
    {
        base.Awake();
        boardSizeSlider.onValueChanged.AddListener((float value) => { boardSizeText.text = "Board Size: " + ((int)boardSizeSlider.value).ToString(); });
    }
}
