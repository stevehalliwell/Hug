using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace AID
{
    public static class ConsoleDefaultCommands
    {
        [ConsoleCommand("Console.AddStaticTypeByString", "Attempt to add all static methods, props and fields of the type given by a string")]
        public static void AddStaticTypeByString(string typeName)
        {
            var t = ConsoleBindingHelper.FindTypeByNameInAllAssemblies(typeName);

            if (t == null)
            {
                Console.Log("Could not find type of name " + typeName + ". Make sure you are using the correct namespaces, nestedclass and assembly.");
                return;
            }

            ConsoleBindingHelper.AddAllStaticsToConsole(t);
            Console.Log("Found " + typeName + " in " + t.Assembly.FullName);
        }

        [ConsoleCommand("all", "Gathers and shows all command")]
        public static void All()
        {
            var nList = new List<ConsoleCommandTreeNode>();
            System.Func<ConsoleCommandTreeNode, bool> gatherNames = (ConsoleCommandTreeNode t) =>
            {
                if (t.Command != null && t.Command.callback != null)
                {
                    nList.Add(t);
                }
                return true;
            };

            Console.Visit(gatherNames);
            nList = nList.OrderBy(x => x.FullCommandPath).ToList();

            var toJoin = nList.Select((item) => item.FullCommandPath +
                ((item.Command != null && !string.IsNullOrEmpty(item.Command.help)) ?
                    "\n  " + item.Command.help :
                    ""));

            Console.Log(string.Join("\n", toJoin));
        }

        [ConsoleCommand("find", "Searches through all commands and conducts a partial match against the given string")]
        public static void Find(string s)
        {
            s = s.ToLower();
            var nList = new List<ConsoleCommandTreeNode>();
            System.Func<ConsoleCommandTreeNode, bool> findNames = (ConsoleCommandTreeNode t) =>
            {
                if (t.Command != null && t.Command.localName.ToLower().Contains(s))
                {
                    nList.Add(t);
                    return false;   //no point checking children as they will match this already
                }

                //try it's children
                return true;
            };

            Console.Visit(findNames);

            Console.Log(string.Join("\n", nList.OrderBy(x => x.FullCommandPath).Select(x => x.FullCommandPath)));
        }

        /* Print a list of all console commands */

        [ConsoleCommand("quit", "Quits application")]
        public static void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }
    }
}