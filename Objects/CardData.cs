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
}
