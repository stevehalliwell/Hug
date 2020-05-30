using UnityEngine;

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
        }

        public void OnEnable()
        {
            Console.OnOutputUpdated += devConsole.AddToMainOutput;
            devConsole.OnConsoleCommandInput += RunConsoleCommand;
            devConsole.OnConsoleCompleteRequested += Console.Complete;
            Console.RegisterCommand("clear", "clears the output text of the console", (s) => devConsole.Clear());
        }

        public void OnDisable()
        {
            Console.OnOutputUpdated -= devConsole.AddToMainOutput;
            devConsole.OnConsoleCommandInput -= RunConsoleCommand;
            devConsole.OnConsoleCompleteRequested -= Console.Complete;
        }

        private void RunConsoleCommand(string input)
        {
            Console.Run(input, out ConsoleCommandTreeNode tree);
        }
    }
}