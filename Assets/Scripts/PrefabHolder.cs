using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct PrefabEntry
{
    public string key;
    public GameObject prefab;
}

public class PrefabHolder : MonoBehaviour
{
    public RoomController roomPrefab;
    public LaserShot laserPrefab;
    public CardController cardPrefab;
    public SpaceshipController enemySpaceshipPrefab;
    public SpaceshipController playerSpaceshipPrefab;
    public IntentLine intentLine;

    [SerializeField]
    public List<PrefabEntry> prefabList = new List<PrefabEntry>();
}
