using System.Linq;
using UnityEngine;

namespace AID.Examples
{
    public class ConsoleDemo : MonoBehaviour
    {
        public InstanceData instTest = new InstanceData();

        private void Start()
        {
            //we can add our own type to the StringToType system
            StringToType.SetConverterDelegateForType(
                typeof(ConsoleStaticCommandsTest.SomethingSupported),
                (s) => { return new ConsoleStaticCommandsTest.SomethingSupported() { val = int.Parse(s) }; });

            //we temp bind the error log to nothing, so we aren't spammed during adding items we know will
            //  have lots of failures.
            var prevLog = ConsoleBindingHelper.OnErrorLogDelegate;
            ConsoleBindingHelper.OnErrorLogDelegate = delegate { };
            
            //binding a class intended to have all static items added
            ConsoleBindingHelper.AddAllToConsole(null, null, typeof(ConsoleStaticCommandsTest));

            //binding a specific instance to the console
            ConsoleBindingHelper.AddAllToConsole(instTest, "instTest");

            //binding a class that is a monostate/c# wrapper around a singleton from Unity
            ConsoleBindingHelper.AddAllToConsole(null, null, typeof(Physics));

            Console.Log("Try entering Console.AddStaticTypeByString Physics2D");
            
            //restore the logger
            ConsoleBindingHelper.OnErrorLogDelegate = prevLog;
        }

        //binding via attribute, the RegisterAttributes call in the binder will cause this to be added to the console
        [ConsoleCommand("Console.AddStaticTypeByString", "Attempt to add all static methods, props and fields of the type given by a string")]
        public static void AddStaticTypeByString(string typeName)
        {
            var alltypes = System.AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes());

            var t = alltypes.FirstOrDefault(x => x.Name == typeName);

            if (t == null)
            {
                Console.Log("Could not find type of name " + typeName + ". Make sure you are using the correct namespaces, nestedclass and assembly.");
                return;
            }

            ConsoleBindingHelper.AddAllToConsole(null, null, t);
            Console.Log("Found " + typeName + " in " + t.Assembly.FullName);
        }
    }

    [System.Serializable]
    public class InstanceData
    {
        public float f;
        public int i;

        public void MethodString(string s)
        {
            Debug.Log(s);
        }
    }

    //putting these in a static class so we can easily bind all of them automatically
    public static class ConsoleStaticCommandsTest
    {
        public static int testField;

        public static float testProp { get; set; }

        public static Vector3 vec3;

        //ignore these
        [ConsoleCommandIgnore]
        public static int ignoredField;

        [ConsoleCommandIgnore]
        public static int ignoredProp { get; set; }

        [ConsoleCommandIgnore]
        public static void IgnoredMethod()
        {
        }

        public static void test()
        {
            Debug.Log("test print");
        }

        public static void PrintInt(int i)
        {
            Debug.Log(i.ToString());
        }

        public static void PrintFloat(float f)
        {
            Debug.Log(f.ToString());
        }

        public static void PrintIntThenFloat(int i, float f)
        {
            Debug.Log(i.ToString() + ": " + f.ToString());
        }

        public static void PrintIntStringFloat(int i, string str, float f)
        {
            Debug.Log(i.ToString() + ", " + str + ", " + f.ToString());
        }

        public static void PrintStringVector3(string str, Vector3 v3)
        {
            Debug.Log(str + ", " + v3.ToString());
        }

        public static void PrintStringVector3Float(string str, Vector3 v3, float a)
        {
            Debug.Log(str + ", " + v3.ToString() + ", " + a.ToString());
        }

        public class SomethingUnsupported { }
        public class SomethingSupported { public int val; }

        public static void MethodWithUnsupportedParamType(SomethingUnsupported s)
        {
        }
        public static void MethodWithSupportedParamType(SomethingSupported s)
        {
            Debug.Log(s.val);
        }
    }
}