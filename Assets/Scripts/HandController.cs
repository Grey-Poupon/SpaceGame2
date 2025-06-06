using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class Hand
{
    public List<Card> cards = new List<Card>();
    private Dictionary<Type, List<Card>> cardsByAction = new Dictionary<Type, List<Card>>();
    public List<Card> GetCardsByAction(Type actionType)
    {
        if (!cardsByAction.ContainsKey(actionType)){
            return new List<Card>();
        }
        return cardsByAction[actionType];
    }

    public Hand Clone()
    {
        Hand c_hand = new Hand();
        List<Card> cards = GetCards();
        foreach (Card card in cards)
        {
            c_hand.AddCard(card.Clone());
        }
        return c_hand;
    }

    public void Clear()
    {
        cards.Clear();
        cardsByAction.Clear();
    }

    public List<Card> GetCards()
    {
        return cards;
    }

    public void AddCard(Card card)
    {
        cards.Add(card);
        if (!cardsByAction.ContainsKey(card.cardAction.GetType()))
        {
            cardsByAction[card.cardAction.GetType()] = new List<Card>();
        }
        cardsByAction[card.cardAction.GetType()].Add(card);
    }
    public void DestroyAll()
    {
        foreach (var card in cards)
        {
            if (card.cardController != null) card.cardController.destroy();
        }
    }

}
