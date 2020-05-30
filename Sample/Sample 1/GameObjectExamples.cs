using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Net;

namespace AID.Exaples
{
    /**
     * Example console commands for getting information about GameObjects
     */
    public static class GameObjectCommands
    {

        [ConsoleCommand("GO.List", "lists all the game objects in the scene")]
        public static void ListGameObjects()
        {
            UnityEngine.Object[] objects = UnityEngine.Object.FindObjectsOfType(typeof(GameObject));
            foreach (UnityEngine.Object obj in objects)
            {
                Console.Log(obj.name);
            }
        }

        [ConsoleCommand("GO.Print", "lists properties of the object")]
        public static void PrintGameObject(string name)
        {
            var obj = GameObject.Find(name);
            if (obj == null)
            {
                Console.Log("GameObject not found : " + name);
            }
            else
            {
                Console.Log("Game Object : " + obj.name);
                foreach (var comp in obj.GetComponents(typeof(Component)))
                {
                    Console.Log("  Component : " + comp.GetType());
                    foreach (var f in comp.GetType().GetFields())
                    {
                        Console.Log("    " + f.Name + " : " + f.GetValue(comp));
                    }
                    foreach (var p in comp.GetType().GetProperties())
                    {
                        Console.Log("    " + p.Name + " : " + p.GetValue(comp));
                    }
                }
            }
        }
    }
}