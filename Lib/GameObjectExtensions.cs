using UnityEngine;

public static class GameObjectExtensions
{
    public static void DestroyAllChildren(GameObject parent)
    {
        // Check if the parent GameObject is not null
        if (parent == null)
        {
            Debug.LogWarning("Parent GameObject is null. Cannot destroy children.");
            return;
        }

        // Get all child GameObjects, including inactive ones
        Transform[] children = parent.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child != parent.transform) // Avoid destroying the parent itself
            {
                Object.Destroy(child.gameObject);
            }
        }
    }
}
