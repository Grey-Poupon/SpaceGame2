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
            
            if (control == Control.FinishTurn)
            {
                GameManager.Instance.FinishTurn();
            }
            else if (control == Control.Action1)
            {
                GameManager.Instance.Action1();
            }
            
        }
    }
}
