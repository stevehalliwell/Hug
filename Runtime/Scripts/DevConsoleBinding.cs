﻿using UnityEngine;

namespace AID
{
    /// <summary>
    /// Binds and routes Console requirements to DevConsole Gameobjects, UI, and user input.
    /// </summary>
    [RequireComponent(typeof(DevConsole))]
    public class DevConsoleBinding : MonoBehaviour
    {
        private DevConsole devConsole;

        public void Awake()
        {
            devConsole = GetComponent<DevConsole>();
            ConsoleBindingHelper.RegisterAttributes();
        }

        public void OnEnable()
        {
            Console.OnOutputUpdated += devConsole.AddToMainOutput;
            devConsole.OnConsoleCommandInput += RunConsoleCommand;
            devConsole.OnConsoleCompleteRequested += Console.Complete;
            Console.RegisterCommand("clear", "clears the output text of the console", (s) => devConsole.ClearMainOutput());
        }

        public void OnDisable()
        {
            Console.OnOutputUpdated -= devConsole.AddToMainOutput;
            devConsole.OnConsoleCommandInput -= RunConsoleCommand;
            devConsole.OnConsoleCompleteRequested -= Console.Complete;
            Console.DeregisterCommand("clear");
        }

        private void RunConsoleCommand(string input)
        {
            Console.Run(input);
        }
    }
}