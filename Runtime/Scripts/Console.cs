using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

//todo failure to complete should do something
//todo help ?
//todo find with 1 result could auto fill it?
//todo find with no result shouldn't do nothing

namespace AID
{
    public class Console
    {
        public delegate void CommandCallback(string paramString);

        public static System.Action<string> OnOutputUpdated;

        // Prefix for user inputted command
        public const string COMMAND_OUTPUT_PREFIX = "> ";

        public const char NodeSeparatorChar = '.';
        public static readonly string NodeSeparator = System.Convert.ToString(NodeSeparatorChar);

        private static Console instance;
        private ConsoleCommandTreeNode commandRoot;

        private Console()
        {
            commandRoot = new ConsoleCommandTreeNode();
        }

        public static Console Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Console();
                    ConsoleBindingHelper.RegisterAttributes();
                }

                return instance;
            }
        }

        public static string[] Complete(string partialCommand)
        {
            InputStringToCommandAndParams(partialCommand, out string[] commandName, out string paramString);
            bool isCompleteMatch = Instance.commandRoot.FindClosestMatch(commandName, out ConsoleCommandTreeNode bestMatch);

            //if it is a complete match show all child commands
            if (bestMatch == null)
            {
                return new string[0] { };
            }
            else if (isCompleteMatch)
            {
                return bestMatch.GetSubCommands().Select(x => x.FullCommandPath).OrderBy(x => x).ToArray();
            }
            else
            {
                List<ConsoleCommandTreeNode> matches = new List<ConsoleCommandTreeNode>();
                var allPossible = bestMatch.GetSubCommands();
                var lastPartial = commandName[commandName.Length - 1];
                lastPartial = lastPartial.ToLower();

                foreach (var item in allPossible)
                {
                    if (item.Command.localName.ToLower().StartsWith(lastPartial))
                    {
                        matches.Add(item);
                    }
                }

                return matches.Select(x => x.FullCommandPath).OrderBy(x => x).ToArray();
            }
        }

        public static void Echo(string cmd)
        {
            Log(COMMAND_OUTPUT_PREFIX + cmd);
        }

        public static void Log(string str)
        {
            if (OnOutputUpdated != null)
                OnOutputUpdated.Invoke(str);
        }

        //take something like Time.timeScale 1.5 and give back commandName {"Time", "timeScale"} and commandParams {"1.5"}
        public static void InputStringToCommandAndParams(string str, out string[] commandName, out string paramString)
        {
            str = str.Trim();
            var endOfCommand = str.IndexOf(' ');
            paramString = string.Empty;
            string commandStr;
            if (endOfCommand != -1)
            {
                commandStr = str.Substring(0, endOfCommand);
                paramString = str.Substring(endOfCommand).Trim();
            }
            else
            {
                commandStr = str;
            }

            commandName = commandStr.Split(Console.NodeSeparatorChar);
        }

        public static void RegisterCommand(string commandName, string helpText, CommandCallback callback)
        {
            Assert.IsFalse(string.IsNullOrEmpty(commandName));

            InputStringToCommandAndParams(commandName, out string[] names, out string param);

            Instance.commandRoot.Add(names, callback, helpText);
        }

        public static void DeregisterCommand(string commandName)
        {
            Assert.IsFalse(string.IsNullOrEmpty(commandName));

            InputStringToCommandAndParams(commandName, out string[] names, out string param);

            Instance.commandRoot.Remove(names);
        }

        //this is the entry point for moving all console commands on device to CUDLR as this just takes raw inputstring
        public static bool Run(string str, out ConsoleCommandTreeNode cmd)
        {
            cmd = null;
            if (string.IsNullOrEmpty(str))
                return false;

            Echo(str);

            InputStringToCommandAndParams(str, out string[] commandName, out string paramString);

            if (Instance.commandRoot.FindClosestMatch(commandName, out cmd))
            {
                if (cmd != null && cmd.Command != null)
                {
                    if (cmd.Command.callback != null)
                    {
                        cmd.Command.callback(paramString);
                        return true;
                    }
                    else
                    {
                        Console.Log(string.Join(Console.NodeSeparator, commandName) + " exists but is not a runnable entry.");
                        return false;
                    }
                }
            }

            Console.Log("Console cannot find command by name " + string.Join(Console.NodeSeparator, commandName));
            return false;
        }

        public static void Visit(System.Func<ConsoleCommandTreeNode, bool> visitor)
        {
            instance.commandRoot.Visit(visitor);
        }
    }
}