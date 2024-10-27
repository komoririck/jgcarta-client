using UnityEngine;
using System;

[Serializable]
public class PlayerBadge : MonoBehaviour
{
    public int badgeID { get; set; }

    public int rank { get; set; }

    public int playerID { get; set; }

    public DateTime? obtainedDate { get; set; }

}