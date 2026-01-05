using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static DuelField;


[Serializable]
public class DuelFieldData
{
    [JsonIgnore]
    public GAMEPHASE currentGamePhase = GAMEPHASE.StartMatch;
    [JsonIgnore]
    public List<Card> playerLimiteCardPlayed = new();
    [Flags]
    public enum GAMEPHASE : byte
    {
        StartMatch,
        Mulligan,
        MulliganForced,
        SettingUpBoard,
        ResetStep,
        SetHolomemStep,
        DrawStep,
        CheerStep,
        CheerStepChoose,
        CheerStepChoosed,
        MainStep,
        PerformanceStep,
        UseArt,
        EndStep,
        DamageCalculation,
        InflictDamage,
    }

    public List<CardData> playerAHand = new List<CardData>();
    public List<CardData> playerBHand = new List<CardData>();

    public List<CardData> playerAHoloPower = new List<CardData>();
    public List<CardData> playerBHoloPower = new List<CardData>();

    public List<CardData> playerADeck = new List<CardData>();
    public List<CardData> playerBDeck = new List<CardData>();

    public List<CardData> playerABackPosition = new List<CardData>();
    public List<CardData> playerBBackPosition = new List<CardData>();

    public CardData playerAFavourite = null;
    public CardData playerBFavourite = null;

    public CardData playerAStage = null;
    public CardData playerBStage = null;

    public CardData playerACollaboration = null;
    public CardData playerBCollaboration = null;

    public List<CardData> playerAArquive = new List<CardData>();
    public List<CardData> playerBArquive = new List<CardData>();

    public List<CardData> playerALife = new List<CardData>();
    public List<CardData> playerBLife = new List<CardData>();

    public List<CardData> playerACardCheer = new List<CardData>();
    public List<CardData> playerBCardCheer = new List<CardData>();

    public Dictionary<Player, string> players { get; set; }
    public Dictionary<string, Player> playersType { get; set; }
    public Player turnPlayer;

    public static DuelFieldData MapDuelFieldData(List<GameObject> field)
    {
        DuelFieldData duelFieldData = DuelField.INSTANCE.DUELFIELDDATA;

        if (!duelFieldData.players.First().Equals(PlayerInfo.INSTANCE.PlayerID))
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

            duelFieldData.playersType = duelFieldData.players.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

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

        duelFieldData.playersType = duelFieldData.players.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        return duelFieldData;
    }
}