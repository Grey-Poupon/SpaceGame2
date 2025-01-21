using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

    public Card Clone()
    {
        CardAction c_cardAction = cardAction.Clone();
        Card c_card = new Card(c_cardAction);
        c_card.turnsUntilReady = this.turnsUntilReady;
        return c_card;
    }

    public void UpdateText()
    {
        this.cardController.UpdateText(cardAction);
    }

    public void Setup(CardController cardController)
    {
        this.cardController = cardController;
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
            if (turnsUntilReady < 1)
            {
                if (cardController)
                    cardController.ToggleTransparency(false);
            }
        }
    }
}

public class CardController : MonoBehaviour
{
    public float disappearDistance = -450;
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
    private SpriteRenderer[] childSprites;

    private TextMeshProUGUI[] childTexts;

    [SerializeField]
    private float defaultAlpha = 0.5f;

    void Start()
    {
        childSprites = GetComponentsInChildren<SpriteRenderer>();
        childTexts = GetComponentsInChildren<TextMeshProUGUI>();
        canvas = GetComponentInParent<Canvas>();
        cardRectTransform = GetComponent<RectTransform>();

        // Find the TextMeshPro objects by name
        Title = transform.Find("Title").GetComponent<TextMeshProUGUI>();
        Cooldown = transform.Find("Cooldown").GetComponent<TextMeshProUGUI>();
        Cost = transform.Find("Cost").GetComponent<TextMeshProUGUI>();
        Description = transform.Find("Description").GetComponent<TextMeshProUGUI>();

        if (card != null)
        {
            UpdateText(card.cardAction);
        }
    }

    private void Update() { }

    public void UpdateText(CardAction cardAction)
    {
        if (
            this.Title != null
            && this.Cooldown != null
            && this.Cost != null
            && this.Description != null
        )
        {
            string name_text = cardAction.name;
            string cooldown_text = cardAction.cooldown.ToString();
            string cost_text = cardAction.cost.ToString();
            string description_text = cardAction.description.ToString();

            this.Title.text = name_text;
            this.Cooldown.text = cooldown_text;
            this.Cost.text = cost_text;
            this.Description.text = description_text;
        }
    }

    public void Setup(Card card)
    {
        this.card = card;
        UpdateText(card.cardAction);
    }
        public void SetTransparency(float alpha)
    {

        alpha = Mathf.Clamp01(alpha);

        // Adjust sprite renderer transparencies
        foreach (SpriteRenderer sprite in childSprites)
        {
            Color spriteColor = sprite.color;
            spriteColor.a = alpha;
            sprite.color = spriteColor;
        }

        // Adjust TextMeshPro text transparencies
        foreach (TextMeshProUGUI text in childTexts)
        {
            Color textColor = text.color;
            textColor.a = alpha;
            text.color = textColor;
        }
    }

    // Toggle on/off (fully transparent or fully opaque)
    public void ToggleTransparency(bool on)
    {
        float targetAlpha = on ? 0.5f : 1f;
        SetTransparency(targetAlpha);
    }
    public void destroy(){
        Destroy(this.gameObject);
    }
}
