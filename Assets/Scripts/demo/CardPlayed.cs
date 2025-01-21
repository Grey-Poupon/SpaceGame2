using events;
using UnityEngine;

public class CardSubmit : MonoBehaviour
{
    public CardContainer container;

    public void OnCardSubmit(CardPlayed evt)
    {
        Card card = evt.card.gameObject.GetComponent<CardController>().card;
        if (card.CanBeUsed(GameManagerController.Instance.playerShip.AP))
        {
            GameManagerController.Instance.PickCard(card);
        }
        else
        {
            UnityEngine.Debug.Log(
                card.cardAction.name
                    + " Was not played because: Disabled: "
                    + card.cardAction.sourceRoom.disabled
                    + "| Destroyed: "
                    + card.cardAction.sourceRoom.destroyed
                    + (card.turnsUntilReady != 0 ? "| Action: Not Ready" : "| Action: Ready")
                    + "| Enough AP: "
                    + (
                        card.cardAction.cost <= GameManagerController.Instance.playerShip.AP
                    ).ToString()
            );
        }
    }
}

