using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Hand : MonoBehaviour
{
    public Card cardPrefab; // Reference to your card prefab
    private List<Card> cards = new List<Card>();
    private HorizontalLayoutGroup layoutGroup;

    private void Awake()
    {
        GameManager.Instance.RegisterPlayerHand(this);
        layoutGroup = GetComponent<HorizontalLayoutGroup>();
    }

    public void AddCard(string title, string cooldown, string cost, string description, RoomType roomType, bool targetSelf, Room sourceRoom)
    {
        // Instantiate a new card prefab
        Card newCard = Instantiate(cardPrefab, transform);
        newCard.setup(title, cooldown, cost, description, roomType, targetSelf, sourceRoom);
        cards.Add(newCard);
        
        // Ensure the card is properly positioned within the layout group
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
    }

    public void RemoveCard(int i)
    {
        // Remove the last card from the list
        Card cardToRemove = cards[i];
        cards.Remove(cardToRemove);
        
        // Destroy the card GameObject
        Destroy(cardToRemove);
        if (cards.Count > 0)
        {
            // Ensure the layout is updated after removing the card
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
        }
    }
}
