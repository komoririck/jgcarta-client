using System;
using System.Collections.Generic;

[Serializable]
public class DuelAction
{
    public string playerID;
    public CardData usedCard { get; set; }
    public Lib.GameZone activationZone { get; set; }
    public Lib.GameZone targetZone { get; set; }
    public CardData targetCard { get; set; }
    public CardData cheerCostCard { get; set; }
    public string selectedSkill { get; set; }
    public List<string> SelectedCards { get; set; }
    public List<int> Order { get; set; }
    public bool suffle { get; set; }
    public bool suffleHandBackToDeck { get; set; }
    public List<CardData> cardList { get; set; }
    public DuelFieldData duelFieldData { get; set; }
    public bool yesOrNo { get; set; }
    public bool toBottom { get; set; }
    public List<int> diceRoll { get; set; }
    public int hpAmount { get; set; }
    public string actionType { get; set; }
    public string actionObject { get; set; }
}