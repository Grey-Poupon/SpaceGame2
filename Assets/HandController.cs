using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;


public class HandController : MonoBehaviour
{
    private HorizontalLayoutGroup layoutGroup;
    private void Awake()
    {
        Hand playerHand = new Hand();
        playerHand.layoutGroup = layoutGroup;
        playerHand.parent = this;

        GameManager.Instance.RegisterPlayerHand(playerHand);
        GameManager.Instance.RegisterEnemyHand(new Hand());
        layoutGroup = GetComponent<HorizontalLayoutGroup>();
        Organise();
    }
    public void Organise()
    {
        if(layoutGroup != null){LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());}
    }
}
public class Hand
{
        private List<Card> cards = new List<Card>();
        private Dictionary<Type, List<Card>> cardsByAction = new Dictionary<Type, List<Card>>();
        public HorizontalLayoutGroup layoutGroup;
        public HandController parent;

    public void Organise()
    {
        if (parent != null) {parent.Organise();}
    }
    public List<Card> GetCardsByAction(Type actionType)
    {
        return cardsByAction[actionType];
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
}