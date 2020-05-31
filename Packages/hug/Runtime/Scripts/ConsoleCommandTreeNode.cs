using System.Collections.Generic;
using System.Linq;

namespace AID
{
    public class ConsoleCommandData
    {
        public Console.CommandCallback callback;

        public string localName, help;
    }

    public class ConsoleCommandTreeNode
    {
        public ConsoleCommandData Command { get; protected set; }
        private ConsoleCommandTreeNode parentNode;
        private Dictionary<string, ConsoleCommandTreeNode> subCommandsLookUp = new Dictionary<string, ConsoleCommandTreeNode>();

        public ConsoleCommandTreeNode()
        {
        }

        protected ConsoleCommandTreeNode(ConsoleCommandTreeNode parent, ConsoleCommandData data)
        {
            parentNode = parent;
            Command = data;
        }

        public string FullCommandPath
        {
            get
            {
                var hasParent = parentNode != null;
                var parentPart = hasParent ? parentNode.FullCommandPath : string.Empty;
                var myPart = (hasParent && parentNode.Command != null ? Console.NodeSeparator : string.Empty) + (Command != null ? Command.localName : string.Empty);
                return parentPart + myPart;
            }
        }

        public int NumSubComands { get { return subCommandsLookUp.Count; } }

        public void Add(string[] names, Console.CommandCallback callback, string helpText, int command_index = 0)
        {
            if (names.Length == command_index)
            {
                Command = new ConsoleCommandData { localName = names.Last(), callback = callback, help = helpText };
                return;
            }

            string token = names[command_index];
            string lowerToken = token.ToLower();
            if (!subCommandsLookUp.ContainsKey(lowerToken))
            {
                ConsoleCommandData data = new ConsoleCommandData
                {
                    localName = token
                };
                subCommandsLookUp[lowerToken] = new ConsoleCommandTreeNode(this, data);
            }
            subCommandsLookUp[lowerToken].Add(names, callback, helpText, command_index + 1);
        }

        public void Remove(string[] names, int command_index = 0)
        {
            if (names.Length == command_index + 1)
            {
                //its here or it isn't
                if (subCommandsLookUp.TryGetValue(names.Last(), out var val))
                {
                    val.Clear();
                }
                subCommandsLookUp.Remove(names.Last());
                return;
            }
            else
            {
                //if we have a matching container ask it to continue recursing
                if (subCommandsLookUp.TryGetValue(names[command_index], out var val))
                {
                    val.Remove(names, command_index + 1);
                }
            }
        }

        public bool FindClosestMatch(string[] commandName, out ConsoleCommandTreeNode cur)
        {
            int index = 0;
            cur = this;

            while (index != commandName.Length)
            {
                string lowerToken = commandName[index].ToLower();
                if (cur.subCommandsLookUp.TryGetValue(lowerToken, out ConsoleCommandTreeNode outTemp))
                {
                    index++;
                    cur = outTemp;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public ConsoleCommandTreeNode[] GetSubCommands()
        {
            return subCommandsLookUp.Values.ToArray();
        }

        //takes a func that is given the current command tree and if it returns true it is then given to child objects too
        public void Visit(System.Func<ConsoleCommandTreeNode, bool> visitor)
        {
            if (visitor(this))
            {
                foreach (var item in GetSubCommands())
                {
                    item.Visit(visitor);
                }
            }
        }

        public void Clear()
        {
            parentNode = null;
            if (Command != null)
            {
                //clear and abandon it
                Command.callback = null;
                Command.help = string.Empty;
                Command.localName = string.Empty;
                Command = null;
            }
            //all children to clear their commands and children
            foreach (var item in subCommandsLookUp)
            {
                item.Value.Clear();
            }

            //clear the actual dictionary to abandon refs
            subCommandsLookUp.Clear();
        }
    }
}