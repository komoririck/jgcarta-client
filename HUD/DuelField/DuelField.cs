using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static DuelField_UI_MAP;
using static DuelFieldData;

public class DuelField : MonoBehaviour
{
    public static DuelField INSTANCE;
    public static Lib.GameZone[] DEFAULTHOLOMEMZONE = new Lib.GameZone[] { Lib.GameZone.Stage, Lib.GameZone.Collaboration, Lib.GameZone.BackStage1, Lib.GameZone.BackStage2, Lib.GameZone.BackStage3, Lib.GameZone.BackStage4, Lib.GameZone.BackStage5 };

    public bool NeedsOrganize = false;

    private const int TURN_TIMER_SECONDS = 120;
    private int playerTimers;
    private CancellationTokenSource countdownTokenSource;
    [SerializeField] private TMP_Text TimmerText;
    [SerializeField] private TMP_Text TurnCounterText;

    [SerializeField] GameObject cardPrefab;
    [SerializeField] GameObject oldCardPrefab;

    PhaseMessage GamePhaseMsg;

    public RectTransform cardHolderPlayer;
    public RectTransform cardHolderOponnent;

    public RectTransform cardLifeHolderA;
    public RectTransform cardLifeHolderB;

    float sendCardToZoneAnimationTimming = 0.2f;

    [SerializeField] private GameObject MulliganMenu = null;
    [SerializeField] private GameObject ReadyButton = null;
    [SerializeField] private GameObject EndTurnButton = null;

    [SerializeField] private GameObject EffectConfirmationTab = null;
    [SerializeField] private GameObject EffectConfirmationYesButton = null;
    [SerializeField] private GameObject EffectConfirmationNoButton = null;

    private bool ReadyButtonShowed = false;
    bool startofmain = false;

    private int currentTurn;
    [SerializeField] int currentGameHigh = 0;

    public bool LockGameFlow = false;

    bool playerMulligan = false;
    bool oponnentMulligan = false;

    bool playerMulliganF = false;
    bool oponnentMulliganF = false;


    public bool centerStageArtUsed = false;
    public bool collabStageArtUsed = false;

    public bool usedSPOshiSkill = false;
    public bool usedOshiSkill = false;


    [SerializeField] List<GameObject> GameZones = new();
    public List<GameObject> GetGameZones() { return GameZones; }

    private bool InitialDraw = false;
    private bool InitialDrawP2 = false;

    [SerializeField] Sprite viewTypeActionImg;
    [SerializeField] Sprite viewTypeViewImg;

    private bool playerCannotDrawFromCheer;
    private int cheersAssignedThisChainTotal;
    private int cheersAssignedThisChainAmount;

    public GameObject CardEffectPanel = null;
    public GameObject ArtPanel = null;
    public GameObject OshiPowerPanel = null;

    public bool isViewMode = true;

    [Flags]
    public enum TargetPlayer : byte
    {
        Player = 0,
        Oponnent = 1
    }

    public DuelAction curResDA;
    string curResDAType;
    Dictionary<string, Func<IEnumerator>> actionHandlers;

    public bool isProcessing = false;
    public DuelFieldData duelFieldData;

    Button toggle = null;

    void Start()
    {
        INSTANCE = this;
        GamePhaseMsg = FindAnyObjectByType<PhaseMessage>();
        DuelField_UI_MAP.INSTANCE = FindAnyObjectByType<DuelField_UI_MAP>();
        SetActionToggleMode(ToggleStatus.on);

        var action = MatchConnection.INSTANCE.GetPendingActions();
        while (!action.Item1.Equals("mt"))
            action = MatchConnection.INSTANCE.GetPendingActions();

        ClearLogConsole();
    }
    void Update()
    {
        if (toggle == null)
        {
            if (toggle == null)
                toggle = DuelField_UI_MAP.INSTANCE.SS_UI_ActionToggleButton.GetComponent<Button>();

            if (toggle != null)
                toggle.onClick.AddListener(() => { SetActionToggleMode(ToggleStatus.flip); });
        }

        if (MatchConnection.INSTANCE == null)
        {
            SceneManager.LoadScene("Login");
            return;
        }

        if (actionHandlers == null)
            actionHandlers = MapActions();

        if (MatchConnection.INSTANCE.GetPendingActionsCount() > 0 && !isProcessing)
            StartCoroutine(ProcessActions());

        if (playerMulliganF && oponnentMulliganF && !ReadyButtonShowed)
        {
            DuelField_UI_MAP.INSTANCE.SS_BlockView.SetActive(false);
            DuelField_UI_MAP.INSTANCE.WS_ReadyButton.SetActive(true);
            ReadyButtonShowed = true;
        }
    }
    private IEnumerator ProcessActions()
    {
        if (isProcessing) yield break;
        isProcessing = true;

        while (MatchConnection.INSTANCE.GetPendingActionsCount() > 0)
        {
            var action = MatchConnection.INSTANCE.GetPendingActions();

            curResDA = action.Item2;
            curResDAType = action.Item1;

            if (curResDAType.Equals("StartDuel"))
                duelFieldData = curResDA.duelFieldData;

            if (!actionHandlers.TryGetValue(curResDAType, out Func<IEnumerator> handler))
            {
                Debug.LogWarning("Unhandled action: " + curResDAType);
                continue;
            }
            yield return handler();

            yield return new WaitForSeconds(1);

            if (!DuelField_HandDragDrop.IsDragging && NeedsOrganize)
            {
            }
            yield return OrganizeGameZone();

            yield return new WaitForSeconds(1);

            yield return new WaitUntil(() => !NeedsOrganize);
            yield return new WaitUntil(() => !LockGameFlow);
            currentGameHigh++;
        }
        isProcessing = false;
    }
    public Dictionary<string, Func<IEnumerator>> MapActions()
    {
        return new Dictionary<string, Func<IEnumerator>> {
                    { "StartDuel", HandleStartDuel },
                    { "InitialDraw", HandleInitialDraw },
                    { "InitialDrawP2", HandleInitialDraw },
                    { "PAMulligan", HandleMulligan },
                    { "PBMulligan", HandleMulligan },
                    { "PANoMulligan", HandleMulligan },
                    { "PBNoMulligan", HandleMulligan },
                    { "PAMulliganF", HandleMulliganForced },
                    { "PBMulliganF", HandleMulliganForced },
                    { "DuelUpdate", HandleDuelUpdate},
                    { "ResetStep", HandleResetStep },
                    { "ReSetStage", HandleReSetStage },
                    { "DrawPhase", HandleDrawPhase },
                    { "DefeatedHoloMember", HandleDefeatedHoloMember },
                    { "DefeatedHoloMemberByEffect", HandleDefeatedHoloMember },
                    { "HolomemDefatedSoGainCheer", HandleHolomemDefatedSoGainCheer },
                    { "CheerStepEndDefeatedHolomem", HandleCheerStepEndDefeatedHolomem },
                    { "CheerStep", HandleCheerStep },
                    { "CheerStepEnd", HandleCheerStepEnd },
                    { "MainPhase", HandleMainPhase },
                    { "MainPhaseDoAction", HandleMainPhaseDoAction },
                    { "Endturn", HandleEndturn },
                    { "Endduel", HandleEndduel },
                    { "AttachSupportItem", HandleAttachSupportItem },
                    { "PlayHolomem", HandlePlayHolomem },
                    { "BloomHolomem", HandleBloomHolomem },
                    { "DoCollab", HandleDoCollab },
                    { "UnDoCollab", HandleUnDoCollab },
                    { "RemoveEnergyFrom", HandleRemoveEnergyFrom },
                    { "AttachEnergyResponse", HandleAttachEnergyResponse },
                    { "PayHoloPowerCost", HandlePayHoloPowerCost },
                    { "MoveCardToZone", HandleMoveCardToZone },
                    { "DisposeUsedSupport", HandleDisposeUsedSupport },
                    { "ResolveOnSupportEffect", HandleResolveOnEffect },
                    { "OnCollabEffect", HandleResolveOnEffect },
                    { "OnArtEffect", HandleResolveOnEffect },
                    { "ResolveOnAttachEffect", HandleResolveOnEffect },
                    { "ActiveArtEffect", HandleActiveArtEffect },
                    { "PickFromListThenGiveBacKFromHandDone", HandlePickFromListThenGiveBackFromHandDone },
                    { "RemoveCardsFromArquive", HandleRemoveCardsFromArquive },
                    { "RemoveCardsFromHand", HandleRemoveCardsFromHand },
                    { "DrawOshiEffect", HandleDrawOshiEffect },
                    { "DrawBloomEffect", HandleDrawByEffect },
                    { "DrawBloomIncreaseEffect", HandleDrawByEffect },
                    { "DrawCollabEffect", HandleDrawByEffect },
                    { "DrawArtEffect", HandleDrawByEffect },
                    { "SupportEffectDraw", HandleDrawByEffect },
                    { "DrawAttachEffect", HandleDrawByEffect },
                    { "ShowCard", HandleShowCard },
                    { "RollDice", HandleShowCard },
                    { "OnlyDiceRoll", HandleOnlyDiceRoll },
                    { "RecoverHolomem", HandleRecoverHolomem },
                    { "InflicArtDamageToHolomem", HandleInflicArtDamageToHolomem },
                    { "InflicDamageToHolomem", HandleInflicDamageToHolomem },
                    { "InflicRecoilDamageToHolomem", HandleInflicRecoilDamageToHolomem },
                    { "SetHPToFixedValue", HandleResolveDamageToHolomem },
                    { "ResolveDamageToHolomem", HandleResolveDamageToHolomem },
                    { "SwitchStageCard", HandleSwitchStageCard },
                    { "SwitchStageCardByRetreat", HandleSwitchStageCard },
                    { "SwitchOpponentStageCard", HandleSwitchOpponentStageCard },
                    { "RemoveEnergyAtAndDestroy", HandleRemoveEnergyAtAndSendToArquive },
                    { "RemoveEnergyAtAndSendToArquive", HandleRemoveEnergyAtAndSendToArquive },
                    { "RemoveEquipAtAndSendToArquive", HandleRemoveEquipAtAndSendToArquive },
                    { "SuffleDeck", HandleSuffleDeck }
                };
    }
    ////////////////////////////////////////////////////////////////////////
    public void GenericActionCallBack(DuelAction _DuelAction, string type)
    {
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            Formatting = Formatting.Indented
        };

        switch (type)
        {
            case "MainEndturnRequest":
                _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "MainEndturnRequest", "Endturn");
                duelFieldData.currentGamePhase = GAMEPHASE.EndStep;
                EndTurnButton.SetActive(false);
                break;
            case "CheerChooseRequest":
                if (duelFieldData.currentGamePhase == GAMEPHASE.HolomemDefeatedEnergyChoose)
                    _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "CheerChooseRequestHolomemDown", "", _DuelAction);
                if (duelFieldData.currentGamePhase == GAMEPHASE.CheerStepChoose)
                    _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "CheerChooseRequest", "", _DuelAction);
                LockGameFlow = false;
                break;
            case "standart":
                _DuelAction.playerID = PlayerInfo.INSTANCE.PlayerID;
                _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "MainDoActionRequest", "", _DuelAction);
                LockGameFlow = false;
                break;
            default:
                _DuelAction.playerID = PlayerInfo.INSTANCE.PlayerID;
                _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, type, "", _DuelAction);
                LockGameFlow = false;
                break;
        }
    }
    public void AttachEnergyCallBack(string energyNumber)
    {
        DuelAction da = new DuelAction() { usedCard = new CardData() { cardNumber = energyNumber } };

        LockGameFlow = false;
        _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "AskAttachEnergy", "", da);
    }

    //DuelField_SelectableCardMenu CALL THIS WHEN WE HAVE SELECTED THE CARD WE WANT TO SUMMOM 
    public void SuporteEffectSummomIfCallBack(List<string> cards)
    {
        LockGameFlow = false;
        //da.obj = cards
        DuelAction da = new DuelAction() { };
        string jsonString = JsonConvert.SerializeObject(cards[0]);
        _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "MainConditionedSummomResponse", "", da);
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

        _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "MainConditionedDrawResponse", "", _ConditionedDraw);
    }
    //FUNCTIONS ASSIGNED IN THE INSPECTOR

    public void ReturnButton()
    {
        if (MatchConnection.INSTANCE._webSocket.State.Equals(WebSocketState.Open))
        {
            _ = MatchConnection.INSTANCE._webSocket.Close();
        }

        _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "EndDuel", "", null);
        Destroy(GameObject.Find("HUD DuelField"));
        Destroy(MatchConnection.INSTANCE = null);
        Destroy(this);
        SceneManager.LoadScene("Match");
    }

    public void DuelReadyButton()
    {
        if (GameZones[6].GetComponentInChildren<Card>() == null)
        {
            return;
        }
        ReadyButton.SetActive(false);
        DuelAction da = new DuelAction() { duelFieldData = DuelFieldData.MapDuelFieldData(GameZones) };
        _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "DuelFieldReady", "", da);
        LockGameFlow = false;
    }

    public void EndTurnHUDButton()
    {
        GenericActionCallBack(null, "MainEndturnRequest");
    }

    public void MulliganBoxYesButton()
    {
        DuelField_UI_MAP.INSTANCE.SetPanel(false, DuelField_UI_MAP.PanelType.SS_MulliganPanel);
        DuelField_UI_MAP.INSTANCE.SetPanel(true, DuelField_UI_MAP.PanelType.SS_OponentHand);
        DuelAction da = new DuelAction() { yesOrNo = true };
        _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "AskForMulligan", "", da);
        LockGameFlow = false;
    }

    public void MulliganBoxNoButton()
    {
        DuelField_UI_MAP.INSTANCE.SetPanel(false, DuelField_UI_MAP.PanelType.SS_MulliganPanel);
        DuelField_UI_MAP.INSTANCE.SetPanel(true, DuelField_UI_MAP.PanelType.SS_OponentHand);
        DuelAction da = new DuelAction() { yesOrNo = false };
        _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "AskForMulligan", "", da);
        LockGameFlow = false;
    }


    //FUNCTIONS ASSIGNED IN THE INSPECTOR - END

    public IEnumerator OrganizeGameZone()
    {
        foreach (var obj in GameZones)
        {
            if (obj.name.Equals("PlayerHand"))
            {
                yield return ArrangeCards(cardHolderPlayer);
                yield return GetUsableCards();
            }
            else if (obj.name.Equals("OponentHand"))
            {
                yield return ArrangeCards(cardHolderOponnent);
            }
            else if (obj.name.Equals(Lib.GameZone.Life.ToString()))
            {
                yield return ArrangeCards(obj.GetComponent<RectTransform>(), true);
            }
            else
            {
                yield return StackCardsEffect(obj);
            }
            cardCounter(obj);
        }

        foreach (var zone in GameZones)
        {
            var collider = zone.GetComponent<Collider>();
            if (collider != null)
            {
                if (zone.GetComponentsInChildren<Card>().Count() > 0)
                    collider.enabled = false;
                else
                    collider.enabled = true;
            }
        }

        void cardCounter(GameObject countObj)
        {
            GameObject gAmount = countObj.transform.Find("Amount")?.gameObject;
            if (gAmount != null)
            {
                gAmount.GetComponent<TMP_Text>().text = (countObj.transform.childCount - 1).ToString();
                gAmount.transform.SetAsLastSibling();
                if (gAmount.transform.parent.childCount > 2)
                    gAmount.transform.position = new Vector3(gAmount.transform.position.x, gAmount.transform.position.y, gAmount.transform.parent.GetChild(gAmount.transform.parent.childCount - 2).transform.position.z - 10f);
            }
        }

        NeedsOrganize = false;
    }

    public IEnumerator GetUsableCards(bool clearList = false)
    {
        if (!duelFieldData.currentPlayerTurn.Equals(PlayerInfo.INSTANCE.PlayerID) && !(duelFieldData.currentGamePhase == GAMEPHASE.SettingUpBoard || duelFieldData.currentGamePhase == GAMEPHASE.HolomemDefeatedEnergyChoose))
            clearList = true;

        if (!clearList)
        {
            var cards = cardHolderPlayer.GetComponentsInChildren<RectTransform>().Select(r => r.GetComponent<Card>()).Where(c => c != null).ToList();
            foreach (var cardComponent in cards)
            {
                DuelField_HandDragDrop handDragDrop = cardComponent.GetComponent<DuelField_HandDragDrop>()?? cardComponent.gameObject.AddComponent<DuelField_HandDragDrop>();
                bool enableDrag = false;
                switch (duelFieldData.currentGamePhase)
                {
                    case GAMEPHASE.HolomemDefeatedEnergyChoose:
                    case GAMEPHASE.CheerStepChoose:
                        if (cardComponent.cardType != null && cardComponent.cardType.Equals("エール"))
                            enableDrag = true;
                        break;

                    case GAMEPHASE.MainStep:
                        if (cardComponent.cardType == null)
                            break;

                        // Support cards
                        if (cardComponent.cardType.StartsWith("サポート"))
                        {
                            if (cardComponent.cardType.Contains("LIMITED"))
                            {
                                enableDrag = CheckForPlayRestrictions(cardComponent.cardNumber);
                            }
                            else if (cardComponent.cardType.Contains("ツール") || cardComponent.cardType.Contains("マスコット") || cardComponent.cardType.Contains("ファン"))
                            {
                                enableDrag = !HasRestrictionsToPlayEquipCheckField(cardComponent);
                            }
                            else
                            {
                                enableDrag = true;
                            }
                        }
                        // Holomem cards
                        else if (cardComponent.cardType.Equals("ホロメン") || cardComponent.cardType.Equals("Buzzホロメン"))
                        {
                            if (cardComponent.bloomLevel.Equals("Debut") || cardComponent.bloomLevel.Equals("Spot"))
                                enableDrag = true;

                            if (cardComponent.bloomLevel.Equals("1st") || cardComponent.cardNumber.Equals("hBP01-045"))
                            {
                                if (NamesThatCanBloom("Debut").Contains(cardComponent.cardName))
                                    enableDrag = true;
                            }

                            if (cardComponent.bloomLevel.Equals("2nd") || cardComponent.cardNumber.Equals("hBP01-045"))
                            {
                                if (NamesThatCanBloom("1st").Contains(cardComponent.cardName))
                                    enableDrag = true;
                            }
                        }
                        break;

                    case GAMEPHASE.SettingUpBoard:
                        if (cardComponent.bloomLevel != null && (cardComponent.bloomLevel.Equals("Debut") || cardComponent.bloomLevel.Equals("Spot")))
                            enableDrag = true;
                        break;
                }
                handDragDrop.enabled = enableDrag;
            }
        }
        yield break;
    }
    public bool CheckForPlayRestrictions(string cardNumber)
    {
        switch (cardNumber)
        {
            case "hSD01-016":
                var deckCount = GetZone(Lib.GameZone.Deck, TargetPlayer.Player).transform.childCount - 1;
                if (deckCount < 3)
                    return false;
                break;
            case "hSD01-021":
            case "hSD01-018":
            case "hBP01-111":
            case "hBP01-113":
                deckCount = GetZone(Lib.GameZone.Deck, TargetPlayer.Player).transform.childCount - 1;
                if (deckCount == 0)
                    return false;
                break;
            case "hBP01-109":
            case "hBP01-102":
                if (cardHolderPlayer.childCount > 6)
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
                var energyList = GetZone(Lib.GameZone.Arquive, TargetPlayer.Player).GetComponentsInChildren<Card>();
                foreach (Card card in energyList)
                {
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

    public bool HasRestrictionsToPlayEquipCheckField(Card card)
    {
        foreach (Card target in GameObject.Find("MatchField").transform.Find("Player").GetComponentsInChildren<Card>())
            if (HasRestrictionsToPlayEquip(card, target)
                && (target.curZone.Equals(Lib.GameZone.Stage)
                || target.curZone.Equals(Lib.GameZone.Collaboration)
                || target.curZone.Equals(Lib.GameZone.BackStage1)
                || target.curZone.Equals(Lib.GameZone.BackStage2)
                || target.curZone.Equals(Lib.GameZone.BackStage3)
                || target.curZone.Equals(Lib.GameZone.BackStage4)
                || target.curZone.Equals(Lib.GameZone.BackStage5))
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

        playerAttachments.AddRange(GetZone(Lib.GameZone.Stage, TargetPlayer.Player).GetComponentInChildren<Card>().attachedEquipe);
        playerAttachments.AddRange(GetZone(Lib.GameZone.Stage, TargetPlayer.Player).GetComponentInChildren<Card>().attachedEquipe);

        for (int i = 1; i <= 5; i++)
            playerAttachments.AddRange(GetZone((Lib.GameZone)Enum.Parse(typeof(Lib.GameZone), $"BackStage{i}"), TargetPlayer.Player).GetComponentInChildren<Card>().attachedEquipe);

        foreach (GameObject cardObj in playerAttachments)
        {
            Card card = cardObj.GetComponentInChildren<Card>();
            if (card.cardNumber.Equals(cardNumber))
                return true;
        }
        return false;
    }



    public List<string> NamesThatCanBloom(string level)
    {
        List<string> validNames = new();

        Card BackStage1Card = GetZone(Lib.GameZone.BackStage1, TargetPlayer.Player).transform.GetComponentInChildren<Card>();
        Card BackStage2Card = GetZone(Lib.GameZone.BackStage2, TargetPlayer.Player).transform.GetComponentInChildren<Card>();
        Card BackStage3Card = GetZone(Lib.GameZone.BackStage3, TargetPlayer.Player).transform.GetComponentInChildren<Card>();
        Card BackStage4Card = GetZone(Lib.GameZone.BackStage4, TargetPlayer.Player).transform.GetComponentInChildren<Card>();
        Card BackStage5Card = GetZone(Lib.GameZone.BackStage5, TargetPlayer.Player).transform.GetComponentInChildren<Card>();
        Card CollaborationCard = GetZone(Lib.GameZone.Collaboration, TargetPlayer.Player).transform.GetComponentInChildren<Card>();
        Card StageCard = GetZone(Lib.GameZone.Stage, TargetPlayer.Player).transform.GetComponentInChildren<Card>();

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
    public List<GameObject> GetChildrenWithName(GameObject parent, string name)
    {
        List<GameObject> matchingChildren = new();

        foreach (Transform child in parent.transform)
        {
            if (child.gameObject.name == name)
            {
                matchingChildren.Add(child.gameObject);
            }
            matchingChildren.AddRange(GetChildrenWithName(child.gameObject, name));
        }

        return matchingChildren;
    }
    public IEnumerator AddOrMoveCardToGameZone(List<CardData> cardsToBeCreated, List<GameObject> cardsToBeMoved, TargetPlayer player, bool toBottom, bool shuffle)
    {
        yield return AddOrMoveCardToGameZone_Internal(cardsToBeCreated, cardsToBeMoved, player, toBottom, shuffle);
    }
    public IEnumerator AddOrMoveCardToGameZone(Lib.GameZone newHolder, Lib.GameZone oldHolder, int amount, TargetPlayer player, bool toBottom, bool shuffle)
    {
        List<CardData> cardsToBeCreated = new();
        for (int i = 0; i < amount; i++)
        {
            cardsToBeCreated.Add(new CardData() { curZone = newHolder, lastZone = oldHolder });
        }
        yield return AddOrMoveCardToGameZone_Internal(cardsToBeCreated, null, player, toBottom, shuffle);
    }
    public IEnumerator AddOrMoveCardToGameZone_Internal(List<CardData> cardsToBeCreated, List<GameObject> cardsToBeMoved, TargetPlayer player, bool toBottom = false, bool shuffle = false)
    {
        // CREATE
        GameObject newHolder;
        GameObject oldHolder;

        cardsToBeMoved ??= new List<GameObject>();
        if (cardsToBeCreated != null)
        {
            foreach (CardData cardDataGeneric in cardsToBeCreated)
            {
                newHolder = GetZone(cardDataGeneric.curZone, player);
                oldHolder = GetZone(cardDataGeneric.lastZone, player);

                GameObject obj = Instantiate(GetCardPrefab(newHolder.name), Vector3.zero, Quaternion.identity);
                // if (cardPrefab == GetCardPrefab(newHolder.name))
                //   obj.transform.Sca = new Vector3(3.3f, 3.3f, 3.3f);

                obj.name = "Card";
                Card cardInfo = obj.GetComponent<Card>().Init(cardDataGeneric);
                cardInfo.SetCardArt((newHolder.name.Equals("CardCheer") || newHolder.name.Equals("Life") || oldHolder.name.Equals("CardCheer")) ? Lib.GameZone.CardCheer : 0);

                cardInfo.curZone = GetZoneByString(newHolder.name);
                cardsToBeMoved.Add(obj);

            }
        }
        // Prepare MOVE
        List<GameObject> allCardsToMove = new();
        foreach (GameObject obj in cardsToBeMoved)
        {
            Card card = obj.GetComponent<Card>();
            allCardsToMove.AddRange(card?.attachedEnergy);
            allCardsToMove.AddRange(card?.attachedEquipe);
        }
        allCardsToMove.AddRange(cardsToBeMoved);
        // MOVE
        //bool isPlayer = (oldHolder.name.Equals("PlayerHand") || oldHolder.transform.parent.name.Equals("PlayerGeneral")) ? true : false;
        //bool isPlayedFromHand = oldHolder.name.Equals("PlayerHand") || oldHolder.name.Equals("OponentHand");
        foreach (GameObject obj in allCardsToMove)
        {

            if (!obj.name.Contains("Card")) continue;
            Card card = obj.GetComponent<Card>();

            newHolder = GetZone(card.curZone, player);
            oldHolder = GetZone(card.lastZone, player);

            bool isPlayer = player.Equals(TargetPlayer.Player);
            bool isPlayedFromHand = card.lastZone.Equals(Lib.GameZone.Hand) || card.lastZone.Equals(Lib.GameZone.CardCheer);

            obj.transform.localScale *= 0.9f;

            if (toBottom) card.transform.SetSiblingIndex(0);
            card.transform.SetParent(newHolder.transform, true);
            card.ScaleToFather().Flip(true);

            if ((newHolder.name == Lib.GameZone.Deck.ToString() || newHolder.name == Lib.GameZone.CardCheer.ToString() || newHolder.name == Lib.GameZone.Life.ToString() || newHolder.name.Equals("OponentHand") || string.IsNullOrEmpty(card?.cardNumber)))
            {
                Destroy(obj.GetComponent<DuelField_HandClick>());
                Destroy(card);
            }

            Transform hpBar = obj.transform.Find("HPBAR");
            if (hpBar != null)
                UpdateHP(card);

            if (curResDAType.Equals("StartDuel"))
                StartCoroutine(HandleAnimateCardMovement(obj.transform, oldHolder.transform, newHolder.transform, isPlayedFromHand, isPlayer));
            else
                yield return HandleAnimateCardMovement(obj.transform, oldHolder.transform, newHolder.transform, isPlayedFromHand, isPlayer);

            NeedsOrganize = true;
        }
    }
    public Lib.GameZone GetZoneByString(string name)
    {
        if ((name.Equals("PlayerHand") || name.Equals("OponentHand") || name.Equals("PlayerGeneral")))
            return Lib.GameZone.Hand;

        try { return (Lib.GameZone)Enum.Parse(typeof(Lib.GameZone), name); } catch (Exception e) { Debug.Log(e); }
        return 0;
    }
    private IEnumerator HandleAnimateCardMovement(Transform cardTransform, Transform initialZone, Transform targetZone, bool isPlayedFromHand, bool isPlayer)
    {
        yield return AnimateCardMovement(cardTransform, initialZone, targetZone, isPlayedFromHand, isPlayer);
    }
    private IEnumerator AnimateCardMovement(Transform cardTransform, Transform initialZone, Transform targetZone, bool isPlayedFromHand = false, bool isPlayer = true)
    {
        float duration = 0.10f;
        float handEntryOffset = 1200f;

        Vector3 endPos = targetZone.TransformPoint(Vector3.zero);
        Vector3 startPos;

        if (isPlayedFromHand)
            startPos = initialZone != null ? initialZone.TransformPoint(Vector3.zero) : endPos;
        else
            startPos = endPos + ((isPlayer ? Vector3.up : Vector3.down) * handEntryOffset);

        cardTransform.position = startPos;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            if (cardTransform != null)
                cardTransform.position = Vector3.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (cardTransform != null)
            cardTransform.position = endPos;
        //Debug.LogError($"AnimateCardMovement {curResDAType}\n LOG:\n" + $"initialZone={(initialZone ? initialZone.name : y "NULL")}\n" + $"targetZone={(targetZone ? targetZone.name : "NULL")}\n");
        else
            Debug.LogError($"AnimateCardMovement {curResDAType}\n Error:\n" + $"initialZone={(initialZone ? initialZone.name : "NULL")}\n" + $"targetZone={(targetZone ? targetZone.name : "NULL")}\n");


    }
    public void DrawCard(DuelAction draw)
    {
        var player = (draw.playerID == PlayerInfo.INSTANCE.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent;

        if (draw.suffleHandBackToDeck)
        {
            StartCoroutine(AddOrMoveCardToGameZone(Lib.GameZone.Deck, Lib.GameZone.Hand, GetZone(Lib.GameZone.Hand, player).transform.childCount, player, false, true));
            RemoveCardFromZone(GetZone(Lib.GameZone.Hand, player), GetZone(Lib.GameZone.Hand, player).transform.childCount);
        }

        StartCoroutine(AddOrMoveCardToGameZone(draw.cardList, (List<GameObject>)null, player, draw.toBottom, draw.suffle));
        RemoveCardFromZone(GetZone(draw.cardList[0].lastZone, player), draw.cardList.Count);
    }
    public void RemoveCardFromZone(GameObject father, int amount)
    {
        List<GameObject> cards = new();

        Transform[] all = father.GetComponentsInChildren<Transform>();
        foreach (Transform child in all)
            if (child.name == "Card")
                cards.Add(child.gameObject);

        for (int i = 0; i < Mathf.Min(amount, cards.Count); i++)
        {
            GameObject card = cards[i];
            Destroy(card);
        }

        NeedsOrganize = true;
    }
    public IEnumerator StackCardsEffect(GameObject father)
    {
        if (father.name.Equals(Lib.GameZone.Life))
            yield break;

        int childCount = father.transform.childCount;

        if (!(father.transform.parent.name.Equals("Oponente") || !father.transform.parent.name.Equals("Player")))
            yield break;

        for (int n = childCount; n > 0; n--)
        {
            Transform child = father.transform.GetChild(n - 1);
            if (!child.name.Equals("Card"))
                continue;

            Vector3 newPos = child.localPosition;
            newPos.z = -1f * n;
            child.localPosition = newPos;
        }
    }
    public GameObject GetZone(Lib.GameZone s, TargetPlayer player)
    {
        if (s.Equals(Lib.GameZone.Hand))
        {
            return (TargetPlayer.Player == player ? cardHolderPlayer.gameObject : cardHolderOponnent.gameObject);
        }

        if (s.Equals(Lib.GameZone.na))
            s = Lib.GameZone.Deck;


        int maxZones = GameZones.Count;
        int nZones = 0;

        if (TargetPlayer.Oponnent == player)
            nZones = GameZones.Count / 2;

        if (TargetPlayer.Player == player)
            maxZones = GameZones.Count / 2;

        for (; nZones < maxZones; nZones++)
        {
            if (GameZones[nZones].name.Equals(s.ToString()))
            {
                return GameZones[nZones];
            }
        }
        Debug.Log("No zone found");
        return GameZones[0];
    }
    void ResetCardTurnStatusForPlayer(TargetPlayer t)
    {
        // i dont have a clue why we need this now
        HashSet<string> skipZoneNames = new HashSet<string>
        {
            "Favourite",
            "Deck",
            "Arquive",
            "Life",
            "CardCheer",
            "HoloPower"
        };

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
                    cardComponent.transform.rotation = Quaternion.identity;
                }
            }
        }
    }
    public IEnumerator ArrangeCards(RectTransform cardHolder, bool IDONTKNOWMATH = false)
    {
        var cards = cardHolder.GetComponentsInChildren<RectTransform>().ToList();
        cards.RemoveAll(item => !item.name.Equals("Card"));

        if (cards.Count == 0) yield break;

        RectTransform firstCard = cards[0];
        float cardWidth = firstCard.rect.width * firstCard.localScale.x;
        float holderWidth = cardHolder.rect.width;

        float totalWidth = cardWidth * cards.Count;
        float spacing = cardWidth;

        holderWidth = (IDONTKNOWMATH ? holderWidth - cardWidth : holderWidth);

        if (totalWidth > holderWidth)
        {
            spacing = holderWidth / cards.Count;
        }

        float totalCardsWidth = spacing * cards.Count;

        float startX = -totalCardsWidth / 2 + spacing / 2;

        for (int i = 0; i < cards.Count; i++)
        {
            float xPos = startX + spacing * i;
            cards[i].localPosition = new Vector3(xPos, firstCard.localPosition.y, firstCard.localPosition.z); // 0.01f * i);
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
    public enum ToggleStatus : byte
    {
        off = 0,
        on = 1,
        flip = 2
    }
    public void SetActionToggleMode(ToggleStatus status)
    {
        if (status == ToggleStatus.off)
            isViewMode = false;
        else if (status == ToggleStatus.on)
            isViewMode = true;
        else
            isViewMode = !isViewMode;

        Image img = DuelField_UI_MAP.INSTANCE.SS_UI_ActionToggleButton.GetComponent<Image>();

        if (isViewMode)
            img.sprite = viewTypeViewImg;
        else
            img.sprite = viewTypeActionImg;
    }
    GameObject GetCardPrefab(string target)
    {
        if (target.Equals("PlayerHand") || target.Equals("OponentHand"))
            return oldCardPrefab;
        else
            return cardPrefab;
    }
    void AttachCardToTarget(DuelAction duelAction, TargetPlayer target, bool bottomOfStack = false)
    {
        GameObject cardZone = GetZone(duelAction.targetCard.curZone, target);

        GameObject usedCardGameObject = Instantiate(GetCardPrefab(cardZone.name), Vector3.zero, Quaternion.identity);
        usedCardGameObject.name = "Card";
        Card usedCardGameObjectCard = usedCardGameObject.GetComponent<Card>().Init(duelAction.usedCard);

        //GETTING the father FOR
        Card newObjectCard = cardZone.GetComponentInChildren<Card>();

        bool isPlayer = usedCardGameObject != null && (usedCardGameObject.name.Equals("OponentHand") || (usedCardGameObject.transform.parent != null && usedCardGameObject.transform.parent.name.Equals("OponenteGeneral")));
        bool isPlayedFromHand = usedCardGameObject.name.Equals("PlayerHand") || usedCardGameObject.name.Equals("OponentHand");

        StartCoroutine(HandleAnimateCardMovement(usedCardGameObject.transform, GetZone(Lib.GameZone.Hand, target).transform, cardZone.transform, isPlayedFromHand, isPlayer));

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
        usedCardGameObject.transform.localScale = new Vector3(usedCardGameObject.transform.localScale.x * 0.9f, usedCardGameObject.transform.localScale.y * 0.9f, 1f);

        //usedCardGameObject.SetActive(false);

        if (!duelFieldData.currentPlayerTurn.Equals(PlayerInfo.INSTANCE.PlayerID))
        {
            RemoveCardFromZone(cardHolderOponnent.gameObject, 1);
        }
        else
        {
            if (!usedCardGameObjectCard.cardType.Equals("エール"))
                EffectController.INSTANCE.ResolveOnAttachEffect(duelAction);
        }

        if (bottomOfStack)
            cardZone.GetComponentInChildren<Card>().transform.SetAsLastSibling();

        usedCardGameObject.SetActive(false);

        NeedsOrganize = true;
    }
    void RemoveCardFromPosition(DuelAction duelAction)
    {
        //need to make this comparisson better latter, comparing the last information send by the server may lead to errors 
        if (curResDAType.Equals("CheerStepEndDefeatedHolomem"))
        {
            RemoveCardFromZone(cardHolderOponnent.gameObject, 1);
            return;
        }
        else if (duelAction.usedCard.curZone.Equals(Lib.GameZone.Arquive))
        {
            GameObject ZoneToRemove = GetZone(Lib.GameZone.Arquive, duelFieldData.currentPlayerTurn.Equals(PlayerInfo.INSTANCE.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent);

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
            RemoveCardFromZone(GetZone(Lib.GameZone.CardCheer, duelFieldData.currentPlayerTurn.Equals(PlayerInfo.INSTANCE.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent), 1);
        }
    }
    public void UpdateHP(Card card)
    {
        if (card.transform == null)
            return;

        if (card.transform.parent == null)
            return;

        if (card.transform.parent.name.Equals(Lib.GameZone.Favourite.ToString()) || card.transform.parent.name.Equals(Lib.GameZone.Deck.ToString())
            || card.transform.parent.name.Equals(Lib.GameZone.CardCheer.ToString()) || card.transform.parent.name.Equals(Lib.GameZone.Life.ToString())
            || card.transform.parent.name.Equals(Lib.GameZone.HoloPower.ToString()) || card.transform.parent.name.Equals(Lib.GameZone.Arquive.ToString()))
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
    public Card GetLastCardInZone(Lib.GameZone zoneName, DuelField.TargetPlayer targetPlayer)
    {
        var zone = GetZone(zoneName, targetPlayer);
        if (zone.transform.childCount > 0)
        {
            var card = zone.transform.GetChild(zone.transform.childCount - 1).GetComponent<Card>();
            return card != null ? card : null;
        }
        return null;
    }
    public void PopulateSelectableCards(TargetPlayer target, Lib.GameZone[] zoneNames, GameObject holder, List<CardData> SelectableCards)
    {
        GameObjectExtensions.DestroyAllChildren(holder);
        SelectableCards.Clear();
        foreach (var zoneName in zoneNames)
        {
            var existingCard = GetLastCardInZone(zoneName, target);
            if (existingCard != null)
            {

                SelectableCards.Add(existingCard.ToCardData());
            }
        }
    }
    public int CountBackStageTotal(bool onlyBackstage = false, TargetPlayer target = TargetPlayer.Player)
    {
        int count = 0;
        if (GetZone(Lib.GameZone.BackStage1, target).GetComponentInChildren<Card>() != null)
            count++;
        if (GetZone(Lib.GameZone.BackStage2, target).GetComponentInChildren<Card>() != null)
            count++;
        if (GetZone(Lib.GameZone.BackStage3, target).GetComponentInChildren<Card>() != null)
            count++;
        if (GetZone(Lib.GameZone.BackStage4, target).GetComponentInChildren<Card>() != null)
            count++;
        if (GetZone(Lib.GameZone.BackStage5, target).GetComponentInChildren<Card>() != null)
            count++;
        if (!onlyBackstage)
        {
            if (GetZone(Lib.GameZone.Collaboration, target).GetComponentInChildren<Card>() != null)
                count++;
        }
        return count;
    }
    IEnumerator HandleStartDuel()
    {
        yield return AddOrMoveCardToGameZone(Lib.GameZone.Deck, Lib.GameZone.Deck, 50, TargetPlayer.Player, false, false);
        yield return AddOrMoveCardToGameZone(Lib.GameZone.Deck, Lib.GameZone.Deck, 50, TargetPlayer.Oponnent, false, false);


        yield return AddOrMoveCardToGameZone(Lib.GameZone.CardCheer, Lib.GameZone.CardCheer, 20, TargetPlayer.Player, false, false);
        yield return AddOrMoveCardToGameZone(Lib.GameZone.CardCheer, Lib.GameZone.CardCheer, 20, TargetPlayer.Oponnent, false, false);

        yield return AddOrMoveCardToGameZone(Lib.GameZone.Favourite, Lib.GameZone.Favourite, 1, TargetPlayer.Player, false, false);
        yield return AddOrMoveCardToGameZone(Lib.GameZone.Favourite, Lib.GameZone.Favourite, 1, TargetPlayer.Oponnent, false, false);

        duelFieldData.currentGamePhase = GAMEPHASE.InitialDraw;
        GamePhaseMsg.gameObject.SetActive(true);
        GamePhaseMsg.StartMessage("Starting Duel");
        currentGameHigh = 1;

        StartTurnCounter();
        TurnCounterText.text = currentTurn.ToString();

        yield break;
    }
    IEnumerator HandleInitialDraw()
    {
        if (InitialDraw && curResDAType.Equals("InitialDraw"))
           yield break;

        if (InitialDrawP2 && !curResDAType.Equals("InitialDraw"))
            yield break;

        if (curResDAType.Equals("InitialDraw"))
            InitialDraw = true;
        else
            InitialDrawP2 = true;

        DrawCard(curResDA);

        if (InitialDraw && InitialDrawP2)
        {
            DuelField_UI_MAP.INSTANCE.SetPanel(true, DuelField_UI_MAP.PanelType.SS_MulliganPanel);
            DuelField_UI_MAP.INSTANCE.SetPanel(false, DuelField_UI_MAP.PanelType.SS_OponentHand);
            duelFieldData.currentGamePhase = GAMEPHASE.Mulligan;
            LockGameFlow = true;
        }
    }
    IEnumerator HandleMulligan()
    {
        var player = (curResDA.playerID.Equals(PlayerInfo.INSTANCE.PlayerID)) ? TargetPlayer.Player : TargetPlayer.Oponnent;

        switch (curResDAType)
        {
            case "PAMulligan":
            case "PBMulligan":
                if (!string.IsNullOrEmpty(curResDA.actionObject))
                    if (curResDA.actionObject.Equals("True"))
                    {
                        RemoveCardFromZone(GetZone(Lib.GameZone.Hand, player), curResDA.cardList.Count);
                        yield return AddOrMoveCardToGameZone(Lib.GameZone.Hand, Lib.GameZone.Deck, 7, player, false, false);
                        DrawCard(curResDA);
                    }
                break;
            case "PBNoMulligan":
            case "PANoMulligan":
                break;
        }

        if (player.Equals(TargetPlayer.Player))
            playerMulligan = true;
        else
            oponnentMulligan = true;

        if (playerMulligan && oponnentMulligan)
            duelFieldData.currentGamePhase = GAMEPHASE.ForcedMulligan;

        yield break;
    }
    IEnumerator HandleMulliganForced()
    {
        var player = (curResDA.playerID.Equals(PlayerInfo.INSTANCE.PlayerID)) ? TargetPlayer.Player : TargetPlayer.Oponnent;

        if (!string.IsNullOrEmpty(curResDA.actionObject))
            if (curResDA.actionObject.Equals("True"))
            {
                RemoveCardFromZone(GetZone(Lib.GameZone.Deck, player), 7);
                yield return AddOrMoveCardToGameZone(Lib.GameZone.Hand, Lib.GameZone.Deck, 7, player, false, false);
                DrawCard(curResDA);
            }

        if (player.Equals(TargetPlayer.Player))
            playerMulliganF = true;
        else
            oponnentMulliganF = true;

        if (playerMulliganF && oponnentMulliganF)
        {
            duelFieldData.currentGamePhase = GAMEPHASE.SettingUpBoard;
            LockGameFlow = true;
        }
    }
    IEnumerator HandleDuelUpdate()
    {
        DuelFieldData boardinfo = curResDA.duelFieldData;

        RemoveCardFromZone(GetZone(Lib.GameZone.Favourite, TargetPlayer.Player), 1);
        RemoveCardFromZone(GetZone(Lib.GameZone.Favourite, TargetPlayer.Oponnent), 1);

        bool isMyTurn = duelFieldData.currentPlayerTurn.Equals(PlayerInfo.INSTANCE.PlayerID);

        var myLife = isMyTurn ? boardinfo.playerALife : boardinfo.playerBLife;
        var oppLife = isMyTurn ? boardinfo.playerBLife : boardinfo.playerALife;

        var myFav = isMyTurn ? boardinfo.playerAFavourite : boardinfo.playerBFavourite;
        var oppFav = isMyTurn ? boardinfo.playerBFavourite : boardinfo.playerAFavourite;

        var myStage = isMyTurn ? boardinfo.playerAStage : boardinfo.playerBStage;
        var oppStage = isMyTurn ? boardinfo.playerBStage : boardinfo.playerAStage;

        var myBack = isMyTurn ? boardinfo.playerABackPosition : boardinfo.playerBBackPosition;
        var oppBack = isMyTurn ? boardinfo.playerBBackPosition : boardinfo.playerABackPosition;

        yield return AddOrMoveCardToGameZone(myLife, (List<GameObject>)null, TargetPlayer.Player, false, false);
        yield return AddOrMoveCardToGameZone(oppLife, (List<GameObject>)null, TargetPlayer.Oponnent, false, false);
        yield return AddOrMoveCardToGameZone(new List<CardData>() { myFav }, (List<GameObject>)null, TargetPlayer.Player, false, false);
        yield return AddOrMoveCardToGameZone(new List<CardData>() { oppFav }, (List<GameObject>)null, TargetPlayer.Oponnent, false, false);

        //No me stage since we drop direct from drag and drop
        yield return AddOrMoveCardToGameZone(new List<CardData>() { oppStage }, (List<GameObject>)null, TargetPlayer.Oponnent, false, false);

        //No me backstage since we drop direct from drag and drop
        for (int n = 0; n < oppBack.Count; n++)
            yield return AddOrMoveCardToGameZone(oppBack, (List<GameObject>)null, TargetPlayer.Oponnent, false, false);

        int removeAmount = oppBack.Count + 1;

        if (isMyTurn)
            MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "DrawRequest", "AskDrawPhase", null);

        StartTurnCounter();
        TurnCounterText.text = currentTurn.ToString();

        duelFieldData.currentGamePhase = GAMEPHASE.DrawStep;
        yield break;
    }
    IEnumerator HandleResetStep()
    {
        StartTurnCounter();
        TurnCounterText.text = currentTurn++.ToString();

        var player = (curResDA.playerID == PlayerInfo.INSTANCE.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent;

        ResetCardTurnStatusForPlayer(player);

        HandleUnDoCollab();

        if (duelFieldData.currentPlayerTurn.Equals(PlayerInfo.INSTANCE.PlayerID))
        {
            if (GetZone(Lib.GameZone.Stage, TargetPlayer.Player).transform.GetComponentInChildren<Card>() != null)
            {
                duelFieldData.currentGamePhase = GAMEPHASE.DrawStep;
                _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "DrawRequest", "DrawRequest", null);
            }
            else
            {
                GamePhaseMsg.gameObject.SetActive(true);
                GamePhaseMsg.StartMessage("Select a new stage member");
                duelFieldData.currentGamePhase = GAMEPHASE.ResetStepReSetStage;
                LockGameFlow = true;
            }
        }
        else
        {
            duelFieldData.currentGamePhase = (GetZone(Lib.GameZone.Stage, TargetPlayer.Player).transform.GetComponentInChildren<Card>() != null) ? GAMEPHASE.DrawStep : GAMEPHASE.ResetStepReSetStage;
        }

        GamePhaseMsg.gameObject.SetActive(true);
        GamePhaseMsg.StartMessage("Reset Step");
        yield break;
    }
    IEnumerator HandleReSetStage()
    {
        if (duelFieldData.currentGamePhase != GAMEPHASE.ResetStepReSetStage)
        {
            throw new Exception("not in the right gamephase, we're at " + curResDAType + " and tried to enter at" + duelFieldData.currentGamePhase.GetType());
        }

        TargetPlayer player = PlayerInfo.INSTANCE.PlayerID == curResDA.playerID ? TargetPlayer.Player : TargetPlayer.Oponnent;

        if (curResDA.usedCard != null)
        {
            var cards = GetZone(curResDA.usedCard.curZone, player).GetComponentsInChildren<RectTransform>().GameObjectInChildren();

            List<GameObject> cardsToBeMoved = new();
            foreach (var card in cards)
            {
                card.transform.Rotate(0, 0, 0);
                var cardComp = card.GetComponent<Card>();
                cardComp.suspended = false;
                cardComp.curZone = curResDA.usedCard.curZone;
                cardComp.lastZone = curResDA.usedCard.lastZone;
                cardsToBeMoved.Add(card);
            }
            yield return AddOrMoveCardToGameZone(null, cardsToBeMoved, player, false, false);

        }

        duelFieldData.currentGamePhase = GAMEPHASE.DrawStep;

        if (duelFieldData.currentPlayerTurn.Equals(PlayerInfo.INSTANCE.PlayerID))
        {
            _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "DrawRequest", "DrawRequest", null);
        }
        yield break;
    }
    IEnumerator HandleDrawPhase()
    {
        DrawCard(curResDA);

        if (duelFieldData.currentPlayerTurn.Equals(PlayerInfo.INSTANCE.PlayerID))
            _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "CheerRequest", "AskNextPhase", null);
        duelFieldData.currentGamePhase = GAMEPHASE.CheerStep;

        GamePhaseMsg.gameObject.SetActive(true);
        GamePhaseMsg.StartMessage("Draw Step");
        yield break;
    }
    IEnumerator HandleDefeatedHoloMember()
    {
        TargetPlayer player = curResDA.playerID == PlayerInfo.INSTANCE.PlayerID ? TargetPlayer.Player : TargetPlayer.Oponnent;
        GameObject zoneArt = GetZone(curResDA.targetCard.curZone, player);

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
            var zone = GetZone(Lib.GameZone.Arquive, player);
            yield return AddOrMoveCardToGameZone(null, cardsToBeMoved: new List<GameObject> { childObject }, player, false, false);
        }

        if (curResDAType.Equals("DefeatedHoloMember"))
        {

            if (duelFieldData.currentPlayerTurn.Equals(PlayerInfo.INSTANCE.PlayerID))
            {
                EndTurnButton.SetActive(false);
                //cardlist.Clear(); removed after the reformulation and breakdown of the class
            }

            duelFieldData.currentGamePhase = GAMEPHASE.HolomemDefeated;

            if ((!duelFieldData.currentPlayerTurn.Equals(PlayerInfo.INSTANCE.PlayerID)))
                _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "CheerRequestHolomemDown", "", null);
        }
        yield break;

    }
    IEnumerator HandleHolomemDefatedSoGainCheer()
    {
        if (curResDA.cardList[0].cardNumber == "Empty")
        {
            playerCannotDrawFromCheer = true;
            if (duelFieldData.currentGamePhase == GAMEPHASE.HolomemDefeatedEnergyChoose)
                _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "CheerChooseRequestHolomemDown", "", null);

            if (duelFieldData.currentGamePhase == GAMEPHASE.CheerStepChoose)
                _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "CheerChooseRequest", "", null);
        }
        {
            DrawCard(curResDA);
            cheersAssignedThisChainAmount = 0;
            cheersAssignedThisChainTotal = curResDA.cardList.Count;
        }

        if (curResDA.playerID == PlayerInfo.INSTANCE.PlayerID)
            LockGameFlow = true;

        duelFieldData.currentGamePhase = GAMEPHASE.HolomemDefeatedEnergyChoose;
        yield break;
    }
    IEnumerator HandleCheerStepEndDefeatedHolomem()
    {
        //validation to check if the player still have energy to assign due to Buzzholomem, for exemple
        if (cheersAssignedThisChainAmount < cheersAssignedThisChainTotal - 1)
        {
            duelFieldData.currentGamePhase = GAMEPHASE.HolomemDefeatedEnergyChoose;
            cheersAssignedThisChainAmount++;
            LockGameFlow = true;
        }
        else
        {
            cheersAssignedThisChainAmount = 0;
            cheersAssignedThisChainTotal = 0;
            duelFieldData.currentGamePhase = GAMEPHASE.MainStep;
        }

        var target = (curResDA.playerID.Equals(PlayerInfo.INSTANCE.PlayerID)) ? TargetPlayer.Player : TargetPlayer.Oponnent;

        // if player still have cheer, we attach, else, we skip
        if (!playerCannotDrawFromCheer)
            AttachCardToTarget(curResDA, target, true);

        //if the player who is not the player is here, we return, he one assigning energy since his holomem died, we do not need to assign again
        if (!duelFieldData.currentPlayerTurn.Equals(PlayerInfo.INSTANCE.PlayerID))
            yield break;

        if (cheersAssignedThisChainAmount > cheersAssignedThisChainTotal - 1)
        {
            EndTurnButton.SetActive(true);
            _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "MainStartRequest", "CalledAt:CheerStepEndDefeatedHolomem", null);
        }
        yield break;
    }
    IEnumerator HandleCheerStep()
    {
        if (curResDA.cardList[0].cardNumber == "Empty")
        {
            playerCannotDrawFromCheer = true;
            if (duelFieldData.currentGamePhase == GAMEPHASE.HolomemDefeatedEnergyChoose)
                _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "CheerChooseRequestHolomemDown", "", null);

            if (duelFieldData.currentGamePhase == GAMEPHASE.CheerStepChoose)
                if (duelFieldData.currentPlayerTurn.Equals(PlayerInfo.INSTANCE.PlayerID))
                    _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "CheerChooseRequest", "", null);
        }
        {
            DrawCard(curResDA);
        }
        if (curResDA.playerID == PlayerInfo.INSTANCE.PlayerID)
            LockGameFlow = true;

        duelFieldData.currentGamePhase = GAMEPHASE.CheerStepChoose;

        GamePhaseMsg.gameObject.SetActive(true);
        GamePhaseMsg.StartMessage("Cheer Step");
        yield break;
    }
    IEnumerator HandleCheerStepEnd()
    {
        var target = (duelFieldData.currentPlayerTurn == PlayerInfo.INSTANCE.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent;
        AttachCardToTarget(curResDA, target, true);

        duelFieldData.currentGamePhase = GAMEPHASE.MainStep;

        if (duelFieldData.currentPlayerTurn.Equals(PlayerInfo.INSTANCE.PlayerID))
            _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "MainStartRequest", "CalledAt:CheerStepEnd", null);

        startofmain = true;
        yield break;
    }
    IEnumerator HandleMainPhase()
    {
        if (startofmain)
        {
            GamePhaseMsg.gameObject.SetActive(true);
            GamePhaseMsg.StartMessage("Main Step");
        }

        startofmain = false;

        if (duelFieldData.currentGamePhase != GAMEPHASE.MainStep)
        {
            throw new Exception("not in the right gamephase, we're at " + curResDAType + " and tried to enter at" + duelFieldData.currentGamePhase.GetType());
        }

        if (duelFieldData.currentPlayerTurn.Equals(PlayerInfo.INSTANCE.PlayerID))
            DuelField_UI_MAP.INSTANCE.WS_PassTurnButton.SetActive(true);

        duelFieldData.currentGamePhase = GAMEPHASE.MainStep;
        yield break;
    }
    IEnumerator HandleMainPhaseDoAction()
    {
        yield break;
    }
    IEnumerator HandleEndturn()
    {
        startofmain = false;

        if (curResDA == null)
            yield break;

        duelFieldData.currentPlayerTurn = curResDA.playerID;

        centerStageArtUsed = false;
        collabStageArtUsed = false;
        usedOshiSkill = false;

        currentTurn++;

        //by default set next gamephase to reset
        duelFieldData.currentGamePhase = GAMEPHASE.ResetStep;

        duelFieldData.playerLimiteCardPlayed.Clear();

        //we changed the current player, so, the next player is the oponnent now, the calls the server
        if (duelFieldData.currentPlayerTurn.Equals(PlayerInfo.INSTANCE.PlayerID))
            _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "ResetRequest", "CalledAt:Endturn", null);

        GamePhaseMsg.gameObject.SetActive(true);
        GamePhaseMsg.StartMessage("End Step");

        ClearLogConsole();
        yield break;
    }
    public static void ClearLogConsole()
    {
#if UNITY_EDITOR
        System.Reflection.Assembly assembly = System.Reflection.Assembly.GetAssembly(typeof(UnityEditor.SceneView));

        System.Type type = assembly.GetType("UnityEditor.LogEntries");
        System.Reflection.MethodInfo method = type.GetMethod("Clear");
        method.Invoke(new object(), null);
#endif
    }

    IEnumerator HandleEndduel()
    {
        if (curResDA == null && curResDA.playerID == null)
            yield break;

        if (PlayerInfo.INSTANCE.PlayerID.Equals(curResDA.playerID))
            DuelField_UI_MAP.INSTANCE.DisableAllOther().SetPanel(true, PanelType.SS_UI_General).SetPanel(true, PanelType.SS_WinPanel);
        else
            DuelField_UI_MAP.INSTANCE.DisableAllOther().SetPanel(true, PanelType.SS_UI_General).SetPanel(true, PanelType.SS_LosePanel);

        currentGameHigh = 999999999;
    }
    IEnumerator HandleAttachSupportItem()
    {
        var target = (duelFieldData.currentPlayerTurn == PlayerInfo.INSTANCE.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent;
        AttachCardToTarget(curResDA, target, true);
        yield break;
    }
    IEnumerator HandlePlayHolomem()
    {
        string currentPlayer = duelFieldData.currentPlayerTurn;
        var player = (duelFieldData.currentPlayerTurn == PlayerInfo.INSTANCE.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent;

        var zone = GetZone(curResDA.targetZone, player);
        yield return AddOrMoveCardToGameZone(cardsToBeCreated: new List<CardData>() { curResDA.usedCard }, null, player, false, false);

        if (!currentPlayer.Equals(PlayerInfo.INSTANCE.PlayerID))
        {
            RemoveCardFromZone(cardHolderOponnent.gameObject, 1);
        }
        yield break;
    }
    IEnumerator HandleBloomHolomem()
    {
        var target = curResDA.playerID.Equals(PlayerInfo.INSTANCE.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent;

        GameObject cardZone = GetZone(curResDA.targetZone, target);

        GameObject usedCardGameObject = Instantiate(GetCardPrefab(cardZone.name), Vector3.zero, Quaternion.identity);
        usedCardGameObject.name = "Card";
        Card usedCardGameObjectCard = usedCardGameObject.GetComponent<Card>().SetCardNumber(curResDA.usedCard.cardNumber);
        usedCardGameObjectCard.curZone = curResDA.targetCard.curZone;

        GameObject FatherZoneActiveCard = cardZone.transform.GetChild(cardZone.transform.childCount - 1).gameObject;

        usedCardGameObject.transform.SetParent(cardZone.transform, false);

        usedCardGameObject.transform.SetSiblingIndex(cardZone.transform.childCount - 1);

        usedCardGameObject.transform.localPosition = Vector3.zero;
        usedCardGameObject.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

        FatherZoneActiveCard.SetActive(false);

        usedCardGameObjectCard.bloomChild.Add(FatherZoneActiveCard);
        usedCardGameObjectCard.attachedEnergy = FatherZoneActiveCard.GetComponent<Card>().attachedEnergy;
        FatherZoneActiveCard.GetComponent<Card>().attachedEnergy = null;

        usedCardGameObjectCard.curZone = Lib.GameZone.Hand;
        usedCardGameObjectCard.playedThisTurn = true;
        UpdateHP(usedCardGameObjectCard);

        if (!duelFieldData.currentPlayerTurn.Equals(PlayerInfo.INSTANCE.PlayerID))
        {
            RemoveCardFromZone(cardHolderOponnent.gameObject, 1);
        }
        else
        {
            EffectController.INSTANCE.ResolveOnBloomEffect(curResDA);
        }

        yield break;

    }
    IEnumerator HandleDoCollab()
    {
        var player = (duelFieldData.currentPlayerTurn == PlayerInfo.INSTANCE.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent;

        var zone = GetZone(curResDA.targetZone, player);
        var oldzonePower = GetZone(Lib.GameZone.Deck, player);
        var oldzoneHolomem = GetZone(curResDA.usedCard.curZone, player);

        yield return AddOrMoveCardToGameZone(null, cardsToBeMoved: new List<GameObject> { oldzonePower.transform.GetChild(0).gameObject }, player, false, false);
        yield return AddOrMoveCardToGameZone(null, cardsToBeMoved: oldzoneHolomem.transform.GetComponentsInChildren<RectTransform>().GameObjectInChildren().ToList(), player, false, false);

        zone.GetComponentInChildren<Card>().curZone = curResDA.targetZone;
        zone.GetComponentInChildren<Card>().lastZone = Lib.GameZone.Collaboration;

        if (duelFieldData.currentPlayerTurn == PlayerInfo.INSTANCE.PlayerID)
            EffectController.INSTANCE.ResolveOnCollabEffect(curResDA);
        yield break;
    }
    IEnumerator HandleUnDoCollab()
    {
        if (!string.IsNullOrEmpty(curResDA.usedCard?.cardNumber))
        {
            var player = (curResDA.playerID == PlayerInfo.INSTANCE.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent;

            var cards = GetZone(curResDA.usedCard.curZone, player).GetComponentsInChildren<RectTransform>().GameObjectInChildren();

            List<GameObject> cardsToBeMoved = new();
            foreach (var card in cards)
            {
                card.transform.Rotate(0, 0, 90);
                var cardComp = card.GetComponent<Card>();
                cardComp.lastZone = curResDA.usedCard.lastZone;
                cardComp.curZone = curResDA.usedCard.curZone;
                cardComp.suspended = true;
                cardsToBeMoved.Add(card);
            }
            yield return AddOrMoveCardToGameZone(null, cardsToBeMoved, player, false, false);
        }
        yield break;
    }
    IEnumerator HandleRemoveEnergyFrom()
    {
        RemoveCardFromPosition(curResDA);
        yield break;
    }
    IEnumerator HandleAttachEnergyResponse()
    {
        var target = (duelFieldData.currentPlayerTurn == PlayerInfo.INSTANCE.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent;

        // if player still have cheer, we attach, else, we skip
        if (!playerCannotDrawFromCheer)
            AttachCardToTarget(curResDA, target, true);

        EffectController.INSTANCE.isServerResponseArrive = true;
        yield break;
    }
    IEnumerator HandlePayHoloPowerCost()
    {
        var player = curResDA.playerID.Equals(PlayerInfo.INSTANCE.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent;

        GameObject zone = GetZone(Lib.GameZone.HoloPower, player);
        for (int n = 0; n < curResDA.cardList.Count; n++)
        {
            foreach (Transform obj in zone.transform.GetComponentsInChildren<Transform>())
            {
                if (obj.name.Equals("Card"))
                {
                    Destroy(obj);
                    yield return AddOrMoveCardToGameZone(new List<CardData>() { curResDA.cardList[n] }, null, player, false, false);
                    break;
                }
            }
        }
        yield break;

    }
    IEnumerator HandleMoveCardToZone()
    {
        var target = curResDA.playerID.Equals(PlayerInfo.INSTANCE.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent;
        GameObject OrigemZone = GetZone(curResDA.usedCard.curZone, target);
        GameObject targetObj = OrigemZone.transform.GetChild(OrigemZone.transform.childCount - 1).gameObject;

        if (!string.IsNullOrEmpty(curResDA.usedCard.cardNumber))
            foreach (Card _card in OrigemZone.GetComponentsInChildren<Card>())
                if (_card.cardNumber.Equals(curResDA.usedCard.cardNumber))
                    targetObj = _card.Init(curResDA.usedCard).gameObject;

        yield return AddOrMoveCardToGameZone(null, new List<GameObject> { targetObj }, target, false, false);

        yield break;
    }
    IEnumerator HandleDisposeUsedSupport()
    {
        var target = curResDA.playerID.Equals(PlayerInfo.INSTANCE.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent;
        var oldzone = GetZone(curResDA.usedCard.curZone, target);
        yield return AddOrMoveCardToGameZone(new List<CardData>() { curResDA.usedCard }, null, target, false, false);

        if (!curResDA.playerID.Equals(PlayerInfo.INSTANCE.PlayerID))
            RemoveCardFromZone((curResDA.playerID.Equals(PlayerInfo.INSTANCE.PlayerID) ? cardHolderPlayer : cardHolderOponnent).gameObject, 1);

        yield break;
    }
    IEnumerator HandleResolveOnEffect()
    {
        if (curResDA.playerID == PlayerInfo.INSTANCE.PlayerID)
            LockGameFlow = true;

        EffectController.INSTANCE.isServerResponseArrive = true;

        yield break;
    }
    IEnumerator HandleActiveArtEffect()
    {
        if (curResDA.playerID == PlayerInfo.INSTANCE.PlayerID)
        {
            LockGameFlow = true;
            EffectController.INSTANCE.ResolveOnArtEffect(curResDA);
        }
        yield break;
    }
    IEnumerator HandlePickFromListThenGiveBackFromHandDone()
    {
        if (curResDA.playerID != PlayerInfo.INSTANCE.PlayerID)
        {
            //we remove only one card from the pllayer hand, bacause we're using draw so the hands match
            RemoveCardFromZone(cardHolderOponnent.gameObject, 1);
            // add card to holopower since we are using draw which removes a card from the zone
            yield return AddOrMoveCardToGameZone(new List<CardData>() { new CardData() { curZone = Lib.GameZone.HoloPower, lastZone = Lib.GameZone.Hand } }, null, TargetPlayer.Oponnent, false, false);
            //just making the card empty so the player dont see in the oponent hand holder, we can check in the log
            curResDA.cardList[0].cardNumber = "";
            DrawCard(curResDA);
        }
        else
        {
            //removing from the player hand the picked card to add with the draw the one from the holopower
            int n = -1;

            for (int contadorCardHand = 0; contadorCardHand < cardHolderPlayer.childCount; contadorCardHand++)
            {
                Card cardInHand = cardHolderPlayer.GetComponentsInChildren<RectTransform>()[contadorCardHand].GetComponent<Card>();
                if (cardInHand.cardNumber.Equals(curResDA.targetCard.cardNumber))
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
                DrawCard(curResDA);
            }
            // add card to holopower since we are using draw which removes a card from the zone
            // /\ saporra é gambiarra, ctz
            yield return AddOrMoveCardToGameZone(new List<CardData>() { new CardData() { curZone = Lib.GameZone.HoloPower, lastZone = Lib.GameZone.Hand } }, null, TargetPlayer.Player, false, false);
        }
        yield break;
    }
    IEnumerator HandleRemoveCardsFromArquive()
    {
        var target = curResDA.playerID == PlayerInfo.INSTANCE.PlayerID ? TargetPlayer.Player : TargetPlayer.Oponnent;
        List<Card> canSelect = GetZone(Lib.GameZone.Arquive, target).GetComponentsInChildren<Card>().ToList();
        for (int i = 0; i < curResDA.cardList.Count; i++)
        {
            bool match = false;
            int j = 0;
            for (; j < canSelect.Count; j++)
            {
                if (curResDA.cardList[i].cardNumber.Equals(canSelect[j].cardNumber))
                {
                    match = true;
                    break;
                }
            }
            if (match)
            {
                Destroy(canSelect[j].gameObject);
                continue;
            }
        }
        yield break;
    }
    IEnumerator HandleRemoveCardsFromHand()
    {
        if (curResDA.playerID == PlayerInfo.INSTANCE.PlayerID)
        {
            var canSelect = cardHolderPlayer.GetComponentsInChildren<Card>().ToList();

            for (int i = 0; i < curResDA.cardList.Count; i++)
            {
                bool match = false;
                int j = 0;
                for (; j < canSelect.Count; j++)
                {
                    if (curResDA.cardList[i].cardNumber.Equals(canSelect[j].cardNumber))
                    {
                        match = true;
                        break;
                    }
                }
                if (match)
                {
                    Destroy(canSelect[j].gameObject);
                    continue;
                }
            }
        }
        else
        {
            RemoveCardFromZone(cardHolderOponnent.gameObject, curResDA.cardList.Count);
        }
        yield break;
    }
    IEnumerator HandleDrawOshiEffect()
    {
        DrawCard(curResDA);
        yield break;
    }
    IEnumerator HandleDrawByEffect()
    {
        DrawCard(curResDA);
        yield break;
    }
    IEnumerator HandleShowCard()
    {
        EffectController.INSTANCE.EffectInformation.Add(curResDA);
        EffectController.INSTANCE.isServerResponseArrive = true;
        yield break;

    }
    IEnumerator HandleOnlyDiceRoll()
    {
        EffectController.INSTANCE.EffectInformation.Add(curResDA);
        yield break;
    }
    IEnumerator HandleRecoverHolomem()
    {
        GameObject zone = GetZone(curResDA.targetCard.curZone, (curResDA.playerID.Equals(PlayerInfo.INSTANCE.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent));
        Card targetedCard = zone.GetComponentInChildren<Card>();
        targetedCard.currentHp = Math.Min(targetedCard.currentHp + int.Parse(curResDA.actionObject), int.Parse(targetedCard.hp));
        UpdateHP(targetedCard);

        EffectController.INSTANCE.ResolveOnRecoveryEffect(targetedCard.ToCardData());
        yield break;
    }
    IEnumerator HandleInflicArtDamageToHolomem()
    {
        EffectController.INSTANCE.ResolveOnDamageResolveEffect(curResDA);
        yield break;
    }
    IEnumerator HandleInflicDamageToHolomem()
    {
        EffectController.INSTANCE.ResolveOnDamageResolveEffect(curResDA);
        yield break;
    }
    IEnumerator HandleInflicRecoilDamageToHolomem()
    {
        EffectController.INSTANCE.ResolveOnDamageResolveEffect(curResDA);
        yield break;
    }
    IEnumerator HandleResolveDamageToHolomem()
    {
        var zoneArt = GetZone(curResDA.targetCard.curZone, !curResDA.playerID.Equals(PlayerInfo.INSTANCE.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent);

        Card card = zoneArt.GetComponentInChildren<Card>();

        if (curResDAType.Equals("SetHPToFixedValue"))
            card.currentHp = int.Parse(curResDA.actionObject);

        card.currentHp -= int.Parse(curResDA.actionObject);

        UpdateHP(card);

        if (curResDAType.Equals("UsedArt"))
        {
            if (curResDA.usedCard.curZone.Equals(Lib.GameZone.Stage))
            {
                centerStageArtUsed = true;
            }
            else if (curResDA.usedCard.curZone.Equals(Lib.GameZone.Collaboration))
            {
                collabStageArtUsed = true;
            }

            if (curResDA.playerID.Equals(PlayerInfo.INSTANCE.PlayerID))
                if (centerStageArtUsed && collabStageArtUsed)
                    GenericActionCallBack(null, "MainEndturnRequest");
        }
        yield break;
    }
    IEnumerator HandleSwitchStageCard()
    {
        //if is a retreat using the skill
        if (curResDAType.Equals("SwitchStageCardByRetreat"))
        {
            centerStageArtUsed = true;
        }

        var player = curResDA.playerID.Equals(PlayerInfo.INSTANCE.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent;
        SwitchCard(player, Lib.GameZone.Stage, curResDA.targetCard.curZone);
        EffectController.INSTANCE.isServerResponseArrive = true;
        yield break;
    }
    IEnumerator HandleSwitchOpponentStageCard()
    {
        var player = curResDA.playerID.Equals(PlayerInfo.INSTANCE.PlayerID) ? TargetPlayer.Oponnent : TargetPlayer.Player;
        SwitchCard(player, Lib.GameZone.Stage, curResDA.targetCard.curZone);
        yield break;
    }
    void SwitchCard(TargetPlayer player, Lib.GameZone from, Lib.GameZone target)
    {

        var fromZone = GetZone(from, player);
        var targetZone = GetZone(target, player);

        List<GameObject> cardsToBeMoved = new();
        foreach (RectTransform card in targetZone.GetComponentsInChildren<RectTransform>())
        {
            Card cardComp = card.GetComponent<Card>();
            cardComp.curZone = target;
            cardComp.lastZone = from;
            cardsToBeMoved.Add(card.gameObject);
        }
        foreach (RectTransform card in fromZone.GetComponentsInChildren<RectTransform>())
        {
            Card cardComp = card.GetComponent<Card>();
            cardComp.curZone = from;
            cardComp.lastZone = target;
            cardsToBeMoved.Add(card.gameObject);
        }
        StartCoroutine( AddOrMoveCardToGameZone(null, cardsToBeMoved, player, false, false));

    }
    IEnumerator HandleRemoveEnergyAtAndSendToArquive()
    {
        var target = PlayerInfo.INSTANCE.PlayerID == curResDA.playerID ? TargetPlayer.Player : TargetPlayer.Oponnent;

        GameObject TargetZone = GetZone(curResDA.usedCard.curZone, target);
        Card targetCard = TargetZone.transform.GetChild(TargetZone.transform.childCount - 1).GetComponent<Card>();
        int nn = 0;
        int jj = 0;

        foreach (GameObject attachEnergy in targetCard.attachedEnergy)
        {
            Card energyInfo = attachEnergy.GetComponent<Card>();
            if (energyInfo.cardNumber.Equals(curResDA.usedCard.cardNumber))
            {
                energyInfo.Init(curResDA.usedCard);
                nn = jj;
                break;
            }
            jj++;
        }
        if (curResDAType.Equals("RemoveEnergyAtAndSendToArquive"))
        {
            yield return AddOrMoveCardToGameZone(null, cardsToBeMoved: new List<GameObject> { targetCard.attachedEnergy[nn] }, target, false, false);
            targetCard.attachedEnergy[nn].gameObject.SetActive(true);
        }
        else
        {
            Destroy(targetCard.attachedEnergy[nn]);
        }
        targetCard.attachedEnergy.RemoveAt(nn);

        yield break;
    }
    IEnumerator HandleRemoveEquipAtAndSendToArquive()
    {

        var target = PlayerInfo.INSTANCE.PlayerID == curResDA.playerID ? TargetPlayer.Player : TargetPlayer.Oponnent;

        GameObject TargetZone = GetZone(curResDA.usedCard.curZone, target);
        var targetCard = TargetZone.transform.GetChild(TargetZone.transform.childCount - 1).GetComponent<Card>();
        int i = 0;
        int j = 0;

        foreach (GameObject attachEnergy in targetCard.attachedEquipe)
        {
            Card energyInfo = attachEnergy.GetComponent<Card>();
            if (energyInfo.cardNumber.Equals(curResDA.usedCard.cardNumber))
            {
                energyInfo.Init(curResDA.usedCard);
                i = j;
                break;
            }
            j++;
        }
        yield return AddOrMoveCardToGameZone(null, cardsToBeMoved: new List<GameObject> { targetCard.attachedEquipe[i] }, target, false, false);
        targetCard.attachedEquipe[i].gameObject.SetActive(true);

        targetCard.attachedEquipe.RemoveAt(i);

        yield break;
    }
    IEnumerator HandleSuffleDeck()
    {
        yield return StartCoroutine(ShuffleCardsCoroutine(GetChildrenWithName(GetZone(Lib.GameZone.Deck, curResDA.playerID.Equals(PlayerInfo.INSTANCE.PlayerID) ? TargetPlayer.Player : TargetPlayer.Oponnent), "Card"), 0.5f, 50f));
    }

    internal bool CanSummonHolomemHere(Card cardBeingChecked, Lib.GameZone targetZoneEnum)
    {
        bool canContinue = false;
        if (cardBeingChecked.bloomLevel.Equals("Debut") || cardBeingChecked.bloomLevel.Equals("Spot")) canContinue = true;

        if (!canContinue) return false;

        int count = CountBackStageTotal();

        if (targetZoneEnum.Equals(Lib.GameZone.BackStage1) || targetZoneEnum.Equals(Lib.GameZone.BackStage2) || targetZoneEnum.Equals(Lib.GameZone.BackStage3) || targetZoneEnum.Equals(Lib.GameZone.BackStage4) || targetZoneEnum.Equals(Lib.GameZone.BackStage5))
        {
            canContinue = false;
            if (count < 5)
                canContinue = true;
        }
        return canContinue;
    }

    internal bool CanBloomHolomem(Card pointedCard, Card thisCard)
    {
        if (pointedCard.playedThisTurn == true || pointedCard.cardType.Equals("Buzzホロメン")) return false;

        bool canContinue = false;
        if (thisCard.cardType.Equals("ホロメン") || thisCard.cardType.Equals("Buzzホロメン"))
        {
            string bloomToLevel = pointedCard.bloomLevel.Equals("Debut") ? "1st" : "2nd";
            // especial card condition to bloom match
            if (thisCard.cardNumber.Equals("hSD01-013") && thisCard.bloomLevel.Equals(bloomToLevel) && (pointedCard.cardName.Equals("ときのそら") || pointedCard.cardName.Equals("AZKi")))
            {
                canContinue = true;
            }
            else if (pointedCard.cardNumber.Equals("hSD01-013") && (thisCard.cardName.Equals("ときのそら") || thisCard.cardName.Equals("AZKi")) && bloomToLevel.Equals("2nd"))
            {
                canContinue = true;
            }
            else if (thisCard.cardNumber.Equals("hBP01-045"))
            {
                int lifeCounter = GetZone(Lib.GameZone.Life, TargetPlayer.Player).transform.childCount - 1;
                if (lifeCounter < 4 && pointedCard.cardName.Equals("AZKi") || pointedCard.cardName.Equals("SorAZ"))
                {
                    canContinue = true;
                }
            }
            // normal condition to bloom match
            else if (thisCard.cardName.Equals(pointedCard.cardName) && thisCard.bloomLevel.Equals(bloomToLevel))
            {
                canContinue = true;
            }
        }
        return canContinue;
    }

    enum TypeOfDuelAction
    {
        StartDuel,
        InitialDraw,
        InitialDrawP2,
        PAMulligan,
        PBMulligan,
        PBNoMulligan,
        PANoMulligan,
        PBMulliganF,
        PAMulliganF,
        DuelUpdate,
        ResetStep,
        ReSetStage,
        DrawPhase,
        DefeatedHoloMember,
        DefeatedHoloMemberByEffect,
        HolomemDefatedSoGainCheer,
        CheerStepEndDefeatedHolomem,
        CheerStep,
        CheerStepEnd,
        MainPhase,
        MainPhaseDoAction,
        Endturn,
        Endduel,
        AttachSupportItem,
        PlayHolomem,
        BloomHolomem,
        DoCollab,
        UnDoCollab,
        RemoveEnergyFrom,
        AttachEnergyResponse,
        PayHoloPowerCost,
        MoveCardToZone,
        DisposeUsedSupport,
        ResolveOnSupportEffect,
        OnCollabEffect,
        OnArtEffect,
        ResolveOnAttachEffect,
        ActiveArtEffect,
        PickFromListThenGiveBacKFromHandDone,
        RemoveCardsFromArquive,
        RemoveCardsFromHand,
        DrawOshiEffect,
        DrawBloomEffect,
        DrawBloomIncreaseEffect,
        DrawCollabEffect,
        DrawArtEffect,
        SupportEffectDraw,
        DrawAttachEffect,
        ShowCard,
        RollDice,
        OnlyDiceRoll,
        RecoverHolomem,
        InflicArtDamageToHolomem,
        InflicDamageToHolomem,
        InflicRecoilDamageToHolomem,
        SetHPToFixedValue,
        ResolveDamageToHolomem,
        SwitchStageCard,
        SwitchStageCardByRetreat,
        SwitchOpponentStageCard,
        RemoveEnergyAtAndDestroy,
        RemoveEnergyAtAndSendToArquive,
        RemoveEquipAtAndSendToArquive,
        SuffleDeck
    };
}
