using System.Collections.Generic;
using UnityEngine;

namespace AID
{
    /// <summary>
    /// Binds and routes Console requirements to DevConsoleUI Gameobjects, UI, and user input.
    /// </summary>
    [RequireComponent(typeof(DevConsoleUI))]
    public class DevConsoleBinding : MonoBehaviour
    {
        private DevConsoleUI devConsole;
        private List<string> bindingErrors = new List<string>();

        public void Awake()
        {
            devConsole = GetComponent<DevConsoleUI>();
            ConsoleBindingHelper.OnErrorLogDelegate = BindingLog;
            ConsoleBindingHelper.RegisterAttributes();

            if (bindingErrors.Count > 0)
                UnityEngine.Debug.LogWarning(bindingErrors.Count.ToString() + " errors during RegisterAttributes");

            ConsoleBindingHelper.OnErrorLogDelegate = UnityEngine.Debug.LogError;
            bindingErrors.Clear();
        }

        private void BindingLog(string obj)
        {
            bindingErrors.Add(obj);
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