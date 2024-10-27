using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOnDisable : MonoBehaviour
{
    // Start is called before the first frame update
    private void OnDisable()
    {
        Destroy(this.gameObject);
    }
}
