using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DevConsoleTesting : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        AID.ConsoleBindingHelper.AddAllToConsole(null,null, typeof(Physics));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
