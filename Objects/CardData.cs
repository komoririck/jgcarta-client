using System;
using static DuelField;

[Serializable]
public class CardData
{
    public string cardNumber { get; set; }
    public Lib.GameZone curZone { get; set; }
    public Lib.GameZone lastZone { get; set; }
    public Player owner { get; set; }
}