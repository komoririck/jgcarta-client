using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mission : MonoBehaviour
{
    public int MissionID = 0;
    public string MissionName = "";
    public enum MissionType : byte
    {
        Normal = 0,
        Player = 1,
        Event = 2,
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
