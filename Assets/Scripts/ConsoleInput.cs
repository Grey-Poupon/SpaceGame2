using UnityEngine;
using System;
using System.Threading;
using System.Collections;

public class ConsoleInput : MonoBehaviour
{
    private Thread consoleThread;

    private void Start()
    {
        // Start a separate thread to read input from the console.
        consoleThread = new Thread(ReadConsoleInput);
        consoleThread.Start();
    }

    private void ReadConsoleInput()
    {
        while (true)
        {
            string input = Console.ReadLine();
            
            // Process the input (you can add your own logic here).
            if (!string.IsNullOrEmpty(input))
            {
                Debug.Log("Input from console: " + input);
            }
        }
    }

    private void OnApplicationQuit()
    {
        // Ensure the console thread is terminated when the application quits.
        consoleThread.Abort();
    }
}
