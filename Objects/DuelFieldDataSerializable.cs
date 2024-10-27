using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class DuelFieldDataSerializable
{
    public List<CardData> playerAHand { get; set; } = new List<CardData>();
    public List<CardData> playerAArquive { get; set; } = new List<CardData>();
    public List<CardData> playerADeck { get; set; } = new List<CardData>();
    public List<CardData> playerAHoloPower { get; set; } = new List<CardData>();
    public List<CardData> playerABackPosition { get; set; } = new List<CardData>();
    public CardData playerAFavourite { get; set; } = new CardData();
    public CardData playerAStage { get; set; } = new CardData();
    public CardData playerACollaboration { get; set; } = new CardData();
    public List<CardData> playerACardCheer { get; set; } = new List<CardData>();
    public List<CardData> playerALife { get; set; } = new List<CardData>();

    public List<CardData> playerBHand { get; set; } = new List<CardData>();
    public List<CardData> playerBArquive { get; set; } = new List<CardData>();
    public List<CardData> playerBDeck { get; set; } = new List<CardData>();
    public List<CardData> playerBHoloPower { get; set; } = new List<CardData>();
    public List<CardData> playerBBackPosition { get; set; } = new List<CardData>();
    public CardData playerBFavourite { get; set; } = new CardData();
    public CardData playerBStage { get; set; } = new CardData();
    public CardData playerBCollaboration { get; set; } = new CardData();
    public List<CardData> playerBCardCheer { get; set; } = new List<CardData>();
    public List<CardData> playerBLife { get; set; } = new List<CardData>();

    public int currentTurn { get; set; }
    public int currentPlayerTurn { get; set; }
    public int currentPlayerActing { get; set; }
    public int currentGamePhase { get; set; }
    public int firstPlayer { get; set; }
    public int secondPlayer { get; set; }
    public int currentGameHigh { get; set; }
}

[Serializable]
public class CardData
{
    public string cardNumber { get; set; } = "";
    public string playerdFrom { get; set; } = "";
    public string cardPosition { get; set; } = "";
}

public static class DataConverter
{
    public static DuelFieldDataSerializable ConvertToSerializable(DuelFieldData data)
    {
        return new DuelFieldDataSerializable
        {
            playerAHand = data.playerAHand.Select(CreateCardDataFromCard).ToList(),
            playerAArquive = data.playerAArquive.Select(CreateCardDataFromCard).ToList(),
            playerADeck = data.playerADeck.Select(CreateCardDataFromCard).ToList(),
            playerAHoloPower = data.playerAHoloPower.Select(CreateCardDataFromCard).ToList(),
            playerABackPosition = data.playerABackPosition.Select(CreateCardDataFromCard).ToList(),
            playerAFavourite = CreateCardDataFromCard(data.playerAFavourite),
            playerAStage = CreateCardDataFromCard(data.playerAStage),
            playerACollaboration = CreateCardDataFromCard(data.playerACollaboration),
            playerACardCheer = data.playerACardCheer.Select(CreateCardDataFromCard).ToList(),
            playerALife = data.playerALife.Select(CreateCardDataFromCard).ToList(),

            playerBHand = data.playerBHand.Select(CreateCardDataFromCard).ToList(),
            playerBArquive = data.playerBArquive.Select(CreateCardDataFromCard).ToList(),
            playerBDeck = data.playerBDeck.Select(CreateCardDataFromCard).ToList(),
            playerBHoloPower = data.playerBHoloPower.Select(CreateCardDataFromCard).ToList(),
            playerBBackPosition = data.playerBBackPosition.Select(CreateCardDataFromCard).ToList(),
            playerBFavourite = CreateCardDataFromCard(data.playerBFavourite),
            playerBStage = CreateCardDataFromCard(data.playerBStage),
            playerBCollaboration = CreateCardDataFromCard(data.playerBCollaboration),
            playerBCardCheer = data.playerBCardCheer.Select(CreateCardDataFromCard).ToList(),
            playerBLife = data.playerBLife.Select(CreateCardDataFromCard).ToList(),

            currentTurn = data.currentTurn,
            currentPlayerTurn = data.currentPlayerTurn,
            currentPlayerActing = data.currentPlayerActing,
            currentGamePhase = (int)data.currentGamePhase,
            firstPlayer = data.firstPlayer,
            secondPlayer = data.secondPlayer,
            currentGameHigh = data.currentGameHigh
        };
    }

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
