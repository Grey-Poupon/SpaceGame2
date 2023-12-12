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

    public PrefabHolder prefabHolder;

    private void Awake()
    {
        if (GameManager.Instance == null)
        {
            GameManager.Instance = new GameManager();
            GameManager.Instance.gameManagerController = this;
            Start();
        }
    }
    private void Start()
    {
        playerTurnActionsText = GameObject.Find("PlayerActionList").GetComponent<TextMeshProUGUI>();
        enemyTurnActionsText = GameObject.Find("EnemyActionList").GetComponent<TextMeshProUGUI>();
        playerSpeedText = GameObject.Find("PlayerSpeedText").GetComponent<TextMeshProUGUI>();
        enemySpeedText = GameObject.Find("EnemySpeedText").GetComponent<TextMeshProUGUI>();
        playerAPText = GameObject.Find("PlayerAPText").GetComponent<TextMeshProUGUI>();
        enemyAPText = GameObject.Find("EnemyAPText").GetComponent<TextMeshProUGUI>();
        GameManager.Instance.IsSimulation=false;
        StartCoroutine(GameManager.Instance.StartGame());

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
        playerAPText.text    = GameManager.Instance.playerShip.AP.ToString();
        enemyAPText.text     = GameManager.Instance.enemyShip.AP.ToString();
        playerSpeedText.text = GameManager.Instance.playerShip.speed.ToString();
        enemySpeedText.text  = GameManager.Instance.enemyShip.speed.ToString();
    }

    public void UpdateAPGraphics(bool isPlayer)
    {
        if (isPlayer) playerAPText.text = GameManager.Instance.playerShip.AP.ToString();
        else           enemyAPText.text = GameManager.Instance.enemyShip.AP.ToString();
    }
    public void UpdateSpeedGraphics(bool isPlayer)
    {
        if (isPlayer) playerSpeedText.text = GameManager.Instance.playerShip.speed.ToString();
        else           enemySpeedText.text = GameManager.Instance.enemyShip.speed.ToString();
    }
    public void ClearIntentLines()
    {
        foreach (IntentLine intentLine in GameManager.Instance.activeIntentLines) Destroy(intentLine.gameObject);
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
}