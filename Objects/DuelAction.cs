using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using static DuelField;
using static Lib;

[Serializable]
public class DuelAction
{
    public Player player;
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
    public int? minPick { get; set; }
    public bool reSelect { get; set; }
    public bool targetType { get; set; }
    public bool lookLastZone { get; set; }
    public bool canClosePanel { get; set; }
    public GAMEPHASE gamePhase { get; set; }

    public void OnAfterDeserialize()
    {
        ResolveOwner();
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
        for (int n = 0; n < cards.Count; n++) {
            if (indexes[n] == 0)
                valid.Add(cards[n]);
        }
        return valid;
    }
    public void ResolveOwner()
    {
        if (DuelField.INSTANCE == null) return;
        player = (DuelField.INSTANCE.players[player] == PlayerInfo.INSTANCE.PlayerID) ? Player.Player : Player.Oponnent;

        if (DuelField.INSTANCE.players.Count == 1) 
        {
            var value = (DuelField.INSTANCE.players.Values.First() == PlayerInfo.INSTANCE.PlayerID) ? Player.Player : Player.Oponnent;
            DuelField.INSTANCE.players.Clear();
            DuelField.INSTANCE.players.Add(value, "x");
        }
    }
}