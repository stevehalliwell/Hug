using System;

namespace AID
{
    /// <summary>
    /// To be attached to static fields, props, classes, or methods for automatic adding to the console during a ConsoleHelper or RegisterAttributes.
    /// Recommendation is to place this directly on the data and functions desired rather than enclosing class as that allows help strings.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ConsoleCommandAttribute : Attribute
    {
        public ConsoleCommandAttribute(string cmdName, string helpTxt)
        {
            name = cmdName;
            help = helpTxt;
        }

        public string name;
        public string help;
    }

    /// <summary>
    /// To be attached to elements that are to be skipped over during the ConsoleHelper or RegisterAttributes process. So you can call
    /// bind all statics on a class that contains more elements that desired in the console, specifying which parts to skip.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ConsoleCommandIgnoreAttribute : Attribute
    {
    }
}