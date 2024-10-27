using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetChild : MonoBehaviour
{
    static public GameObject sourceGameObject;   // The original object from which to replicate children
    static public GameObject targetGameObject;   // The target object where children will be replicated

    static void ValidateAndReplicateChildren(GameObject sourceGameObject)
    {
        // Get all child objects of the source GameObject
        List<GameObject> sourceChildren = new List<GameObject>();
        foreach (Transform child in sourceGameObject.transform)
        {
            sourceChildren.Add(child.gameObject);
        }

        // Get all child objects of the target GameObject
        List<GameObject> targetChildren = new List<GameObject>();
        foreach (Transform child in targetGameObject.transform)
        {
            targetChildren.Add(child.gameObject);
        }

        // Remove any target children that no longer exist in the source
        foreach (GameObject targetChild in targetChildren)
        {
            if (!sourceChildren.Exists(sc => sc.name == targetChild.name))
            {
                Destroy(targetChild);
            }
        }

        // Replicate missing children from the source to the target
        foreach (GameObject sourceChild in sourceChildren)
        {
            if (!targetChildren.Exists(tc => tc.name == sourceChild.name))
            {
                // Instantiate a clone of the source child
                GameObject clonedChild = Instantiate(sourceChild, targetGameObject.transform);
                clonedChild.name = sourceChild.name; // Keep the original name for validation
            }
        }
    }
}