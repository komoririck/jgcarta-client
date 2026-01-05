using System;
using System.Collections.Generic;
using static DuelField;
using static DuelFieldData;
using static Lib;

[Serializable]
public class DuelAction
{
    public Player playerID;
    public CardData usedCard { get; set; }
    public Lib.GameZone activationZone { get; set; }
    public Lib.GameZone targetZone { get; set; }
    public CardData targetCard { get; set; }
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
    public GAMEPHASE gamePhase { get; set; }

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

    public DuelAction GetClientSidePlayers()
    {
        if (playerID != null || playerID != Player.na)
            playerID = GetClientSideType(playerID);

        if (actionTarget != null || actionTarget != Player.na)
            actionTarget = GetClientSideType(actionTarget);

        return this;
    }

    private Player GetClientSideType(Player type)
    {
        // if (DuelField.INSTANCE == null || DuelField.INSTANCE.DUELFIELDDATA == null)
        //   return Player.na;
        try 
        {
            var player = Player.PlayerA;
            if (DuelField.INSTANCE.DUELFIELDDATA.players[Player.PlayerB].Equals(PlayerInfo.INSTANCE.PlayerID))
                player = Player.PlayerB;

            if (type == player)
                return Player.Player;
            else
                return Player.Oponnent;
        } catch (Exception e) 
        {
            return Player.na;
        }
    }
}