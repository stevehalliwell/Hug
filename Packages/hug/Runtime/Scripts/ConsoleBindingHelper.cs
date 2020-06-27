using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

//todo autogen help string

namespace AID
{
    /// <summary>
    /// These methods provide the bridge between the Console and methods or fields/properties. The generate
    /// Console.CommandCallback delegates that handle the conversion of the input string from the user to the required
    /// typed objects needed by the navitive method. This is done with the help fo the StringToType class.
    ///
    /// It also provides helpers for binding all of a static class, an object instance, or all items with the ConsoleCommand
    ///  Attribute attached to them.
    ///
    /// Presently using regex to determine parameter lists from user string. Meaning whitespace between params determines
    /// the number of params. If you are using functions with multiple parameters be aware of this limitation/requirement.
    /// For example a method that takes a string, a vector3 and a float needs to be "the string input" 1,2,3 3.14.
    /// This will be interpreted as 3 params {"the string input", "1,2,3", "3.14"}
    ///
    /// Notes for IL2CPP and code stripping: the binding and attributes usage is all based on reflection, so when Unity
    /// is stripping code it may not see that we are trying to reference certain elements. For example, a
    /// AddAllToConsole(null,null, typeof(Physics)); will result in all elements of the physics class not directly
    /// referenced elsewhere in your code, missing, a link.xml or attributes may be required.
    /// See https://docs.unity3d.com/Manual/ManagedCodeStripping.html
    /// </summary>
    public static class ConsoleBindingHelper
    {
        /// <summary>
        /// The ConsoleBindingHelper can generate a lot of failures when requested to operate on assemblies or classes, when
        /// errors or limits are hit they are logged via this delegate so they can be routed or logged as loudly or quitely
        /// as desired by the user.
        /// </summary>
        public static Action<string> OnErrorLogDelegate = delegate { };

        /// <summary>
        /// Will attempt to add all aspects of the given instance or type to the Console. If an instance is provided
        /// that will be used to deduce the type and will bind instance declarations. If no instance is given, a type
        /// must be given and all static declarations.
        /// </summary>
        /// <param name="instance">if set, type will be fetched and instance binding</param>
        /// <param name="startingName">if not set will be set to type.Name</param>
        /// <param name="type">if set and instance is null, static binding</param>
        public static void AddAllToConsole(
            object instance,
            string startingName,
            Type type = null,
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.DeclaredOnly,
            bool shouldAddMethods = true,
            bool shouldAddFields = true,
            bool shouldAddProps = true,
            bool suppressAutoAddOfCommandTaggedMethods = true)
        {
            if (instance == null && type == null)
                return;

            if (type == null && instance != null)
                type = instance.GetType();

            if (type.IsDefined(typeof(ConsoleCommandIgnoreAttribute)))
                return;

            if (string.IsNullOrEmpty(startingName))
                startingName = type.Name;

            //if we have an instance then we should push binding instance in there if not we should push static in there
            if (instance == null)
                bindingFlags |= BindingFlags.Static;
            else
                bindingFlags |= BindingFlags.Instance;

            if (shouldAddMethods)
            {
                var smList = type.GetMethods(bindingFlags).Where(m => !m.IsSpecialName);

                foreach (var item in smList)
                {
                    if (suppressAutoAddOfCommandTaggedMethods && item.IsDefined(typeof(ConsoleCommandAttribute)))
                        continue;

                    if (item.IsDefined(typeof(ConsoleCommandIgnoreAttribute)))
                        continue;

                    var wrappedFunc = CallbackFromMethod(item, instance);

                    if (wrappedFunc != null)
                    {
                        var paramTypesNames = item.GetParameters().Select(x => x.ParameterType.Name + " " + x.Name).ToArray();
                        var methodSig = "Expects " + string.Join(", ", paramTypesNames);
                        Console.RegisterCommand(startingName + Console.NodeSeparator + item.Name, methodSig, wrappedFunc);
                    }
                }
            }
            if (shouldAddFields)
            {
                var sfList = type.GetFields(bindingFlags);

                foreach (var item in sfList)
                {
                    if (suppressAutoAddOfCommandTaggedMethods && item.IsDefined(typeof(ConsoleCommandAttribute)))
                        continue;

                    if (item.IsDefined(typeof(ConsoleCommandIgnoreAttribute)))
                        continue;

                    PropAndFieldInternalHelper(instance, item, null, startingName + Console.NodeSeparator + item.Name);
                }
            }
            if (shouldAddProps)
            {
                var spList = type.GetProperties(bindingFlags);

                foreach (var item in spList)
                {
                    if (suppressAutoAddOfCommandTaggedMethods && item.IsDefined(typeof(ConsoleCommandAttribute)))
                        continue;

                    if (item.IsDefined(typeof(ConsoleCommandIgnoreAttribute)))
                        continue;

                    PropAndFieldInternalHelper(instance, null, item, startingName + Console.NodeSeparator + item.Name);
                }
            }
        }

        /// <summary>
        /// Extracts delimited elements such as 0,hello,7 becomes {"0","hello","7"} kept as a separate method to allow testing and iteration
        /// </summary>
        public static string[] ParamStringToElements(string s)
        {
            //thx http://regexr.com/
            Regex regex = new Regex(@"\(.*?\)|\[.*?\]|"".*?""|[^\s]+");
            MatchCollection matches = regex.Matches(s);
            string[] res = new string[matches.Count];
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = matches[i].Value;
            }
            return res;
        }

        /// <summary>
        /// Generate and return a wrapping closure that is aware of param list required.
        /// Takes a string of arguments and a list of parameters to a function and using the converter
        /// functions attempts to build the object list to be passed to the method invoke.
        /// </summary>
        public static Console.CommandCallback CallbackFromMethod(MethodInfo item, object instance)
        {
            var pList = item.GetParameters();

            //check that its even possible to support these params
            bool paramSuccess = true;
            var report = string.Empty;
            for (int i = 0; i < pList.Length; i++)
            {
                if (!StringToType.IsSupported(pList[i].ParameterType))
                {
                    paramSuccess = false;
                    report += string.Format("Param #{0} \"{1}\" is not supported by StringToType", i, pList[i].ParameterType.Name);
                }
            }

            if (!paramSuccess)
            {
                OnErrorLogDelegate(string.Format("Cannot generate callback for method {0}. {1}", item.Name, report));
                return null;
            }

            //we could probably optimise this if it just takes a string but what would the point be
            return (string stringIn) =>
            {
                var sParams = ParamStringToElements(stringIn);

                var parameters = new object[pList.Length];

                if (sParams.Length != pList.Length)
                {
                    OnErrorLogDelegate("Param count mismatch. Expected " + pList.Length.ToString() + " got " + sParams.Length);
                    return;
                }
                else
                {
                    for (int i = 0; i < pList.Length; i++)
                    {
                        var res = StringToType.TryGetTypeFromString(pList[i].ParameterType, sParams[i]);

                        if (res == null)
                        {
                            OnErrorLogDelegate(string.Format("Param #{0} failed. Could not convert \"{1}\" to type {2}", i, sParams[i], pList[i].ParameterType.Name));
                            return;
                        }

                        parameters[i] = res;
                    }
                }

                item.Invoke(instance, parameters);
            };
        }

        /// <summary>
        /// Check for compatibility with the variable in question, generate and add the closure. If no params are given it will act as a get,
        /// if params are given it will attempt to use them as a set.
        ///
        /// Wrap the functionality that is DAMN NEAR identical for fields and properties so we don't have to maintain two versions in 2 locations
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="field"></param>
        /// <param name="property"></param>
        /// <param name="finalName"></param>
        private static void PropAndFieldInternalHelper(object instance, FieldInfo field, PropertyInfo property, string finalName)
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

            if (!StringToType.IsSupported(paramType))
            {
                OnErrorLogDelegate(string.Format("Cannot generate variable wrapper on {0}, type {1} is not supported.", name, paramType.Name));
                return;
            }

            //fancy wrapper goes here that returns value safely on no params and tries to convert on 1 param
            Console.CommandCallback wrappedFunc = (string stringIn) =>
            {
                //do they want to set
                if (!string.IsNullOrEmpty(stringIn))
                {
                    object parameter = StringToType.TryGetTypeFromString(paramType, stringIn);

                    if (parameter != null)
                    {
                        //use it as a set
                        if (field != null)
                        {
                            field.SetValue(instance, parameter);
                            //log new val
                            Console.Log("=" + field.GetValue(instance).ToString());
                        }
                        else
                        {
                            if (property.CanWrite)
                            {
                                property.SetValue(instance, parameter, null);
                                if (property.CanRead) Console.Log("=" + property.GetValue(instance, null).ToString());
                            }
                            else
                            {
                                Console.Log(property.Name + " cannot be set.");
                            }
                        }
                    }
                }
                else
                {
                    //get only
                    if (field != null)
                    {
                        Console.Log("=" + field.GetValue(instance).ToString());
                    }
                    else
                    {
                        if (property.CanRead)
                        {
                            Console.Log("=" + property.GetValue(instance, null).ToString());
                        }
                        else
                        {
                            Console.Log(property.Name + " cannot be read.");
                        }
                    }
                }
            };

            var attrs = attrProv.GetCustomAttributes(typeof(ConsoleCommandAttribute), false) as ConsoleCommandAttribute[];
            var attr = attrs.Length > 0 ? attrs[0] : null;

            Console.RegisterCommand(finalName, attr != null ? attr.help : "Expects " + paramType.ToString(), wrappedFunc);
        }

        /// <summary>
        /// Finds all statics in classes, methods, fields, and properties that have the ConsoleCommand attribute on them and attempts to add them
        /// to the console. By default this is called when the Console is first reqested.
        ///
        /// Note that a ConsoleCommand attribute placed on a class, it will attempt to add all contained methods, fields and properties that are not
        /// already tagged up with the attribute.
        /// </summary>
        public static void RegisterAttributes()
        {
            var alltypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes());

            foreach (var type in alltypes)
            {
                //if the type has the console on it then pass it to the helper
                if (type.IsDefined(typeof(ConsoleCommandAttribute)))
                {
                    if (type.IsDefined(typeof(ConsoleCommandIgnoreAttribute)))
                        return;

                    AddAllToConsole(null, null, type);
                }

                //do any methods have it within the type have the console
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

                foreach (var methInfo in methods)
                {
                    if (!(methInfo.GetCustomAttribute(typeof(ConsoleCommandAttribute), true) is ConsoleCommandAttribute cmdAttr))
                        continue;

                    //it is annoying that this is so similar to AddMethodToConsole but we have need of extra info and
                    //  extra checks given attribute used
                    var cb = CallbackFromMethod(methInfo, null);

                    if (cb == null)
                    {
                        OnErrorLogDelegate(string.Format("Method {0}.{1} takes the wrong arguments for a console command.", type, methInfo.Name));
                        continue;
                    }

                    if (string.IsNullOrEmpty(cmdAttr.name))
                    {
                        OnErrorLogDelegate(string.Format("Method {0}.{1} needs a valid command name.", type, methInfo.Name));
                        continue;
                    }

                    Console.RegisterCommand(cmdAttr.name, cmdAttr.help, cb);
                }

                //do any fields have it
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

                foreach (var fieldInfo in fields)
                {
                    if (!(fieldInfo.GetCustomAttribute(typeof(ConsoleCommandAttribute), true) is ConsoleCommandAttribute cmdAttr))
                        continue;

                    PropAndFieldInternalHelper(null, fieldInfo, null, cmdAttr.name);
                }

                //do any props have it
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Static);

                foreach (var propInfo in props)
                {
                    if (!(propInfo.GetCustomAttribute(typeof(ConsoleCommandAttribute), true) is ConsoleCommandAttribute cmdAttr))
                        continue;

                    PropAndFieldInternalHelper(null, null, propInfo, cmdAttr.name);
                }
            }
        }
    }
}