using System.Collections.Generic;
using System.Linq;

namespace AID
{
    public class ConsoleCommandData
    {
        public Callback callback;

        public string localName, help;

        public delegate void Callback(string paramString);
    }

    public class ConsoleCommandTreeNode
    {
        public const char NodeSeparatorChar = '.';
        public static readonly string NodeSeparator = System.Convert.ToString(NodeSeparatorChar);

        private ConsoleCommandTreeNode parentNode;
        private Dictionary<string, ConsoleCommandTreeNode> subCommandsLookUp = new Dictionary<string, ConsoleCommandTreeNode>();

        public ConsoleCommandTreeNode()
        {
        }

        public ConsoleCommandTreeNode(ConsoleCommandTreeNode parent, ConsoleCommandData data)
        {
            parentNode = parent;
            Command = data;
        }

        public ConsoleCommandData Command { get; private set; }

        public string FullCommandPath
        {
            get
            {
                var hasParent = parentNode != null;
                var parentPart = hasParent ? parentNode.FullCommandPath : string.Empty;
                var myPart = (hasParent && parentNode.Command != null ? NodeSeparator : string.Empty) + (Command != null ? Command.localName : string.Empty);
                return parentPart + myPart;
            }
        }

        public int NumSubComands { get { return subCommandsLookUp.Count; } }

        public void Add(string[] names, ConsoleCommandData cmd, int command_index = 0)
        {
            if (names.Length == command_index)
            {
                Command = cmd;
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
            subCommandsLookUp[lowerToken].Add(names, cmd, command_index + 1);
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
    }
}