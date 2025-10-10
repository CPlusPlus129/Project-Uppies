using System;
using UnityEngine;
using UnityEngine.UI;

public class ClickTheButton : MonoBehaviour
{
    public static event Action<String> ButtonPressed = delegate { };

    private int splitID;
    private string buttonName;
    private string buttonNumber;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        buttonName =gameObject.name;
        splitID = buttonName.IndexOf("-");
        buttonNumber = buttonName.Substring(0, splitID);

        gameObject.GetComponent<Button>().onClick.AddListener(ButtonClicked);
    }

    private void ButtonClicked()
    {
        ButtonPressed(buttonNumber);
    }
}
