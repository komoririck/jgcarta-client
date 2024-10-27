using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerMatchRoom : MonoBehaviour
{
    public string RoomID { get; set; }
    public DateTime RegDate { get; set; }
    public int RoomCode { get; set; }
    public int MaxPlayer { get; set; }
    public int OwnerID { get; set; }

    public List<PlayerMatchRoomPool> PlayerMatchRoomPool { get; set; } = new List<PlayerMatchRoomPool>();

    void Start()
    {
        
    }
    void Update()
    {
        
    }
}
