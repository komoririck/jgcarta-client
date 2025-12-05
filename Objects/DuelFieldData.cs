using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[Serializable]
public class DuelFieldData
{
    [JsonIgnore]
    public GAMEPHASE currentGamePhase = GAMEPHASE.StartDuel;
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


    public List<CardData> playerABackPosition { get; set; } = new List<CardData>();
    public CardData playerAFavourite { get; set; } 
    public CardData playerAStage { get; set; }
    public CardData playerACollaboration { get; set; }
    public List<CardData> playerALife { get; set; } = new List<CardData>();
    public List<CardData> playerBBackPosition { get; set; } = new List<CardData>();
    public CardData playerBFavourite { get; set; }
    public CardData playerBStage { get; set; }
    public CardData playerBCollaboration { get; set; }
    public List<CardData> playerBLife { get; set; } = new List<CardData>();
    public List<CardData> playerBArquive { get; set; } = new List<CardData>();
    public List<CardData> playerBHoloPower { get; set; } = new List<CardData>();
    public List<CardData> playerAArquive { get; set; } = new List<CardData>();
    public List<CardData> playerAHoloPower { get; set; } = new List<CardData>();
    public List<CardData> playerACardCheer { get; set; }
    public List<CardData> playerBCardCheer { get; set; }

    public string currentPlayerTurn;
    public string firstPlayer;
    public string secondPlayer;

    public static DuelFieldData MapDuelFieldData(List<GameObject> field)
    {
        DuelFieldData duelFieldData = DuelField.INSTANCE.duelFieldData;

        if (!duelFieldData.firstPlayer.Equals(PlayerInfo.INSTANCE.PlayerID))
        {
            duelFieldData.playerBArquive = field[0].GetComponentsInChildren<Card>()?.ToList().Select(item => item.ToCardData()).ToList();
            duelFieldData.playerBHoloPower = field[2].GetComponentsInChildren<Card>()?.ToList().Select(item => item.ToCardData()).ToList();
            duelFieldData.playerBBackPosition = field[3].GetComponentsInChildren<Card>()?.ToList().Select(item => item.ToCardData()).ToList();
            duelFieldData.playerBFavourite = field[4].GetComponentInChildren<Card>()?.ToCardData();
            duelFieldData.playerBStage = field[6].GetComponentInChildren<Card>()?.ToCardData();
            duelFieldData.playerBCollaboration = field[5].GetComponentInChildren<Card>()?.ToCardData();
            duelFieldData.playerBLife = field[8].GetComponentsInChildren<Card>()?.ToList().Select(item => item.ToCardData()).ToList();

            duelFieldData.playerAArquive = field[9].GetComponentsInChildren<Card>()?.Select(item => item.ToCardData()).ToList();
            duelFieldData.playerAHoloPower = field[11].GetComponentsInChildren<Card>()?.Select(item => item.ToCardData()).ToList();
            duelFieldData.playerABackPosition = field[12].GetComponentsInChildren<Card>()?.ToList().Select(item => item.ToCardData()).ToList();
            duelFieldData.playerAFavourite = field[13].GetComponentInChildren<Card>()?.ToCardData();
            duelFieldData.playerAStage = field[15].GetComponentInChildren<Card>()?.ToCardData();
            duelFieldData.playerACollaboration = field[14].GetComponentInChildren<Card>()?.ToCardData();
            duelFieldData.playerALife = field[17].GetComponentsInChildren<Card>()?.ToList().Select(item => item.ToCardData()).ToList();

            return duelFieldData;
        }

        duelFieldData.playerAArquive = field[0].GetComponentsInChildren<Card>()?.ToList().Select(item => item.ToCardData()).ToList();
        duelFieldData.playerAHoloPower = field[2].GetComponentsInChildren<Card>()?.ToList().Select(item => item.ToCardData()).ToList();
        duelFieldData.playerABackPosition = field[3].GetComponentsInChildren<Card>()?.ToList().Select(item => item.ToCardData()).ToList();
        duelFieldData.playerAFavourite = field[4].GetComponentInChildren<Card>()?.ToCardData();
        duelFieldData.playerAStage = field[6].GetComponentInChildren<Card>()?.ToCardData() ;
        duelFieldData.playerACollaboration = field[5].GetComponentInChildren<Card>()?.ToCardData();
        duelFieldData.playerALife = field[8].GetComponentsInChildren<Card>().ToList()?.Select(item => item.ToCardData()).ToList();

        duelFieldData.playerBArquive = field[9].GetComponentsInChildren<Card>()?.ToList().Select(item => item.ToCardData()).ToList();
        duelFieldData.playerBHoloPower = field[11].GetComponentsInChildren<Card>()?.ToList().Select(item => item.ToCardData()).ToList();
        duelFieldData.playerBBackPosition = field[12].GetComponentsInChildren<Card>()?.ToList().Select(item => item.ToCardData()).ToList();
        duelFieldData.playerBFavourite = field[13].GetComponentInChildren<Card>()?.ToCardData() ;
        duelFieldData.playerBStage = field[15].GetComponentInChildren<Card>()?.ToCardData();
        duelFieldData.playerBCollaboration = field[14].GetComponentInChildren<Card>()?.ToCardData();
        duelFieldData.playerBLife = field[17].GetComponentsInChildren<Card>().ToList()?.Select(item => item.ToCardData()).ToList();

        return duelFieldData;
    }
}