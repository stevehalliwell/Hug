using System.Linq;
using UnityEngine.Assertions;

namespace AID
{
    /// <summary>
    /// Console itself is fairly simple, it holds collections of delegates by name. It's a singleton and is separated from the tasks of UI and param 
    /// lists and type conversion by other classes. This is the simplest it can take.
    /// 
    /// Commands take the form of void Func(string), where string is the uneditted param list given by the user.
    /// Command names are expecting to be separated by the NodeSeparatorChar, defaults to . to indicate namespaces/folders.
    /// Commands are split on the CommandParamSeparator, defaults to ' '. Everything before it is the command name, every after is the param
    /// 
    /// All strings used and passed are trimmed. If whitespace is important to your param, enclose in quotes is recommended.
    /// 
    /// See the BindingHelper for working with functions with automatic conversation from user input string to paramlist.
    /// See DevConsole for working with UI, showing output and feeding input.
    /// </summary>
    public class Console
    {
        public delegate void CommandCallback(string paramString);

        /// <summary>
        /// Needs to be set by whatever is going to display the results to the user of the Console. See DevConsole.
        /// </summary>
        public static System.Action<string> OnOutputUpdated;

        // Prefix for user inputted command
        public const string ConsoleOutputPrefix = "> ";

        public const char NodeSeparatorChar = '.';
        public const char HelpChar = '?';
        public const char CommandParamSeparatorChar = ' ';
        public readonly static string NodeSeparator = System.Convert.ToString(NodeSeparatorChar);
        public readonly static string Help = System.Convert.ToString(HelpChar);
        public readonly static string CommandParamSeparator = System.Convert.ToString(CommandParamSeparatorChar);

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

        /// <summary>
        /// Find all commands we have that are the equivilant of StartsWith.
        /// </summary>
        /// <param name="partialCommand">string that will be used to locate partial matches</param>
        /// <returns>Full names of all commands that are a partial match to the input</returns>
        public static string[] Complete(string partialCommand)
        {
            InputStringToCommandAndParams(partialCommand, out string[] commandName, out string paramString);
            bool isCompleteMatch = Instance.commandRoot.FindClosestMatch(commandName, out ConsoleCommandTreeNode bestMatch);

            if (bestMatch == null)
            {
                return new string[0] { };
            }
            else if (isCompleteMatch)
            {
                if (bestMatch.Command.callback != null)
                {
                    return new string[] { bestMatch.FullCommandPath };
                }
                else
                {
                    //if it is a complete match show all child commands
                    return bestMatch.GetSubCommands().Select(x => x.FullCommandPath).OrderBy(x => x).ToArray();
                }
            }
            else
            {
                var lastPartial = commandName.Last();
                lastPartial = lastPartial.ToLowerInvariant();

                var partialMatches = bestMatch.GetSubCommands().Where(x => x.Command.localName.ToLowerInvariant().StartsWith(lastPartial));

                return partialMatches.Select(x => x.FullCommandPath).OrderBy(x => x).ToArray();
            }
        }

        /// <summary>
        /// Log prefixed with the ConsoleOutputPrefix
        /// </summary>
        /// <param name="cmd"></param>
        public static void Echo(string cmd)
        {
            Log(ConsoleOutputPrefix + cmd);
        }
        
        /// <summary>
        /// Forwards to the output delegate.
        /// </summary>
        /// <param name="str"></param>
        public static void Log(string str)
        {
            if (OnOutputUpdated != null)
                OnOutputUpdated.Invoke(str);
        }

        /// <summary>
        /// Standard console method to convert user input into data used to locate command and give param string to the command.
        /// E.g. "Time.timeScale 1.5" and give back commandName {"Time", "timeScale"} and commandParams "1.5"
        /// </summary>
        /// <param name="str">full input to convert</param>
        /// <param name="commandName">separated command name</param>
        /// <param name="paramString"></param>
        public static void InputStringToCommandAndParams(string str, out string[] commandName, out string paramString)
        {
            str = str.Trim();
            var endOfCommand = str.IndexOf(CommandParamSeparatorChar);
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

        /// <summary>
        /// Add a command, passes info down.
        /// </summary>
        /// <param name="commandName">taken in user input mode, single string including separators</param>
        /// <param name="helpText"></param>
        /// <param name="callback"></param>
        public static void RegisterCommand(string commandName, string helpText, CommandCallback callback)
        {
            Assert.IsFalse(string.IsNullOrEmpty(commandName));

            InputStringToCommandAndParams(commandName, out string[] names, out string param);

            Instance.commandRoot.Add(names, callback, helpText);
        }

        /// <summary>
        /// Remove  a command.
        /// </summary>
        /// <param name="commandName">taken in user input mode, single string including separators</param>
        public static void DeregisterCommand(string commandName)
        {
            Assert.IsFalse(string.IsNullOrEmpty(commandName));

            InputStringToCommandAndParams(commandName, out string[] names, out string param);

            Instance.commandRoot.Remove(names);
        }

        /// <summary>
        /// The entry point for running commands.
        /// </summary>
        /// <param name="str">string including command with separators and params</param>
        /// <returns>false if no command was run</returns>
        public static bool Run(string str)
        {
            if (string.IsNullOrEmpty(str))
                return false;

            Echo(str);  //echo what we got, easier for users to identify their error if there was one

            InputStringToCommandAndParams(str, out string[] commandName, out string paramString);

            if (Instance.commandRoot.FindClosestMatch(commandName, out var cmd))
            {
                if (cmd != null && cmd.Command != null)
                {
                    if (paramString == Help)
                    {
                        Console.Log(cmd.Command.help.Length > 0 ? cmd.Command.help : "No help string supplied.");
                        return false;
                    }
                    else
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
            }

            Console.Log("Console cannot find command by name " + string.Join(Console.NodeSeparator, commandName));
            return false;
        }

        /// <summary>
        /// Visitor to pass through all the TreeNode visitor method. See ConsoleCommandTreeNode.Visit for more info.
        /// </summary>
        /// <param name="visitor"></param>
        public static void Visit(System.Func<ConsoleCommandTreeNode, bool> visitor)
        {
            instance.commandRoot.Visit(visitor);
        }
    }
}