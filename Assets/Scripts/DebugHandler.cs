using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugHandler : MonoBehaviour
{
    uint qsize = 5;  // number of messages to keep
    Queue myLogQueue = new Queue();
    public GUIStyle customStyle; // Create a GUIStyle for custom text style
    void Start() {
        Debug.Log("Started up logging.");
        // Initialize the custom style here (you can customize it further)
        customStyle = new GUIStyle();
        customStyle.fontSize = 30;
        customStyle.wordWrap = true;
        customStyle.normal.textColor = Color.white;

    }

    void OnEnable() {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable() {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type) {
        myLogQueue.Enqueue("[" + type + "] : " + logString);
        if (type == LogType.Exception)
            myLogQueue.Enqueue(stackTrace);
        while (myLogQueue.Count > qsize)
            myLogQueue.Dequeue();
    }

    void OnGUI() {
        GUILayout.BeginArea(new Rect(Screen.width - 450, 0, 400, Screen.height - 400));
        GUILayout.Label("\n" + string.Join("\n", myLogQueue.ToArray()), customStyle);
        GUILayout.EndArea();
    }
}