using System;
using UnityEngine;

public class PlayerItemBox : MonoBehaviour
{
    public int playerItemBoxID { get; set; }

    public int playerID { get; set; }

    public int itemID { get; set; }

    public int amount { get; set; }

    public DateTime obtainedDate { get; set; }

    public DateTime? expirationDate { get; set; }
}