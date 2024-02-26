/*
 * Author: Lance
 */
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginUIPanel : AbstractUIPanel
{
    public TMP_InputField inputField;
    public TextMeshProUGUI warningText;
    public Button StartButton;

    public override void Awake()
    {
        base.Awake();
        warningText.gameObject.SetActive(false);
    }

    public void OnDisable()
    {
        warningText.gameObject.SetActive(false);
    }
}
