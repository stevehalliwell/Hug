using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AID
{
    public partial class Console
    {
        public const BindingFlags PublicStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly;
        public const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

        private static void AddCommand(string name, ICustomAttributeProvider item, ConsoleCommandData.Callback wrappedFunc)
        {
            var cmdAttrs = item.GetCustomAttributes(typeof(ConsoleCommandAttribute), false);
            var attr = (cmdAttrs != null && cmdAttrs.Length > 0) ? (ConsoleCommandAttribute)cmdAttrs[0] : null;
            var desc = attr != null ? attr.help : string.Empty;

            RegisterCommand(name, desc, wrappedFunc);
        }

        public static void AddAllStaticsToConsole(Type type, string startingName = null, BindingFlags bindingFlags = PublicStatic, bool shouldAddMethods = true, bool shouldAddFields = true, bool shouldAddProps = true)
        {
            if (string.IsNullOrEmpty(startingName))
                startingName = type.ToString();

            AddAllToConsole(null, startingName, type, bindingFlags, shouldAddMethods, shouldAddFields, shouldAddProps);
        }

        public static void AddAllInstanceToConsole<T>(T instance, string startingName = null, BindingFlags bindingFlags = PublicInstance, bool shouldAddMethods = true, bool shouldAddFields = true, bool shouldAddProps = true)
        {
            var type = instance.GetType();
            if (string.IsNullOrEmpty(startingName))
                startingName = type.ToString();

            AddAllToConsole(instance, startingName, type, bindingFlags, shouldAddMethods, shouldAddFields, shouldAddProps);
        }

        //will deduce type if null
        public static void AddAllToConsole(object instance, string startingName, Type type = null, BindingFlags bindingFlags = PublicInstance, bool shouldAddMethods = true, bool shouldAddFields = true, bool shouldAddProps = true, bool suppressAutoAddOfCommandTaggedMethods = true)
        {
            if (instance == null && type == null)
                return;

            if (type == null && instance != null)
                type = instance.GetType();

            var attrs = type.GetCustomAttributes(typeof(ConsoleCommandIgnoreAttribute), false);
            if (attrs != null && attrs.Length > 0)
                return;

            if (shouldAddMethods)
            {
                var smList = type.GetMethods(bindingFlags)
                .Where(m => !m.IsSpecialName);

                foreach (var item in smList)
                {
                    attrs = item.GetCustomAttributes(typeof(ConsoleCommandAttribute), false);
                    if (suppressAutoAddOfCommandTaggedMethods && attrs != null && attrs.Length > 0)
                        continue;

                    attrs = item.GetCustomAttributes(typeof(ConsoleCommandIgnoreAttribute), false);
                    if (attrs != null && attrs.Length > 0)
                        continue;

                    AddMethodToConsole(item, instance, startingName);
                }
            }
            if (shouldAddFields)
            {
                var sfList = type.GetFields(bindingFlags);

                foreach (var item in sfList)
                {
                    attrs = item.GetCustomAttributes(typeof(ConsoleCommandIgnoreAttribute), false);
                    if (attrs != null && attrs.Length > 0)
                        continue;

                    PropAndFieldInternalHelper(instance, item, null, startingName);
                }
            }
            if (shouldAddProps)
            {
                var spList = type.GetProperties(bindingFlags);

                foreach (var item in spList)
                {
                    attrs = item.GetCustomAttributes(typeof(ConsoleCommandIgnoreAttribute), false);
                    if (attrs != null && attrs.Length > 0)
                        continue;

                    PropAndFieldInternalHelper(instance, null, item, startingName);
                }
            }
        }

        public static bool AddMethodToConsole(MethodInfo item, object instance, string startingName)
        {
            var wrappedFunc = CallbackFromMethod(item, instance);

            if (wrappedFunc == null)
                return false;

            var nameToAdd = (!string.IsNullOrEmpty(startingName) ? startingName + AID.ConsoleCommandTreeNode.NodeSeparator : string.Empty) + item.Name;

            AddCommand(nameToAdd, item, wrappedFunc);
            return true;
        }

        //generate and return a wrapping closure that is aware of param list required
        public static ConsoleCommandData.Callback CallbackFromMethod(MethodInfo item, object instance)
        {
            var pList = item.GetParameters();

            if (!StringToType.Supports(pList, out string report))
            {
                Debug.LogError(string.Format("Cannot generate callback for method {0}. {1}", item.Name, report));
                return null;
            }

            //we could probably optimise this if it just takes a string but what would the point be
            return (string stringIn) =>
            {
                if (StringToType.ParamStringToObjects(stringIn, pList, out object[] parameters))
                {
                    item.Invoke(instance, parameters);
                }
            };
        }

        //wrap the functionality that is DAMN NEAR identical for fields and properties so we don't have to maintain two versions in 2 locations
        private static void PropAndFieldInternalHelper(object instance, FieldInfo field, PropertyInfo property, string startingName)
        {
            Type paramType;
            string name;
            ICustomAttributeProvider attrProv;
            if (field != null)
            {
                paramType = field.FieldType;
                name = field.Name;
                attrProv = field;
            }
            else
            {
                paramType = property.PropertyType;
                name = property.Name;
                attrProv = property;
            }

            if (!StringToType.Supports(paramType))
            {
                UnityEngine.Debug.LogError(string.Format("Cannot generate variable wrapper on {0}, type {1} is not supported.", name, paramType.Name));
                return;
            }

            //fancy wrapper goes here that returns value safely on no params and tries to convert on 1 param
            ConsoleCommandData.Callback wrappedFunc = (string stringIn) =>
            {
                if (string.IsNullOrEmpty(stringIn))
                {
                    //use it as a get
                    if (field != null)
                    {
                        Log("=" + field.GetValue(instance).ToString());
                    }
                    else
                    {
                        if (property.CanRead)
                            Log("=" + property.GetValue(instance, null).ToString());
                        else
                            Log("Not allowed to read property");
                    }
                    return;
                }

                object parameter = StringToType.TryGetTypeFromString(paramType, stringIn);

                if (parameter != null)
                {
                    //use it as a set
                    if (field != null)
                    {
                        field.SetValue(instance, parameter);
                    }
                    else
                    {
                        if (property.CanWrite)
                            property.SetValue(instance, parameter, null);
                        else
                            Log("Not allowed to write property");
                    }
                }
            };

            AddCommand(startingName + ConsoleCommandTreeNode.NodeSeparator + name, attrProv, wrappedFunc);
        }

        public static Type FindTypeByNameInAllAssemblies(string typeName, Assembly[] assms = null)
        {
            if (assms == null)
                assms = System.AppDomain.CurrentDomain.GetAssemblies();

            System.Type t = null;
            System.Reflection.Assembly assm = null;

            //find first match of type in any of the used assemblies
            for (int i = 0; i < assms.Length && t == null; i++)
            {
                assm = assms[i];
                t = assm.GetType(typeName, false, true);
            }

            return t;
        }

        public static void RegisterAttributes()
        {
            var alltypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes());

            foreach (var type in alltypes)
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

                foreach (var methInfo in methods)
                {
                    var attrs = methInfo.GetCustomAttributes(typeof(ConsoleCommandAttribute), true) as ConsoleCommandAttribute[];
                    if (attrs.Length == 0)
                        continue;

                    var cb = CallbackFromMethod(methInfo, null);

                    if (cb == null)
                    {
                        Debug.LogError(string.Format("Method {0}.{1} takes the wrong arguments for a console command.", type, methInfo.Name));
                        continue;
                    }

                    // try with a bare action
                    foreach (var cmd in attrs)
                    {
                        if (string.IsNullOrEmpty(cmd.name))
                        {
                            Debug.LogError(string.Format("Method {0}.{1} needs a valid command name.", type, methInfo.Name));
                            continue;
                        }

                        RegisterCommand(cmd.name, cmd.help, cb);
                    }
                }
            }
        }
    }
}