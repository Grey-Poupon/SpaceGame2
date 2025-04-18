using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class TestRoom : MonoBehaviour, IPointerClickHandler
{
    public UnityEvent onRoomClick;

    public float outlineWidth = 0.1f;
    public Material outlineMaterial;
    private LineRenderer lineRenderer;
    private SpriteRenderer internalRooms;

   protected void Start()
    {
        GenerateOutline();
        internalRooms = gameObject.GetComponent<SpriteRenderer>();
        HideOutline();
    }


    void GenerateOutline()
    {
        PolygonCollider2D polygonCollider = GetComponent<PolygonCollider2D>();
        if (polygonCollider == null)
        {
            Debug.LogError("No PolygonCollider2D found on the GameObject.");
            return;
        }

        // Ensure we have a LineRenderer
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // Configure LineRenderer
        lineRenderer.material = outlineMaterial;
        lineRenderer.startWidth = outlineWidth;
        lineRenderer.endWidth = outlineWidth;
        lineRenderer.loop = true; // Ensures the line closes the shape
        lineRenderer.useWorldSpace = true;

        // Get collider points and transform them to world space
        Vector2[] colliderPoints = polygonCollider.points;
        Vector3[] worldPoints = new Vector3[colliderPoints.Length];

        for (int i = 0; i < colliderPoints.Length; i++)
        {
            Vector2 localPoint = colliderPoints[i];
            worldPoints[i] = polygonCollider.transform.TransformPoint(new Vector3(localPoint.x, localPoint.y, -0.1f));
        }

        // Assign points to the LineRenderer
        lineRenderer.positionCount = worldPoints.Length;
        lineRenderer.SetPositions(worldPoints);

        // Close the loop by adding the first point again
        lineRenderer.positionCount += 1;
        lineRenderer.SetPosition(worldPoints.Length, worldPoints[0]);    }

    void Update()
    {

    }
    public void OnPointerClick(PointerEventData eventData)
    {
        onRoomClick?.Invoke();
    }


    public void ShowOutline()
    {
        if (internalRooms != null)
        {
            internalRooms.enabled = true;
        }
        if (lineRenderer != null)
        {
            lineRenderer.enabled =true;
        }
    }

    public void HideOutline()
    {
        if (internalRooms != null)
        {
            internalRooms.enabled = false;
        }
        if (lineRenderer != null)
        {
            lineRenderer.enabled =false;
        }
    }


    public void OnMouseEnter()
    {
        UnityEngine.Debug.Log("OnPointerEnter");
        ShowOutline();
    }

    public void OnMouseExit()
    {
        UnityEngine.Debug.Log("OnPointerExit");
        HideOutline();
    }

}