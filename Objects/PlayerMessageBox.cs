
using System;
using UnityEngine;

[Serializable]
public class PlayerMessageBox : MonoBehaviour
{
    public int messageID { get; set; }

    public int playerID { get; set; }
    public string title { get; set; }
    public string description { get; set; }
    public DateTime? obtainedDate { get; set; }

}