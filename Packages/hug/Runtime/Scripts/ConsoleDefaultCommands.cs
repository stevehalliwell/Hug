using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace AID
{
    /// <summary>
    /// Standard, default commands assumed to be desired by all consoles. Will be found and added by the RegisterAttributes.
    /// </summary>
    public static class ConsoleDefaultCommands
    {
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
            s = s.ToLowerInvariant();
            var nList = new List<ConsoleCommandTreeNode>();
            System.Func<ConsoleCommandTreeNode, bool> findNames = (ConsoleCommandTreeNode t) =>
            {
                if (t.Command != null && t.Command.localName.ToLowerInvariant().Contains(s))
                {
                    nList.Add(t);
                    return false;   //no point checking children as they will match this already
                }

                //try it's children
                return true;
            };

            Console.Visit(findNames);

            var output = string.Join("\n", nList.OrderBy(x => x.FullCommandPath).Select(x => x.FullCommandPath));

            Console.Log(output.Length > 0 ? output : "No matches found.");
        }

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