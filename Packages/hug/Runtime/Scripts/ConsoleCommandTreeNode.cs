using System.Collections.Generic;
using System.Linq;

namespace AID
{
    /// <summary>
    /// Simple data storage for a single command
    /// </summary>
    public class ConsoleCommandData
    {
        public Console.CommandCallback callback;

        public string localName, help;
    }

    /// <summary>
    /// Keeping commands organised in a tree makes a certain sense, given the heirarchical nature we support in command names.
    ///
    /// At present each node is either a command or a folder of nodes. Commands are at leafs. This is easy to reason about and move over the
    /// graph in visitors. It is however not the fastest method, the console command lookup time however should not be a pain point of performance.
    /// </summary>
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

        /// <summary>
        /// Add a new command with names separated by folders/namespaces, last is command name, others are holders. These will be made as required
        /// during the add.
        /// </summary>
        /// <param name="names">separated command name e.g. Physics.gravity is now {"Physics","gravity"}</param>
        /// <param name="callback">action of the command to add</param>
        /// <param name="helpText">help text to show to user about the command</param>
        /// <param name="command_index">used in the recursion, indicates current depth in names array</param>
        public void Add(string[] names, Console.CommandCallback callback, string helpText, int command_index = 0)
        {
            if (names.Length == command_index)
            {
                Command = new ConsoleCommandData { localName = names.Last(), callback = callback, help = helpText };
                return;
            }

            string token = names[command_index];
            string lowerToken = token.ToLowerInvariant();
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

        /// <summary>
        /// Removes the TreeNode of given name, regardless of it being a leaf or not. Use of Clear when found will clear all child nodes.
        /// </summary>
        /// <param name="names">separated command name</param>
        /// <param name="command_index">used in the recursion, indicates current depth in names array</param>
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

        /// <summary>
        /// Find a command of given separated name. If no exact match is found, returns false and out node is the closest node that was found.
        /// </summary>
        /// <param name="commandName"></param>
        /// <param name="node"></param>
        /// <returns>true on perfect match.</returns>
        public bool FindClosestMatch(string[] commandName, out ConsoleCommandTreeNode node)
        {
            int index = 0;
            node = this;

            while (index != commandName.Length)
            {
                string lowerToken = commandName[index].ToLowerInvariant();
                if (node.subCommandsLookUp.TryGetValue(lowerToken, out ConsoleCommandTreeNode outTemp))
                {
                    index++;
                    node = outTemp;
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

        /// <summary>
        /// Vistor is used to depth first traverse over the command tree. It is given the content of the node and must return true or false. On a
        /// true it continue to traverse the subcommands of the branch.
        /// </summary>
        /// <param name="visitor"></param>
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

        /// <summary>
        /// Zeros, clears and nulls, itself and all subcommands.
        /// </summary>
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