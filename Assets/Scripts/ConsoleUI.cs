using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ConsoleUI : MonoBehaviour
{
    public TMP_InputField inputField;

    private void Start()
    {
        // Attach a listener to the input field's "End Edit" event.
        inputField.onEndEdit.AddListener(ReadUserInput);
        Debug.Log("name a card to play it, then click a room");

    }

    private void ReadUserInput(string inputText)
    {   
        if (inputText.ToLower() == "end")
        {
            GameManagerController.Instance.FinishTurn();
        }
        List<string> cardNames = new List<string>();
        foreach (Card card in GameManagerController.Instance.playerHand.GetCards())
        {
            cardNames.Add(card.cardAction.name);
            if (inputText.ToLower() == card.cardAction.name.ToLower())
            {
                GameManagerController.Instance.PickCard(card);
                break;
            }
        }
        Debug.Log(string.Join("\n", cardNames));
        // Write the input text to the Unity console.


        // Clear the input field after logging the input.
        inputField.text = "";
    }
}
