using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomController : MonoBehaviour
{
    private float health = 100;
    private SpriteRenderer spriteRenderer;

    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnMouseDown()
    {
        GameManager.Instance.FireLaserAtTarget(Camera.main.ScreenToWorldPoint(Input.mousePosition));

        TakeDamage(33f);
    }

    public void TakeDamage(float damage)
    {
        health = health < damage ? 0 : health - damage;

        // Increase the red component of the color
        float redness = 1 - (health/100);
        Color currentColor = spriteRenderer.color;
        currentColor.r = currentColor.r + ((1-currentColor.r) * redness);
        spriteRenderer.color = currentColor;

    }
}