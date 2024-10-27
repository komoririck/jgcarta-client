
using System;
using UnityEngine;

[Serializable]
public class PlayerMission : MonoBehaviour
{
    public int playerMissionListID { get; set; }

    public int playerID { get; set; }
    public int missionID { get; set; }
    public DateTime? obtainedDate { get; set; }
    public DateTime? clearData { get; set; }

}