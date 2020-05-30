using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace AID
{
    public partial class Console
    {
        public static Action<string> OnOutputUpdated;

        // Prefix for user inputted command
        private const string COMMAND_OUTPUT_PREFIX = "> ";

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
                    RegisterAttributes();
                }

                return instance;
            }
        }

        public static string[] Complete(string partialCommand)
        {
            DevConsole.InputStringToCommandAndParams(partialCommand, out string[] commandName, out string paramString);
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
            OnOutputUpdated?.Invoke(str);
        }

        public static void RegisterCommand(string commandName, string helpText, ConsoleCommandData.Callback callback)
        {
            Assert.IsFalse(string.IsNullOrEmpty(commandName));

            DevConsole.InputStringToCommandAndParams(commandName, out string[] names, out string param);

            ConsoleCommandData cmd = new ConsoleCommandData
            {
                localName = names[names.Length - 1],
                help = helpText,
                callback = callback
            };

            Instance.commandRoot.Add(names, cmd);
        }

        //this is the entry point for moving all console commands on device to CUDLR as this just takes raw inputstring
        public static bool Run(string str, out ConsoleCommandTreeNode cmd)
        {
            cmd = null;
            if (string.IsNullOrEmpty(str))
                return false;

            Echo(str);

            DevConsole.InputStringToCommandAndParams(str, out string[] commandName, out string paramString);

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
                        Console.Log(string.Join(ConsoleCommandTreeNode.NodeSeparator, commandName) + " exists but is not a runnable entry.");
                        return false;
                    }
                }
            }

            Console.Log("Console cannot find command by name " + string.Join(ConsoleCommandTreeNode.NodeSeparator, commandName));
            return false;
        }
    }
}