using System.Security.Cryptography;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum Control {
    FinishTurn,
    Action1,
    None,
}

public class KeyboardController : MonoBehaviour
{

    public Dictionary<KeyCode, Control> KeyboardControls = new Dictionary<KeyCode, Control>
    {
        { KeyCode.Return, Control.FinishTurn },
        { KeyCode.Alpha1, Control.Action1 }
    };
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void OnGUI()
    {
        Event e = Event.current;
        if (e.isKey && e.type == EventType.KeyUp && KeyboardControls.ContainsKey(e.keyCode))
        {
            Control control = KeyboardControls[e.keyCode];
            
            // if (control == Control.FinishTurn)
            // {
            //     GameManager.Instance.FinishTurn();
            // }
            // else if (control == Control.Action1)
            // {
            //     GameManager.Instance.Action1();
            // }
            
        }
        else if (e.type == EventType.MouseDown && e.button == 0) // 0 corresponds to the left mouse button
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            // Perform raycast
            if (hit.collider != null &&hit.collider.GetComponent<RoomController>() != null)
            {
                return;
            }
            GameManager.Instance.ReleaseCard();
        }  
        
    }
}
