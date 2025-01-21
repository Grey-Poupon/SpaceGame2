using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public enum Control
{
    FinishTurn,
    Action1,
    None,
    Undo,
    Reset,
}

public class KeyboardController : MonoBehaviour
{
    public Dictionary<KeyCode, Control> KeyboardControls = new Dictionary<KeyCode, Control>
    {
        { KeyCode.Return, Control.FinishTurn },
        { KeyCode.Alpha1, Control.Action1 },
        { KeyCode.Backspace, Control.Undo },
        { KeyCode.R, Control.Reset }
    };

    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void OnGUI()
    {
        Event e = Event.current;
        if (GameManagerController.Instance.IsGameOver())
        {
            if (e.isKey && e.type == EventType.KeyUp && KeyboardControls.ContainsKey(e.keyCode))
            {
                GameManagerController.Instance.gameManagerController.Restart();
            }
            return;
        }
        if (e.isKey && e.type == EventType.KeyUp && KeyboardControls.ContainsKey(e.keyCode))
        {
            Control control = KeyboardControls[e.keyCode];

            if (control == Control.FinishTurn)
            {
                GameManagerController.Instance.FinishTurn();
            }
            else if (control == Control.Reset)
            {
                GameManagerController.Instance.RestartTurn();
            }
            else if (control == Control.Undo)
            {
                GameManagerController.Instance.UndoAction();
            }
        }
        else if (e.type == EventType.MouseDown && e.button == 0) // 0 corresponds to the left mouse button
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            // Perform raycast
            if (hit.collider != null && hit.collider.GetComponent<RoomController>() != null)
            {
                return;
            }
            GameManagerController.Instance.DeselectCard();
        }
    }
}
