using System;
using System.Collections.Generic;
using static DuelField;
using static Lib;
using static UnityEngine.Rendering.BoolParameter;

[Serializable]
public class DuelAction
{
    public string playerID;
    public CardData usedCard { get; set; }
    public Lib.GameZone activationZone { get; set; }
    public Lib.GameZone targetZone { get; set; }
    public CardData targetCard { get; set; }
    public List<CardData> attachmentCost { get; set; }
    public string selectedSkill { get; set; }
    public List<int> Order { get; set; }
    public bool suffle { get; set; }
    public bool suffleHandBackToDeck { get; set; }
    public List<CardData> cardList { get; set; }
    public DuelFieldData duelFieldData { get; set; }
    public bool yesOrNo { get; set; }
    public bool toBottom { get; set; }
    public List<int> diceRoll { get; set; }
    public int hpAmount { get; set; }
    public int hpFixedValue { get; set; }
    public DuelField.Player actionTarget { get; set; }
    public GameZone[] targetedZones { get; set; }
    public Display displayType { get; set; }
    public Message message { get; set; }
    public List<int> selectableList { get; set; }
    public int maxPick { get; set; }
    public bool reSelect { get; set; }
    public bool targetType { get; set; }
    public bool lookLastZone { get; set; }
    public bool canClosePanel { get; set; }
    public DuelAction GetPlayerTypeById()
    {
        if (DuelField.INSTANCE.DUELFIELDDATA.firstPlayer.Equals(playerID))
            actionTarget = Player.FirstPlayer;
        else if (DuelField.INSTANCE.DUELFIELDDATA.secondPlayer.Equals(playerID))
            actionTarget = Player.SecondPlayer;

        return this;
    }
    public Player GetClientSideType(Player type)
    {
        var player = Player.FirstPlayer;
        if (DuelField.INSTANCE.DUELFIELDDATA.secondPlayer.Equals(PlayerInfo.INSTANCE.PlayerID))
            player = Player.SecondPlayer;

        if (type == player)
                return Player.Player;
            else
                return Player.Oponnent;

        return Player.na;
    }
    [Flags]
    public enum Display : byte
    {
        na = 0,
        Target = 1,
        ListPickAndReorder = 2,
        Number = 3,
        ListPickOne = 4,
        YesOrNo = 5,
        Detach = 6,
    };
    public enum Message : byte
    {
        na = 0,
        Will_you_activate_this_effect = 1,
    };
    public List<CardData> MapSelectable() {

        List<CardData> valid = new();
        for (int n = 0; n < cardList.Count; n++) {
            if (selectableList[n] == 0)
                valid.Add(cardList[n]);
        }
        return valid;
    }
}