using System;

[Serializable]
public class CardData
{

    public string cardNumber { get; set; } = "";
    public string playerdFrom { get; set; } = "";
    public string cardPosition { get; set; } = "";

    public static CardData CreateCardDataFromCard(Card card)
    {
        if (card == null) return new CardData();

        return new CardData
        {
            cardNumber = card.cardNumber,
            playerdFrom = card.playedFrom,
            cardPosition = card.cardPosition,
        };
    }
    public static CardData CreateCardDataFromCard(string cardnumber, string playedfrom, string cardposition)
    {
        return new CardData
        {
            cardNumber = cardnumber,
            playerdFrom = playedfrom,
            cardPosition = cardposition,
        };
    }
}
