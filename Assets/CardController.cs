using System.Diagnostics;
using System.Globalization;
using System.ComponentModel.Design.Serialization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Card
{

    public CardAction cardAction;
    public CardController cardController;
    public int turnsUntilReady = 0;
    public Card(CardAction cardAction)
    {
        this.cardAction = cardAction;
        this.cardAction.card = this;
    }
    public void UpdateText()
    {
        this.cardController.UpdateText(cardAction);
    }
    public void Setup(CardController cardController)
    {
        this.cardController=cardController;
    }
    public bool CanBeUsed(float AP)
    {
        return cardAction.CanBeUsed(AP);
    }
    public bool IsReady()
    {
        return cardAction.IsReady();
    }
    public void NextTurn()
    {
        if (turnsUntilReady > 0)
        {
            turnsUntilReady -= 1;
            if (cardAction.IsReady())
            {
                if (cardController) cardController.gameObject.SetActive(true);
            }
        }
    }
}
public class CardController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public float disappearDistance = 100;
    public Card card;
    private Canvas canvas;
    private RectTransform cardRectTransform;
    private bool isDragging = false;
    private bool isPointing = false;
    private bool isMouseOver = false;
    private Vector3 offset;
    private TextMeshProUGUI Title;
    private TextMeshProUGUI Cooldown;
    private TextMeshProUGUI Cost;
    private TextMeshProUGUI Description;
    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        cardRectTransform = GetComponent<RectTransform>();
        
        // Find the TextMeshPro objects by name
        Title       = transform.Find("Title").GetComponent<TextMeshProUGUI>();
        Cooldown    = transform.Find("Cooldown").GetComponent<TextMeshProUGUI>();
        Cost        = transform.Find("Cost").GetComponent<TextMeshProUGUI>();
        Description = transform.Find("Description").GetComponent<TextMeshProUGUI>();

        if (card != null){UpdateText(card.cardAction);}
    }
    private void Update()
    {
        if (isDragging)
        {
            // Calculate the new position of the card based on the mouse position
            Vector3 newPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition) + offset;

            // Restrict the card's movement to the plane of the screen
            newPosition.z = 0f;

            // Update the card's position
            cardRectTransform.position = newPosition;

            if (cardRectTransform.anchoredPosition.y > -450 && card.turnsUntilReady==0)
            {  
                GameManager.Instance.PickCard(card);
                isDragging = false;
            }
        }
    }
    void OnMouseDown()
    {
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isDragging)
        {
            isDragging = false;
            LayoutRebuilder.MarkLayoutForRebuild(GetComponentInParent<RectTransform>());
            return;
        }
        
        isDragging = GameManager.Instance.turn == TurnTypes.Player && GameManager.Instance.playerShip.AP > 0;
        if (isDragging)
        {
            offset = cardRectTransform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // if (isDragging)
        // {
        //     // Move the card with the mouse cursor
        //     cardRectTransform.anchoredPosition += eventData.delta; /// canvas.scaleFactor;

        //     // Check if the card should disappear (you can adjust the threshold)
        //     if (cardRectTransform.anchoredPosition.y > -450)
        //     {  
        //         GameManager.Instance.PickCard(this);
        //     }
        // }
    }

    public void OnPointerUp(PointerEventData eventData)
    {        
    }

    public void UpdateText(CardAction cardAction)
    {
        if(this.Title != null && this.Cooldown != null && this.Cost != null && this.Description != null)
        {
            string name_text        = cardAction.name;                    
            string cooldown_text    = cardAction.cooldown.ToString();                        
            string cost_text        = cardAction.cost.ToString();                        
            string description_text = cardAction.description.ToString();                        
            
            this.Title.text       = name_text;
            this.Cooldown.text    = cooldown_text;
            this.Cost.text        = cost_text;
            this.Description.text = description_text;
        }
    }

    public void Setup(Card card)
    {
        this.card=card;
        UpdateText(card.cardAction);
    }
}
