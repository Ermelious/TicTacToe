/*
 * Author: Lance
 */
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateLobbyPanel : AbstractUIPanel
{
    public TextMeshProUGUI lobbyNameInputField;
    public Button createButton, homeButton;
    public Slider boardSizeSlider;
    [SerializeField]
    private TextMeshProUGUI boardSizeText;

    public override void Awake()
    {
        base.Awake();
        boardSizeSlider.onValueChanged.AddListener((float value) => { boardSizeText.text = "Board Size: " + ((int)boardSizeSlider.value).ToString(); });
    }
}
