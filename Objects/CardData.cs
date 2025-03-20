using System;

[Serializable]
public class CardData
{

    public string cardNumber { get; set; } = "";
    public string playedFrom { get; set; } = "";
    public string cardPosition { get; set; } = "";

    public static CardData CreateCardDataFromCard(Card card)
    {
        if (card == null) return new CardData();

        return new CardData
        {
            cardNumber = card.cardNumber,
            playedFrom = card.playedFrom,
            cardPosition = card.cardPosition,
        };
    }
    public static CardData CreateCardDataFromCard(string cardnumber, string playedFrom, string cardposition)
    {
        return new CardData
        {
            cardNumber = cardnumber,
            playedFrom = playedFrom,
            cardPosition = cardposition,
        };
    }
}
