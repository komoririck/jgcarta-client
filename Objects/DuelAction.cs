using Newtonsoft.Json;
using System;
using System.Collections.Generic;

[Serializable]
public class DuelAction
{
    public string playerID;
    public CardData usedCard = new();
    public string playedFrom;
    public string local;
    public CardData targetCard;
    public CardData cheerCostCard;
    public string actionObject { get; set; }
    public string actionType { get; set; }
    public string actionType_Two { get; set; }
    public string selectedSkill { get; set; }
    public List<string> SelectedCards { get; set; }
    public List<int> Order { get; set; }
    public bool suffle { get; set; }
    public bool suffleBackToDeck { get; set; }
    public string zone { get; set; }
    public List<Card> cardList { get; set; } = new List<Card>();
}