using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Spaceship
{
    public SpaceshipController controller;
    public bool isPlayer;
    public float defaultAP = 3;
    public float AP;
    public float defaultSpeed = 0;
    public float speed;

    public void ResetAP(bool IsSimulation = false)
    {
        AP = defaultAP;
        if (IsSimulation == false && GameManagerController.Instance != null)
        {
            GameManagerController.Instance.gameManagerController.UpdateAPGraphics(this.isPlayer);
        }
    }

    public void ResetSpeed(bool IsSimulation = false)
    {
        speed = defaultSpeed;
        if (IsSimulation == false && GameManagerController.Instance != null)
        {
            GameManagerController.Instance.gameManagerController.UpdateSpeedGraphics(this.isPlayer);
        }
    }

    public void AdjustAP(float change, bool IsSimulation = false)
    {
        AP += change;
        if (IsSimulation == false && GameManagerController.Instance != null)
        {
            GameManagerController.Instance.gameManagerController.UpdateAPGraphics(this.isPlayer);
        }
    }

    public void AdjustSpeed(float change, bool IsSimulation = false)
    {
        speed += change;
        if (IsSimulation == false && GameManagerController.Instance != null)
        {
            GameManagerController.Instance.gameManagerController.UpdateSpeedGraphics(this.isPlayer);
        }
    }

    public void ResetTempRoomStats()
    {
        foreach (Room room in GetRoomList())
        {
            room.defence = 0;
            room.incomingDamage = 0;
            room.UpdateHealthBar();
        }
    }

    public Spaceship(float defaultAP, float defaultSpeed, bool isPlayer)
    {
        this.defaultAP = defaultAP;
        this.defaultSpeed = defaultSpeed;
        this.isPlayer = isPlayer;
    }

    public Dictionary<RoomType, List<Room>> GetRooms()
    {
        if (this.isPlayer)
        {
            return GameManagerController.Instance.playerRooms;
        }
        return GameManagerController.Instance.enemyRooms;
    }

    public List<Room> GetRoomList()
    {
        return GetRooms().Values.SelectMany(x => x).ToList();
    }

    public void onDestroy() { }

    public Spaceship Clone()
    {
        Spaceship clone = new Spaceship(defaultAP, defaultSpeed, isPlayer);
        clone.AP = AP;
        clone.speed = speed;
        return clone;
    }
}

public class SpaceshipController : MonoBehaviour
{
    public Spaceship spaceship;
    public PolygonCollider2D shipBoundaryCollider;
    public GameObject roomPrefab;
    public List<Vector2> validRoomPositions = new List<Vector2>();

    void Awake()
    {
        init();
    }

    public void init()
    {
        if (spaceship is not null)
            return;
        if (gameObject.tag.Contains("player"))
        {
            this.spaceship = new Spaceship(3, 0, true);
            GameManagerController.Instance.RegisterPlayerShip(this.spaceship);
            this.spaceship.controller = this;
        }
        if (gameObject.tag.Contains("enemy"))
        {
            this.spaceship = new Spaceship(3, 0, false);
            this.spaceship.ResetTempRoomStats();
            GameManagerController.Instance.RegisterEnemyShip(this.spaceship);
            this.spaceship.controller = this;
        }
        this.roomPrefab = GameManagerController
            .Instance
            .gameManagerController
            .prefabHolder
            .roomPrefab
            .gameObject;
        if (gameObject.tag.Contains("enemy"))
        {
            this.FindValidPlacementPoints();
        }
    }

    void FindValidPlacementPoints()
    {
        HashSet<Rect> validPlacementCenters = new HashSet<Rect>();
        Bounds shipBounds = shipBoundaryCollider.bounds;
        RectTransform rt = roomPrefab.GetComponent<RectTransform>();

        float fidelity = 2f;

        // Adjust step to avoid zero division
        float stepX = Mathf.Max(0.1f, rt.rect.width / fidelity);
        float stepY = Mathf.Max(0.1f, rt.rect.height / fidelity);
        Vector2 start = shipBounds.min;
        UnityEngine.Debug.Log(
            $"Bounds Center: {shipBounds.center} Extent: {shipBounds.extents} Min: {shipBounds.min} Max: {shipBounds.max}"
        );

        float GridOffsetX = 0;
        float GridOffsetY = 0;
        float GridWidth = Mathf.Ceil(shipBounds.size.x / stepX);
        float GridHeight = Mathf.Ceil(shipBounds.size.y / stepY);

        // Iterate grid points covering the bounds
        for (float x = GridOffsetX; x < GridOffsetX + GridWidth; x += 1)
        {
            for (float y = GridOffsetY; y < GridOffsetY + GridHeight; y += 1)
            {
                float posX = start.x + (stepX * x);
                float posY = start.y + (stepY * y);
                Vector2 potentialPos = new Vector2(posX, posY);
                UnityEngine.Debug.Log($"{x} - {y}");

                Rect testRect = new Rect(
                    potentialPos.x,
                    potentialPos.y,
                    rt.rect.width,
                    rt.rect.height
                );

                // Check if this potential room location is valid
                if (IsRectFullyInsideBoundary(testRect, shipBoundaryCollider))
                {
                    validPlacementCenters.Add(testRect);
                }
            }
        }
        UnityEngine.Debug.Log($"Found {validPlacementCenters.Count} potential valid room centers.");
        PlaceRooms(rt.rect, stepX, stepY, validPlacementCenters);
    }

    bool IsRectFullyInsideBoundary(Rect rect, PolygonCollider2D boundary)
    {
        Vector2 p1 = rect.min; // Bottom-left
        Vector2 p2 = rect.max; // Top-right
        Vector2 p3 = new Vector2(rect.xMin, rect.yMax); // Top-left
        Vector2 p4 = new Vector2(rect.xMax, rect.yMin); // Bottom-right
        Vector2 p5 = rect.center;
        Vector2 p6 = new Vector2(rect.center.x, rect.yMin); // Mid-Bottom
        Vector2 p7 = new Vector2(rect.center.x, rect.yMax); // Mid-Top
        Vector2 p8 = new Vector2(rect.xMin, rect.center.y); // Mid-Left
        Vector2 p9 = new Vector2(rect.xMax, rect.center.y); // Mid-Right
        int collisions = 0;

        if (!boundary.OverlapPoint(p1))
            collisions += 1;
        if (!boundary.OverlapPoint(p2))
            collisions += 1;
        if (!boundary.OverlapPoint(p3))
            collisions += 1;
        if (!boundary.OverlapPoint(p4))
            collisions += 1;
        if (!boundary.OverlapPoint(p5))
            collisions += 1;
        if (!boundary.OverlapPoint(p6))
            collisions += 1;
        if (!boundary.OverlapPoint(p7))
            collisions += 1;
        if (!boundary.OverlapPoint(p8))
            collisions += 1;
        if (!boundary.OverlapPoint(p9))
            collisions += 1;

        DrawRectangle(rect, Color.red, collisions < 1);
        return collisions < 1;
    }

    void DrawRectangle(Rect rect, Color color, bool good = false)
    {
        Vector3 bottomLeft = new Vector3(rect.xMin, rect.yMax, 0);
        Vector3 bottomRight = new Vector3(rect.xMax, rect.yMax, 0);
        Vector3 topRight = new Vector3(rect.xMax, rect.yMin, 0);
        Vector3 topLeft = new Vector3(rect.xMin, rect.yMin, 0);

        Debug.DrawLine(bottomLeft, bottomRight, color, 100f); // duration in seconds
        Debug.DrawLine(bottomRight, topRight, color, 100f);
        Debug.DrawLine(topRight, topLeft, color, 100f);
        Debug.DrawLine(topLeft, bottomLeft, color, 100f);

        if (good)
        {
            Debug.DrawLine(topLeft, bottomRight, Color.green, 100f);
            Debug.DrawLine(topRight, bottomLeft, Color.green, 100f);
        }
    }

    void PlaceRooms(
        Rect roomSize,
        float gridStepX,
        float gridStepY,
        HashSet<Rect> validPlacementCenters
    )
    {
        float roomWidth = Mathf.Ceil(roomSize.width / gridStepX);
        float roomHeight = Mathf.Ceil(roomSize.height / gridStepY);
        int i = 0;
        HashSet<Rect> placedRects = new HashSet<Rect>();

        while (validPlacementCenters.Count > 0)
        {
            i++;
            if (i > 1000)
                break;
            UnityEngine.Debug.Log($"{validPlacementCenters.Count} valid places left");
            Rect roomLocation = validPlacementCenters.ElementAt(
                UnityEngine.Random.Range(0, validPlacementCenters.Count)
            );

            validPlacementCenters.Remove(roomLocation);

            bool overlaps = false;
            foreach (Rect placedRect in placedRects)
            {
                Rect paddedRect = new Rect(
                    placedRect.x - gridStepX,
                    placedRect.y - gridStepY,
                    placedRect.width + gridStepX * 2,
                    placedRect.height + gridStepY * 2
                );

                if (roomLocation.Overlaps(paddedRect))
                {
                    overlaps = true;
                    break;
                }
            }
            if (overlaps)
                continue;

            validRoomPositions.Add(transform.InverseTransformPoint(roomLocation.center));
            placedRects.Add(roomLocation);
            UnityEngine.Debug.Log($"Placing at -> {roomLocation.x}, {roomLocation.y}");
        }
    }
}
