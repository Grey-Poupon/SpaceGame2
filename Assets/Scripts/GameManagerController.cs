using System.Collections.Specialized;
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class GameManagerController : MonoBehaviour
{
    public TextMeshProUGUI playerTurnActionsText;
    public TextMeshProUGUI enemyTurnActionsText;

    public TextMeshProUGUI playerSpeedText;
    public TextMeshProUGUI enemySpeedText;
    public TextMeshProUGUI playerAPText;
    public TextMeshProUGUI enemyAPText;
    public ISMCTS ISMCTS;
    public PrefabHolder prefabHolder;
    public Texture2D customCursor;
    public List<Move> moves;
    public List<Move> untriedMoves;
    public static GameManager Instance;




    private void Awake()
    {
        if (GameManagerController.Instance == null)
        {
            GameManagerController.Instance = new GameManager();
            GameManagerController.Instance.gameManagerController = this;
            Start1();
        }
    }
    private void Start1()
    {
        playerTurnActionsText = GameObject.Find("PlayerActionList").GetComponent<TextMeshProUGUI>();
        enemyTurnActionsText = GameObject.Find("EnemyActionList").GetComponent<TextMeshProUGUI>();
        playerSpeedText = GameObject.Find("PlayerSpeedText").GetComponent<TextMeshProUGUI>();
        enemySpeedText = GameObject.Find("EnemySpeedText").GetComponent<TextMeshProUGUI>();
        playerAPText = GameObject.Find("PlayerAPText").GetComponent<TextMeshProUGUI>();
        enemyAPText = GameObject.Find("EnemyAPText").GetComponent<TextMeshProUGUI>();
        GameManagerController.Instance.IsSimulation=false;
        StartCoroutine(GameManagerController.Instance.StartGame());

    }

    public void ClearTurnText()
    {
        enemyTurnActionsText.text = "";
        playerTurnActionsText.text = "";
    }
    public void UndoTurnText(CardAction lastAction)
    {
        playerTurnActionsText.text=playerTurnActionsText.text.Substring(0,playerTurnActionsText.text.Length-1-lastAction.name.Length -" -> ".Length -lastAction.affectedRoom.roomType.ToString().Length);
    }
    public void AddActionToTurnText(CardAction action, bool isPlayer)
    {
        TextMeshProUGUI textObj = isPlayer ? playerTurnActionsText : enemyTurnActionsText;
        if (textObj.text != "") textObj.text += '\n';
        textObj.text += action.name;
        textObj.text += " -> ";
        textObj.text += action.affectedRoom.roomType.ToString();
    }
    public void UpdateUIText()
    {
        playerAPText.text    = GameManagerController.Instance.playerShip.AP.ToString();
        enemyAPText.text     = GameManagerController.Instance.enemyShip.AP.ToString();
        playerSpeedText.text = GameManagerController.Instance.playerShip.speed.ToString();
        enemySpeedText.text  = GameManagerController.Instance.enemyShip.speed.ToString();
    }

    public void UpdateAPGraphics(bool isPlayer)
    {
        if (isPlayer) playerAPText.text = GameManagerController.Instance.playerShip.AP.ToString();
        else           enemyAPText.text = GameManagerController.Instance.enemyShip.AP.ToString();
    }
    public void UpdateSpeedGraphics(bool isPlayer)
    {
        if (isPlayer) playerSpeedText.text = GameManagerController.Instance.playerShip.speed.ToString();
        else           enemySpeedText.text = GameManagerController.Instance.enemyShip.speed.ToString();
    }
    public void ClearIntentLines()
    {
        foreach (IntentLine intentLine in GameManagerController.Instance.activeIntentLines) Destroy(intentLine.gameObject);
    }
    public void _StartCoroutine(IEnumerator routine)
    {
        StartCoroutine(routine);
    }
    public UnityEngine.Object _Instantiate(UnityEngine.Object original)
    {
        return Instantiate(original);
    }
    public UnityEngine.Object _Instantiate(UnityEngine.Object original, Transform parent)
    {
        return Instantiate(original, parent);
    }
    public UnityEngine.Object _Instantiate(UnityEngine.Object original, Transform parent, bool instantiateInWorldSpace)
    {
        return Instantiate(original, parent, instantiateInWorldSpace);
    }
    public UnityEngine.Object _Instantiate(UnityEngine.Object original, Vector3 position, Quaternion rotation)
    {
        return Instantiate(original, position, rotation);
    }
    public UnityEngine.Object _Instantiate(UnityEngine.Object original, Vector3 position, Quaternion rotation, Transform parent)
    {
        return Instantiate(original, position, rotation, parent);
    }
    public void SetCursor(bool setToNull=false)
    {
        if (setToNull)
        {   
            Cursor.SetCursor(customCursor, new Vector2(customCursor.width / 2, customCursor.height / 2), CursorMode.ForceSoftware);
        }
        else
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}