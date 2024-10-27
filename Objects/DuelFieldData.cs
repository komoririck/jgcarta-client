using Newtonsoft.Json;
using System;
using System.Collections.Generic;

[Serializable]
public class DuelFieldData
{
    public List<Card> playerAHand { get; set; } = new List<Card>();
    public List<Card> playerAArquive { get; set; } = new List<Card>();
    public List<Card> playerADeck { get; set; } = new List<Card>();
    public List<Card> playerAHoloPower { get; set; } = new List<Card>();
    public List<Card> playerABackPosition { get; set; } = new List<Card>();
    public Card playerAFavourite { get; set; } = new Card("");
    public Card playerAStage { get; set; } = new Card("");
    public Card playerACollaboration { get; set; } = new Card("");
    public List<Card> playerACardCheer { get; set; } = new List<Card>();
    public List<Card> playerALife { get; set; } = new List<Card>();

    public List<Card> playerBHand { get; set; } = new List<Card>();
    public List<Card> playerBArquive { get; set; } = new List<Card>();
    public List<Card> playerBDeck { get; set; } = new List<Card>();
    public List<Card> playerBHoloPower { get; set; } = new List<Card>();
    public List<Card> playerBBackPosition { get; set; } = new List<Card>();
    public Card playerBFavourite { get; set; } = new Card("");
    public Card playerBStage { get; set; } = new Card("");
    public Card playerBCollaboration { get; set; } = new Card("");
    public List<Card> playerBCardCheer { get; set; } = new List<Card>();
    public List<Card> playerBLife { get; set; } = new List<Card>();

    public int currentTurn { get; set; }
    public int currentPlayerTurn { get; set; }
    public int currentPlayerActing { get; set; }
    [JsonIgnore]
    public GAMEPHASE currentGamePhase { get; set; } = GAMEPHASE.StartDuel;
    public int firstPlayer { get; set; }
    public int secondPlayer { get; set; }
    public int currentGameHigh { get; set; }

    public List<Card> playerLimiteCardPlayed { get; set; } = new List<Card>();
    public string currentCardResolving { get; set; }
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
}
