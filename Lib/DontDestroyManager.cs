using System.Collections.Generic;
using UnityEngine;

public class DontDestroyManager : MonoBehaviour
{
    private static List<GameObject> dontDestroyObjects = new();

    public static void MarkAsDontDestroy(GameObject obj)
    {
        DontDestroyOnLoad(obj);
        dontDestroyObjects.Add(obj);
    }

    public static void DestroyAllDontDestroyOnLoadObjects()
    {
        foreach (GameObject obj in dontDestroyObjects)
        {
            Destroy(obj);
        }
        dontDestroyObjects.Clear();
    }
}