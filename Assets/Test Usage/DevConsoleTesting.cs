using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DevConsoleTesting : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        AID.Console.AddAllStaticsToConsole(typeof(Physics),"Physics");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
