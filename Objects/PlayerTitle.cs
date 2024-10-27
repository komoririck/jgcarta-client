
using System;
using UnityEngine;

[Serializable]
public class PlayerTitle : MonoBehaviour
{
    public int titleID { get; set; }
    public int playerID { get; set; }
    public string titleName { get; set; }
    public string titleDescription { get; set; }
    public DateTime obtainedDate { get; set; }
}