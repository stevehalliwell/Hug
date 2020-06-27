using AID;
using System.Collections.Generic;
using UnityEngine;

public class DevConsoleTesting : MonoBehaviour
{
    private List<string> bindingErrors = new List<string>();

    // Start is called before the first frame update
    private void Start()
    {
        ConsoleBindingHelper.OnErrorLogDelegate = BindingLog;
        ConsoleBindingHelper.AddAllToConsole(null, null, typeof(Physics));

        if (bindingErrors.Count > 0)
            UnityEngine.Debug.LogWarning(bindingErrors.Count.ToString() + " errors during AddAll Physics");

        ConsoleBindingHelper.OnErrorLogDelegate = UnityEngine.Debug.LogError;
        bindingErrors.Clear();
    }

    private void BindingLog(string obj)
    {
        bindingErrors.Add(obj);
    }

    // Update is called once per frame
    private void Update()
    {
    }
}