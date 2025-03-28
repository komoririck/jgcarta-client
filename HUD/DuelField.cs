﻿using Assets.Scripts.Lib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static DuelField;
using static DuelFieldData;

public class DuelField : MonoBehaviour
{
    private const int TURN_TIMER_SECONDS = 120;
    private int playerTimers;
    private CancellationTokenSource countdownTokenSource;
    [SerializeField] private TMP_Text TimmerText;
    [SerializeField] private TMP_Text TurnCounterText;

    public GameObject cardPrefab;

    public PlayerInfo PlayerInfo = null;
    [SerializeField] private GameSettings GameSettings = null;
    public MatchConnection _MatchConnection = null;

    PhaseMessage GamePhaseMsg;

    public RectTransform cardHolderPlayer;
    public List<RectTransform> cardsPlayer; 

    public RectTransform cardHolderOponnent;
    public List<RectTransform> cardsOponnent; 

    public RectTransform cardLifeHolderA; 
    public List<RectTransform> cardsLifeStageA; 

    public RectTransform cardLifeHolderB; 
    public List<RectTransform> cardsLifeStageB;

    public GameObject DeckPlayer;
    public GameObject DeckOponnent;

    public GameObject VictoryPanel;
    public GameObject LosePanel;

    public float overlapFactor = 0.8f; // Factor to control the amount of overlap (0 = full overlap, 1 = no overlap)

    //SendCardToZoneAnimation
    public float moveDuration = 030f;

    [SerializeField] private GameObject MulliganMenu = null;
    [SerializeField] private GameObject ReadyButton = null;
    [SerializeField] private GameObject EndTurnButton = null;

    [SerializeField] private GameObject EffectConfirmationTab = null;
    [SerializeField] private GameObject EffectConfirmationYesButton = null;
    [SerializeField] private GameObject EffectConfirmationNoButton = null;

    private bool ReadyButtonShowed = false;

    public int currentGameHigh = 0;
    private List<Card> holoPowerList;
    public bool LockGameFlow = false;

    bool playerMulligan = false;
    bool oponnentMulligan = false;

    bool playerMulliganF = false;
    bool oponnentMulliganF = false;

    public bool centerStageArtUsed = false;
    public bool collabStageArtUsed = false;

    public bool usedSPOshiSkill = false;
    public bool usedOshiSkill = false;

    bool startofmain = false;

    public List<GameObject> GameZones = new();

    private bool InitialDraw = false;
    private bool InitialDrawP2 = false;

    public Sprite viewTypeActionImg;
    public Sprite viewTypeViewImg;

    private bool playerCannotDrawFromCheer;
    private int cheersAssignedThisChainTotal;
    private int cheersAssignedThisChainAmount;

    [SerializeField] public GameObject CardEffectPanel = null;
    [SerializeField] public GameObject ArtPanel = null;
    [SerializeField] public GameObject OshiPowerPanel = null;

    [Flags]
    public enum TargetPlayer : byte
    {
        Player = 0,
        Oponnent = 1
    }

    EffectController _EffectController;
    DuelField_LogManager _DuelField_LogManager;

    private int currentTurn;

    void Start()
    {
        PlayerInfo = GameObject.Find("PlayerInfo").GetComponent<PlayerInfo>();
        GameSettings = GameObject.Find("GameSettings").GetComponent<GameSettings>();
        _MatchConnection = FindAnyObjectByType<MatchConnection>();
        GamePhaseMsg = FindAnyObjectByType<PhaseMessage>();
        _MatchConnection._DuelFieldData = FindAnyObjectByType<MatchConnection>()._DuelFieldData;
        _EffectController = FindAnyObjectByType<EffectController>();
        _DuelField_LogManager = FindAnyObjectByType<DuelField_LogManager>();

        //this fix the button initial icon
        SetViewMode(GameObject.Find("MatchUI").transform.Find("ActionTypeToggle").GetComponent<Image>());
    }

    void Update()
    {
        UpdateBoard();

        if (_MatchConnection.DuelActionListIndex.Count > currentGameHigh && !LockGameFlow)
        {
            for (int SrvMessageCounter = currentGameHigh; SrvMessageCounter < _MatchConnection.DuelActionListIndex.Count; SrvMessageCounter++)
            {
                List<Record> cardlist = new List<Record>();
                GameObject zone = null;

                string DuelActionTypeOfAction = _MatchConnection.DuelActionListIndex[SrvMessageCounter];
                JsonSerializerSettings jsonsetting = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore };
                DuelAction SrvMessageCounter_DuelAction = null;
                try
                {
                    SrvMessageCounter_DuelAction = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), jsonsetting);
                }
                catch (Exception ex) { }

                switch (DuelActionTypeOfAction)
                {
                    case "StartDuel":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.StartDuel)
                        {
                            throw new Exception("not in the right gamephase, we're at " + DuelActionTypeOfAction + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        AddCardsToDeck(GetZone("Deck", TargetPlayer.Player), 50, true);
                        AddCardsToDeck(GetZone("Deck", TargetPlayer.Oponnent), 50, true);


                        AddCardsToDeck(GetZone("CardCheer", TargetPlayer.Player), 20, true);
                        AddCardsToDeck(GetZone("CardCheer", TargetPlayer.Oponnent), 20, true);

                        if (!_MatchConnection._DuelFieldData.firstPlayer.Equals(PlayerInfo.PlayerID))
                        {
                            AddOrMoveCardToGameZone(GetZone("Favourite", TargetPlayer.Player), new List<Card>() { new Card("") });
                            AddOrMoveCardToGameZone(GetZone("Favourite", TargetPlayer.Oponnent), new List<Card>() { new Card("") });
                        }
                        else
                        {
                            AddOrMoveCardToGameZone(GetZone("Favourite", TargetPlayer.Player), new List<Card>() { new Card("") });
                            AddOrMoveCardToGameZone(GetZone("Favourite", TargetPlayer.Oponnent), new List<Card>() { new Card("") });
                        }

                        _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.InitialDraw;
                        GamePhaseMsg.gameObject.SetActive(true);
                        GamePhaseMsg.StartMessage("Starting Duel");
                        currentGameHigh = 1;

                        StartTurnCounter();
                        TurnCounterText.text = currentTurn.ToString();
                        break;
                    case "InitialDraw":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.InitialDraw)
                        {
                            throw new Exception("not in the right gamephase, we're at " + DuelActionTypeOfAction + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        //effect draw
                        DrawCard(SrvMessageCounter_DuelAction);
                        InitialDraw = true;

                        currentGameHigh++;

                        if (InitialDraw && InitialDrawP2)
                        {
                            MulliganBox();
                            _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.Mulligan;
                            LockGameFlow = true;
                        }
                        break;
                    case "InitialDrawP2":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.InitialDraw)
                        {
                            throw new Exception("not in the right gamephase, we're at " + DuelActionTypeOfAction + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        //effect draw
                        DrawCard(SrvMessageCounter_DuelAction);
                        InitialDrawP2 = true;

                        currentGameHigh++;
                        if (InitialDraw && InitialDrawP2)
                        {
                            MulliganBox();
                            _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.Mulligan;
                            LockGameFlow = true;
                        }
                        break;
                    case "PAMulligan":
                    case "PBMulligan":

                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.Mulligan)
                        {
                            throw new Exception("not in the right gamephase, we're at " + DuelActionTypeOfAction + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        if (SrvMessageCounter_DuelAction.playerID.Equals(PlayerInfo.PlayerID))
                        {
                            if (!string.IsNullOrEmpty(SrvMessageCounter_DuelAction.actionObject))
                                if (SrvMessageCounter_DuelAction.actionObject.Equals("True"))
                                {
                                    RemoveCardsFromCardHolder(SrvMessageCounter_DuelAction.cardList.Count, cardsPlayer, cardHolderPlayer);
                                    AddCardsToDeck(GetZone("Deck", TargetPlayer.Player), 7, SrvMessageCounter_DuelAction.suffle);
                                    DrawCard(SrvMessageCounter_DuelAction);
                                }
                            playerMulligan = true;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(SrvMessageCounter_DuelAction.actionObject))
                                if (SrvMessageCounter_DuelAction.actionObject.Equals("True"))
                                {
                                    RemoveCardsFromCardHolder(SrvMessageCounter_DuelAction.cardList.Count, cardsOponnent, cardHolderOponnent);
                                    AddCardsToDeck(GetZone("Deck", TargetPlayer.Oponnent), 7, SrvMessageCounter_DuelAction.suffle);
                                    DrawCard(SrvMessageCounter_DuelAction);
                                }
                            oponnentMulligan = true;
                        }
                        //suffle oponent hands and redraw
                        //suffle our hands and redraw

                        if (playerMulligan && oponnentMulligan)
                        {
                            _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.ForcedMulligan;
                            currentGameHigh++;
                            currentGameHigh++;
                        }

                        break;
                    case "PBNoMulligan":
                    case "PANoMulligan":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.Mulligan)
                        {
                            throw new Exception("not in the right gamephase, we're at " + DuelActionTypeOfAction + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        if (SrvMessageCounter_DuelAction.playerID.Equals(PlayerInfo.PlayerID))
                            playerMulligan = true;
                        else
                            oponnentMulligan = true;

                        if (playerMulligan && oponnentMulligan)
                        {
                            _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.ForcedMulligan;
                            currentGameHigh++;
                            currentGameHigh++;
                        }
                        break;
                    case "PBMulliganF":
                    case "PAMulliganF":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.ForcedMulligan)
                        {
                            throw new Exception("not in the right gamephase, we're at " + DuelActionTypeOfAction + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        // ESSA LOGICA PODE ESTAR ERRADA NO CENARIO FORA DO MOC QUE O PLAYER È O PA E NAO O PB
                        if (SrvMessageCounter_DuelAction.playerID.Equals(PlayerInfo.PlayerID))
                        {
                            if (DuelActionTypeOfAction.Equals("PAMulliganF") && playerMulliganF)
                                break;

                            if (!string.IsNullOrEmpty(SrvMessageCounter_DuelAction.actionObject))
                                if (SrvMessageCounter_DuelAction.actionObject.Equals("True"))
                                {
                                    RemoveCardsFromCardHolder(7, cardsPlayer, cardHolderPlayer);
                                    AddCardsToDeck(GetZone("Deck", TargetPlayer.Player), 7, SrvMessageCounter_DuelAction.suffle);
                                    DrawCard(SrvMessageCounter_DuelAction);
                                }
                            playerMulliganF = true;
                        }
                        else
                        {
                            if (DuelActionTypeOfAction.Equals("PBMulliganF") && oponnentMulliganF)
                                break;

                            if (!string.IsNullOrEmpty(SrvMessageCounter_DuelAction.actionObject))
                                if (SrvMessageCounter_DuelAction.actionObject.Equals("True"))
                                {
                                    RemoveCardsFromCardHolder(7, cardsOponnent, cardHolderOponnent);
                                    AddCardsToDeck(GetZone("Deck", TargetPlayer.Oponnent), 7, SrvMessageCounter_DuelAction.suffle);
                                    DrawCard(SrvMessageCounter_DuelAction);
                                }
                            oponnentMulliganF = true;
                        }


                        if (playerMulliganF && oponnentMulliganF)
                        {
                            currentGameHigh++;
                            currentGameHigh++;
                            _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.SettingUpBoard;
                            LockGameFlow = true;
                        }
                        break;
                    case "DuelUpdate":

                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.SettingUpBoard)
                        {
                            throw new Exception("not in the right gamephase, we're at " + DuelActionTypeOfAction + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        DuelFieldData boardinfo = JsonConvert.DeserializeObject<DuelFieldData>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), jsonsetting);


                        RemoveCardFromZone(GetZone("Favourite", TargetPlayer.Player), 1);
                        RemoveCardFromZone(GetZone("Favourite", TargetPlayer.Oponnent), 1);

                        if (_MatchConnection._DuelFieldData.currentPlayerTurn.Equals(PlayerInfo.PlayerID))
                        {
                            AddOrMoveCardToGameZone(GetZone("Life", TargetPlayer.Oponnent), boardinfo.playerBLife);
                            RemoveCardFromZone(GetZone("CardCheer", TargetPlayer.Oponnent), boardinfo.playerBLife.Count);
                            AddOrMoveCardToGameZone(GetZone("Life", TargetPlayer.Player), boardinfo.playerALife);
                            RemoveCardFromZone(GetZone("CardCheer", TargetPlayer.Player), boardinfo.playerALife.Count);

                            AddOrMoveCardToGameZone(GetZone("Favourite", TargetPlayer.Player), new List<Card>() { boardinfo.playerAFavourite });
                            AddOrMoveCardToGameZone(GetZone("Favourite", TargetPlayer.Oponnent), new List<Card>() { boardinfo.playerBFavourite });

                            AddOrMoveCardToGameZone(GetZone("Stage", TargetPlayer.Oponnent), new List<Card>() { boardinfo.playerBStage });
                            for (int n = 0; n < boardinfo.playerBBackPosition.Count; n++)
                            {
                                AddOrMoveCardToGameZone(GetZone(boardinfo.playerBBackPosition[n].cardPosition, TargetPlayer.Oponnent), new List<Card>() { boardinfo.playerBBackPosition[n] });
                            }
                            int x = boardinfo.playerBBackPosition.Count + 1;
                            RemoveCardsFromCardHolder(x, cardsOponnent, cardHolderOponnent);

                        }
                        else
                        {
                            AddOrMoveCardToGameZone(GetZone("Life", TargetPlayer.Player), boardinfo.playerBLife);
                            RemoveCardFromZone(GetZone("CardCheer", TargetPlayer.Player), boardinfo.playerBLife.Count);
                            AddOrMoveCardToGameZone(GetZone("Life", TargetPlayer.Oponnent), boardinfo.playerALife);
                            RemoveCardFromZone(GetZone("CardCheer", TargetPlayer.Oponnent), boardinfo.playerALife.Count);

                            AddOrMoveCardToGameZone(GetZone("Favourite", TargetPlayer.Player), new List<Card>() { boardinfo.playerBFavourite });
                            AddOrMoveCardToGameZone(GetZone("Favourite", TargetPlayer.Oponnent), new List<Card>() { boardinfo.playerAFavourite });

                            AddOrMoveCardToGameZone(GetZone("Stage", TargetPlayer.Oponnent), new List<Card>() { boardinfo.playerAStage });
                            for (int n = 0; n < boardinfo.playerABackPosition.Count; n++)
                            {
                                AddOrMoveCardToGameZone(GetZone(boardinfo.playerABackPosition[n].cardPosition, TargetPlayer.Oponnent), new List<Card>() { boardinfo.playerABackPosition[n] });
                            }

                            int x = boardinfo.playerABackPosition.Count + 1;
                            RemoveCardsFromCardHolder(x, cardsOponnent, cardHolderOponnent);
                        }

                        foreach (RectTransform lifecard in cardLifeHolderA)
                        {
                            cardsLifeStageA.Add(lifecard);
                        }
                        ArrangeCards(cardsLifeStageA, cardLifeHolderA);

                        foreach (RectTransform lifecard in cardLifeHolderB)
                        {
                            cardsLifeStageB.Add(lifecard);
                        }
                        ArrangeCards(cardsLifeStageB, cardLifeHolderB);

                        _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "DrawRequest", "AskDrawPhase", "");



                        StartTurnCounter();
                        TurnCounterText.text = currentTurn.ToString();

                        currentGameHigh++;
                        _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.DrawStep;
                        break;
                    case "ResetStep":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.ResetStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + DuelActionTypeOfAction + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        StartTurnCounter();
                        TurnCounterText.text = currentTurn++.ToString();

                        if (_MatchConnection._DuelFieldData.currentPlayerTurn.Equals(PlayerInfo.PlayerID))
                            ResetCardTurnStatusForPlayer(TargetPlayer.Player);
                        else
                            ResetCardTurnStatusForPlayer(TargetPlayer.Oponnent);


                        if (!string.IsNullOrEmpty(SrvMessageCounter_DuelAction.usedCard.cardNumber))
                            if (PlayerInfo.PlayerID != SrvMessageCounter_DuelAction.playerID)
                            {
                                zone = GetZone(SrvMessageCounter_DuelAction.usedCard.cardPosition, TargetPlayer.Oponnent);
                                AddOrMoveCardToGameZone(zone, cardsToBeMoved: GetZone(SrvMessageCounter_DuelAction.playedFrom, TargetPlayer.Oponnent).GetComponentsInChildren<GameObject>().ToList<GameObject>());
                                Card unRestCard = zone.GetComponentInChildren<Card>();
                                unRestCard.suspended = true;
                                unRestCard.cardPosition = SrvMessageCounter_DuelAction.usedCard.cardPosition;
                                zone.transform.GetChild(zone.transform.childCount - 1).Rotate(0, 0, 90);
                                //maybe add the playedfrom here
                            }
                            else
                            {
                                zone = GetZone(SrvMessageCounter_DuelAction.usedCard.cardPosition, TargetPlayer.Player);
                                AddOrMoveCardToGameZone(zone, cardsToBeMoved: GetZone(SrvMessageCounter_DuelAction.playedFrom, TargetPlayer.Player).GetComponentsInChildren<GameObject>().ToList<GameObject>());
                                Card unBloomCard = zone.GetComponentInChildren<Card>();
                                unBloomCard.suspended = true;
                                unBloomCard.cardPosition = SrvMessageCounter_DuelAction.usedCard.cardPosition;
                                zone.transform.GetChild(zone.transform.childCount - 1).Rotate(0, 0, 90);
                                //maybe add the playedfrom here
                            }


                        if (_MatchConnection._DuelFieldData.currentPlayerTurn.Equals(PlayerInfo.PlayerID))
                        {
                            if (GetZone("Stage", TargetPlayer.Player).transform.GetComponentInChildren<Card>() != null)
                            {
                                _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.DrawStep;
                                _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "DrawRequest", "DrawRequest", "DrawRequest");
                            }
                            else
                            {
                                GamePhaseMsg.gameObject.SetActive(true);
                                GamePhaseMsg.StartMessage("Select a new stage member");
                                _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.ResetStepReSetStage;
                                LockGameFlow = true;
                            }
                        }
                        else
                        {
                            if (GetZone("Stage", TargetPlayer.Player).transform.GetComponentInChildren<Card>() != null)
                            {
                                _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.DrawStep;
                            }
                            else
                            {
                                _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.ResetStepReSetStage;
                            }
                        }

                        GamePhaseMsg.gameObject.SetActive(true);
                        GamePhaseMsg.StartMessage("Reset Step");
                        currentGameHigh++;
                        break;
                    case "ReSetStage":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.ResetStepReSetStage)
                        {
                            throw new Exception("not in the right gamephase, we're at " + DuelActionTypeOfAction + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        TargetPlayer target = PlayerInfo.PlayerID == SrvMessageCounter_DuelAction.playerID ? TargetPlayer.Player : TargetPlayer.Oponnent;

                        if (!string.IsNullOrEmpty(SrvMessageCounter_DuelAction.usedCard.cardNumber))
                        {
                            zone = GetZone(SrvMessageCounter_DuelAction.usedCard.cardPosition, target);
                            AddOrMoveCardToGameZone(zone, cardsToBeMoved: GetZone(SrvMessageCounter_DuelAction.playedFrom, target).GetComponentsInChildren<GameObject>().ToList<GameObject>());
                            Card unRestCard = zone.GetComponentInChildren<Card>();
                            unRestCard.suspended = false;
                            unRestCard.cardPosition = SrvMessageCounter_DuelAction.usedCard.cardPosition;
                        }

                        _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.DrawStep;

                        if (_MatchConnection._DuelFieldData.currentPlayerTurn.Equals(PlayerInfo.PlayerID))
                        {
                            _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "DrawRequest", "DrawRequest", "DrawRequest");
                        }
                        currentGameHigh++;
                        break;
                    case "DrawPhase":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.DrawStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + DuelActionTypeOfAction + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        DrawCard(SrvMessageCounter_DuelAction);


                        if (_MatchConnection._DuelFieldData.currentPlayerTurn.Equals(PlayerInfo.PlayerID))
                            _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "CheerRequest", "AskNextPhase", "DrawPhase");
                        _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.CheerStep;
                        currentGameHigh++;
                        GamePhaseMsg.gameObject.SetActive(true);
                        GamePhaseMsg.StartMessage("Draw Step");
                        break;
                    case "DefeatedHoloMember":
                    case "DefeatedHoloMemberByEffect":

                        TargetPlayer _TargetPlayer = SrvMessageCounter_DuelAction.playerID == PlayerInfo.PlayerID ? TargetPlayer.Player : TargetPlayer.Oponnent;
                        GameObject zoneArt = GetZone(SrvMessageCounter_DuelAction.targetCard.cardPosition, _TargetPlayer);

                        // Get all child objects, including inactive ones, excluding the parent
                        foreach (Card child in zoneArt.GetComponentsInChildren<Card>(true))
                        {
                            // Skip the parent object
                            if (child == zoneArt.transform)
                                continue;

                            GameObject childObject = child.gameObject;

                            // Activate the child object
                            childObject.SetActive(true);

                            // Reset the attachedCards and bloomChild fields
                            Card cardComponent = childObject.GetComponent<Card>();
                            if (cardComponent != null)
                            {
                                cardComponent.attachedEnergy = null;
                                cardComponent.bloomChild = null;
                            }
                            // Send card to zone
                            AddOrMoveCardToGameZone(GetZone("Arquive", SrvMessageCounter_DuelAction.playerID == PlayerInfo.PlayerID ? TargetPlayer.Player : TargetPlayer.Oponnent), cardsToBeMoved:  new List<GameObject>{ childObject });
                        }

                        if (DuelActionTypeOfAction.Equals("DefeatedHoloMember"))
                        {

                            if (_MatchConnection._DuelFieldData.currentPlayerTurn.Equals(PlayerInfo.PlayerID))
                            {
                                EndTurnButton.SetActive(false);
                                cardlist.Clear();
                            }

                            _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.HolomemDefeated;

                            if ((!_MatchConnection._DuelFieldData.currentPlayerTurn.Equals(PlayerInfo.PlayerID)))
                                _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "CheerRequestHolomemDown", "", "");
                        }
                        currentGameHigh++;
                        break;
                    case "HolomemDefatedSoGainCheer":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.HolomemDefeated)
                        {
                            throw new Exception("not in the right gamephase, we're at " + DuelActionTypeOfAction + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        if (SrvMessageCounter_DuelAction.cardList[0].cardNumber == "Empty")
                        {
                            playerCannotDrawFromCheer = true;
                            if (_MatchConnection._DuelFieldData.currentGamePhase == GAMEPHASE.HolomemDefeatedEnergyChoose)
                                _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "CheerChooseRequestHolomemDown", "", "");

                            if (_MatchConnection._DuelFieldData.currentGamePhase == GAMEPHASE.CheerStepChoose)
                                _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "CheerChooseRequest", "", "");
                        }
                        {
                            DrawCard(SrvMessageCounter_DuelAction);
                            cheersAssignedThisChainAmount = 0;
                            cheersAssignedThisChainTotal = SrvMessageCounter_DuelAction.cardList.Count;
                        }

                        if (SrvMessageCounter_DuelAction.playerID == PlayerInfo.PlayerID)
                            LockGameFlow = true;

                        _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.HolomemDefeatedEnergyChoose;
                        currentGameHigh++;

                        break;
                    case "CheerStepEndDefeatedHolomem":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.HolomemDefeatedEnergyChoose)
                        {
                            throw new Exception("not in the right gamephase, we're at " + DuelActionTypeOfAction + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        //validation to check if the player still have energy to assign due to Buzzholomem, for exemple
                        if (cheersAssignedThisChainAmount < cheersAssignedThisChainTotal - 1)
                        {
                            _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.HolomemDefeatedEnergyChoose;
                            cheersAssignedThisChainAmount++;
                            LockGameFlow = true;
                        }
                        else
                        {
                            cheersAssignedThisChainAmount = 0;
                            cheersAssignedThisChainTotal = 0;
                            _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.MainStep;
                        }


                        currentGameHigh++;

                        target = (SrvMessageCounter_DuelAction.playerID.Equals(PlayerInfo.PlayerID)) ? TargetPlayer.Player : TargetPlayer.Oponnent;

                        // if player still have cheer, we attach, else, we skip
                        if (!playerCannotDrawFromCheer)
                            AttachCardToTarget(SrvMessageCounter_DuelAction, target);

                        //if the player who is not the player is here, we return, he one assigning energy since his holomem died, we do not need to assign again
                        if (!_MatchConnection._DuelFieldData.currentPlayerTurn.Equals(PlayerInfo.PlayerID))
                            break;

                        if (cheersAssignedThisChainAmount > cheersAssignedThisChainTotal - 1)
                        {
                            EndTurnButton.SetActive(true);
                            _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "MainStartRequest", "CalledAt:CheerStepEndDefeatedHolomem", "");
                        }
                        break;
                    case "CheerStep":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.CheerStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + DuelActionTypeOfAction + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        if (SrvMessageCounter_DuelAction.cardList[0].cardNumber == "Empty")
                        {
                            playerCannotDrawFromCheer = true;
                            if (_MatchConnection._DuelFieldData.currentGamePhase == GAMEPHASE.HolomemDefeatedEnergyChoose)
                                _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "CheerChooseRequestHolomemDown", "", "");

                            if (_MatchConnection._DuelFieldData.currentGamePhase == GAMEPHASE.CheerStepChoose)
                                if (_MatchConnection._DuelFieldData.currentPlayerTurn.Equals(PlayerInfo.PlayerID))
                                    _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "CheerChooseRequest", "", "");
                        }
                        {
                            DrawCard(SrvMessageCounter_DuelAction);
                        }
                        if (SrvMessageCounter_DuelAction.playerID == PlayerInfo.PlayerID)
                            LockGameFlow = true;

                        _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.CheerStepChoose;
                        currentGameHigh++;
                        GamePhaseMsg.gameObject.SetActive(true);
                        GamePhaseMsg.StartMessage("Cheer Step");
                        break;
                    case "CheerStepEnd":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.CheerStepChoose)
                        {
                            throw new Exception("not in the right gamephase, we're at " + DuelActionTypeOfAction + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        target = (_MatchConnection._DuelFieldData.currentPlayerTurn == PlayerInfo.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent;
                        AttachCardToTarget(SrvMessageCounter_DuelAction, target);

                        currentGameHigh++;
                        _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.MainStep;

                        if (_MatchConnection._DuelFieldData.currentPlayerTurn.Equals(PlayerInfo.PlayerID))
                            _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "MainStartRequest", "CalledAt:CheerStepEnd", "");

                        startofmain = true;
                        break;
                    case "MainPhase":
                        if (startofmain)
                        {
                            GamePhaseMsg.gameObject.SetActive(true);
                            GamePhaseMsg.StartMessage("Main Step");
                        }

                        startofmain = false;


                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.MainStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + DuelActionTypeOfAction + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        if (_MatchConnection._DuelFieldData.currentPlayerTurn.Equals(PlayerInfo.PlayerID))
                            EndTurnButton.SetActive(true);

                        _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.MainStep;
                        currentGameHigh++;
                        break;
                    case "MainPhaseDoAction":
                        currentGameHigh++;
                        break;
                    case "Endturn":
                        startofmain = false;


                        _MatchConnection._DuelFieldData.currentPlayerTurn = _MatchConnection._DuelFieldData.currentPlayerTurn == _MatchConnection._DuelFieldData.firstPlayer ? _MatchConnection._DuelFieldData.secondPlayer : _MatchConnection._DuelFieldData.firstPlayer;

                        centerStageArtUsed = false;
                        collabStageArtUsed = false;
                        usedOshiSkill = false;

                        currentTurn++;

                        //by default set next gamephase to reset
                        _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.ResetStep;


                        _MatchConnection._DuelFieldData.playerLimiteCardPlayed.Clear();

                        currentGameHigh++;

                        //we changed the current player, so, the next player is the oponnent now, the calls the server
                        if (_MatchConnection._DuelFieldData.currentPlayerTurn.Equals(PlayerInfo.PlayerID))
                            _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "ResetRequest", "CalledAt:Endturn", "");

                        GamePhaseMsg.gameObject.SetActive(true);
                        GamePhaseMsg.StartMessage("End Step");
                        break;
                    case "Endduel":

                        if (PlayerInfo.PlayerID.Equals(SrvMessageCounter_DuelAction.playerID))
                            VictoryPanel.SetActive(true);
                        else
                            LosePanel.SetActive(true);

                        currentGameHigh = 999999999;
                        break;
                    case "AttachSupportItem":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.MainStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + DuelActionTypeOfAction + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        target = (_MatchConnection._DuelFieldData.currentPlayerTurn == PlayerInfo.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent;
                        AttachCardToTarget(SrvMessageCounter_DuelAction, target);

                        currentGameHigh++;
                        break;
                    case "PlayHolomem":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.MainStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + DuelActionTypeOfAction + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        string currentPlayer = _MatchConnection._DuelFieldData.currentPlayerTurn;
                        target = (_MatchConnection._DuelFieldData.currentPlayerTurn == PlayerInfo.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent;

                        
                        AddOrMoveCardToGameZone<CardData>(GetZone(SrvMessageCounter_DuelAction.local, target), cardsToBeCretated: new List<CardData>() { SrvMessageCounter_DuelAction.usedCard });

                        if (!currentPlayer.Equals(PlayerInfo.PlayerID))
                        {
                            RemoveCardsFromCardHolder(1, cardsOponnent, cardHolderOponnent);
                        }

                        currentGameHigh++;
                        break;
                    case "BloomHolomem":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.MainStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + DuelActionTypeOfAction + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        target = SrvMessageCounter_DuelAction.playerID.Equals(PlayerInfo.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent;

                        GameObject cardZone = GetZone(SrvMessageCounter_DuelAction.local, target);

                        GameObject usedCardGameObject = Instantiate(cardPrefab, Vector3.zero, Quaternion.identity);
                        Card usedCardGameObjectCard = usedCardGameObject.GetComponent<Card>().SetCardNumber(SrvMessageCounter_DuelAction.usedCard.cardNumber).GetCardInfo();
                        usedCardGameObjectCard.cardPosition = SrvMessageCounter_DuelAction.targetCard.cardPosition;

                        GameObject FatherZoneActiveCard = cardZone.transform.GetChild(cardZone.transform.childCount - 1).gameObject;

                        usedCardGameObject.transform.SetParent(cardZone.transform, false);

                        usedCardGameObject.transform.SetSiblingIndex(cardZone.transform.childCount - 1);

                        usedCardGameObject.transform.localPosition = Vector3.zero;
                        usedCardGameObject.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

                        FatherZoneActiveCard.SetActive(false);

                        usedCardGameObjectCard.bloomChild.Add(FatherZoneActiveCard);
                        usedCardGameObjectCard.attachedEnergy = FatherZoneActiveCard.GetComponent<Card>().attachedEnergy;
                        FatherZoneActiveCard.GetComponent<Card>().attachedEnergy = null;

                        usedCardGameObjectCard.playedFrom = "hand";
                        usedCardGameObjectCard.playedThisTurn = true;
                        UpdateHP(usedCardGameObjectCard);

                        if (!_MatchConnection._DuelFieldData.currentPlayerTurn.Equals(PlayerInfo.PlayerID))
                        {
                            RemoveCardsFromCardHolder(1, cardsOponnent, cardHolderOponnent);
                        }
                        else
                        {
                            _EffectController.ResolveOnBloomEffect(SrvMessageCounter_DuelAction);
                        }

                        currentGameHigh++;
                        break;
                    case "DoCollab":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.MainStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + DuelActionTypeOfAction + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }
                        target = (_MatchConnection._DuelFieldData.currentPlayerTurn == PlayerInfo.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent;

                        zone = GetZone(SrvMessageCounter_DuelAction.local, target);

                        AddOrMoveCardToGameZone(GetZone("HoloPower", target), cardsToBeMoved: new List<GameObject> { GetZone("Deck", target).transform.GetChild(0).gameObject });
                        AddOrMoveCardToGameZone(zone, cardsToBeMoved: GetZone(SrvMessageCounter_DuelAction.playedFrom, target).GetComponentsInChildren<GameObject>().ToList<GameObject>());

                        zone.GetComponentInChildren<Card>().cardPosition = SrvMessageCounter_DuelAction.local;

                        if (_MatchConnection._DuelFieldData.currentPlayerTurn == PlayerInfo.PlayerID)
                            _EffectController.ResolveOnCollabEffect(SrvMessageCounter_DuelAction);

                        currentGameHigh++;
                        break;
                    case "UnDoCollab":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.MainStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + DuelActionTypeOfAction + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        if (!string.IsNullOrEmpty(SrvMessageCounter_DuelAction.usedCard.cardNumber))
                        {
                            zone = null;
                            if (PlayerInfo.PlayerID != SrvMessageCounter_DuelAction.playerID)
                            {
                                zone = GetZone(SrvMessageCounter_DuelAction.usedCard.cardPosition, TargetPlayer.Oponnent);
                                AddOrMoveCardToGameZone(zone, cardsToBeMoved: GetZone(SrvMessageCounter_DuelAction.playedFrom, TargetPlayer.Oponnent).GetComponentsInChildren<GameObject>().ToList<GameObject>());
                            }
                            else
                            {
                                zone = GetZone(SrvMessageCounter_DuelAction.usedCard.cardPosition, TargetPlayer.Player);
                                AddOrMoveCardToGameZone(zone, cardsToBeMoved: GetZone(SrvMessageCounter_DuelAction.playedFrom, TargetPlayer.Player).GetComponentsInChildren<GameObject>().ToList<GameObject>());
                            }
                            Card unRestCard = zone.GetComponentInChildren<Card>();
                            unRestCard.cardPosition = SrvMessageCounter_DuelAction.usedCard.cardPosition;
                            unRestCard.suspended = true;
                            zone.transform.GetChild(zone.transform.childCount - 1).Rotate(0, 0, 90);
                        }

                        currentGameHigh++;
                        break;
                    case "RemoveEnergyFrom":
                        RemoveCardFromPosition(SrvMessageCounter_DuelAction);
                        currentGameHigh++;
                        break;
                    case "AttachEnergyResponse":
                        target = (_MatchConnection._DuelFieldData.currentPlayerTurn == PlayerInfo.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent;

                        // if player still have cheer, we attach, else, we skip
                        if (!playerCannotDrawFromCheer)
                            AttachCardToTarget(SrvMessageCounter_DuelAction, target);

                        _EffectController.isServerResponseArrive = true;
                        currentGameHigh++;
                        break;
                    case "PayHoloPowerCost":
                        zone = GetZone("HoloPower", (SrvMessageCounter_DuelAction.playerID.Equals(PlayerInfo.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent));
                        for (int n = 0; n < SrvMessageCounter_DuelAction.cardList.Count; n++)
                        {
                            foreach (Transform obj in zone.transform.GetComponentsInChildren<Transform>())
                            {
                                if (obj.name.Equals("Card(Clone)"))
                                {

                                    Card _card = obj.GetComponent<Card>();
                                    if (_card == null)
                                        _card = obj.AddComponent<Card>();
                                    _card.cardNumber = SrvMessageCounter_DuelAction.cardList[n].cardNumber;
                                    _card.GetCardInfo();
                                    AddOrMoveCardToGameZone(GetZone("Arquive", SrvMessageCounter_DuelAction.playerID.Equals(PlayerInfo.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent), cardsToBeMoved: new List<GameObject> { obj.gameObject });
                                    break;
                                }
                            }
                        }
                        currentGameHigh++;
                        break;
                    case "MoveCardToZone":
                        target = SrvMessageCounter_DuelAction.playerID.Equals(PlayerInfo.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent;
                        GameObject OrigemZone = GetZone(SrvMessageCounter_DuelAction.usedCard.playedFrom, target);
                        GameObject targetObj = OrigemZone.transform.GetChild(OrigemZone.transform.childCount -1).gameObject;

                        if (target.Equals(TargetPlayer.Player))
                            if (!string.IsNullOrEmpty(SrvMessageCounter_DuelAction.usedCard.cardNumber)) 
                                foreach (Card _card in OrigemZone.GetComponentsInChildren<Card>()) 
                                    if (_card.cardNumber.Equals(SrvMessageCounter_DuelAction.usedCard.cardNumber)) 
                                        targetObj = _card.gameObject;
                                    
                        if (SrvMessageCounter_DuelAction.usedCard.cardPosition.Equals("hand")) {
                            var handcardlist = (target.Equals(TargetPlayer.Player)) ? cardsPlayer : cardsOponnent;
                            handcardlist.Add(targetObj.GetComponent<RectTransform>());
                        }

                        AddOrMoveCardToGameZone(GetZone(SrvMessageCounter_DuelAction.usedCard.cardPosition, target), cardsToBeMoved: new List<GameObject> { targetObj });
                        currentGameHigh++;
                        break;
                    case "DisposeUsedSupport":
                        target = SrvMessageCounter_DuelAction.playerID.Equals(PlayerInfo.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent;
                        AddOrMoveCardToGameZone(GetZone("Arquive", target), new List<Card>() { new Card("") { cardNumber = SrvMessageCounter_DuelAction.usedCard.cardNumber } });

                        if (!SrvMessageCounter_DuelAction.playerID.Equals(PlayerInfo.PlayerID))
                            RemoveCardsFromCardHolder(1, SrvMessageCounter_DuelAction.playerID.Equals(PlayerInfo.PlayerID) ? cardsPlayer : cardsOponnent, SrvMessageCounter_DuelAction.playerID.Equals(PlayerInfo.PlayerID) ? cardHolderPlayer : cardHolderOponnent);
                        currentGameHigh++;
                        break;
                    case "ResolveOnSupportEffect":
                    case "OnCollabEffect":
                    case "OnArtEffect":
                    case "ResolveOnAttachEffect":
                        if (SrvMessageCounter_DuelAction.playerID == PlayerInfo.PlayerID)
                            LockGameFlow = true;

                        _EffectController.isServerResponseArrive = true;
                        currentGameHigh++;
                        break;
                    case "ActiveArtEffect":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.MainStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + DuelActionTypeOfAction + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }
                        currentGameHigh++;
                        if (SrvMessageCounter_DuelAction.playerID == PlayerInfo.PlayerID)
                        {
                            LockGameFlow = true;
                            _EffectController.ResolveOnArtEffect(SrvMessageCounter_DuelAction);
                        }
                        break;
                    case "PickFromListThenGiveBacKFromHandDone":
                         if (SrvMessageCounter_DuelAction.playerID != PlayerInfo.PlayerID)
                        {
                            //we remove only one card from the pllayer hand, bacause we're using draw so the hands match
                            RemoveCardsFromCardHolder(1, cardsOponnent, cardHolderOponnent);
                            // add card to holopower since we are using draw which removes a card from the zone
                            AddOrMoveCardToGameZone(GetZone("HoloPower", TargetPlayer.Oponnent), new List<Card>() { new Card("") });
                            //just making the card empty so the player dont see in the oponent hand holder, we can check in the log
                            SrvMessageCounter_DuelAction.cardList[0].cardNumber = "";
                            DrawCard(SrvMessageCounter_DuelAction);
                        }
                        else
                        {
                            //removing from the player hand the picked card to add with the draw the one from the holopower
                            int n = -1;

                            for (int contadorCardHand = 0; contadorCardHand < cardsPlayer.Count; contadorCardHand++)
                            {
                                Card cardInHand = cardsPlayer[contadorCardHand].GetComponent<Card>();
                                if (cardInHand.cardNumber.Equals(SrvMessageCounter_DuelAction.targetCard.cardNumber))
                                {
                                    n = contadorCardHand;
                                }
                            }

                            if (n == -1)
                            {
                                Debug.Log("Used card do not exist in the player hand");
                            }
                            else
                            {
                                cardsPlayer.RemoveAt(n);
                                DrawCard(SrvMessageCounter_DuelAction);
                            }
                            // add card to holopower since we are using draw which removes a card from the zone
                            AddOrMoveCardToGameZone(GetZone("HoloPower", TargetPlayer.Player), new List<Card>() { new Card("") });
                        }


                        currentGameHigh++;
                        break;
                    case "RemoveCardsFromArquive":
                        target = SrvMessageCounter_DuelAction.playerID == PlayerInfo.PlayerID ? TargetPlayer.Player : TargetPlayer.Oponnent;
                        List<Card> canSelect = GetZone("Arquive", target).GetComponentsInChildren<Card>().ToList();
                        for (int i = 0; i < SrvMessageCounter_DuelAction.cardList.Count; i++)
                        {
                            bool match = false;
                            int j = 0;
                            for (; j < canSelect.Count; j++)
                            {
                                if (SrvMessageCounter_DuelAction.cardList[i].cardNumber.Equals(canSelect[j].cardNumber))
                                {
                                    match = true;
                                    break;
                                }
                            }
                            if (match)
                            {
                                cardsPlayer.RemoveAt(j);
                                Destroy(canSelect[j].gameObject);
                                continue;
                            }
                        }
                        currentGameHigh++;
                        break;
                    case "RemoveCardsFromHand":
                        if (SrvMessageCounter_DuelAction.playerID == PlayerInfo.PlayerID)
                        {
                            canSelect = cardHolderPlayer.GetComponentsInChildren<Card>().ToList();

                            for (int i = 0; i < SrvMessageCounter_DuelAction.cardList.Count; i++)
                            {
                                bool match = false;
                                int j = 0;
                                for (; j < canSelect.Count; j++)
                                {
                                    if (SrvMessageCounter_DuelAction.cardList[i].cardNumber.Equals(canSelect[j].cardNumber))
                                    {
                                        match = true;
                                        break;
                                    }
                                }
                                if (match)
                                {
                                    cardsPlayer.RemoveAt(j);
                                    Destroy(canSelect[j].gameObject);
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            RemoveCardsFromCardHolder(SrvMessageCounter_DuelAction.cardList.Count, cardsOponnent, cardHolderOponnent);
                        }
                        currentGameHigh++;
                        break;
                    case "DrawOshiEffect":
                        if (SrvMessageCounter_DuelAction.playerID == PlayerInfo.PlayerID)
                            AddCardToCardHolder(SrvMessageCounter_DuelAction.cardList.Count, cardsPlayer, cardHolderPlayer, SrvMessageCounter_DuelAction.cardList);
                        else if (SrvMessageCounter_DuelAction.playerID != PlayerInfo.PlayerID)
                            AddCardToCardHolder(SrvMessageCounter_DuelAction.cardList.Count, cardsOponnent, cardHolderOponnent, SrvMessageCounter_DuelAction.cardList);
                        currentGameHigh++;
                        break;
                    case "DrawBloomEffect":
                    case "DrawBloomIncreaseEffect":
                    case "DrawCollabEffect":
                    case "DrawArtEffect":
                    case "SupportEffectDraw":
                    case "DrawAttachEffect":
                        //if the player recieved a card to draw
                        if (SrvMessageCounter_DuelAction.cardList.Count > 0)
                        {
                            //since this suffleAll, we destroy the player hand, then add the used card to the other player arquive
                            if (SrvMessageCounter_DuelAction.playerID.Equals(PlayerInfo.PlayerID))
                            {
                                //WE ARE ADDING CARDS HERE, BECAUSE WE ARE DRAWING IF DRAWCARD() TO THE COUNT MATCH
                                AddCardsToDeck(GetZone("Deck", TargetPlayer.Player), cardsPlayer.Count, SrvMessageCounter_DuelAction.suffle);
                                if (SrvMessageCounter_DuelAction.suffleBackToDeck)
                                {
                                    foreach (RectTransform gmd in cardsPlayer)
                                    {
                                        Destroy(gmd.gameObject);
                                    }
                                    cardsPlayer.Clear();
                                }
                            }
                            else
                            {
                                //WE ARE ADDING CARDS HERE, BECAUSE WE ARE DRAWING IF DRAWCARD() TO THE COUNT MATCH
                                AddCardsToDeck(GetZone("Deck", TargetPlayer.Oponnent), cardsOponnent.Count, SrvMessageCounter_DuelAction.suffle);
                                if (SrvMessageCounter_DuelAction.suffleBackToDeck)
                                {
                                    foreach (RectTransform gmd in cardsOponnent)
                                    {
                                        Destroy(gmd.gameObject);
                                    }
                                    cardsOponnent.Clear();
                                }
                            }
                        }

                        DrawCard(SrvMessageCounter_DuelAction);
                        currentGameHigh++;
                        break;
                    case "ShowCard":
                        List<int> ShowDuelAction = JsonConvert.DeserializeObject<List<int>>(SrvMessageCounter_DuelAction.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        _EffectController.EffectInformation.AddRange(ShowDuelAction);
                        _EffectController.isServerResponseArrive = true;
                        currentGameHigh++;
                        break;
                    case "RollDice":
                        List<int> Dices = JsonConvert.DeserializeObject<List<int>>(SrvMessageCounter_DuelAction.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        _EffectController.EffectInformation.AddRange(Dices);
                        _EffectController.isServerResponseArrive = true;
                        currentGameHigh++;
                        break;
                    case "OnlyDiceRoll":
                        Dices = JsonConvert.DeserializeObject<List<int>>(SrvMessageCounter_DuelAction.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        _EffectController.EffectInformation.AddRange(Dices);
                        currentGameHigh++;
                        break;
                    case "RecoverHolomem":
                        zone = GetZone(SrvMessageCounter_DuelAction.targetCard.cardPosition, (SrvMessageCounter_DuelAction.playerID.Equals(PlayerInfo.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent));
                        Card targetedCard = zone.GetComponentInChildren<Card>();
                        targetedCard.currentHp = Math.Min(targetedCard.currentHp + int.Parse(SrvMessageCounter_DuelAction.actionObject), int.Parse(targetedCard.hp));
                        UpdateHP(targetedCard);

                        _EffectController.ResolveOnRecoveryEffect(targetedCard);
                        currentGameHigh++;
                        break;
                    case "InflicArtDamageToHolomem":
                    case "InflicDamageToHolomem":
                    case "InflicRecoilDamageToHolomem":
                        _EffectController.ResolveOnDamageResolveEffect(SrvMessageCounter_DuelAction);

                        currentGameHigh++;
                        break;
                    case "SetHPToFixedValue":
                    case "ResolveDamageToHolomem":
                        zoneArt = GetZone(SrvMessageCounter_DuelAction.targetCard.cardPosition, !SrvMessageCounter_DuelAction.playerID.Equals(PlayerInfo.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent);

                        Card card = zoneArt.GetComponentInChildren<Card>();

                        if (DuelActionTypeOfAction.Equals("SetHPToFixedValue"))
                            card.currentHp = int.Parse(SrvMessageCounter_DuelAction.actionObject);

                        card.currentHp -= int.Parse(SrvMessageCounter_DuelAction.actionObject);

                        UpdateHP(card);

                        if (DuelActionTypeOfAction.Equals("UsedArt"))
                        {
                            if (SrvMessageCounter_DuelAction.usedCard.cardPosition.Equals("Stage"))
                            {
                                centerStageArtUsed = true;
                            }
                            else if (SrvMessageCounter_DuelAction.usedCard.cardPosition.Equals("Collaboration"))
                            {
                                collabStageArtUsed = true;
                            }

                            if (SrvMessageCounter_DuelAction.playerID.Equals(PlayerInfo.PlayerID))
                                if (centerStageArtUsed && collabStageArtUsed)
                                    GenericActionCallBack(null, "Endturn");
                        }
                        currentGameHigh++;

                        break;
                    case "SwitchStageCard":
                    case "SwitchStageCardByRetreat":

                        //if is a retreat using the skill
                        if (DuelActionTypeOfAction.Equals("SwitchStageCardByRetreat"))
                        {
                            centerStageArtUsed = true;
                        }

                        target = SrvMessageCounter_DuelAction.playerID.Equals(PlayerInfo.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent;
                        zoneArt = GetZone(SrvMessageCounter_DuelAction.targetCard.cardPosition, target);
                        GameObject stageZone = GetZone("Stage", target);

                        // Move cards to the temporary holder
                        AddOrMoveCardToGameZone(GameObject.Find("HUD DuelField"), cardsToBeMoved: zoneArt.transform.GetComponentsInChildren<GameObject>().ToList<GameObject>());

                        // Move cards from stage to backstage
                        AddOrMoveCardToGameZone(zoneArt, cardsToBeMoved: stageZone.transform.GetComponentsInChildren<GameObject>().ToList<GameObject>());

                        // Move cards from the temporary holder to the center stage
                        AddOrMoveCardToGameZone(stageZone, cardsToBeMoved: GameObject.Find("HUD DuelField").GetComponentsInChildren<GameObject>().ToList<GameObject>());

                        // Correct the positions for the cards
                        zoneArt.GetComponentInChildren<Card>().cardPosition = SrvMessageCounter_DuelAction.targetCard.cardPosition;
                        stageZone.GetComponentInChildren<Card>().cardPosition = "Stage";


                        _EffectController.isServerResponseArrive = true;

                        currentGameHigh++;
                        break;
                    case "SwitchOpponentStageCard":

                        target = SrvMessageCounter_DuelAction.playerID.Equals(PlayerInfo.PlayerID) ? TargetPlayer.Oponnent : TargetPlayer.Player;
                        zoneArt = GetZone(SrvMessageCounter_DuelAction.targetCard.cardPosition, target);
                        stageZone = GetZone("Stage", target);

                        // Move cards to the temporary holder
                        AddOrMoveCardToGameZone(GameObject.Find("HUD DuelField"), cardsToBeMoved: zoneArt.GetComponentsInChildren<GameObject>().ToList<GameObject>());

                        // Move cards from stage to backstage
                        AddOrMoveCardToGameZone(zoneArt, cardsToBeMoved: stageZone.GetComponentsInChildren<GameObject>().ToList<GameObject>());

                        // Move cards from the temporary holder to the center stage
                        AddOrMoveCardToGameZone(stageZone, cardsToBeMoved: stageZone.GetComponentsInChildren<GameObject>().ToList<GameObject>());

                        // Correct the positions for the cards
                        zoneArt.GetComponentInChildren<Card>().cardPosition = SrvMessageCounter_DuelAction.targetCard.cardPosition;
                        stageZone.GetComponentInChildren<Card>().cardPosition = "Stage";

                        currentGameHigh++;
                        break;
                    case "RemoveEnergyAtAndDestroy":
                    case "RemoveEnergyAtAndSendToArquive":
                        target = PlayerInfo.PlayerID == SrvMessageCounter_DuelAction.playerID ? TargetPlayer.Player : TargetPlayer.Oponnent;

                        GameObject TargetZone = GetZone(SrvMessageCounter_DuelAction.usedCard.cardPosition, target);
                        Card targetCard = TargetZone.transform.GetChild(TargetZone.transform.childCount - 1).GetComponent<Card>();
                        int nn = 0;
                        int jj = 0;

                        foreach (GameObject attachEnergy in targetCard.attachedEnergy)
                        {
                            Card energyInfo = attachEnergy.GetComponent<Card>();
                            if (energyInfo.cardNumber.Equals(SrvMessageCounter_DuelAction.usedCard.cardNumber))
                            {
                                nn = jj;
                                break;
                            }
                            jj++;
                        }
                        if (DuelActionTypeOfAction.Equals("RemoveEnergyAtAndSendToArquive"))
                        {
                            AddOrMoveCardToGameZone(GetZone("Arquive", target), cardsToBeMoved: new List<GameObject> { targetCard.attachedEnergy[nn] });
                            targetCard.attachedEnergy[nn].gameObject.SetActive(true);
                        }
                        else
                        {
                            Destroy(targetCard.attachedEnergy[nn]);
                        }
                        targetCard.attachedEnergy.RemoveAt(nn);

                        currentGameHigh++;
                        break;
                    case "RemoveEquipAtAndSendToArquive":
                        target = PlayerInfo.PlayerID == SrvMessageCounter_DuelAction.playerID ? TargetPlayer.Player : TargetPlayer.Oponnent;

                        TargetZone = GetZone(SrvMessageCounter_DuelAction.usedCard.cardPosition, target);
                        targetCard = TargetZone.transform.GetChild(TargetZone.transform.childCount - 1).GetComponent<Card>();
                        nn = 0;
                        jj = 0;

                        foreach (GameObject attachEnergy in targetCard.attachedEquipe)
                        {
                            Card energyInfo = attachEnergy.GetComponent<Card>();
                            if (energyInfo.cardNumber.Equals(SrvMessageCounter_DuelAction.usedCard.cardNumber))
                            {
                                nn = jj;
                                break;
                            }
                            jj++;
                        }
                        AddOrMoveCardToGameZone(GetZone("Arquive", target), cardsToBeMoved: new List<GameObject> { targetCard.attachedEquipe[nn] });
                        targetCard.attachedEquipe[nn].gameObject.SetActive(true);

                        targetCard.attachedEquipe.RemoveAt(nn);

                        currentGameHigh++;
                        break;
                    case "SuffleDeck":
                        StartCoroutine(ShuffleCardsCoroutine(GetChildrenWithName(GetZone("Deck", SrvMessageCounter_DuelAction.playerID.Equals(PlayerInfo.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent), "Card(Clone)"), 0.5f, 50f));
                        break;
                    default:
                        Debug.Log("Action not found: " + DuelActionTypeOfAction);
                        ConsoleClearer.ClearConsole();
                        break;
                }

                if (_MatchConnection.DuelActionListIndex.Count == currentGameHigh)
                    _DuelField_LogManager.AddLog(SrvMessageCounter_DuelAction, DuelActionTypeOfAction);
            }

            GetUsableCards();
            ArrangeCards(cardsPlayer, cardHolderPlayer);
            ArrangeCards(cardsOponnent, cardHolderOponnent);
        }

        if (playerMulliganF && oponnentMulliganF && !ReadyButtonShowed)
        {
            ReadyButton.SetActive(true);
            ReadyButtonShowed = true;
        }
    }
    ////////////////////////////////////////////////////////////////////////
    public void GenericActionCallBack(DuelAction _DuelAction, string type)
    {
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore, // Ignore null values
            DefaultValueHandling = DefaultValueHandling.Ignore, // Ignore default values (e.g., false for booleans)
            Formatting = Formatting.Indented // Optional: to format the JSON nicely
        };

        string jsonString;
        switch (type)
        {
            case "Endturn":
                _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "MainEndturnRequest", "Endturn");
                _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.EndStep;
                EndTurnButton.SetActive(false);
                break;
            case "CheerChooseRequest":
                jsonString = JsonConvert.SerializeObject(_DuelAction);
                if (_MatchConnection._DuelFieldData.currentGamePhase == GAMEPHASE.HolomemDefeatedEnergyChoose)
                    _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "CheerChooseRequestHolomemDown", "", jsonString);
                if (_MatchConnection._DuelFieldData.currentGamePhase == GAMEPHASE.CheerStepChoose)
                    _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "CheerChooseRequest", "", jsonString);
                LockGameFlow = false;
                break;
            case "standart":
                _DuelAction.playerID = PlayerInfo.PlayerID;
                jsonString = JsonConvert.SerializeObject(_DuelAction);
                _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "MainDoActionRequest", "", jsonString);
                LockGameFlow = false;
                break;
            default:
                _DuelAction.playerID = PlayerInfo.PlayerID;
                jsonString = JsonConvert.SerializeObject(_DuelAction, settings);
                _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, type, "", jsonString);
                LockGameFlow = false;
                break;
        }
    }

    //DuelField_SelectableCardMenu CALL THIS WHEN WE HAVE SELECTED THE CARD WE WANT TO SUMMOM 
    public void AttachEnergyCallBack(string energyNumber)
    {

        LockGameFlow = false;

        string jsonString = JsonConvert.SerializeObject(energyNumber);
        _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "AskAttachEnergy", "", jsonString);
    }

    //DuelField_SelectableCardMenu CALL THIS WHEN WE HAVE SELECTED THE CARD WE WANT TO SUMMOM 
    public void SuporteEffectSummomIfCallBack(List<string> cards)
    {

        LockGameFlow = false;

        string jsonString = JsonConvert.SerializeObject(cards[0]);
        _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "MainConditionedSummomResponse", "", jsonString);
    }

    //DuelField_SelectableCardMenu CALL THIS WHEN WE HAVE SELECTED THE CARD WE WANT TO ADD TO OUR HAND 
    public void SuporteEffectDrawXAddIfCallBack(List<string> cards, List<int> positions)
    {
        DuelAction _ConditionedDraw = new()
        {
            SelectedCards = cards,
            Order = positions
        };

        LockGameFlow = false;

        string jsonString = JsonConvert.SerializeObject(_ConditionedDraw);
        _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "MainConditionedDrawResponse", "", jsonString);
    }
    //FUNCTIONS ASSIGNED IN THE INSPECTOR

    public void ReturnButton()
    {
        Destroy(GameObject.Find("HUD DuelField"));
        Destroy(GameSettings.GetComponent<MatchConnection>());
        Destroy(this);
        _ = _MatchConnection._webSocket.Close();
        SceneManager.LoadScene("Match");
    }

    public void DuelReadyButton()
    {
        if (GameZones[6].GetComponentInChildren<Card>() == null)
        {
            return;
        }
        ReadyButton.SetActive(false);
        string jsonString = JsonConvert.SerializeObject(DuelFieldData.ConvertToSerializable(MapDuelFieldData(GameZones)));
        _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "DuelFieldReady", "0", jsonString);
        LockGameFlow = false;
        GetUsableCards(true);
    }

    public void EndTurnHUDButton()
    {
        GenericActionCallBack(null, "Endturn");
    }

    void MulliganBox()
    {
        MulliganMenu.SetActive(true);
    }

    public void MulliganBoxYesButton()
    {
        MulliganMenu.SetActive(false);
        _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "AskForMulligan", "", "t");
        LockGameFlow = false;
    }

    public void MulliganBoxNoButton()
    {
        MulliganMenu.SetActive(false);
        _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "AskForMulligan", "", "f");
        LockGameFlow = false;
    }


    //FUNCTIONS ASSIGNED IN THE INSPECTOR - END

    public void GetUsableCards(bool clearList = false)
    {
        foreach (RectTransform r in cardsPlayer)
        {
            Card cardComponent = r.GetComponent<Card>();
            DuelField_HandDragDrop handDragDrop = r.GetComponent<DuelField_HandDragDrop>() ?? r.gameObject.AddComponent<DuelField_HandDragDrop>();
            handDragDrop.enabled = false;
        }

        if (!_MatchConnection._DuelFieldData.currentPlayerTurn.Equals(PlayerInfo.PlayerID) 
            && !(_MatchConnection._DuelFieldData.currentGamePhase == GAMEPHASE.SettingUpBoard 
            || _MatchConnection._DuelFieldData.currentGamePhase == GAMEPHASE.HolomemDefeatedEnergyChoose) )
            clearList = true;

        if (!clearList)
        {
            switch (_MatchConnection._DuelFieldData.currentGamePhase)
            {
                case GAMEPHASE.HolomemDefeatedEnergyChoose:
                case GAMEPHASE.CheerStepChoose:
                    foreach (RectTransform r in cardsPlayer)
                    {
                        Card cardComponent = r.GetComponent<Card>();
                        if (cardComponent.cardType.Equals("エール"))
                        {
                            DuelField_HandDragDrop handDragDrop = r.GetComponent<DuelField_HandDragDrop>() ?? r.gameObject.AddComponent<DuelField_HandDragDrop>();
                            handDragDrop.enabled = true;
                        }
                    }
                    break;
                case GAMEPHASE.MainStep:
                    foreach (RectTransform r in cardsPlayer)
                    {
                        Card cardComponent = r.GetComponent<Card>();
                        cardComponent.GetCardInfo();
                        if (cardComponent.cardType.Equals("サポート・スタッフ・LIMITED") || cardComponent.cardType.Equals("サポート・イベント・LIMITED") || cardComponent.cardType.Equals("サポート・アイテム・LIMITED")
                            || cardComponent.cardType.Equals("サポート・スタッフ") || cardComponent.cardType.Equals("サポート・イベント") || cardComponent.cardType.Equals("サポート・アイテム")
                            )
                        {
                            DuelField_HandDragDrop handDragDrop = r.GetComponent<DuelField_HandDragDrop>() ?? r.gameObject.AddComponent<DuelField_HandDragDrop>();
                            if (CheckForPlayRestrictions(cardComponent.cardNumber))
                                handDragDrop.enabled = true;
                            else
                                handDragDrop.enabled = false;

                        }
                        else if (cardComponent.cardType.Equals("ホロメン") || cardComponent.cardType.Equals("Buzzホロメン"))
                        {
                            if (cardComponent.bloomLevel.Equals("Debut") || cardComponent.bloomLevel.Equals("Spot"))
                            {
                                DuelField_HandDragDrop handDragDrop = r.GetComponent<DuelField_HandDragDrop>() ?? r.gameObject.AddComponent<DuelField_HandDragDrop>();
                                handDragDrop.enabled = true;
                            }
                            if (cardComponent.bloomLevel.Equals("1st")
                                || cardComponent.cardNumber.Equals("hBP01-045") //AZKI GIFT
                                )
                            {
                                List<string> bloomableNames;
                                bloomableNames = NamesThatCanBloom("Debut");
                                foreach (string name in bloomableNames)
                                {
                                    if (name.Equals(cardComponent.cardName))
                                    {
                                        DuelField_HandDragDrop handDragDrop = r.GetComponent<DuelField_HandDragDrop>() ?? r.gameObject.AddComponent<DuelField_HandDragDrop>();
                                        handDragDrop.enabled = true;
                                    }
                                }
                            }
                            if (cardComponent.bloomLevel.Equals("2nd")
                                || cardComponent.cardNumber.Equals("hBP01-045") //AZKI GIFT
                                )
                            {
                                List<string> bloomableNames;
                                bloomableNames = NamesThatCanBloom("1st");
                                foreach (string name in bloomableNames)
                                {
                                    if (name.Equals(cardComponent.cardName))
                                    {
                                        DuelField_HandDragDrop handDragDrop = r.GetComponent<DuelField_HandDragDrop>() ?? r.gameObject.AddComponent<DuelField_HandDragDrop>();
                                        handDragDrop.enabled = true;
                                    }
                                }
                            }
                        }
                        else if (cardComponent.cardType.Equals("サポート・ツール") || cardComponent.cardType.Equals("サポート・マスコット") || cardComponent.cardType.Equals("サポート・ファン"))
                        {
                            if (!HasRestrictionsToPlayEquipCheckField(cardComponent)) { 
                               DuelField_HandDragDrop handDragDrop = r.GetComponent<DuelField_HandDragDrop>() ?? r.gameObject.AddComponent<DuelField_HandDragDrop>();
                               handDragDrop.enabled = true;
                            }
                        }

                    }
                    break;
                case GAMEPHASE.SettingUpBoard:
                    foreach (RectTransform r in cardsPlayer)
                    {
                        Card cardComponent = r.GetComponent<Card>();
                        if (cardComponent.bloomLevel.Equals("Debut") || cardComponent.bloomLevel.Equals("Spot"))
                        {
                            DuelField_HandDragDrop handDragDrop = r.GetComponent<DuelField_HandDragDrop>() ?? r.gameObject.AddComponent<DuelField_HandDragDrop>();
                            handDragDrop.enabled = true;
                        }
                    }
                    break;
            }
        }
    }

    public bool CheckForPlayRestrictions(string cardNumber)
    {
        switch (cardNumber)
        {
            case "hSD01-016":
                var deckCount = GetZone("Deck", TargetPlayer.Player).transform.childCount - 1;
                if (deckCount < 3)
                    return false;
                break;
            case "hSD01-021":
            case "hSD01-018":
            case "hBP01-111":
            case "hBP01-113":
                deckCount = GetZone("Deck", TargetPlayer.Player).transform.childCount - 1;
                if (deckCount == 0)
                    return false;
                break;
            case "hBP01-109":
            case "hBP01-102":
                if (cardsPlayer.Count > 6)
                    return false;
                break;
            case "hBP01-105":
            case "hSD01-019":
            case "hBP01-103":
                return EffectController.CheckForDetachableEnergy();
            case "hBP01-106":
                //check if we have back holomems to switch
                int backstagecount = CountBackStageTotal(true);
                return (backstagecount > 0);
            case "hBP01-108":
            case "hBP01-112":
                //check if opponent have back holomems to switch
                backstagecount = CountBackStageTotal(true, TargetPlayer.Oponnent);
                return (backstagecount > 0);
            case "hSD01-020":
            case "hBP01-107":
                var energyList = GetZone("Arquive", TargetPlayer.Player).GetComponentsInChildren<Card>();
                foreach (Card card in energyList)
                {
                    card.GetCardInfo();
                    if (card.cardType.Equals("エール"))
                    {
                        return true;
                    }
                }
                return false;
            case "xxxxxxx":
                break;
        }
        return true;
    }

    public bool HasRestrictionsToPlayEquipCheckField(Card card) {
        foreach (Card target in GameObject.Find("MatchField").transform.Find("Player").GetComponentsInChildren<Card>())
            if (HasRestrictionsToPlayEquip(card, target)
                && (target.cardPosition.Equals("Stage")
                || target.cardPosition.Equals("Collaboration")
                || target.cardPosition.Equals("BackStage1")
                || target.cardPosition.Equals("BackStage2")
                || target.cardPosition.Equals("BackStage3")
                || target.cardPosition.Equals("BackStage4")
                || target.cardPosition.Equals("BackStage5"))
                && target.gameObject.activeInHierarchy)
            {
                return false;
            }
        return true;
    }
    public bool HasRestrictionsToPlayEquip(Card card, Card target)
    {
        switch (card.cardNumber)
        {
            case "hBP01-123":
                if (target.name.Equals("兎田ぺこら"))
                    return false;
                break;
            case "hBP01-122":
                if (target.name.Equals("アキ・ローゼンタール"))
                    return false;
                break;
            case "hBP01-126":
                if (target.name.Equals("尾丸ポルカ"))
                    return false;
                break;
            case "hBP01-125":
                if (target.name.Equals("小鳥遊キアラ"))
                    return false;
                break;
            case "hBP01-124":
                if (target.name.Equals("AZKi") || target.name.Equals("SorAZ"))
                    return false;
                break;
            case "hBP01-114":
            case "hBP01-116":
            case "hBP01-117":
            case "hBP01-118":
            case "hBP01-119":
            case "hBP01-120":
            case "hBP01-115":
            case "hBP01-121":
                foreach (GameObject _Card in card.attachedEquipe)
                {
                    _Card.GetComponent<Card>().cardNumber.Equals(card.cardNumber);
                    return false;
                }
                break;
        }
        return true;
    }


    private bool CheckForOtherCopiesEquipped(string cardNumber)
    {

        List<GameObject> playerAttachments = new();

        playerAttachments.AddRange(GetZone("Stage", TargetPlayer.Player).GetComponentInChildren<Card>().attachedEquipe);
        playerAttachments.AddRange(GetZone("Stage", TargetPlayer.Player).GetComponentInChildren<Card>().attachedEquipe);

        for (int i = 1; i <= 5; i++)
            playerAttachments.AddRange(GetZone($"BackStage{i}", TargetPlayer.Player).GetComponentInChildren<Card>().attachedEquipe);

        foreach (GameObject cardObj in playerAttachments)
        {
            Card card = cardObj.GetComponentInChildren<Card>().GetCardInfo();
            if (card.cardNumber.Equals(cardNumber))
                return true;
        }
        return false;
    }



    public List<string> NamesThatCanBloom(string level)
    {
        List<string> validNames = new();

        Card BackStage1Card = GetZone("BackStage1", TargetPlayer.Player).transform.GetComponentInChildren<Card>();
        Card BackStage2Card = GetZone("BackStage2", TargetPlayer.Player).transform.GetComponentInChildren<Card>();
        Card BackStage3Card = GetZone("BackStage3", TargetPlayer.Player).transform.GetComponentInChildren<Card>();
        Card BackStage4Card = GetZone("BackStage4", TargetPlayer.Player).transform.GetComponentInChildren<Card>();
        Card BackStage5Card = GetZone("BackStage5", TargetPlayer.Player).transform.GetComponentInChildren<Card>();
        Card CollaborationCard = GetZone("Collaboration", TargetPlayer.Player).transform.GetComponentInChildren<Card>();
        Card StageCard = GetZone("Stage", TargetPlayer.Player).transform.GetComponentInChildren<Card>();

        if (BackStage1Card != null)
            if (BackStage1Card.bloomLevel.Equals(level) && BackStage1Card.playedThisTurn == false)
            {
                validNames.Add(BackStage1Card.cardName);
                //EXTRA CONDITIONS
                if (BackStage1Card.bloomLevel.Equals("Debut") && BackStage1Card.cardName.Equals("ときのそら") || BackStage1Card.cardName.Equals("AZKi"))
                    validNames.Add("SorAZ");
            }
        if (BackStage2Card != null)
            if (BackStage2Card.bloomLevel.Equals(level) && BackStage2Card.playedThisTurn == false)
            {
                validNames.Add(BackStage2Card.cardName);
                //EXTRA CONDITIONS
                if (BackStage2Card.bloomLevel.Equals("Debut") && BackStage2Card.cardName.Equals("ときのそら") || BackStage2Card.cardName.Equals("AZKi"))
                    validNames.Add("SorAZ");

            }
        if (BackStage3Card != null)
            if (BackStage3Card.bloomLevel.Equals(level) && BackStage3Card.playedThisTurn == false)
            {
                validNames.Add(BackStage3Card.cardName);
                //EXTRA CONDITIONS
                if (BackStage3Card.bloomLevel.Equals("Debut") && BackStage3Card.cardName.Equals("ときのそら") || BackStage3Card.cardName.Equals("AZKi"))
                    validNames.Add("SorAZ");
            }
        if (BackStage4Card != null)
            if (BackStage4Card.bloomLevel.Equals(level) && BackStage4Card.playedThisTurn == false)
            {
                validNames.Add(BackStage4Card.cardName);
                //EXTRA CONDITIONS
                if (BackStage4Card.bloomLevel.Equals("Debut") && BackStage4Card.cardName.Equals("ときのそら") || BackStage4Card.cardName.Equals("AZKi"))
                    validNames.Add("SorAZ");
            }
        if (BackStage5Card != null)
            if (BackStage5Card.bloomLevel.Equals(level) && BackStage5Card.playedThisTurn == false)
            {
                validNames.Add(BackStage5Card.cardName);
                //EXTRA CONDITIONS
                if (BackStage5Card.bloomLevel.Equals("Debut") && BackStage5Card.cardName.Equals("ときのそら") || BackStage5Card.cardName.Equals("AZKi"))
                    validNames.Add("SorAZ");
            }
        if (CollaborationCard != null)
            if (CollaborationCard.bloomLevel.Equals(level) && CollaborationCard.playedThisTurn == false)
            {
                validNames.Add(CollaborationCard.cardName);
                //EXTRA CONDITIONS
                if (CollaborationCard.bloomLevel.Equals("Debut") && CollaborationCard.cardName.Equals("ときのそら") || CollaborationCard.cardName.Equals("AZKi"))
                    validNames.Add("SorAZ");
            }
        if (StageCard != null)
            if (StageCard.bloomLevel.Equals(level) && StageCard.playedThisTurn == false)
            {
                validNames.Add(StageCard.cardName);
                //EXTRA CONDITIONS
                if (StageCard.bloomLevel.Equals("Debut") && StageCard.cardName.Equals("ときのそら") || StageCard.cardName.Equals("AZKi"))
                    validNames.Add("SorAZ");
            }
        return validNames;
    }

    public void RemoveCardsFromCardHolder(int amountToRemove, List<RectTransform> cardsList, RectTransform holder)
    {
        // Remove the specified number of cards
        for (int i = 0; i < amountToRemove; i++)
        {
            // Get the last card added (or use another rule if needed)
            RectTransform cardToRemove = cardsList[^1];

            // Remove the card from the card list
            cardsList.RemoveAt(cardsList.Count - 1);

            // Destroy the GameObject for the card
            Destroy(cardToRemove.gameObject);
        }
    }

    public List<GameObject> GetChildrenWithName(GameObject parent, string name)
    {
        List<GameObject> matchingChildren = new();

        foreach (Transform child in parent.transform)
        {
            if (child.gameObject.name == name)
            {
                matchingChildren.Add(child.gameObject);
            }

            // Recursively search in the child's children
            matchingChildren.AddRange(GetChildrenWithName(child.gameObject, name));
        }

        return matchingChildren;
    }

    public void UpdateBoard()
    { //SET CARD NUMBERS RIGHT
        foreach (GameObject g in GameZones)
        {
            GameObject gAmount = g.transform.Find("Amount")?.gameObject;
            if (gAmount != null)
            {
                gAmount.GetComponent<TMP_Text>().text = (g.transform.childCount - 1).ToString();
                gAmount.transform.SetAsLastSibling();
            }
        }
    }

    public void AddOrMoveCardToGameZone(GameObject holder, List<Card> cardsToBeCretated = null, List<GameObject> cardsToBeMoved = null, bool TOBOTTOMOFTHELIST = false) {
        AddOrMoveCardToGameZone<Card>(holder, cardsToBeCretated, cardsToBeMoved, TOBOTTOMOFTHELIST);
    }

    public void AddOrMoveCardToGameZone<T>(GameObject holder, List<T> cardsToBeCretated = null, List<GameObject> cardsToBeMoved = null, bool TOBOTTOMOFTHELIST = false)
    {
        cardsToBeMoved = cardsToBeMoved == null ? new List<GameObject>() : cardsToBeMoved;

        if (cardsToBeCretated != null)
        {
            for (int i = 0; i < cardsToBeCretated.Count; i++)
            {
                GameObject newObject = Instantiate(cardPrefab, Vector3.zero, Quaternion.identity);
                Card newCardInfo = newObject.GetComponent<Card>();
                //since cardData dont have a gameobject we use generic to prevent erros releted with rendering of the Card constructor
                if (typeof(T) == typeof(Card))
                {
                    Card cardData = cardsToBeCretated[i] as Card;
                    newCardInfo.cardNumber = cardData.cardNumber;
                    newCardInfo.playedFrom = cardData.playedFrom;
                }
                else if (typeof(T) == typeof(CardData))
                {
                    CardData cardData = cardsToBeCretated[i] as CardData;
                    newCardInfo.cardNumber = cardData.cardNumber;
                    newCardInfo.playedFrom = cardData.playedFrom;
                }
                newCardInfo.cardPosition = holder.name;

                GameObject father = holder;
                if (father.name.Equals("Deck") || father.name.Equals("CardCheer") || father.name.Equals("Life"))
                {
                    Destroy(newObject.GetComponent<DuelField_HandDragDrop>());
                    Destroy(newObject.GetComponent<DuelField_HandClick>());
                    Destroy(newObject.GetComponent<Card>());
                }
                cardsToBeMoved.Add(newObject);
            }
        }

        foreach (GameObject eachCardToBeMoved in cardsToBeMoved)
        {
            if (!eachCardToBeMoved.name.Equals("Card(Clone)"))
                continue;

            var _RectTransform = eachCardToBeMoved.transform;

            StartCoroutine(SendCardToZoneAnimation(eachCardToBeMoved.transform, _RectTransform));

            eachCardToBeMoved.transform.SetParent(holder.transform, false);
            eachCardToBeMoved.transform.localPosition = Vector3.zero;
            eachCardToBeMoved.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            if (TOBOTTOMOFTHELIST)
                eachCardToBeMoved.transform.SetSiblingIndex(0);

            if (eachCardToBeMoved != null)
            {
                Transform hpbarObj = eachCardToBeMoved.transform.Find("HPBAR");
                if (hpbarObj != null)
                {
                    Card eachCardToBeMovedCard = eachCardToBeMoved.GetComponent<Card>();
                    eachCardToBeMovedCard.GetCardInfo();
                    UpdateHP(eachCardToBeMovedCard);
                    hpbarObj.gameObject.SetActive(false);
                }
            }

            if (holder.name.Equals("Stage") || holder.name.Equals("Collaboration") || holder.name.Equals("BackStage1") || holder.name.Equals("BackStage2") || holder.name.Equals("BackStage3") || 
                holder.name.Equals("BackStage4") || holder.name.Equals("BackStage5") || holder.name.Equals("Favourite"))
            {
                _RectTransform.localRotation = Quaternion.Euler(0f, -180f, 0f);
            }    
        }
    }

    private IEnumerator SendCardToZoneAnimation(Transform cardTransform, Transform targetZone)
    {
        Vector3 startPosition;
        // Check the card's current parent to determine the starting position
        Card card = cardTransform.GetComponent<Card>();
        if (card != null)
        {
            if (cardTransform.GetComponent<Card>().playedFrom.Equals("hand"))
            {
                cardTransform.SetParent(GetZone("hand", (targetZone.parent.name.Equals("Player")) ? TargetPlayer.Player : TargetPlayer.Oponnent).transform, false);
                startPosition = cardTransform.parent.name.Equals("PlayerHand") ? startPosition = new Vector3(0, Screen.height / 2, 1) : new Vector3(0, -Screen.height / 2, 1);
            }
            else
            {
                startPosition = cardTransform.localPosition;
            }
        }
        else {
            startPosition = cardTransform.localPosition;
        }

        Vector3 endPosition = Vector3.zero; // Target position within the zone
        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            // Move from startPosition to endPosition over time
            cardTransform.localPosition = Vector3.Lerp(startPosition, endPosition, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the final position is set
        cardTransform.localPosition = endPosition;
    }
    public void DrawCard(DuelAction draw)
    {
        if (draw.playerID == PlayerInfo.PlayerID)
        {
            AddCardToCardHolder(draw.cardList.Count, cardsPlayer, cardHolderPlayer, draw.cardList);
            RemoveCardFromZone(GetZone(draw.zone, TargetPlayer.Player), draw.cardList.Count);
        }
        if (draw.playerID != PlayerInfo.PlayerID)
        {
            AddCardToCardHolder(draw.cardList.Count, cardsOponnent, cardHolderOponnent, draw.cardList);
            RemoveCardFromZone(GetZone(draw.zone, TargetPlayer.Oponnent), draw.cardList.Count);
        }
    }
    public void AddCardToCardHolder(int amount, List<RectTransform> cardsList, RectTransform holder, List<Card> cardNumbers)
    {
        if (amount == 0)
            amount = 1;

        if (amount > cardNumbers.Count)
        {
            for (int i = 0; i < (amount - cardNumbers.Count); i++)
            {
                cardNumbers.Add(new Card(""));
            }
        }

        for (int n = 0; n < amount; n++)
        {
            GameObject newObject = Instantiate(cardPrefab, Vector3.zero, Quaternion.identity);

            Card newCard = newObject.GetComponent<Card>();
            newCard.cardNumber = cardNumbers[n].cardNumber;

            cardsList.Add(newObject.GetComponent<RectTransform>());
            newObject.transform.SetParent(holder, false);
            newObject.transform.localPosition = Vector3.zero;

            newCard.GetCardInfo();

            if (holder.name.Equals("OponentHand"))
            {
                Destroy(newObject.GetComponent<DuelField_HandDragDrop>());
                Destroy(newObject.GetComponent<DuelField_HandClick>());
                Destroy(newObject.GetComponent<Card>());
            }
        }
    }
    public void RemoveCardFromZone(GameObject father, int amount)
    {
        List<GameObject> cards = new();
        foreach (Transform child in father.transform)
            if (child.name == "Card(Clone)")
                cards.Add(child.gameObject);

        for (int i = 0; i < Mathf.Min(amount, cards.Count); i++)
        {
            GameObject card = cards[i];
            Destroy(card);
        }
        StackCardsEffect(father);
    }

    public void AddCardsToDeck(GameObject father, int amount, bool suffle)
    {
        for (int i = 0; i < amount; i++)
        {
            if (cardPrefab != null)
            {
                GameObject newObject = Instantiate(cardPrefab);

                newObject.transform.SetParent(father.transform);
                newObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                newObject.transform.localScale = Vector3.one;
                newObject.name = "Card(Clone)";

                if (father.name.Equals("Deck") || father.name.Equals("CardCheer") || father.name.Equals("Life"))
                {
                    Destroy(newObject.GetComponent<DuelField_HandDragDrop>());
                    Destroy(newObject.GetComponent<DuelField_HandClick>());
                    Card newObjectCard = newObject.GetComponent<Card>();
                    newObjectCard.GetCardInfo();
                    Destroy(newObjectCard);
                }
            }
            else
            {
                Debug.LogError("Card prefab is null. Please assign a valid prefab.");
                break;
            }
        }

        if (suffle)
        {
            StartCoroutine(ShuffleCardsCoroutine(GetChildrenWithName(father, "Card(Clone)"), 0.5f, 50f));
        }

        StackCardsEffect(father);
    }

    public void StackCardsEffect(GameObject father)
    {
        int childCount = father.transform.childCount;

        if (!(father.transform.parent.name.Equals("Oponente") || !father.transform.parent.name.Equals("Player")))
            return;

        for (int n = 0; n < childCount; n++)
        {
            Transform child = father.transform.GetChild(n);
            if (!child.name.Equals("Card(Clone)"))
                continue;

            Vector3 newPos = child.localPosition;
            newPos.y += 1.5f * n * (father.transform.parent.Equals("Oponente") ? 1 : -1);
            child.localPosition = newPos;
        }
    }

    public GameObject GetZone(string s, TargetPlayer player)
    {
        if (s.Equals("hand")) {
            return (TargetPlayer.Player == player ? cardHolderPlayer.gameObject : cardHolderOponnent.gameObject); 
        }

        int maxZones = GameZones.Count;
        int nZones = 0;

        if (TargetPlayer.Oponnent == player)
            nZones = GameZones.Count / 2;

        if (TargetPlayer.Player == player)
            maxZones = GameZones.Count / 2;

        for (; nZones < maxZones; nZones++)
        {
            if (GameZones[nZones].name.Equals(s))
            {
                return GameZones[nZones];
            }
        }
        return null;
    }

    void ResetCardTurnStatusForPlayer(TargetPlayer t)
    {
        // Define a list of zone names to skip
        HashSet<string> skipZoneNames = new HashSet<string>
        {
            "Favourite",
            "Deck",
            "Arquive",
            "Life",
            "CardCheer",
            "HoloPower"
        };

        // Determine the number of zones based on the target player
        int nZones = (TargetPlayer.Oponnent == t) ? GameZones.Count / 2 : 0;
        int maxZones = (TargetPlayer.Player == t) ? GameZones.Count / 2 : GameZones.Count;

        for (; nZones < maxZones; nZones++)
        {
            if (skipZoneNames.Contains(GameZones[nZones].name))
                continue;

            Card cardComponent = GameZones[nZones].GetComponentInChildren<Card>();

            if (cardComponent != null)
            {
                cardComponent.playedThisTurn = false;

                if (cardComponent.suspended)
                {
                    cardComponent.suspended = false;
                    cardComponent.transform.rotation = Quaternion.identity; // Resets rotation to zero
                }
            }
        }

    }
    public void ArrangeCards(List<RectTransform> c, RectTransform cardHolder)
    {
        c.RemoveAll(item => item == null || !item.name.Equals("Card(Clone)"));

        if (c.Count == 0) return;

        RectTransform rectTransform = c[0].GetComponent<RectTransform>();

        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;

        float totalChildsWidth = width * c.Count;
        float diference = cardHolder.rect.width - totalChildsWidth;

        for (int i = 0; i < c.Count; i++)
        {
            float xPos = (width * i) + (diference / (c.Count - 1) * i) - (cardHolder.rect.width / 2) + (width / 2);
            c[i].anchoredPosition = new Vector2(xPos, 0);

            if (c[i].transform.parent.name.Equals("PlayerHand"))
            {
                c[i].localRotation = Quaternion.Euler(0f, -180f, 0f);
                c[i].GetComponent<DuelField_HandDragDrop>().defaultValues = new RectTransformData(c[i].GetComponent<RectTransform>());
            }
            else 
            {
                c[i].transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            }
        }
    }

    //Counter For the Duel Timmer
    public void StartTurnCounter()
    {
        // Cancel any existing countdown before starting a new one
        countdownTokenSource?.Cancel();
        countdownTokenSource = new CancellationTokenSource();

        playerTimers = TURN_TIMER_SECONDS;
        StartCountdown(countdownTokenSource.Token);
    }
    private async void StartCountdown(CancellationToken token)
    {
        try
        {
            while (playerTimers > 0 && !token.IsCancellationRequested)
            {
                await Task.Delay(1000, token);
                if (token.IsCancellationRequested) return;

                playerTimers--;
                TimmerText.text = playerTimers.ToString();
            }
        }
        catch (TaskCanceledException)
        {
        }
    }
    IEnumerator ShuffleCardsCoroutine(List<GameObject> cards, float shuffleTime, float shuffleRange)
    {
        Vector3[] startPositions = new Vector3[cards.Count];
        Vector3[] randomPositions = new Vector3[cards.Count];

        // Store initial positions and generate random ones
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null)
            {
                startPositions[i] = cards[i].transform.localPosition;
                randomPositions[i] = startPositions[i] + new Vector3(
                    UnityEngine.Random.Range(-shuffleRange, shuffleRange),
                    UnityEngine.Random.Range(-shuffleRange, shuffleRange),
                    0);
            }
            else
            {
                startPositions[i] = Vector3.zero;
                randomPositions[i] = Vector3.zero;
            }
        }

        // Shuffle animation
        float elapsedTime = 0f;
        while (elapsedTime < shuffleTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / shuffleTime;

            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null)
                {
                    cards[i].transform.localPosition = Vector3.Lerp(startPositions[i], randomPositions[i], t);
                }
            }

            yield return null;
        }

        // Return cards to their original positions
        elapsedTime = 0f;
        while (elapsedTime < shuffleTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / shuffleTime;

            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null)
                {
                    //i'm counting that cards arrive ordered, because if is not, the positions will be suffled 
                    Vector3 newPos = startPositions[i];
                    newPos.y += 1.5f * i;
                    cards[i].transform.localPosition = Vector3.Lerp(randomPositions[i], startPositions[i], t);
                }
            }

            yield return null;
        }
    }


    public void SetViewMode(Image img)
    {
        if (DuelField_HandClick.isViewMode)
        {
            img.sprite = viewTypeActionImg;
            DuelField_HandClick.isViewMode = false;
        }
        else
        {
            img.sprite = viewTypeViewImg;
            DuelField_HandClick.isViewMode = true;
        }
    }

    public DuelFieldData MapDuelFieldData(List<GameObject> field)
    {
        DuelFieldData _DuelFieldData = new();

        if (!_MatchConnection._DuelFieldData.firstPlayer.Equals(PlayerInfo.PlayerID))
        {
            _DuelFieldData.playerBArquive = field[0].GetComponentsInChildren<Card>().ToList();
            _DuelFieldData.playerBHoloPower = field[2].GetComponentsInChildren<Card>().ToList();
            _DuelFieldData.playerBBackPosition = field[3].GetComponentsInChildren<Card>().ToList();
            _DuelFieldData.playerBFavourite = field[4].GetComponentInChildren<Card>();
            _DuelFieldData.playerBStage = field[6].GetComponentInChildren<Card>();
            _DuelFieldData.playerBCollaboration = field[5].GetComponentInChildren<Card>();
            _DuelFieldData.playerBLife = field[8].GetComponentsInChildren<Card>().ToList();

            _DuelFieldData.playerAArquive = field[9].GetComponentsInChildren<Card>().ToList();
            _DuelFieldData.playerAHoloPower = field[11].GetComponentsInChildren<Card>().ToList();
            _DuelFieldData.playerABackPosition = field[12].GetComponentsInChildren<Card>().ToList();
            _DuelFieldData.playerAFavourite = field[13].GetComponentInChildren<Card>();
            _DuelFieldData.playerAStage = field[15].GetComponentInChildren<Card>();
            _DuelFieldData.playerACollaboration = field[14].GetComponentInChildren<Card>();
            _DuelFieldData.playerALife = field[17].GetComponentsInChildren<Card>().ToList();

            return _DuelFieldData;
        }

        _DuelFieldData.playerAArquive = field[0].GetComponentsInChildren<Card>().ToList();
        _DuelFieldData.playerAHoloPower = field[2].GetComponentsInChildren<Card>().ToList();
        _DuelFieldData.playerABackPosition = field[3].GetComponentsInChildren<Card>().ToList();
        _DuelFieldData.playerAFavourite = field[4].GetComponentInChildren<Card>();
        _DuelFieldData.playerAStage = field[6].GetComponentInChildren<Card>();
        _DuelFieldData.playerACollaboration = field[5].GetComponentInChildren<Card>();
        _DuelFieldData.playerALife = field[8].GetComponentsInChildren<Card>().ToList();

        _DuelFieldData.playerBArquive = field[9].GetComponentsInChildren<Card>().ToList();
        _DuelFieldData.playerBHoloPower = field[11].GetComponentsInChildren<Card>().ToList();
        _DuelFieldData.playerBBackPosition = field[12].GetComponentsInChildren<Card>().ToList();
        _DuelFieldData.playerBFavourite = field[13].GetComponentInChildren<Card>();
        _DuelFieldData.playerBStage = field[15].GetComponentInChildren<Card>();
        _DuelFieldData.playerBCollaboration = field[14].GetComponentInChildren<Card>();
        _DuelFieldData.playerBLife = field[17].GetComponentsInChildren<Card>().ToList();

        return _DuelFieldData;
    }

    void AttachCardToTarget(DuelAction duelAction, TargetPlayer target)
    {
        GameObject cardZone = GetZone(duelAction.targetCard.cardPosition, target);

        GameObject usedCardGameObject = Instantiate(cardPrefab, Vector3.zero, Quaternion.identity);
        Card usedCardGameObjectCard = usedCardGameObject.GetComponent<Card>().SetCardNumber(duelAction.usedCard.cardNumber).GetCardInfo();

        //GETTING the father FOR
        Card newObjectCard = cardZone.GetComponentInChildren<Card>();

        StartCoroutine(SendCardToZoneAnimation(usedCardGameObject.transform, cardZone.transform));

        if (usedCardGameObjectCard.cardType.Equals("エール"))
        {
            newObjectCard.attachedEnergy ??= new List<GameObject>();
            newObjectCard.attachedEnergy.Add(usedCardGameObject);
        }
        else //equipe item
        {
            newObjectCard.attachedEquipe ??= new List<GameObject>();
            newObjectCard.attachedEquipe.Add(usedCardGameObject);
        }

        usedCardGameObject.transform.SetParent(cardZone.transform, false);
        usedCardGameObject.transform.localPosition = Vector3.zero;
        usedCardGameObject.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

        usedCardGameObject.transform.localRotation = Quaternion.Euler(0f, -180f, 0f);
        //usedCardGameObject.SetActive(false);

        cardZone.GetComponentInChildren<Card>().transform.SetAsLastSibling();

        if (!_MatchConnection._DuelFieldData.currentPlayerTurn.Equals(PlayerInfo.PlayerID))
        {
            RemoveCardsFromCardHolder(1, cardsOponnent, cardHolderOponnent);
        }
        else
        {
            if (!usedCardGameObjectCard.cardType.Equals("エール"))
                _EffectController.ResolveOnAttachEffect(duelAction);
        }
    }

    void RemoveCardFromPosition(DuelAction duelAction)
    {
        //need to make this comparisson better latter, comparing the last information send by the server may lead to errors 
        if (_MatchConnection.DuelActionListIndex.Last().Equals("CheerStepEndDefeatedHolomem"))
        {
            RemoveCardsFromCardHolder(1, cardsOponnent, cardHolderOponnent);
            return;
        }
        else if (duelAction.usedCard.cardPosition.Equals("Arquive"))
        {
            GameObject ZoneToRemove = GetZone("Arquive", _MatchConnection._DuelFieldData.currentPlayerTurn.Equals(PlayerInfo.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent);

            var cardList = ZoneToRemove.GetComponentsInChildren<Card>();
            foreach (Card card in cardList)
            {
                if (card.cardNumber.Equals(duelAction.usedCard.cardNumber))
                {
                    Destroy(card.gameObject);
                }
            }
        }
        else
        {
            RemoveCardFromZone(GetZone("CardCheer", _MatchConnection._DuelFieldData.currentPlayerTurn.Equals(PlayerInfo.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent), 1);
        }
    }

    public void UpdateHP(Card card)
    {
        if (card.transform == null)
            return;

        if (card.transform.parent == null)
            return;

        if (card.transform.parent.name.Equals("Favourite") || card.transform.parent.name.Equals("Deck") || card.transform.parent.name.Equals("CardCheer") || card.transform.parent.name.Equals("Life") || card.transform.parent.name.Equals("HoloPower") || card.transform.parent.name.Equals("Arquive"))
            return;

        card.transform.parent.transform.Find("HPBAR").gameObject.SetActive(true);
        card.transform.parent.transform.Find("HPBAR").Find("HPCurrent").GetComponent<TMP_Text>().text = card.currentHp.ToString();
        card.transform.parent.transform.Find("HPBAR").Find("HPMax").GetComponent<TMP_Text>().text = card.hp.ToString();
        if (card.transform.parent.parent.name.Equals("Oponente") || card.transform.parent.name.Equals("Oponente"))
        {
            card.transform.parent.transform.Find("HPBAR").Find("HPCurrent").localEulerAngles = new Vector3(0, 0, 180);
            card.transform.parent.transform.Find("HPBAR").Find("HPBar").localEulerAngles = new Vector3(0, 0, 180);
            card.transform.parent.transform.Find("HPBAR").Find("HPMax").localEulerAngles = new Vector3(0, 0, 180);
        }

    }

    // Helper method to get the last card in the zone, if any
    public Card GetLastCardInZone(string zoneName, DuelField.TargetPlayer targetPlayer)
    {
        var zone = GetZone(zoneName, targetPlayer);
        if (zone.transform.childCount > 0)
        {
            var card = zone.transform.GetChild(zone.transform.childCount - 1).GetComponent<Card>();
            return card != null ? card : null;
        }
        return null;
    }

    public void PopulateSelectableCards(TargetPlayer target, string[] zoneNames, GameObject holder, List<Card> SelectableCards)
    {
        GameObjectExtensions.DestroyAllChildren(holder);
        SelectableCards.Clear();
        foreach (var zoneName in zoneNames)
        {
            var existingCard = GetLastCardInZone(zoneName, target);
            if (existingCard != null)
            {
                SelectableCards.Add(existingCard);
            }
        }
    }

    public int CountBackStageTotal(bool onlyBackstage = false, TargetPlayer target = TargetPlayer.Player)
    {
        int count = 0;
        if (GetZone("BackStage1", target).GetComponentInChildren<Card>() != null)
            count++;
        if (GetZone("BackStage2", target).GetComponentInChildren<Card>() != null)
            count++;
        if (GetZone("BackStage3", target).GetComponentInChildren<Card>() != null)
            count++;
        if (GetZone("BackStage4", target).GetComponentInChildren<Card>() != null)
            count++;
        if (GetZone("BackStage5", target).GetComponentInChildren<Card>() != null)
            count++;
        if (!onlyBackstage)
        {
            if (GetZone("Collaboration", target).GetComponentInChildren<Card>() != null)
                count++;
        }
        return count;
    }
}
