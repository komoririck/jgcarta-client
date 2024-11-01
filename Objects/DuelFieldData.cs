using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;


public class DuelFieldData
{
    public List<Card> playerAArquive;
    public List<Card> playerAHoloPower;
    public List<Card> playerABackPosition;
    public Card playerAFavourite;
    public Card playerAStage;
    public Card playerACollaboration;
    public List<Card> playerALife;
    public List<Card> playerBArquive;
    public List<Card> playerBHoloPower;
    public List<Card> playerBBackPosition;
    public Card playerBFavourite;
    public Card playerBStage;
    public Card playerBCollaboration;
    public List<Card> playerBLife;

    public string currentPlayerTurn;
    [JsonIgnore]
    public GAMEPHASE currentGamePhase = GAMEPHASE.StartDuel;
    public string firstPlayer;
    public string secondPlayer;
    [JsonIgnore]
    public List<Card> playerLimiteCardPlayed = new();
    [Flags]
    public enum GAMEPHASE : byte
    {
        StartDuel = 200,
        InitialDraw = 201,
        Mulligan = 202,
        ForcedMulligan = 203,
        SettingUpBoard = 204,
        StartMatch = 0,
        ResetStep = 1,
        ResetStepReSetStage = 11,
        DrawStep = 2,
        CheerStep = 3,
        CheerStepChoose = 4,
        CheerStepChoosed = 5,
        MainStep = 6,
        PerformanceStep = 7,
        UseArt = 8,
        EndStep = 9,
        ConditionedDraw = 101,
        ConditionedSummom = 102,
        HolomemDefeated = 103,
        HolomemDefeatedEnergyChoose = 104
    }



    [Serializable]
    public class DuelFieldDataSerializable
    {
        public List<CardData> playerABackPosition { get; set; } = new List<CardData>();
        public CardData playerAFavourite { get; set; } = new CardData();
        public CardData playerAStage { get; set; } = new CardData();
        public CardData playerACollaboration { get; set; } = new CardData();
        public List<CardData> playerALife { get; set; } = new List<CardData>();
        public List<CardData> playerBBackPosition { get; set; } = new List<CardData>();
        public CardData playerBFavourite { get; set; } = new CardData();
        public CardData playerBStage { get; set; } = new CardData();
        public CardData playerBCollaboration { get; set; } = new CardData();
        public List<CardData> playerBLife { get; set; } = new List<CardData>();
    }

    public static DuelFieldDataSerializable ConvertToSerializable(DuelFieldData data)
    {
        return new DuelFieldDataSerializable
        {
            playerABackPosition = data.playerABackPosition.Select(CardData.CreateCardDataFromCard).ToList(),
            playerAFavourite = CardData.CreateCardDataFromCard(data.playerAFavourite),
            playerAStage = CardData.CreateCardDataFromCard(data.playerAStage),
            playerACollaboration = CardData.CreateCardDataFromCard(data.playerACollaboration),
            playerALife = data.playerALife.Select(CardData.CreateCardDataFromCard).ToList(),

            playerBBackPosition = data.playerBBackPosition.Select(CardData.CreateCardDataFromCard).ToList(),
            playerBFavourite = CardData.CreateCardDataFromCard(data.playerBFavourite),
            playerBStage = CardData.CreateCardDataFromCard(data.playerBStage),
            playerBCollaboration = CardData.CreateCardDataFromCard(data.playerBCollaboration),
            playerBLife = data.playerBLife.Select(CardData.CreateCardDataFromCard).ToList(),

        };
    }
}
