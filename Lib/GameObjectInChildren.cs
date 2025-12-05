using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
    public static List<GameObject> GameObjectInChildren<T>(this IEnumerable<T> objs) where T : Component
    {
        List<GameObject> results = new List<GameObject>();

        foreach (T item in objs)
        {
            results.Add(item.gameObject);
        }

        return results;
    }
}