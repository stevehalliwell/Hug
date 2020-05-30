using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AID;

//ok lets 
//  smart get set - give it a full var path and it'll try to set it
//      could replace minding many many things that we can just reflect anyway
//  something to specify don't wrap and convert params just pass the string down


public class ConsoleTest : MonoBehaviour {

    public InstanceData instTest =  new InstanceData();
    //public TextAsset txt;

    // Use this for initialization
    void Start ()
    {
        //List<AID.ConsoleHelper.ClassByNameData> toAdd = new List<AID.ConsoleHelper.ClassByNameData>();
        //toAdd.Add(new AID.ConsoleHelper.ClassByNameData("ConsoleStaticCommandsTest"));
        //toAdd.Add(new AID.ConsoleHelper.ClassByNameData("UnityEngine.Time", "Time"));
        //toAdd.Add(new AID.ConsoleHelper.ClassByNameData("UnityEngine.Physics", "Physics"));

        ////proving we can go thro csv and back to load from file
        //var asStr = DeadSimpleCSV.CreateFromList(toAdd).GetAsCSVString();
        //AID.UTIL.WriteAllText(Application.dataPath + "//testConsoleCommand.csv", asStr);
        

        Console.AddAllStaticsToConsole(typeof(ConsoleStaticCommandsTest));
        Console.AddAllInstanceToConsole(instTest, "instTest");
        //ConsoleHelper.AddAllToConsole(TestFancyScriptableObject.GetInstances()[0], "TestFancyScriptableObject");

        var  g = Physics.gravity;

        Console.AddAllStaticsToConsole(typeof(Physics));
        //ConsoleHelper.AddAllStaticsToConsole(typeof(Physics2D));
        //ConsoleHelper.AddAllStaticsToConsole(typeof(Time));
        //ConsoleHelper.AddAllStaticsToConsole(typeof(QualitySettings));
        //ConsoleHelper.AddAllStaticsToConsole(typeof(Graphics));
        //ConsoleHelper.AddAllStaticsToConsole(typeof(Renderer));
        //ConsoleHelper.AddAllStaticsToConsole(typeof(RenderSettings));
        //ConsoleHelper.AddAllStaticsToConsole(typeof(Application));
        //ConsoleHelper.AddAllStaticsToConsole(typeof(Screen));
        //ConsoleHelper.AddAllStaticsToConsole(typeof(CUDLR.Console));


        //StartCoroutine(DoALog());
    }

    //public IEnumerator DoALog()
    //{
    //    yield return new WaitForSeconds(0.15f);
    //    Debug.Log("I logged after a wait");
    //}
}

[System.Serializable]
public class InstanceData
{
    public float f;
    public int i;

    public void Method(string s)
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

    public static void PrintInt( int i)
    {
        Debug.Log(i.ToString());
    }
    
    public static void PrintFloat(float f)
    {
        Debug.Log(f.ToString());
    }
    
    public static void PrintIntThenFloat(int i, float f)
    {
        Debug.Log(i.ToString() +": " + f.ToString());
    }

    public static void PrintIntStringFloat(int i, string str, float f)
    {
        Debug.Log(i.ToString() + ", " + str + ", " + f.ToString());
    }

    public class Something { }
    public static void MethodWithUnsupportedParamType(Something s)
    {

    }
}