using Assets.Scripts.Lib;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static DuelField;
using static DuelFieldData;

public class DuelField : MonoBehaviour
{
    public GameObject cardPrefab;

    public PlayerInfo PlayerInfo = null;
    [SerializeField] private GameSettings GameSettings = null;
    public MatchConnection _MatchConnection = null;

    PhaseMessage GamePhaseMsg;

    /// <summary>
    public RectTransform cardHolderPlayer; // The container holding the cards (this will define the available space)
    public List<RectTransform> cardsPlayer; // List of card RectTransforms

    public RectTransform cardHolderOponnent; // The container holding the cards (this will define the available space)
    public List<RectTransform> cardsOponnent; // List of card RectTransforms

    public RectTransform cardLifeHolderA; // The container holding the cards (this will define the available space)
    public List<RectTransform> cardsLifeStageA; // List of card RectTransforms

    public RectTransform cardLifeHolderB; // The container holding the cards (this will define the available space)
    public List<RectTransform> cardsLifeStageB; // List of card RectTransforms
    /// <summary>

    public GameObject DeckPlayer;
    public GameObject DeckOponnent;

    public GameObject VictoryPanel;
    public GameObject LosePanel;

    public float cardWidth = 58f; // The width of a card
    public float cardHeight = 73f; // The height of a card
    public float overlapFactor = 0.8f; // Factor to control the amount of overlap (0 = full overlap, 1 = no overlap)

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

    bool startofmain = false;

    public List<GameObject> GameZones = new();

    private bool InitialDraw = false;
    private bool InitialDrawP2 = false;

    public Sprite viewTypeActionImg;
    public Sprite viewTypeViewImg;

    private bool playerCannotDrawFromCheer;
    private int cheersAssignedThisChainTotal;
    private int cheersAssignedThisChainAmount;

    [Flags]
    public enum TargetPlayer : byte
    {
        Player = 0,
        Oponnent = 1
    }

    EffectController _EffectController;

    void Start()
    {
        PlayerInfo = GameObject.Find("PlayerInfo").GetComponent<PlayerInfo>();
        GameSettings = GameObject.Find("GameSettings").GetComponent<GameSettings>();
        _MatchConnection = FindAnyObjectByType<MatchConnection>();
        GamePhaseMsg = FindAnyObjectByType<PhaseMessage>();
        _MatchConnection._DuelFieldData = FindAnyObjectByType<MatchConnection>()._DuelFieldData;
        _EffectController = FindAnyObjectByType<EffectController>();

        //this fix the button initial icon
        SetViewMode(GameObject.Find("MatchField").transform.Find("ActionTypeToggle").GetComponent<Image>());
    }

    void Update()
    {
        UpdateBoard();
        DuelAction duelAction;

        if (_MatchConnection.DuelActionListIndex.Count > currentGameHigh && !LockGameFlow)
        {
            for (int SrvMessageCounter = currentGameHigh; SrvMessageCounter < _MatchConnection.DuelActionListIndex.Count; SrvMessageCounter++)
            {
                DuelAction d;
                List<Record> cardlist = new List<Record>();
                switch (_MatchConnection.DuelActionListIndex[SrvMessageCounter])
                {
                    case "StartDuel":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.StartDuel)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        AddCardsToDeck(GetZone("Deck", TargetPlayer.Player), 50, true);
                        AddCardsToDeck(GetZone("Deck", TargetPlayer.Oponnent), 50, true);


                        AddCardsToDeck(GetZone("CardCheer", TargetPlayer.Player), 20, true);
                        AddCardsToDeck(GetZone("CardCheer", TargetPlayer.Oponnent), 20, true);

                        if (_MatchConnection._DuelFieldData.firstPlayer != PlayerInfo.PlayerID)
                        {
                            AddCardToGameZone(GetZone("Favourite", TargetPlayer.Player), new List<Card>() { _MatchConnection._DuelFieldData.playerBFavourite });
                            AddCardToGameZone(GetZone("Favourite", TargetPlayer.Oponnent), new List<Card>() { _MatchConnection._DuelFieldData.playerAFavourite });
                        }
                        else
                        {
                            AddCardToGameZone(GetZone("Favourite", TargetPlayer.Player), new List<Card>() { _MatchConnection._DuelFieldData.playerAFavourite });
                            AddCardToGameZone(GetZone("Favourite", TargetPlayer.Oponnent), new List<Card>() { _MatchConnection._DuelFieldData.playerBFavourite });
                        }

                        _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.InitialDraw;
                        GamePhaseMsg.StartMessage("Starting Duel");
                        currentGameHigh = 1;
                        break;
                    case "InitialDraw":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.InitialDraw)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        d = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        //effect draw
                        DrawCard(d);
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
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        d = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        //effect draw
                        DrawCard(d);
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
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        d = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        if (d.playerID == PlayerInfo.PlayerID)
                        {
                            RemoveCardsFromCardHolder(d.cardList.Count, cardsPlayer, cardHolderPlayer);
                            AddCardsToDeck(GetZone("Deck", TargetPlayer.Player), 7, d.suffle);
                            playerMulligan = true;
                        }
                        else
                        {
                            RemoveCardsFromCardHolder(d.cardList.Count, cardsOponnent, cardHolderOponnent);
                            AddCardsToDeck(GetZone("Deck", TargetPlayer.Oponnent), 7, d.suffle);
                            oponnentMulligan = true;
                        }

                        if (_MatchConnection._DuelFieldData.firstPlayer == d.playerID)
                            _MatchConnection._DuelFieldData.playerAHand = d.cardList;

                        if (_MatchConnection._DuelFieldData.firstPlayer != d.playerID)
                            _MatchConnection._DuelFieldData.playerBHand = d.cardList;


                        //suffle oponent hands and redraw
                        //suffle our hands and redraw

                        DrawCard(d);
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
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        d = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        if (d.playerID == PlayerInfo.PlayerID)
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
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        d = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        // ESSA LOGICA PODE ESTAR ERRADA NO CENARIO FORA DO MOC QUE O PLAYER È O PA E NAO O PB
                        if (d.playerID == PlayerInfo.PlayerID)
                        {
                            if (_MatchConnection.DuelActionListIndex[SrvMessageCounter].Equals("PAMulliganF") && playerMulliganF)
                                break;

                            RemoveCardsFromCardHolder(7, cardsPlayer, cardHolderPlayer);
                            AddCardsToDeck(GetZone("Deck", TargetPlayer.Player), 7, d.suffle);
                            playerMulliganF = true;
                        }
                        else
                        {
                            if (_MatchConnection.DuelActionListIndex[SrvMessageCounter].Equals("PBMulliganF") && oponnentMulliganF)
                                break;

                            RemoveCardsFromCardHolder(7, cardsOponnent, cardHolderOponnent);
                            AddCardsToDeck(GetZone("Deck", TargetPlayer.Oponnent), 7, d.suffle);
                            oponnentMulliganF = true;
                        }


                        if (_MatchConnection._DuelFieldData.firstPlayer == d.playerID)
                            _MatchConnection._DuelFieldData.playerAHand = d.cardList;

                        if (_MatchConnection._DuelFieldData.firstPlayer != d.playerID)
                            _MatchConnection._DuelFieldData.playerBHand = d.cardList;

                        DrawCard(d);

                        if (playerMulliganF && oponnentMulliganF)
                        {
                            currentGameHigh++;
                            currentGameHigh++;
                            LockGameFlow = true;
                        }
                        break;
                    case "BoardReadyToPlay":

                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.SettingUpBoard)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        DuelFieldData boardinfo = JsonConvert.DeserializeObject<DuelFieldData>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });


                        RemoveCardFromZone(GetZone("Favourite", TargetPlayer.Player), 1);
                        RemoveCardFromZone(GetZone("Favourite", TargetPlayer.Oponnent), 1);

                        if (_MatchConnection._DuelFieldData.currentPlayerTurn == PlayerInfo.PlayerID)
                        {
                            AddCardToGameZone(GetZone("Life", TargetPlayer.Oponnent), boardinfo.playerBLife);
                            RemoveCardFromZone(GetZone("CardCheer", TargetPlayer.Oponnent), boardinfo.playerBLife.Count);
                            AddCardToGameZone(GetZone("Life", TargetPlayer.Player), boardinfo.playerALife);
                            RemoveCardFromZone(GetZone("CardCheer", TargetPlayer.Player), boardinfo.playerALife.Count);

                            AddCardToGameZone(GetZone("Favourite", TargetPlayer.Player), new List<Card>() { boardinfo.playerAFavourite });
                            AddCardToGameZone(GetZone("Favourite", TargetPlayer.Oponnent), new List<Card>() { boardinfo.playerBFavourite });

                            AddCardToGameZone(GetZone("Stage", TargetPlayer.Oponnent), new List<Card>() { boardinfo.playerBStage });
                            for (int n = 0; n < boardinfo.playerBBackPosition.Count; n++)
                            {
                                AddCardToGameZone(GetZone(boardinfo.playerBBackPosition[n].cardPosition, TargetPlayer.Oponnent), new List<Card>() { boardinfo.playerBBackPosition[n] });
                            }
                            int x = boardinfo.playerBBackPosition.Count + 1;
                            RemoveCardsFromCardHolder(x, cardsOponnent, cardHolderOponnent);

                        }
                        else
                        {
                            AddCardToGameZone(GetZone("Life", TargetPlayer.Player), boardinfo.playerBLife);
                            RemoveCardFromZone(GetZone("CardCheer", TargetPlayer.Player), boardinfo.playerBLife.Count);
                            AddCardToGameZone(GetZone("Life", TargetPlayer.Oponnent), boardinfo.playerALife);
                            RemoveCardFromZone(GetZone("CardCheer", TargetPlayer.Oponnent), boardinfo.playerALife.Count);

                            AddCardToGameZone(GetZone("Favourite", TargetPlayer.Player), new List<Card>() { boardinfo.playerBFavourite });
                            AddCardToGameZone(GetZone("Favourite", TargetPlayer.Oponnent), new List<Card>() { boardinfo.playerAFavourite });

                            AddCardToGameZone(GetZone("Stage", TargetPlayer.Oponnent), new List<Card>() { boardinfo.playerAStage });
                            for (int n = 0; n < boardinfo.playerABackPosition.Count; n++)
                            {
                                AddCardToGameZone(GetZone(boardinfo.playerABackPosition[n].cardPosition, TargetPlayer.Oponnent), new List<Card>() { boardinfo.playerABackPosition[n] });
                            }

                            int x = boardinfo.playerABackPosition.Count + 1;
                            RemoveCardsFromCardHolder(x, cardsOponnent, cardHolderOponnent);
                        }

                        foreach (RectTransform lifecard in cardLifeHolderA)
                        {
                            cardsLifeStageA.Add(lifecard);
                        }
                        ArrangeCardsCentered(cardsLifeStageA, cardLifeHolderA);

                        foreach (RectTransform lifecard in cardLifeHolderB)
                        {
                            cardsLifeStageB.Add(lifecard);
                        }
                        ArrangeCardsCentered(cardsLifeStageB, cardLifeHolderB);

                        _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "DrawRequest", "AskDrawPhase", "", currentGameHigh);



                        currentGameHigh++;
                        _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.DrawStep;
                        break;

                    //////////////////////////////
                    //START OF DUEL NORMAL FLOW//

                    case "ResetStep":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.ResetStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        duelAction = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });


                        if (PlayerInfo.PlayerID == _MatchConnection._DuelFieldData.currentPlayerTurn)
                            ResetCardTurnStatusForPlayer(TargetPlayer.Player);
                        else
                            ResetCardTurnStatusForPlayer(TargetPlayer.Oponnent);


                        if (!string.IsNullOrEmpty(duelAction.usedCard.cardNumber))
                            if (PlayerInfo.PlayerID != duelAction.playerID)
                            {
                                GameObject zone = GetZone(duelAction.usedCard.cardPosition, TargetPlayer.Oponnent);
                                DuelField_HandClick.MoveCardsToZone(GetZone(duelAction.playedFrom, TargetPlayer.Oponnent).transform, zone.transform);
                                Card unRestCard = zone.GetComponentInChildren<Card>();
                                unRestCard.suspended = true;
                                unRestCard.cardPosition = duelAction.usedCard.cardPosition;
                                zone.transform.GetChild(zone.transform.childCount - 1).Rotate(0, 0, 90);
                                //maybe add the playedfrom here
                            }
                            else
                            {
                                GameObject zone = GetZone(duelAction.usedCard.cardPosition, TargetPlayer.Player);
                                DuelField_HandClick.MoveCardsToZone(GetZone(duelAction.playedFrom, TargetPlayer.Player).transform, zone.transform);
                                Card unBloomCard = zone.GetComponentInChildren<Card>();
                                unBloomCard.suspended = true;
                                unBloomCard.cardPosition = duelAction.usedCard.cardPosition;
                                zone.transform.GetChild(zone.transform.childCount - 1).Rotate(0, 0, 90);
                                //maybe add the playedfrom here
                            }


                        if (_MatchConnection._DuelFieldData.currentPlayerTurn == PlayerInfo.PlayerID)
                        {
                            if (GetZone("Stage", TargetPlayer.Player).transform.childCount > 0)
                            {
                                _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.DrawStep;
                                _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "DrawRequest", "DrawRequest", "DrawRequest", currentGameHigh, "DrawRequest");
                            }
                            else
                            {
                                _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.ResetStepReSetStage;
                                LockGameFlow = true;
                            }
                        }
                        else
                        {
                            if (GetZone("Stage", TargetPlayer.Oponnent).transform.childCount > 0)
                            {
                                _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.DrawStep;
                            }
                            else
                            {
                                _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.ResetStepReSetStage;
                            }
                        }

                        GamePhaseMsg.StartMessage("Reset Step");
                        currentGameHigh++;
                        break;
                    case "ReSetStage":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.ResetStepReSetStage)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        duelAction = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        if (!string.IsNullOrEmpty(duelAction.usedCard.cardNumber))
                            if (PlayerInfo.PlayerID != duelAction.playerID)
                            {
                                GameObject zone = GetZone(duelAction.usedCard.cardPosition, TargetPlayer.Oponnent);
                                DuelField_HandClick.MoveCardsToZone(GetZone(duelAction.playedFrom, TargetPlayer.Oponnent).transform, zone.transform);
                                Card unRestCard = zone.GetComponentInChildren<Card>();
                                unRestCard.suspended = false;
                                unRestCard.cardPosition = duelAction.usedCard.cardPosition;
                            }

                        _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.DrawStep;
                        if (_MatchConnection._DuelFieldData.currentPlayerTurn == PlayerInfo.PlayerID)
                        {
                            _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "DrawRequest", "DrawRequest", "DrawRequest", currentGameHigh, "DrawRequest");
                        }
                        currentGameHigh++;
                        break;
                    case "DrawPhase":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.DrawStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        d = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        DrawCard(d);


                        if (_MatchConnection._DuelFieldData.currentPlayerTurn == PlayerInfo.PlayerID)
                            _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "CheerRequest", "AskNextPhase", "DrawPhase", currentGameHigh);
                        _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.CheerStep;
                        currentGameHigh++;
                        GamePhaseMsg.StartMessage("Draw Step");
                        break;
                    case "DefeatedHoloMember":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.MainStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        duelAction = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        TargetPlayer _TargetPlayer = duelAction.playerID == PlayerInfo.PlayerID ? TargetPlayer.Player : TargetPlayer.Oponnent;
                        GameObject zoneArt = GetZone(duelAction.targetCard.cardPosition, _TargetPlayer);

                        if (_MatchConnection._DuelFieldData.currentPlayerTurn == PlayerInfo.PlayerID)
                        {
                            EndTurnButton.SetActive(false);
                            cardlist.Clear();
                            cardlist = FileReader.QueryRecordsByType(new List<string>() { });
                        }
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
                                cardComponent.attachedCards = null;
                                cardComponent.bloomChild = null;
                            }

                            // Send card to zone
                            SendCardToZone(childObject, "Arquive", duelAction.playerID == PlayerInfo.PlayerID ? TargetPlayer.Player : TargetPlayer.Oponnent);
                        }

                        _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.HolomemDefeated;

                        if ((_MatchConnection._DuelFieldData.currentPlayerTurn != PlayerInfo.PlayerID))
                            _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "CheerRequestHolomemDown", "", "", currentGameHigh);

                        currentGameHigh++;
                        break;
                    case "HolomemDefatedSoGainCheer":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.HolomemDefeated)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        DuelAction drawCheer = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        if (drawCheer.cardList[0].cardNumber == "Empty")
                        {
                            playerCannotDrawFromCheer = true;
                            if (_MatchConnection._DuelFieldData.currentGamePhase == GAMEPHASE.HolomemDefeatedEnergyChoose)
                                _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "CheerChooseRequestHolomemDown", "", "", currentGameHigh, "");

                            if (_MatchConnection._DuelFieldData.currentGamePhase == GAMEPHASE.CheerStepChoose)
                                _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "CheerChooseRequest", "", "", currentGameHigh, "");
                        }
                        {
                            DrawCard(drawCheer);
                            cheersAssignedThisChainAmount = 0;
                            cheersAssignedThisChainTotal = drawCheer.cardList.Count;
                        }

                        if (drawCheer.playerID == PlayerInfo.PlayerID)
                            LockGameFlow = true;

                        //habiliting card that can be played this turn -- energy
                        GetUsableCards();

                        _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.HolomemDefeatedEnergyChoose;
                        currentGameHigh++;

                        break;
                    case "CheerStepEndDefeatedHolomem":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.HolomemDefeatedEnergyChoose)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        // WE ARE ADDING ENERGY THAT THE PLAYER GAINED FROM HIS HOLOMEM BEING DOWN, WE ONLY NEED TO CALL AttachEnergyToTarget FOR THE OTHER PLAYER SINCE WE DID THE ENERGY ATTACHE IN THE SCREEN HERE
                        duelAction = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

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


                        //if the player who is not the player is here, we return, he one assigning energy since his holomem died, we do not need to assign again
                        if (_MatchConnection._DuelFieldData.currentPlayerTurn != PlayerInfo.PlayerID)
                            break;

                        // if player still have cheer, we attach, else, we skip
                        if (!playerCannotDrawFromCheer)
                            AttachEnergyToTarget(duelAction, TargetPlayer.Oponnent);


                        if (cheersAssignedThisChainAmount > cheersAssignedThisChainTotal - 1)
                        {
                            EndTurnButton.SetActive(true);
                            _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "MainStartRequest", "CalledAt:CheerStepEndDefeatedHolomem", "", currentGameHigh, "");
                        }
                        break;
                    case "CheerStep":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.CheerStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        drawCheer = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        if (drawCheer.cardList[0].cardNumber == "Empty")
                        {
                            playerCannotDrawFromCheer = true;
                            if (_MatchConnection._DuelFieldData.currentGamePhase == GAMEPHASE.HolomemDefeatedEnergyChoose)
                                _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "CheerChooseRequestHolomemDown", "", "", currentGameHigh, "");

                            if (_MatchConnection._DuelFieldData.currentGamePhase == GAMEPHASE.CheerStepChoose)
                                if (_MatchConnection._DuelFieldData.currentPlayerTurn == PlayerInfo.PlayerID)
                                    _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "CheerChooseRequest", "", "", currentGameHigh, "");
                        }
                        {
                            DrawCard(drawCheer);
                        }
                        if (drawCheer.playerID == PlayerInfo.PlayerID)
                            LockGameFlow = true;

                        //habiliting card that can be played this turn -- energy
                        GetUsableCards();
                        _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.CheerStepChoose;
                        currentGameHigh++;
                        GamePhaseMsg.StartMessage("Cheer Step");
                        break;
                    case "CheerStepEnd":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.CheerStepChoose)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        duelAction = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        // if player still have cheer, we attach, else, we skip
                        if (!playerCannotDrawFromCheer && _MatchConnection._DuelFieldData.currentPlayerTurn != PlayerInfo.PlayerID)
                            AttachEnergyToTarget(duelAction, TargetPlayer.Oponnent);

                        currentGameHigh++;
                        _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.MainStep;

                        if (_MatchConnection._DuelFieldData.currentPlayerTurn == PlayerInfo.PlayerID)
                            _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "MainStartRequest", "CalledAt:CheerStepEnd", "", currentGameHigh, "");

                        startofmain = true;

                        break;
                    case "MainPhase":
                        if (startofmain)
                            GamePhaseMsg.StartMessage("Main Step");

                        startofmain = false;


                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.MainStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        if (_MatchConnection._DuelFieldData.currentPlayerTurn == PlayerInfo.PlayerID)
                        {
                            EndTurnButton.SetActive(true);
                            GetUsableCards();
                        }
                        _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.MainStep;
                        currentGameHigh++;
                        break;
                    case "PlayHolomem":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.MainStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        duelAction = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        int currentPlayer = _MatchConnection._DuelFieldData.currentPlayerTurn;
                        if (duelAction.playedFrom.Equals("Deck")) // NEED TO SHUFFLE
                        {
                            TargetPlayer targetPlayer = (currentPlayer == PlayerInfo.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent;

                            HandleCardPlay(duelAction, targetPlayer, currentPlayer);
                        }
                        else if (_MatchConnection._DuelFieldData.currentPlayerTurn != PlayerInfo.PlayerID)
                        {
                            HandleCardPlay(duelAction, TargetPlayer.Oponnent, currentPlayer);
                        }
                        currentGameHigh++;
                        break;
                    case "Draw":
                    case "DrawCollabEffect":
                    case "DrawArtEffect":
                    case "DrawSupportEffect":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.MainStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        d = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });


                            //since this suffleAll, we destroy the player hand, then add the used card to the other player arquive
                            if (d.playerID == PlayerInfo.PlayerID)
                            {
                                AddCardsToDeck(GetZone("Deck", TargetPlayer.Player), cardsPlayer.Count, d.suffle);
                                if (d.suffleBackToDeck)
                                {
                                    foreach (RectTransform gmd in cardsPlayer)
                                    {
                                        Destroy(gmd.gameObject);
                                    }
                                    cardsPlayer.Clear();
                                    ArrangeCards(cardsPlayer, cardHolderPlayer);
                                }
                            }
                            else
                            {
                                AddCardsToDeck(GetZone("Deck", TargetPlayer.Oponnent), cardsOponnent.Count, d.suffle);
                                if (d.suffleBackToDeck)
                                {
                                    foreach (RectTransform gmd in cardsOponnent)
                                    {
                                        Destroy(gmd.gameObject);
                                    }
                                    cardsOponnent.Clear();
                                    ArrangeCards(cardsOponnent, cardHolderOponnent);
                                }
                                AddCardToGameZone(GetZone("Arquive", TargetPlayer.Oponnent), new List<Card>() { new Card(d.usedCard.cardNumber) });
                            }

                        DrawCard(d);
                        currentGameHigh++;
                        break;
                    case "RollDice":
                        currentGameHigh++;
                        break;
                    case "BloomHolomem":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.MainStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        //Attaching the recieve energy from the server to the oponnent card
                        duelAction = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        if (_MatchConnection._DuelFieldData.currentPlayerTurn != PlayerInfo.PlayerID)
                        {



                            //BloomCard(GameObject FatherZone, GameObject Card)

                            GameObject cardZone = GetZone(duelAction.local, TargetPlayer.Oponnent);



                            GameObject usedCardGameObject = Instantiate(cardPrefab, Vector3.zero, Quaternion.identity);
                            Card usedCardGameObjectCard = usedCardGameObject.GetComponent<Card>();
                            usedCardGameObjectCard.cardNumber = duelAction.usedCard.cardNumber;
                            usedCardGameObjectCard.GetCardInfo();


                            GameObject FatherZoneActiveCard = cardZone.transform.GetChild(cardZone.transform.childCount - 1).gameObject;

                            usedCardGameObject.transform.SetParent(cardZone.transform, false);

                            usedCardGameObject.transform.SetSiblingIndex(cardZone.transform.childCount - 1);

                            usedCardGameObject.transform.localPosition = Vector3.zero;
                            usedCardGameObject.transform.localScale = new Vector3(0.9f, 0.9f);

                            FatherZoneActiveCard.SetActive(false);


                            usedCardGameObject.GetComponent<Card>().bloomChild.Add(FatherZoneActiveCard);
                            usedCardGameObject.GetComponent<Card>().attachedCards = FatherZoneActiveCard.GetComponent<Card>().attachedCards;
                            FatherZoneActiveCard.GetComponent<Card>().attachedCards = null;

                            usedCardGameObject.GetComponent<Card>().playedFrom = "hand";

                            RemoveCardsFromCardHolder(1, cardsOponnent, cardHolderOponnent);

                        }

                        currentGameHigh++;
                        break;
                    case "DoCollab":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.MainStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        if (_MatchConnection._DuelFieldData.currentPlayerTurn != PlayerInfo.PlayerID)
                        {
                            duelAction = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                            GameObject zone = GetZone(duelAction.local, TargetPlayer.Oponnent);

                            DuelField_HandClick.MoveCardsToZone(GetZone(duelAction.playedFrom, TargetPlayer.Oponnent).transform, zone.transform);
                            SendCardToZone(GetZone("Deck", TargetPlayer.Oponnent).transform.GetChild(0).gameObject, "HoloPower", TargetPlayer.Oponnent);
                        }
                        currentGameHigh++;
                        break;
                    case "UnDoCollab":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.MainStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        duelAction = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        if (!string.IsNullOrEmpty(duelAction.usedCard.cardNumber))
                            if (PlayerInfo.PlayerID != duelAction.playerID)
                            {
                                GameObject zone = GetZone(duelAction.usedCard.cardPosition, TargetPlayer.Oponnent);
                                DuelField_HandClick.MoveCardsToZone(GetZone(duelAction.playedFrom, TargetPlayer.Oponnent).transform, zone.transform);
                                Card unRestCard = zone.GetComponentInChildren<Card>();
                            }
                            else
                            {
                                GameObject zone = GetZone(duelAction.usedCard.cardPosition, TargetPlayer.Player);
                                DuelField_HandClick.MoveCardsToZone(GetZone(duelAction.playedFrom, TargetPlayer.Player).transform, zone.transform);
                                Card unBloomCard = zone.GetComponentInChildren<Card>();
                            }

                        currentGameHigh++;
                        break;

                    case "UseCardEffectDrawXAmountAddAnyIfConditionMatchThenReorderToBottom":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.MainStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        DuelAction duelActionReponse = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        if (duelActionReponse.playerID != PlayerInfo.PlayerID)
                        {

                            //if cost is not null means that the card used need to pay energy in the field to activate, so we need to reflect this information to the oponnent
                            if (duelActionReponse.cheerCostCard != null)
                            {
                                GameObject zone = GetZone(duelActionReponse.cheerCostCard.cardPosition, TargetPlayer.Oponnent);
                                List<GameObject> listOfAvalibleEnergyToRemove = zone.transform.GetChild(zone.transform.childCount - 1).GetComponent<Card>().attachedCards;

                                int n = 0;
                                foreach (GameObject subAvEnergy in listOfAvalibleEnergyToRemove)
                                {
                                    if (subAvEnergy.GetComponent<Card>().cardNumber.Equals(duelActionReponse.cheerCostCard.cardNumber))
                                    {
                                        break;
                                    }
                                    n++;
                                }
                                zone.GetComponentInChildren<Card>().attachedCards.Remove(listOfAvalibleEnergyToRemove[n]);
                                listOfAvalibleEnergyToRemove[n].gameObject.SetActive(true);
                                SendCardToZone(listOfAvalibleEnergyToRemove[n], "Arquive", TargetPlayer.Oponnent);
                            }
                            RemoveCardsFromCardHolder(1, cardsOponnent, cardHolderOponnent);
                            AddCardToGameZone(GetZone("Arquive", TargetPlayer.Oponnent), new List<Card>() { new Card("") { cardNumber = duelActionReponse.usedCard.cardNumber } });
                        }
                        else
                        {
                            _EffectController.isServerResponseArrive = true;
                            LockGameFlow = true;
                        }

                        currentGameHigh++;
                        break;
                    case "SuporteEffectDrawXAddIfDone":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.MainStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        DuelAction drawResponse = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        if (drawResponse.playerID != PlayerInfo.PlayerID)
                        {
                            foreach (Card cardReturnedToDraw in drawResponse.cardList)
                            {
                                cardReturnedToDraw.cardNumber = "";
                            }
                        }

                        DrawCard(drawResponse);

                        currentGameHigh++;
                        break;
                    case "AttachEnergyResponse":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.MainStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        duelAction = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        if (duelAction.playerID == PlayerInfo.PlayerID)
                            AttachEnergyToTarget(duelAction, TargetPlayer.Player);
                        else
                            AttachEnergyToTarget(duelAction, TargetPlayer.Oponnent);

                        _EffectController.isServerResponseArrive = true;
                        currentGameHigh++;
                        break;
                    case "SuporteEffectSummomIf":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.MainStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        duelActionReponse = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        drawResponse = JsonConvert.DeserializeObject<DuelAction>(duelActionReponse.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore }); ;

                        if (duelActionReponse.playerID != PlayerInfo.PlayerID)
                        {
                            RemoveCardsFromCardHolder(1, cardsOponnent, cardHolderOponnent);
                            AddCardToGameZone(GetZone("Arquive", TargetPlayer.Oponnent), new List<Card>() { new Card("") { cardNumber = duelActionReponse.usedCard.cardNumber } });
                        }
                        else
                        {
                            LockGameFlow = true;
                            _EffectController.isServerResponseArrive = true;
                        }
                        currentGameHigh++;
                        break;
                    case "OnCollabEffect":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.MainStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        duelActionReponse = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        if (duelActionReponse.playerID == PlayerInfo.PlayerID)
                        {
                            //lock game flow till player finish selection
                            LockGameFlow = true;
                            _EffectController.ResolveOnCollabEffect(duelActionReponse);
                        }

                        _EffectController.isServerResponseArrive = true;
                        currentGameHigh++;
                        break;
                    case "OnArtEffect":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.MainStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        duelActionReponse = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        if (duelActionReponse.playerID == PlayerInfo.PlayerID)
                        {
                            //lock game flow till player finish selection
                            LockGameFlow = true;
                            _EffectController.ResolveOnArtEffect(duelActionReponse);
                        }

                        _EffectController.isServerResponseArrive = true;
                        currentGameHigh++;
                        break;
                    case "PickFromListThenGiveBacKFromHandDone":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.MainStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        duelActionReponse = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        drawResponse = JsonConvert.DeserializeObject<DuelAction>(duelActionReponse.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore }); ;

                        if (duelActionReponse.playerID != PlayerInfo.PlayerID)
                        {
                            //we remove only one card from the pllayer hand, bacause we're using draw so the hands match
                            RemoveCardsFromCardHolder(1, cardsOponnent, cardHolderOponnent);
                            // add card to holopower since we are using draw which removes a card from the zone
                            AddCardToGameZone(GetZone("HoloPower", TargetPlayer.Oponnent), new List<Card>() { new Card("") });
                            //just making the card empty so the player dont see in the oponent hand holder, we can check in the log
                            drawResponse.cardList[0].cardNumber = "";
                        }
                        else
                        {
                            //removing from the player hand the picked card to add with the draw the one from the holopower
                            int n = -1;
                            for (int contadorCardHand = 0; contadorCardHand < cardsPlayer.Count; contadorCardHand++)
                            {
                                if (cardsPlayer[contadorCardHand].GetComponent<Card>().cardNumber.Equals(duelActionReponse.targetCard.cardNumber))
                                {
                                    n = contadorCardHand;
                                }
                            }

                            if (n == -1)
                                throw new Exception("Used card do not exist in the player hand");

                            //remove usedcard from player hand, usedcard
                            cardsPlayer.RemoveAt(n);

                            // add card to holopower since we are using draw which removes a card from the zone
                            AddCardToGameZone(GetZone("HoloPower", TargetPlayer.Player), new List<Card>() { new Card("") });
                        }

                        DrawCard(drawResponse);

                        currentGameHigh++;
                        break;
                    case "UsedArt":
                        if (_MatchConnection._DuelFieldData.currentGamePhase != GAMEPHASE.MainStep)
                        {
                            throw new Exception("not in the right gamephase, we're at " + _MatchConnection.DuelActionListIndex[SrvMessageCounter] + " and tried to enter at" + _MatchConnection._DuelFieldData.currentGamePhase.GetType());
                        }

                        duelAction = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        zoneArt = GetZone(duelAction.targetCard.cardPosition, duelAction.playerID != PlayerInfo.PlayerID ? TargetPlayer.Player : TargetPlayer.Oponnent);

                        Card card = zoneArt.GetComponentInChildren<Card>();
                        card.currentHp -= int.Parse(duelAction.actionObject);

                        UpdateHP(card);

                        if (duelAction.usedCard.cardPosition.Equals("Stage"))
                        {
                            centerStageArtUsed = true;
                        }
                        else if (duelAction.usedCard.cardPosition.Equals("Collaboration"))
                        {
                            collabStageArtUsed = true;
                        }

                        if (duelAction.playerID.Equals(PlayerInfo.PlayerID))
                            if (centerStageArtUsed && collabStageArtUsed)
                                GenericActionCallBack(null, "Endturn");

                        currentGameHigh++;
                        break;
                    case "MainPhaseDoAction":
                        currentGameHigh++;
                        break;
                    case "Endduel":
                        duelAction = JsonConvert.DeserializeObject<DuelAction>(_MatchConnection.DuelActionList.GetByIndex(SrvMessageCounter), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        if (PlayerInfo.PlayerID.Equals(duelAction.playerID))
                            VictoryPanel.SetActive(true);
                        else
                            LosePanel.SetActive(true);

                        break;
                    case "Endturn":
                        startofmain = false;


                        _MatchConnection._DuelFieldData.currentPlayerTurn = _MatchConnection._DuelFieldData.currentPlayerTurn == _MatchConnection._DuelFieldData.firstPlayer ? _MatchConnection._DuelFieldData.secondPlayer : _MatchConnection._DuelFieldData.firstPlayer;

                        centerStageArtUsed = false;
                        collabStageArtUsed = false;

                        _MatchConnection._DuelFieldData.currentTurn++;
                        _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.ResetStep;
                        _MatchConnection._DuelFieldData.playerLimiteCardPlayed.Clear();

                        currentGameHigh++;

                        //we changed the current player, so, the next player is the oponnent now, the calls the server
                        if (_MatchConnection._DuelFieldData.currentPlayerTurn == PlayerInfo.PlayerID)
                            _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "ResetRequest", "CalledAt:Endturn", "", currentGameHigh, "");
                        GamePhaseMsg.StartMessage("End Step");
                        break;
                    default:
                        Debug.Log("Action done: " + _MatchConnection.DuelActionListIndex[SrvMessageCounter]);
                        ConsoleClearer.ClearConsole();
                        break;
                }
            }
        }

        if (playerMulliganF && oponnentMulliganF && !ReadyButtonShowed)
        {
            ReadyButton.SetActive(true);
            ReadyButtonShowed = true;
            _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.SettingUpBoard;

            GetUsableCards();
        }
    }
    ////////////////////////////////////////////////////////////////////////
    public void GenericActionCallBack(DuelAction _DuelAction, string type = "standart")
    {
        string jsonString;
        switch (type)
        {
            case "ResolveOnSupportEffect":
            case "ReSetCardAtStage":
            case "ResolveOnArtEffect":
            case "ResolveOnCollabEffect":
            case "PickFromListThenGiveBacKFromHand": //hSD01-007
            case "SuporteEffectAttachEnergyIf":
            case "ContinueCurrentPlayerTurn":
            case "MainConditionedSummomResponse":
            case "SuporteEffectSummomIf":
                _DuelAction.playerID = PlayerInfo.PlayerID;
                jsonString = JsonConvert.SerializeObject(_DuelAction);
                _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, type, "", "", currentGameHigh, jsonString);
                LockGameFlow = false;
                break;
            case "Endturn":
                _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "MainEndturnRequest", "Endturn", "", currentGameHigh);
                _MatchConnection._DuelFieldData.currentGamePhase = GAMEPHASE.EndStep;
                EndTurnButton.SetActive(false);
                break;
            case "CheerChooseRequest":
                jsonString = JsonConvert.SerializeObject(_DuelAction);
                if (_MatchConnection._DuelFieldData.currentGamePhase == GAMEPHASE.HolomemDefeatedEnergyChoose)
                    _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "CheerChooseRequestHolomemDown", "", "", currentGameHigh, jsonString);
                if (_MatchConnection._DuelFieldData.currentGamePhase == GAMEPHASE.CheerStepChoose)
                    _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "CheerChooseRequest", "", "", currentGameHigh, jsonString);
                LockGameFlow = false;
                break;
            case "standart":
                _DuelAction.playerID = PlayerInfo.PlayerID;
                jsonString = JsonConvert.SerializeObject(_DuelAction);
                _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "MainDoActionRequest", "", "", currentGameHigh, jsonString);
                LockGameFlow = false;
                break;
        }
    }

    //DuelField_SelectableCardMenu CALL THIS WHEN WE HAVE SELECTED THE CARD WE WANT TO SUMMOM 
    public void AttachEnergyCallBack(string energyNumber)
    {

        LockGameFlow = false;

        string jsonString = JsonConvert.SerializeObject(energyNumber);
        _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "AskAttachEnergy", "", "", currentGameHigh, jsonString);
    }

    //DuelField_SelectableCardMenu CALL THIS WHEN WE HAVE SELECTED THE CARD WE WANT TO SUMMOM 
    public void SuporteEffectSummomIfCallBack(List<string> cards)
    {

        LockGameFlow = false;

        string jsonString = JsonConvert.SerializeObject(cards[0]);
        _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "MainConditionedSummomResponse", "", "", currentGameHigh, jsonString);
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
        _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "MainConditionedDrawResponse", "", "", currentGameHigh, jsonString);
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
        _ = new DuelFieldData();
        string jsonString = JsonConvert.SerializeObject(DataConverter.ConvertToSerializable(MapDuelFieldData(GameZones)));
        _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "DuelFieldReady", "0", jsonString, currentGameHigh);
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
        _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "AskForMulligan", "", "t", currentGameHigh);
        LockGameFlow = false;
    }

    public void MulliganBoxNoButton()
    {
        MulliganMenu.SetActive(false);
        _ = _MatchConnection.SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "AskForMulligan", "", "f", currentGameHigh);
        LockGameFlow = false;
    }


    //FUNCTIONS ASSIGNED IN THE INSPECTOR - END

    public void GetUsableCards(bool clearList = false)
    {
        List<Record> cList = new();
        if (!clearList) { 
            switch (_MatchConnection._DuelFieldData.currentGamePhase) {
                case GAMEPHASE.CheerStep:
                case GAMEPHASE.HolomemDefeated:
                    cList = FileReader.QueryRecordsByType(new List<string>() { "エール" });
                    break;
                case GAMEPHASE.MainStep:
                    cList = FileReader.result;
                    //cList = FileReader.QueryRecordsByType(new List<string>() { "ホロメン", "Buzzホロメン" });
                    break;
                case GAMEPHASE.SettingUpBoard:
                    if (_MatchConnection._DuelFieldData.firstPlayer == PlayerInfo.PlayerID) 
                    { 
                        cList = FileReader.QueryRecordsByNameAndBloom(CardListToStringList(_MatchConnection._DuelFieldData.playerAHand), "Debut");
                        cList.AddRange(FileReader.QueryRecordsByNameAndBloom(CardListToStringList(_MatchConnection._DuelFieldData.playerAHand), "Spot"));
                    }
                    if (_MatchConnection._DuelFieldData.firstPlayer != PlayerInfo.PlayerID) 
                    { 
                        cList = FileReader.QueryRecordsByNameAndBloom(CardListToStringList(_MatchConnection._DuelFieldData.playerBHand), "Debut");
                        cList.AddRange(FileReader.QueryRecordsByNameAndBloom(CardListToStringList(_MatchConnection._DuelFieldData.playerBHand), "Spot"));
                    }
                    break;
            }
        }

        HashSet<string> cardNumbers = new HashSet<string>(cList.Select(record => record.CardNumber));

        if (cList.Count > 0)
        {
            foreach (RectTransform r in cardsPlayer)
            {
                Card cardComponent = r.GetComponent<Card>();
                bool matchFound = cardNumbers.Contains(cardComponent.cardNumber);

                DuelField_HandDragDrop handDragDrop = r.GetComponent<DuelField_HandDragDrop>() ?? r.gameObject.AddComponent<DuelField_HandDragDrop>();

                handDragDrop.enabled = matchFound;
            }
        }
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
        ArrangeCards(cardsList, holder);
    }

    public void AddCardToGameZone(GameObject holder, List<Card> cardNumbers)
    {
        foreach (Card c in cardNumbers)
        {
            GameObject newObject = Instantiate(cardPrefab, Vector3.zero, Quaternion.identity);
            Card newCardInfo = newObject.GetComponent<Card>();
            newCardInfo.cardNumber = c.cardNumber;
            newCardInfo.cardPosition = holder.name;
            newCardInfo.GetCardInfo();
            newObject.transform.SetParent(holder.transform, false);
            newObject.transform.localPosition = Vector3.zero;
            newObject.transform.localScale = new Vector3(0.9f, 0.9f);
            UpdateHP(newCardInfo);

            if (holder.name.Equals("Life") || holder.name.Equals("CardCheer"))
                newObject.transform.Find("CardImage").GetComponent<UnityEngine.UI.Image>().sprite = Resources.Load<Sprite>("CardImages/001");

            GameObject father = holder;
            if (father.name.Equals("Deck") || father.name.Equals("CardCheer") || father.name.Equals("Life"))
            {
                Destroy(newObject.GetComponent<DuelField_HandDragDrop>());
                Destroy(newObject.GetComponent<DuelField_HandClick>());
                Destroy(newObject.GetComponent<Card>());
            }
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

    public void SendCardToZone(GameObject card, string zone, TargetPlayer player, bool toBack = false)
    {
        int maxZones = GameZones.Count;
        int nZones = 0;

        if (TargetPlayer.Oponnent == player)
            nZones = GameZones.Count / 2;

        if (TargetPlayer.Player == player)
            maxZones = GameZones.Count / 2;

        for (; nZones < maxZones; nZones++)
        {
            if (GameZones[nZones].name.Equals(zone))
            {
                _ = card.GetComponent<RectTransform>();
                card.transform.SetParent(GameZones[nZones].transform, false);
                if (toBack)
                    card.transform.SetSiblingIndex(0);
                card.transform.localPosition = Vector3.zero;
                card.transform.localScale = new Vector3(0.9f, 0.9f);
                card.transform.Find("HPBAR").gameObject.SetActive(false);
                if (card.transform.parent.name.Equals("Stage") || card.transform.parent.name.Equals("Collaboration") || card.transform.parent.name.Equals("BackStage1") || card.transform.parent.name.Equals("BackStage2") || card.transform.parent.name.Equals("BackStage3") || card.transform.parent.name.Equals("BackStage4") || card.transform.parent.name.Equals("BackStage5"))
                    UpdateHP(card.GetComponent<Card>());
            }
        }
    }


    public void AddCart(GameObject card, string zone, TargetPlayer player)
    {
        int maxZones = GameZones.Count;
        int nZones = 0;

        if (TargetPlayer.Oponnent == player)
            nZones = 9;

        if (TargetPlayer.Player == player)
            maxZones = 9;

        for (; nZones < maxZones; nZones++)
        {
            if (GameZones[nZones].name.Equals(zone))
            {
                card.transform.SetParent(GameZones[nZones].transform, false);
                card.transform.localPosition = Vector3.zero;
                card.transform.localScale = new Vector3(0.9f, 0.9f);
            }
        }
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
            newCard.GetCardInfo();
            newCard.playedThisTurn = false;
            cardsList.Add(newObject.GetComponent<RectTransform>());
            newObject.transform.SetParent(holder, false);
            newObject.transform.localPosition = Vector3.zero;
            if (holder.name.Equals("OponentHand"))
            {
                Destroy(newObject.GetComponent<DuelField_HandDragDrop>());
                Destroy(newObject.GetComponent<DuelField_HandClick>());
                Destroy(newObject.GetComponent<Card>());
            }
        }
        ArrangeCards(cardsList, holder);
    }
    public void RemoveCardFromZone(string zone, int amount)
    {
        GameObject game = GameObject.Find(zone);
        List<GameObject> cards = new();
        foreach (Transform child in game.transform)
            if (child.name == "Card(Clone)")
                cards.Add(child.gameObject);

        for (int i = 0; i < Mathf.Min(amount, cards.Count); i++)
        {
            GameObject card = cards[i];
            Destroy(card);
        }
    }
    public void RemoveCardFromZone(GameObject game, int amount)
    {
        List<GameObject> cards = new();
        foreach (Transform child in game.transform)
            if (child.name == "Card(Clone)")
                cards.Add(child.gameObject);

        for (int i = 0; i < Mathf.Min(amount, cards.Count); i++)
        {
            GameObject card = cards[i];
            Destroy(card);
        }
    }

    public void AddCardsToDeck(GameObject father, int amount, bool suffle)
    {
        // Loop to add the specified number of cards
        for (int i = 0; i < amount; i++)
        {
            if (cardPrefab != null)
            {
                // Instantiate a new card from the prefab
                GameObject newCard = Instantiate(cardPrefab);
                if (father.name.Equals("Deck") || father.name.Equals("CardCheer") || father.name.Equals("Life"))
                {
                    Destroy(newCard.GetComponent<DuelField_HandDragDrop>());
                    Destroy(newCard.GetComponent<DuelField_HandClick>());
                    Destroy(newCard.GetComponent<Card>());
                }
                // Set the new card's parent to be the "father" GameObject (deck)
                newCard.transform.SetParent(father.transform);

                // Optionally reset the new card's transform (local position, rotation, scale) to match the deck's setup
                newCard.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                newCard.transform.localScale = Vector3.one;

                // Optionally rename the card to differentiate it
                newCard.name = "Card(Clone)";

                if (newCard.transform.parent.name.Equals("Life") || newCard.transform.parent.name.Equals("CardCheer"))
                    newCard.transform.Find("CardImage").GetComponent<UnityEngine.UI.Image>().sprite = Resources.Load<Sprite>("CardImages/001");
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
    }


    public GameObject GetZone(string s, TargetPlayer player)
    {
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



    public List<Card> StringListToCardList(List<string> cards)
    {
        List<Card> returnCards = new();
        foreach (string s in cards)
        {
            Card card = new(s);
            returnCards.Add(card);
        }
        return returnCards;
    }

    public List<string> CardListToStringList(List<Card> cards)
    {
        List<string> returnCards = new();
        foreach (Card c in cards)
        {
            returnCards.Add(c.cardNumber);
        }
        return returnCards;
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
        int cardCount = c.Count;
        if (cardCount == 0) return;

        // Get the width of the card holder to calculate available space
        float holderWidth = cardHolder.rect.width;

        // Calculate the maximum spacing to fit all cards within the holder's width
        float maxSpacing = holderWidth / cardCount;

        // If space is enough, set normal spacing; otherwise, adjust spacing to allow overlap
        float spacing = Mathf.Min(cardWidth, maxSpacing);

        // Calculate the starting position to ensure the rightmost card respects the boundaries
        float startX = -(spacing * (cardCount - 1)) / 2f;

        for (int i = 0; i < cardCount; i++)
        {
            // Calculate each card's position starting from the left boundary and spreading outwards
            float xPos = startX + (spacing * i);

            // Ensure the rightmost card does not exceed the card holder's boundary
            float rightmostBoundary = holderWidth / 2f - cardWidth / 2f;
            xPos = Mathf.Min(xPos, rightmostBoundary);

            // Set the card's position relative to the card holder
            c[i].anchoredPosition = new Vector2(xPos, 0);

            // Ensure the card's local rotation is aligned with the parent
            c[i].localRotation = Quaternion.identity;
        }
        foreach (RectTransform eachHandCard in c)
        {
            if (eachHandCard.TryGetComponent<DuelField_HandDragDrop>(out var _DuelField_HandDragDrop))
                _DuelField_HandDragDrop.defaultValues = new RectTransformData(eachHandCard);
        }
    }

    public void ArrangeCardsCentered(List<RectTransform> c, RectTransform cardHolder)
    {
        int cardCount = c.Count;
        if (cardCount == 0) return;

        // Get the width of the card holder to calculate available space
        float holderWidth = cardHolder.rect.width;

        // Calculate spacing to spread all cards evenly from the leftmost to the rightmost edge
        float spacing = cardCount > 1 ? (holderWidth - cardWidth) / (cardCount - 1) : 0;

        // Calculate the starting position to align the first card to the left edge
        float startX = -holderWidth / 2f;

        for (int i = 0; i < cardCount; i++)
        {
            if (c[i].gameObject.name.Equals("Amount"))
                continue;

            // Calculate each card's position starting from the left edge and spreading outwards
            float xPos = startX + (spacing * i) + (cardWidth / 2f);

            // Set the card's position relative to the card holder
            c[i].anchoredPosition = new Vector2(xPos, 0);

            // Ensure the card's local rotation is aligned with the parent
            c[i].localRotation = Quaternion.identity;
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

        if (_MatchConnection._DuelFieldData.firstPlayer != PlayerInfo.PlayerID)
        {

            _DuelFieldData.playerBHand = new List<Card>();
            _DuelFieldData.playerBArquive = field[0].GetComponentsInChildren<Card>().ToList();
            _DuelFieldData.playerBDeck = new List<Card>();
            _DuelFieldData.playerBHoloPower = field[2].GetComponentsInChildren<Card>().ToList();
            _DuelFieldData.playerBBackPosition = field[3].GetComponentsInChildren<Card>().ToList();
            _DuelFieldData.playerBFavourite = field[4].GetComponentInChildren<Card>();
            _DuelFieldData.playerBStage = field[6].GetComponentInChildren<Card>();
            _DuelFieldData.playerBCollaboration = field[5].GetComponentInChildren<Card>();
            _DuelFieldData.playerBCardCheer = new List<Card>();
            _DuelFieldData.playerBLife = field[8].GetComponentsInChildren<Card>().ToList();

            _DuelFieldData.playerAHand = new List<Card>();
            _DuelFieldData.playerAArquive = field[9].GetComponentsInChildren<Card>().ToList();
            _DuelFieldData.playerADeck = new List<Card>();
            _DuelFieldData.playerAHoloPower = field[11].GetComponentsInChildren<Card>().ToList();
            _DuelFieldData.playerABackPosition = field[12].GetComponentsInChildren<Card>().ToList();
            _DuelFieldData.playerAFavourite = field[13].GetComponentInChildren<Card>();
            _DuelFieldData.playerAStage = field[15].GetComponentInChildren<Card>();
            _DuelFieldData.playerACollaboration = field[14].GetComponentInChildren<Card>();
            _DuelFieldData.playerACardCheer = new List<Card>();
            _DuelFieldData.playerALife = field[17].GetComponentsInChildren<Card>().ToList();

            return _DuelFieldData;
        }

        _DuelFieldData.playerAHand = new List<Card>();
        _DuelFieldData.playerAArquive = field[0].GetComponentsInChildren<Card>().ToList();
        _DuelFieldData.playerADeck = new List<Card>();
        _DuelFieldData.playerAHoloPower = field[2].GetComponentsInChildren<Card>().ToList();
        _DuelFieldData.playerABackPosition = field[3].GetComponentsInChildren<Card>().ToList();
        _DuelFieldData.playerAFavourite = field[4].GetComponentInChildren<Card>();
        _DuelFieldData.playerAStage = field[6].GetComponentInChildren<Card>();
        _DuelFieldData.playerACollaboration = field[5].GetComponentInChildren<Card>();
        _DuelFieldData.playerACardCheer = new List<Card>();
        _DuelFieldData.playerALife = field[8].GetComponentsInChildren<Card>().ToList();

        _DuelFieldData.playerBHand = new List<Card>();
        _DuelFieldData.playerBArquive = field[9].GetComponentsInChildren<Card>().ToList();
        _DuelFieldData.playerBDeck = new List<Card>();
        _DuelFieldData.playerBHoloPower = field[11].GetComponentsInChildren<Card>().ToList();
        _DuelFieldData.playerBBackPosition = field[12].GetComponentsInChildren<Card>().ToList();
        _DuelFieldData.playerBFavourite = field[13].GetComponentInChildren<Card>();
        _DuelFieldData.playerBStage = field[15].GetComponentInChildren<Card>();
        _DuelFieldData.playerBCollaboration = field[14].GetComponentInChildren<Card>();
        _DuelFieldData.playerBCardCheer = new List<Card>();
        _DuelFieldData.playerBLife = field[17].GetComponentsInChildren<Card>().ToList();

        return _DuelFieldData;
    }

    void AttachEnergyToTarget(DuelAction duelAction, TargetPlayer target)
    {
        GameObject cardZone = GetZone(duelAction.targetCard.cardPosition, target);

        GameObject usedCardGameObject = Instantiate(cardPrefab, Vector3.zero, Quaternion.identity);
        Card usedCardGameObjectCard = usedCardGameObject.GetComponent<Card>();
        usedCardGameObjectCard.cardNumber = duelAction.usedCard.cardNumber;
        usedCardGameObjectCard.GetCardInfo();

        //GETTING the father FOR the energy
        Card newObjectCard = cardZone.GetComponentInChildren<Card>();

        newObjectCard.attachedCards ??= new List<GameObject>();

        newObjectCard.attachedCards.Add(usedCardGameObject);

        usedCardGameObject.transform.SetParent(cardZone.transform, false);
        usedCardGameObject.transform.localPosition = Vector3.zero;
        usedCardGameObject.transform.localScale = new Vector3(0.9f, 0.9f);
        usedCardGameObject.SetActive(false);

        cardZone.GetComponentInChildren<Card>().transform.SetAsLastSibling();

       //need to make this comparisson better latter, comparing the last information send by the server may lead to errors 
        if (_MatchConnection.DuelActionListIndex.Last().Equals("CheerStepEndDefeatedHolomem"))
        {
            RemoveCardsFromCardHolder(1, cardsOponnent, cardHolderOponnent);
            return;
        }

        if (_MatchConnection._DuelFieldData.currentPlayerTurn == PlayerInfo.PlayerID)
            RemoveCardFromZone(GetZone("CardCheer", TargetPlayer.Player), 1);
        else
            RemoveCardFromZone(GetZone("CardCheer", TargetPlayer.Oponnent), 1);

    }
    /// <summary>
    /// Play Holomem
    /// </summary>
    private void HandleCardPlay(DuelAction duelAction, TargetPlayer targetPlayer, int currentPlayer)
    {
        GameObject cardZone = GetZone(duelAction.local, targetPlayer);
        GameObject usedCardGameObject = Instantiate(cardPrefab, Vector3.zero, Quaternion.identity);
        Card usedCardGameObjectCard = usedCardGameObject.GetComponent<Card>();
        usedCardGameObjectCard.cardNumber = duelAction.usedCard.cardNumber;
        usedCardGameObjectCard.GetCardInfo();

        if (string.IsNullOrEmpty(duelAction.usedCard.cardPosition))
            Debug.Log(duelAction.usedCard.cardNumber);

        SetupCardTransform(usedCardGameObject, cardZone);
        usedCardGameObject.SetActive(true);

        if (currentPlayer == PlayerInfo.PlayerID)
        {
            RemoveCardFromZone(GetZone("Deck", TargetPlayer.Player), 1);
        }
        else
        {
            RemoveCardsFromCardHolder(1, cardsOponnent, cardHolderOponnent);
        }
    }

    private void SetupCardTransform(GameObject card, GameObject parentZone)
    {
        card.transform.SetParent(parentZone.transform, false);
        card.transform.localPosition = Vector3.zero;
        card.transform.localScale = new Vector3(0.9f, 0.9f);
    }

    public void UpdateHP(Card card)
    {

        if (card.transform.parent.name.Equals("Favourite") || card.transform.parent.name.Equals("Deck") || card.transform.parent.name.Equals("CardCheer") || card.transform.parent.name.Equals("Life") || card.transform.parent.name.Equals("HoloPower") || card.transform.parent.name.Equals("Arquive"))
            return;

        card.transform.Find("HPBAR").gameObject.SetActive(true);
        card.transform.Find("HPBAR").Find("HPCurrent").GetComponent<TMP_Text>().text = card.currentHp.ToString();
        card.transform.Find("HPBAR").Find("HPMax").GetComponent<TMP_Text>().text = card.hp.ToString();
        if (card.transform.parent.parent.name.Equals("Oponente") || card.transform.parent.name.Equals("Oponente"))
        {
            card.transform.Find("HPBAR").Find("HPCurrent").localEulerAngles = new Vector3(0, 0, 180);
            card.transform.Find("HPBAR").Find("HPBar").localEulerAngles = new Vector3(0, 0, 180);
            card.transform.Find("HPBAR").Find("HPMax").localEulerAngles = new Vector3(0, 0, 180);
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

    public int CountBackStageTotal() {
        int count = 0;
        if (GetZone("BackStage1", TargetPlayer.Player).GetComponentInChildren<Card>() != null)
            count++;
        if (GetZone("BackStage2", TargetPlayer.Player).GetComponentInChildren<Card>() != null)
            count++;
        if (GetZone("BackStage3", TargetPlayer.Player).GetComponentInChildren<Card>() != null)
            count++;
        if (GetZone("BackStage4", TargetPlayer.Player).GetComponentInChildren<Card>() != null)
            count++;
        if (GetZone("BackStage5", TargetPlayer.Player).GetComponentInChildren<Card>() != null)
            count++;
        if (GetZone("Collaboration", TargetPlayer.Player).GetComponentInChildren<Card>() != null)
            count++;
        return count;
    }
}
