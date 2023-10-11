using System.ComponentModel.Design.Serialization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Card : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    private Canvas canvas;
    private RectTransform cardRectTransform;
    private bool isDragging = false;
    private bool isPointing = false;
    private bool isMouseOver = false;
    private Vector3 offset;
    public RoomType roomType;
    public bool targetSelf;
    public Room sourceRoom;
    private TextMeshProUGUI Title;
    private TextMeshProUGUI Cooldown;
    private TextMeshProUGUI Cost;
    private TextMeshProUGUI Description;
    public float disappearDistance = 100;


    public void setup(string title, string cooldown, string cost, string description, RoomType roomType, bool targetSelf, Room sourceRoom)
    {
        // Find the TextMeshPro objects by name
        Title       = transform.Find("Title").GetComponent<TextMeshProUGUI>();
        Cooldown    = transform.Find("Cooldown").GetComponent<TextMeshProUGUI>();
        Cost        = transform.Find("Cost").GetComponent<TextMeshProUGUI>();
        Description = transform.Find("Description").GetComponent<TextMeshProUGUI>();

        this.roomType = roomType;
        this.Title.text = title ;
        this.Cooldown.text = cooldown ;
        this.Cost.text = cost ;
        this.Description.text = description ;
        this.targetSelf = targetSelf;
        this.sourceRoom = sourceRoom;
        
    }

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        cardRectTransform = GetComponent<RectTransform>();
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

            if (cardRectTransform.anchoredPosition.y > -450)
            {  
                GameManager.Instance.PlayCard(this);
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
        //         GameManager.Instance.PlayCard(this);
        //     }
        // }
    }

    public void OnPointerUp(PointerEventData eventData)
    {        
    }
}
