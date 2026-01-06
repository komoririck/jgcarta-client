using System;
using System.Collections.Generic;
using System.Linq;
using static DuelField;
using static Lib;

[Serializable]
public class DuelAction
{
    public Player playerID;
	public Dictionary<Player, string>? players { get; set; }
    public CardData used { get; set; }
    public CardData target { get; set; }
	public List<GameZone> targetedZones { get; set; }
	public string selectedSkill { get; set; }
    public List<int> indexes { get; set; }
    public bool suffle { get; set; }
    public bool suffleHandBackToDeck { get; set; }
    public List<CardData> cards { get; set; }
    public bool yesOrNo { get; set; }
	public int hpAmount { get; set; }
	public int hpFixedValue { get; set; }
	public Display displayType { get; set; }
	public Message message { get; set; }
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
        for (int n = 0; n < cards.Count; n++) {
            if (indexes[n] == 0)
                valid.Add(cards[n]);
        }
        return valid;
    }

    public DuelAction GetClientSidePlayers()
    {
        if (playerID != null || playerID != Player.na)
            playerID = GetClientSideType(playerID);

        if (players != null)
            foreach(Player pl in players.Keys.ToList())
                players[GetClientSideType(pl)] = "x";

        return this;
    }

    private Player GetClientSideType(Player type)
    {
        try 
        {
            var player = Player.PlayerA;
            if (DuelField.INSTANCE.players[Player.PlayerB].Equals(PlayerInfo.INSTANCE.PlayerID))
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