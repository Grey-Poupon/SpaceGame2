using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using TMPro;
using UnityEngine;

public class GameManagerController : MonoBehaviour
{
    public TextMeshProUGUI playerTurnActionsText;
    public TextMeshProUGUI enemyTurnActionsText;

    public TextMeshProUGUI playerSpeedText;
    public TextMeshProUGUI enemySpeedText;
    public TextMeshProUGUI playerAPText;
    public TextMeshProUGUI enemyAPText;
    public TextMeshProUGUI GameOverText;
    public CardContainer PlayerCardContainer;
    public PrefabHolder prefabHolder;
    public Texture2D customCursor;
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
        GameOverText = GameObject.Find("GameOverText").GetComponent<TextMeshProUGUI>();
        PlayerCardContainer = GameObject.Find("card-container").GetComponent<CardContainer>();

        playerTurnActionsText.text = "TEST";
        enemyTurnActionsText.text = "TEST";
        playerSpeedText.text = "0";
        enemySpeedText.text = "0";
        playerAPText.text = "0";
        enemyAPText.text = "0";
        GameOverText.text = "";

        GameManagerController.Instance.IsSimulation = false;
        StartCoroutine(GameManagerController.Instance.StartGame());
    }

    public void ClearTurnText()
    {
        enemyTurnActionsText.text = "";
        playerTurnActionsText.text = "";
    }

    public void UndoTurnText(CardAction lastAction)
    {
        playerTurnActionsText.text = playerTurnActionsText.text.Substring(
            0,
            playerTurnActionsText.text.Length
                - 1
                - lastAction.name.Length
                - " -> ".Length
                - lastAction.affectedRoom.roomType.ToString().Length
        );
    }

    public void AddActionToTurnText(CardAction action, bool isPlayer)
    {
        TextMeshProUGUI textObj = isPlayer ? playerTurnActionsText : enemyTurnActionsText;
        if (textObj.text != "")
            textObj.text += '\n';
        if (isPlayer)
        {
            textObj.text += action.name;
            textObj.text += " -> ";
            textObj.text += action.affectedRoom.roomType.ToString();
        }
        else
        {
            textObj.text += action.affectedRoom.roomType.ToString();
            textObj.text += " <- ";
            textObj.text += action.name;
        }
    }

    public void UpdateUIText()
    {
        playerAPText.text = GameManagerController.Instance.playerShip.AP.ToString();
        enemyAPText.text = GameManagerController.Instance.enemyShip.AP.ToString();
        playerSpeedText.text = GameManagerController.Instance.playerShip.speed.ToString();
        enemySpeedText.text = GameManagerController.Instance.enemyShip.speed.ToString();
    }

    public void UpdateAPGraphics(Spaceship spaceship)
    {
        if (spaceship.isPlayer)
            playerAPText.text = spaceship.AP.ToString();
        else
            enemyAPText.text = spaceship.AP.ToString();
    }

    public void UpdateSpeedGraphics(Spaceship spaceship)
    {
        if (spaceship.isPlayer)
            playerSpeedText.text = spaceship.speed.ToString();
        else
            enemySpeedText.text = spaceship.speed.ToString();
    }

    public void FinishTurn()
    {
        GameManagerController.Instance.FinishTurn();
    }

    public void ClearIntentLines()
    {
        foreach (IntentLine intentLine in GameManagerController.Instance.activeIntentLines)
            Destroy(intentLine.gameObject);
        GameManagerController.Instance.activeIntentLines = new List<IntentLine>();
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

    public UnityEngine.Object _Instantiate(
        UnityEngine.Object original,
        Transform parent,
        bool instantiateInWorldSpace
    )
    {
        return Instantiate(original, parent, instantiateInWorldSpace);
    }

    public UnityEngine.Object _Instantiate(
        UnityEngine.Object original,
        Vector3 position,
        Quaternion rotation
    )
    {
        return Instantiate(original, position, rotation);
    }

    public UnityEngine.Object _Instantiate(
        UnityEngine.Object original,
        Vector3 position,
        Quaternion rotation,
        Transform parent
    )
    {
        return Instantiate(original, position, rotation, parent);
    }

    public void SetCursor(bool setToNull = false)
    {
        setToNull = !setToNull;
        if (setToNull)
        {
            Cursor.SetCursor(
                customCursor,
                new Vector2(customCursor.width / 2, customCursor.height / 2),
                CursorMode.ForceSoftware
            );
        }
        else
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    public void PlayerWin()
    {
        GameOverText.text = "You win!";
    }

    public void EnemyWin()
    {
        GameOverText.text = "You lose!";
    }

    public void Restart()
    {
        PlayerCardContainer.DestroyAll();
        Destroy(GameManagerController.Instance.playerShip.controller.gameObject);
        Destroy(GameManagerController.Instance.enemyShip.controller.gameObject);

        GameManagerController.Instance = null;
        GameManagerController.Instance = new GameManager();
        GameManagerController.Instance.gameManagerController = this;
        Start1();
    }
}
