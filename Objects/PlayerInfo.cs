using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerInfo : MonoBehaviour
{

    [SerializeField] public string PlayerID { get; set; }
    public string Password { get; set; }
    [SerializeField] public string PlayerName { get; set; }
    public int PlayerIcon { get; set; }

    public int HoloCoins { get; set; }
    public int HoloGold { get; set; }
    public int NNMaterial { get; set; }
    public int RRMaterial { get; set; }
    public int SRMaterial { get; set; }
    public int URMaterial { get; set; }
    public int MatchVictory { get; set; }
    public int MatchLoses { get; set; }
    public int MatchesTotal { get; set; }
    
    public List<PlayerTitle> PlayerTitles { get; set; } = new List<PlayerTitle>();
    public List<PlayerMessageBox> PlayerMessageBox { get; set; } = new List<PlayerMessageBox>();
    public List<PlayerItemBox> PlayerItemBox { get; set; } = new List<PlayerItemBox>();
    public List<PlayerMission> PlayerMissionList { get; set; } = new List<PlayerMission>();
    public List<PlayerBadge> Badges { get; set; } = new List<PlayerBadge>();
}
