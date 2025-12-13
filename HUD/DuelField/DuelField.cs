using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static DuelField_UI_MAP;
using static DuelFieldData;
using static UnityEngine.GraphicsBuffer;

public class DuelField : MonoBehaviour
{
    public static DuelField INSTANCE;
    public static Lib.GameZone[] DEFAULTHOLOMEMZONE = new Lib.GameZone[] { Lib.GameZone.Stage, Lib.GameZone.Collaboration, Lib.GameZone.BackStage1, Lib.GameZone.BackStage2, Lib.GameZone.BackStage3, Lib.GameZone.BackStage4, Lib.GameZone.BackStage5 };
    [SerializeField] List<GameObject> GameZones = new();
    public List<GameObject> GetGameZones() { return GameZones; }

    private const int TURN_TIMER_SECONDS = 120;
    private int playerTimers;
    private CancellationTokenSource countdownTokenSource;
    [SerializeField] private TMP_Text TimmerText;
    [SerializeField] private TMP_Text TurnCounterText;

    [SerializeField] GameObject cardPrefab;
    [SerializeField] GameObject oldCardPrefab;

    public RectTransform cardHolderPlayer;
    public RectTransform cardHolderOponnent;

    public RectTransform cardLifeHolderA;
    public RectTransform cardLifeHolderB;

    [SerializeField] private GameObject MulliganMenu = null;
    [SerializeField] private GameObject EndTurnButton = null;

    [SerializeField] private GameObject EffectConfirmationTab = null;
    [SerializeField] private GameObject EffectConfirmationYesButton = null;
    [SerializeField] private GameObject EffectConfirmationNoButton = null;

    public bool ReadyButtonShowed = false;
    bool startofmain = false;

    private int currentTurn;

    bool playerMulligan = false;
    bool oponnentMulligan = false;

    public bool hasAlreadyCollabed = false;
    public bool centerStageArtUsed = false;
    public bool collabStageArtUsed = false;

    public bool usedSPOshiSkill = false;
    public bool usedOshiSkill = false;

    private bool playerCannotDrawFromCheer;
    private int cheersAssignedThisChainTotal;
    private int cheersAssignedThisChainAmount;

    private bool playerInitialDraw = false;
    private bool oponnentInitialDrawP2 = false;

    [SerializeField] Sprite viewTypeActionImg;
    [SerializeField] Sprite viewTypeViewImg;
    public bool isViewMode = true;
    Button isViewModeButton = null;

    [Flags]
    public enum TargetPlayer : byte
    {
        Player = 0,
        Oponnent = 1
    }

    public DuelFieldData DUELFIELDDATA;
    public DuelAction CUR_DA;
    string CUR_DA_TYPE;
    bool ISMYTURN;

    Dictionary<string, Func<IEnumerator>> serverActionHandlers;
    private bool isVisualActionRunning = false;
    private bool isServerActionRunning = false;
    public bool isServerActionLocked = false;

    private void Awake()
    {
        INSTANCE = this;
    }
    void Start()
    {
        DuelField_UI_MAP.INSTANCE = FindAnyObjectByType<DuelField_UI_MAP>();
        SetActionToggleMode(ToggleStatus.on);

        var action = MatchConnection.INSTANCE.GetPendingActions();
        while (!action.Item1.Equals("mt"))
            action = MatchConnection.INSTANCE.GetPendingActions();

        ClearLogConsole();

        DuelField_UI_MAP.INSTANCE.SS_MulliganPanelYes.GetComponent<Button>().onClick.AddListener(() => { MulliganBoxAwnser(true); });
        DuelField_UI_MAP.INSTANCE.SS_MulliganPanelNo.GetComponent<Button>().onClick.AddListener(() => { MulliganBoxAwnser(false); });

    }
    void Update()
    {
        if (isViewModeButton == null)
        {
            if (isViewModeButton == null)
                isViewModeButton = DuelField_UI_MAP.INSTANCE.SS_UI_ActionToggleButton.GetComponent<Button>();

            if (isViewModeButton != null)
                isViewModeButton.onClick.AddListener(() => { SetActionToggleMode(ToggleStatus.flip); });
        }

        if (MatchConnection.INSTANCE == null)
        {
            SceneManager.LoadScene("Login");
            return;
        }

        if (serverActionHandlers == null)
            serverActionHandlers = MapActions();

        if (!isServerActionRunning && !isServerActionLocked)
            INSTANCE.StartCoroutine(INSTANCE.ProcessServerActions());

        if (!isVisualActionRunning)
            INSTANCE.StartCoroutine(INSTANCE.ProcessVisualActions());
    }
    private IEnumerator ProcessServerActions()
    {
        isServerActionRunning = true;
        if (MatchConnection.INSTANCE.GetPendingActionsCount() > 0)
        {
            var action = MatchConnection.INSTANCE.GetPendingActions();

            CUR_DA = action.Item2;
            CUR_DA_TYPE = action.Item1;

            if (CUR_DA_TYPE.Equals("StartDuel"))
                DUELFIELDDATA = CUR_DA.duelFieldData;

            if (!serverActionHandlers.TryGetValue(CUR_DA_TYPE, out Func<IEnumerator> handler))
            {
                Debug.LogWarning("Unhandled action: " + CUR_DA_TYPE);
                yield break;
            }

            yield return handler();
        }
        isServerActionRunning = false;
    }
    private IEnumerator ProcessVisualActions()
    {
        isVisualActionRunning = true;
        while (ActionItem.visualActionQueue.Count > 0)
        {
            var visualAction = ActionItem.visualActionQueue.Dequeue();

            ISMYTURN = DUELFIELDDATA.currentPlayerTurn == PlayerInfo.INSTANCE.PlayerID;
            yield return visualAction.Routine;

            yield return null;

            foreach (GameObject zone in GameZones)
            {
                if (zone.name == "PlayerHand")
                {
                    yield return DuelField_ActionLibrary.ArrangeCards(zone);
                    yield return GetUsableCards();
                }
                else if (zone.name == "OponentHand")
                {
                    yield return DuelField_ActionLibrary.ArrangeCards(zone);
                }
                else if (zone.name == Lib.GameZone.Life.ToString())
                {
                    yield return DuelField_ActionLibrary.ArrangeCards(zone, true);
                }
            }
            foreach (DuelField_ZoneWatcher zone in FindObjectsOfType<DuelField_ZoneWatcher>())
            {
                DuelField_ActionLibrary.CardCounter(zone.gameObject);
                DuelField_ActionLibrary.StackCardsEffect(zone.gameObject);
            }
        }
        isVisualActionRunning = false;
    }
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
                DUELFIELDDATA.currentGamePhase = GAMEPHASE.EndStep;
                EndTurnButton.SetActive(false);
                break;
            case "CheerChooseRequest":
                if (DUELFIELDDATA.currentGamePhase == GAMEPHASE.HolomemDefeatedEnergyChoose)
                    _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "CheerChooseRequestHolomemDown", "", _DuelAction);
                if (DUELFIELDDATA.currentGamePhase == GAMEPHASE.CheerStepChoose)
                    _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "CheerChooseRequest", "", _DuelAction);
                isServerActionLocked = false;
                break;
            case "standart":
                _DuelAction.playerID = PlayerInfo.INSTANCE.PlayerID;
                _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "MainDoActionRequest", "", _DuelAction);
                isServerActionLocked = false;
                break;
            default:
                _DuelAction.playerID = PlayerInfo.INSTANCE.PlayerID;
                _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, type, "", _DuelAction);
                isServerActionLocked = false;
                break;
        }
    }
    public void AttachEnergyCallBack(string energyNumber)
    {
        DuelAction da = new DuelAction() { usedCard = new CardData() { cardNumber = energyNumber } };

        isServerActionLocked = false;
        _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "AskAttachEnergy", "", da);
    }
    //DuelField_SelectableCardMenu CALL THIS WHEN WE HAVE SELECTED THE CARD WE WANT TO SUMMOM 
    public void SuporteEffectSummomIfCallBack(List<string> cards)
    {
        isServerActionLocked = false;
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

        isServerActionLocked = false;

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
        DuelField_UI_MAP.INSTANCE.WS_ReadyButton.SetActive(false);
        DuelFieldData dfd = DuelFieldData.MapDuelFieldData(GameZones);

        DuelFieldData smallDFD = new();

        if (DUELFIELDDATA.firstPlayer.Equals(PlayerInfo.INSTANCE.PlayerID))
        {
            smallDFD.playerABackPosition = dfd.playerABackPosition;
            smallDFD.playerAStage = dfd.playerAStage;
        }
        else
        {
            smallDFD.playerBBackPosition = dfd.playerBBackPosition;
            smallDFD.playerBStage = dfd.playerBStage;
        }

        DuelAction da = new();
        da.duelFieldData = smallDFD;
        _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "DuelFieldReady", "", da);
        isServerActionLocked = false;
        ActionItem.Add("GetUsableCards", GetUsableCards());
    }
    public void EndTurnHUDButton()
    {
        GenericActionCallBack(null, "MainEndturnRequest");
    }
    public void MulliganBoxAwnser(bool awnser)
    {
        DuelField_UI_MAP.INSTANCE.SetPanel(false, DuelField_UI_MAP.PanelType.SS_MulliganPanel);
        DuelField_UI_MAP.INSTANCE.SetPanel(true, DuelField_UI_MAP.PanelType.SS_OponentHand);

        MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "AskForMulligan", "", new() { yesOrNo = awnser });
        isServerActionLocked = false;
    }
    //FUNCTIONS ASSIGNED IN THE INSPECTOR - END
    public IEnumerator GetUsableCards()
    {
        var cards = cardHolderPlayer.GetComponentsInChildren<RectTransform>().Select(r => r.GetComponent<Card>()).Where(c => c != null).ToList();
        foreach (var cardComponent in cards)
        {
            DuelField_HandDragDrop handDragDrop = cardComponent.GetComponent<DuelField_HandDragDrop>() ?? cardComponent.gameObject.AddComponent<DuelField_HandDragDrop>();
            handDragDrop.enabled = false;
            bool enableDrag = false;

            switch (DUELFIELDDATA.currentGamePhase)
            {
                case GAMEPHASE.HolomemDefeatedEnergyChoose:
                case GAMEPHASE.CheerStepChoose:
                    if (cardComponent.cardType != null && cardComponent.cardType.Equals("エール"))
                        enableDrag = true;
                    break;

                case GAMEPHASE.MainStep:
                    if (cardComponent.cardType == null)
                        continue;

                    if (!ISMYTURN)
                        continue;

                    // Support cards
                    if (cardComponent.cardType.StartsWith("サポート"))
                    {
                        if (cardComponent.cardType.Contains("LIMITED"))
                        {
                            if (DuelField.INSTANCE.DUELFIELDDATA.playerLimiteCardPlayed.Count > 0)
                                continue;
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
                    if (DuelField_UI_MAP.INSTANCE.WS_ReadyButton.activeInHierarchy)
                        if (cardComponent.bloomLevel != null && (cardComponent.bloomLevel.Equals("Debut") || cardComponent.bloomLevel.Equals("Spot")))
                            enableDrag = true;
                    break;
            }
            handDragDrop.enabled = enableDrag;
            cardComponent.Glow();
        }

        var cardObjts = GetPlayerBackRoll(TargetPlayer.Player);
        foreach (GameObject cardObj in cardObjts) 
        {
            var list = cardObj.GetComponentsInChildren<Card>();
            foreach (Card card in list)
                card.Glow();
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
    void AttachCardToTarget(TargetPlayer target)
    {
        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;


        GameObject newHolder = GetZone(CUR_DA.targetCard.curZone, target);
        GameObject oldHolder = GetZone(CUR_DA.usedCard.lastZone, target);

        Card card = InstantiateAndPrepareCardsToMove(new List<CardData> { CUR_DA.usedCard }, null, target, CUR_DA.toBottom, true).First().GetComponent<Card>();
        Card cardFather = null;

        foreach (Transform child in newHolder.transform)
        {
            if (!child.gameObject.activeSelf)
                continue;

            Card c = child.GetComponent<Card>();
            if (c == null)
                continue;

            if (c.cardNumber == CUR_DA.targetCard.cardNumber)
            {
                cardFather = c;
                break;
            }
        }

        if (cardFather == null)
            return;

        if (card.cardType.Equals("エール"))
        {
            cardFather.attachedEnergy ??= new List<GameObject>();
            cardFather.attachedEnergy.Add(card.gameObject);
        }
        else //equipe item
        {
            cardFather.attachedEquipe ??= new List<GameObject>();
            cardFather.attachedEquipe.Add(card.gameObject);
        }

        bool isPlayedFromHand = card.lastZone.Equals(Lib.GameZone.Hand) || card.lastZone.Equals(Lib.GameZone.na);

        ActionItem.Add("MoveCard", DuelField_ActionLibrary.MoveCard(new List<GameObject>() { card.gameObject }, oldHolder.transform, newHolder.transform, 0.2f, isPlayedFromHand, ISMYACTION));

        if (!ISMYTURN)
        {
            RemoveCardFromZone(cardHolderOponnent.gameObject, 1, findEnergy: true);
        }
        else
        {
            if (!card.cardType.Equals("エール"))
                EffectController.INSTANCE.ResolveOnAttachEffect(CUR_DA);
        }

    }
    public List<GameObject> InstantiateAndPrepareCardsToMove(List<CardData> cardsToBeCreated, List<GameObject> cardsToBeMoved, TargetPlayer player, bool toBottom = false, bool playedThisTurn = false)
    {
        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;

        List<GameObject> allCardsToMove = new();
        if (cardsToBeCreated != null)
        {
            foreach (CardData cardDataGeneric in cardsToBeCreated)
            {
                GameObject newHolder = GetZone(cardDataGeneric.curZone, player);
                GameObject oldHolder = GetZone(cardDataGeneric.lastZone, player);
                GameObject obj = Instantiate(GetCardPrefab(newHolder.name), newHolder.transform);
                obj.name = "Card";
                Card card = obj.GetComponent<Card>().Init(cardDataGeneric).ScaleToFather();
                obj.SetActive(false);

                if (playedThisTurn) card.playedThisTurn = true;
                if (toBottom) card.transform.SetSiblingIndex(0);

                allCardsToMove.Add(obj);
            }
        }

        if (cardsToBeMoved != null)
        {
            foreach (GameObject obj in cardsToBeMoved)
            {
                if (!obj.name.Contains("Card")) continue;

                Card card = obj.GetComponent<Card>();
                obj.transform.SetParent(GetZone(card.curZone, player).transform);

                foreach (GameObject energyObj in card.attachedEnergy)
                {
                    Card energyCard = energyObj.GetComponent<Card>();
                    energyCard.lastZone = card.lastZone;
                    energyCard.curZone = card.curZone;
                    energyObj.transform.SetParent(obj.transform.parent);
                    allCardsToMove.Add(energyObj);
                }
                foreach (GameObject equipObj in card.attachedEquipe)
                {
                    Card equipCard = equipObj.GetComponent<Card>();
                    equipCard.lastZone = card.lastZone;
                    equipCard.curZone = card.curZone;
                    equipObj.transform.SetParent(obj.transform.parent);
                    allCardsToMove.Add(equipObj);
                }
            }
            allCardsToMove.AddRange(cardsToBeMoved);
        }
        return allCardsToMove;
    }
    public void AddOrMoveCardToGameZone(List<CardData> cardsToBeCreated, List<GameObject> cardsToBeMoved, TargetPlayer player, bool shuffle = false, bool toBottom = false, bool playedThisTurn = false, bool MOVEALLATONCE = true, bool SuspendAfter = false)
    {


        var allCardsToMove = InstantiateAndPrepareCardsToMove(cardsToBeCreated, cardsToBeMoved, player, toBottom, playedThisTurn);

        Card card = allCardsToMove.First().GetComponent<Card>();
        GameObject newHolder = GetZone(card.curZone, player);
        GameObject oldHolder = GetZone(card.lastZone, player);
        bool isPlayedFromHand = card.lastZone.Equals(Lib.GameZone.Hand) || card.lastZone.Equals(Lib.GameZone.na);

        if (MOVEALLATONCE)
            ActionItem.Add("MoveCard", DuelField_ActionLibrary.MoveCard(allCardsToMove, oldHolder.transform, newHolder.transform, 0.2f, isPlayedFromHand, ISMYTURN, SuspendAfter));
        else
            foreach (GameObject cardObj in allCardsToMove)
                ActionItem.Add("MoveCard", DuelField_ActionLibrary.MoveCard(new List<GameObject> { cardObj }, oldHolder.transform, newHolder.transform, 0.2f, isPlayedFromHand, ISMYTURN, SuspendAfter));
    }
    public Lib.GameZone GetZoneByString(string name)
    {
        if ((name.Equals("PlayerHand") || name.Equals("OponentHand") || name.Equals("PlayerGeneral")))
            return Lib.GameZone.Hand;

        try { return (Lib.GameZone)Enum.Parse(typeof(Lib.GameZone), name); } catch (Exception e) { Debug.Log(e); }
        return 0;
    }
    public void DrawCard(DuelAction draw)
    {
        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? TargetPlayer.Player : TargetPlayer.Oponnent;

        if (draw.suffleHandBackToDeck)
        {
            var handZone = GetZone(Lib.GameZone.Hand, CUR_DA_PLAYER);
            var cards = handZone.GetComponentsInChildren<Card>();
            int amount = cards.Length;

            RemoveCardFromZone(handZone, amount);

            List<CardData> cardsFakeData = new();
            for (int n = 0; n < amount; n++)
            {
                cardsFakeData.Add(new CardData
                {   cardNumber = "",
                    lastZone = Lib.GameZone.Hand,
                    curZone = Lib.GameZone.Deck
                });
            }

            AddOrMoveCardToGameZone(cardsFakeData, null , CUR_DA_PLAYER, false, false, MOVEALLATONCE: false);

            if (draw.suffle)
                ActionItem.Add("ShuffleDeck", DuelField_ActionLibrary.ShuffleDeck(GetZone(Lib.GameZone.Deck, CUR_DA_PLAYER)));
        }

        AddOrMoveCardToGameZone(draw.cardList, (List<GameObject>)null, CUR_DA_PLAYER, draw.toBottom, draw.suffle, MOVEALLATONCE: false);
        RemoveCardFromZone(GetZone(draw.cardList[0].lastZone, CUR_DA_PLAYER), draw.cardList.Count);
    }
    public void RemoveCardFromZone(GameObject father, int amount, bool findEnergy = false)
    {
        ActionItem.Add("RemoveCardFromZone", DuelField_ActionLibrary.RemoveCardFromZone(father, amount, findEnergy));
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
    void ResetCardTurnStatusForPlayer(TargetPlayer player)
    {
        var zones = GetPlayerBackRoll(player);
        foreach (GameObject obj in zones) {
            var childlist = obj.GetComponentsInChildren<Card>();

            foreach (Card c in childlist)
            {
                if (c != null && c.suspended)
                {
                    c.suspended = false;
                    c.transform.rotation = Quaternion.Euler(0,0,0);
                }
            }

        }
    }

    public List<GameObject> GetPlayerBackRoll(TargetPlayer player)
    {
        //this is kind o garbage, but the list is small anyway
        List<GameObject> zones = new();
        zones.Add(GetZone(Lib.GameZone.BackStage1, player));
        zones.Add(GetZone(Lib.GameZone.BackStage2, player));
        zones.Add(GetZone(Lib.GameZone.BackStage3, player));
        zones.Add(GetZone(Lib.GameZone.BackStage4, player));
        zones.Add(GetZone(Lib.GameZone.BackStage5, player));
        return zones;
    }
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

        UnityEngine.UI.Image img = DuelField_UI_MAP.INSTANCE.SS_UI_ActionToggleButton.GetComponent<UnityEngine.UI.Image>();

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
    void RemoveCardFromPosition(DuelAction duelAction)
    {
        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? TargetPlayer.Player : TargetPlayer.Oponnent;

        if (CUR_DA_TYPE.Equals("CheerStepEndDefeatedHolomem"))
        {
            RemoveCardFromZone(cardHolderOponnent.gameObject, 1);
            return;
        }
        else if (duelAction.usedCard.curZone.Equals(Lib.GameZone.Arquive))
        {
            GameObject ZoneToRemove = GetZone(Lib.GameZone.Arquive, CUR_DA_PLAYER);

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
            RemoveCardFromZone(GetZone(Lib.GameZone.CardCheer, CUR_DA_PLAYER), 1);
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
        ActionItem.Add("ShowMessage", DuelField_ActionLibrary.ShowMessage("Starting Duel"));

        var data = CUR_DA.duelFieldData;

        TargetPlayer playerA = TargetPlayer.Player;
        TargetPlayer playerB = TargetPlayer.Oponnent;

        if (!DUELFIELDDATA.firstPlayer.Equals(PlayerInfo.INSTANCE.PlayerID))
        {
            playerA = TargetPlayer.Oponnent;
            playerB = TargetPlayer.Player;
        }

        AddOrMoveCardToGameZone(data.playerADeck, null, playerA, false, false);
        AddOrMoveCardToGameZone(data.playerBDeck, null, playerB, false, false);

        AddOrMoveCardToGameZone(data.playerACardCheer, null, playerA, false, false);
        AddOrMoveCardToGameZone(data.playerBCardCheer, null, playerB, false, false);

        AddOrMoveCardToGameZone(new List<CardData>() { data.playerAFavourite }, null, playerA, false, false);
        AddOrMoveCardToGameZone(new List<CardData>() { data.playerBFavourite }, null, playerB, false, false);

        IEnumerator ShuffleAll()
        {
            StartCoroutine(DuelField_ActionLibrary.ShuffleDeck(GetZone(Lib.GameZone.Deck, TargetPlayer.Player)));
            StartCoroutine(DuelField_ActionLibrary.ShuffleDeck(GetZone(Lib.GameZone.Deck, TargetPlayer.Oponnent)));
            StartCoroutine(DuelField_ActionLibrary.ShuffleDeck(GetZone(Lib.GameZone.CardCheer, TargetPlayer.Player)));
            StartCoroutine(DuelField_ActionLibrary.ShuffleDeck(GetZone(Lib.GameZone.CardCheer, TargetPlayer.Oponnent)));
            yield break;
        }
        ActionItem.Add("MoveCard", ShuffleAll());

        DUELFIELDDATA.currentGamePhase = GAMEPHASE.InitialDraw;
        

        StartTurnCounter();
        TurnCounterText.text = currentTurn.ToString();

        yield return 0;
    }
    IEnumerator HandleInitialDraw()
    {
        bool ISMYDA = CUR_DA.playerID.Equals(PlayerInfo.INSTANCE.PlayerID);

        if (ISMYDA)
            playerInitialDraw = true;
        else
            oponnentInitialDrawP2 = true;

        DrawCard(CUR_DA);

        if (playerInitialDraw && oponnentInitialDrawP2)
        {
            DuelField.INSTANCE.DUELFIELDDATA.currentGamePhase = GAMEPHASE.Mulligan;
            ActionItem.Add("ShowForcedMulliganPanel", DuelField_ActionLibrary.ShowMulliganDecisionPanel());
        }
        yield break;
    }
    IEnumerator HandleMulligan()
    {
        bool ISMYDA = CUR_DA.playerID.Equals(PlayerInfo.INSTANCE.PlayerID);

        if (CUR_DA.yesOrNo)
            DrawCard(CUR_DA);

        if (ISMYDA)
        {
            playerMulligan = true;
        }
        else
        {
            oponnentMulligan = true;
        }

        if (playerMulligan && oponnentMulligan)
            if (DUELFIELDDATA.currentGamePhase != GAMEPHASE.ForcedMulligan)
            {
                DUELFIELDDATA.currentGamePhase = GAMEPHASE.ForcedMulligan;
                playerMulligan = oponnentMulligan = false;
            }
            else if (DUELFIELDDATA.currentGamePhase == GAMEPHASE.ForcedMulligan)
            {
                DUELFIELDDATA.currentGamePhase = GAMEPHASE.SettingUpBoard;
                ActionItem.Add("ShowSetupBoardReadyButton", DuelField_ActionLibrary.ShowSetupBoardReadyButton());
            }
        yield break;
    }
    IEnumerator HandleDuelUpdate()
    {
        DuelFieldData boardinfo = CUR_DA.duelFieldData;

        var myLife = ISMYTURN ? boardinfo.playerALife : boardinfo.playerBLife;
        var oppLife = ISMYTURN ? boardinfo.playerBLife : boardinfo.playerALife;

        var myFav = ISMYTURN ? boardinfo.playerAFavourite : boardinfo.playerBFavourite;
        var oppFav = ISMYTURN ? boardinfo.playerBFavourite : boardinfo.playerAFavourite;

        var myStage = ISMYTURN ? boardinfo.playerAStage : boardinfo.playerBStage;
        var oppStage = ISMYTURN ? boardinfo.playerBStage : boardinfo.playerAStage;

        var myBack = ISMYTURN ? boardinfo.playerABackPosition : boardinfo.playerBBackPosition;
        var oppBack = ISMYTURN ? boardinfo.playerBBackPosition : boardinfo.playerABackPosition;

        GetZone(Lib.GameZone.Favourite, TargetPlayer.Oponnent).GetComponentInChildren<Card>().Init(oppFav);

        AddOrMoveCardToGameZone(myLife, (List<GameObject>)null, TargetPlayer.Player, false, false, MOVEALLATONCE: false);
        RemoveCardFromZone(GetZone(Lib.GameZone.CardCheer, TargetPlayer.Player), myLife.Count);

        AddOrMoveCardToGameZone(oppLife, (List<GameObject>)null, TargetPlayer.Oponnent, false, false, MOVEALLATONCE: false);
        RemoveCardFromZone(GetZone(Lib.GameZone.CardCheer, TargetPlayer.Oponnent), oppLife.Count);

        AddOrMoveCardToGameZone(new List<CardData>() { oppStage }, (List<GameObject>)null, TargetPlayer.Oponnent, false, false);
        for (int n = 0; n < oppBack.Count; n++)
            AddOrMoveCardToGameZone(oppBack, (List<GameObject>)null, TargetPlayer.Oponnent, false, false);

        int removeAmount = oppBack.Count + 1;

        if (ISMYTURN)
            MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "DrawRequest", "AskDrawPhase", null);

        StartTurnCounter();
        TurnCounterText.text = currentTurn.ToString();

        DUELFIELDDATA.currentGamePhase = GAMEPHASE.DrawStep;

        yield break;
    }
    IEnumerator HandleResetStep()
    {
        var CUR_DA_PLAYER = ISMYTURN ? TargetPlayer.Player : TargetPlayer.Oponnent;

        ActionItem.Add("ShowMessage", DuelField_ActionLibrary.ShowMessage("Reset Step"));

        StartTurnCounter();
        TurnCounterText.text = currentTurn++.ToString();

        ResetCardTurnStatusForPlayer(CUR_DA_PLAYER);

        yield return HandleUnDoCollab();

        if (ISMYTURN)
        {
            if (GetZone(Lib.GameZone.Stage, TargetPlayer.Player).transform.GetComponentInChildren<Card>() != null)
            {
                DUELFIELDDATA.currentGamePhase = GAMEPHASE.DrawStep;
                _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "DrawRequest", "DrawRequest", null);
            }
            else
            {
                ActionItem.Add("ShowMessage", DuelField_ActionLibrary.ShowMessage("Select a new stage member"));
                DUELFIELDDATA.currentGamePhase = GAMEPHASE.ResetStepReSetStage;
                isServerActionLocked = true;
            }
        }
        else
        {
            DUELFIELDDATA.currentGamePhase = (GetZone(Lib.GameZone.Stage, TargetPlayer.Player).transform.GetComponentInChildren<Card>() != null) ? GAMEPHASE.DrawStep : GAMEPHASE.ResetStepReSetStage;
        }

        
        yield break;
    }
    IEnumerator HandleReSetStage()
    {

        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? TargetPlayer.Player : TargetPlayer.Oponnent;

        if (ISMYTURN)
        {
            if (CUR_DA.usedCard != null)
            {
                var cards = GetZone(CUR_DA.usedCard.curZone, CUR_DA_PLAYER).GetComponentsInChildren<RectTransform>().GameObjectInChildren();

                List<GameObject> cardsToBeMoved = new();
                foreach (var card in cards)
                {
                    var cardComp = card.GetComponent<Card>().Init(CUR_DA.usedCard);
                    card.transform.Rotate(0, 0, 0);
                    cardComp.suspended = false;
                    cardsToBeMoved.Add(card);
                }
                AddOrMoveCardToGameZone(null, cardsToBeMoved, CUR_DA_PLAYER, false, false);
            }

            hasAlreadyCollabed = false;
            _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "DrawRequest", "DrawRequest", null);

        }
        DUELFIELDDATA.currentGamePhase = GAMEPHASE.DrawStep;
        yield break;
    }
    IEnumerator HandleDrawPhase()
    {

        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? TargetPlayer.Player : TargetPlayer.Oponnent;

        ActionItem.Add("ShowMessage", DuelField_ActionLibrary.ShowMessage("Draw Step"));
        DrawCard(CUR_DA);

        if (ISMYTURN)
            _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "CheerRequest", "AskNextPhase", null);
        DUELFIELDDATA.currentGamePhase = GAMEPHASE.CheerStep;

        
        yield break;
    }
    IEnumerator HandleDefeatedHoloMember()
    {

        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? TargetPlayer.Player : TargetPlayer.Oponnent;

        GameObject zoneArt = GetZone(CUR_DA.targetCard.curZone, CUR_DA_PLAYER);

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
            var zone = GetZone(Lib.GameZone.Arquive, CUR_DA_PLAYER);
            AddOrMoveCardToGameZone(null, cardsToBeMoved: new List<GameObject> { childObject }, CUR_DA_PLAYER, false, false);
        }

        if (CUR_DA_TYPE.Equals("DefeatedHoloMember"))
        {

            if (ISMYTURN)
            {
                EndTurnButton.SetActive(false);
                //cardlist.Clear(); removed after the reformulation and breakdown of the class
            }

            DUELFIELDDATA.currentGamePhase = GAMEPHASE.HolomemDefeated;

            if ((!ISMYTURN))
                _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "CheerRequestHolomemDown", "", null);
        }
        yield break;

    }
    IEnumerator HandleHolomemDefatedSoGainCheer()
    {


        if (CUR_DA.cardList[0].cardNumber == "Empty")
        {
            playerCannotDrawFromCheer = true;
            if (DUELFIELDDATA.currentGamePhase == GAMEPHASE.HolomemDefeatedEnergyChoose)
                _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "CheerChooseRequestHolomemDown", "", null);

            if (DUELFIELDDATA.currentGamePhase == GAMEPHASE.CheerStepChoose)
                _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "CheerChooseRequest", "", null);
        }
        {
            DrawCard(CUR_DA);
            cheersAssignedThisChainAmount = 0;
            cheersAssignedThisChainTotal = CUR_DA.cardList.Count;
        }

        if (ISMYTURN)
            isServerActionLocked = true;

        DUELFIELDDATA.currentGamePhase = GAMEPHASE.HolomemDefeatedEnergyChoose;
        yield break;
    }
    IEnumerator HandleCheerStepEndDefeatedHolomem()
    {
        //validation to check if the player still have energy to assign due to Buzzholomem, for exemple
        if (cheersAssignedThisChainAmount < cheersAssignedThisChainTotal - 1)
        {
            DUELFIELDDATA.currentGamePhase = GAMEPHASE.HolomemDefeatedEnergyChoose;
            cheersAssignedThisChainAmount++;
            isServerActionLocked = true;
        }
        else
        {
            cheersAssignedThisChainAmount = 0;
            cheersAssignedThisChainTotal = 0;
            DUELFIELDDATA.currentGamePhase = GAMEPHASE.MainStep;
        }

        var target = ISMYTURN ? TargetPlayer.Player : TargetPlayer.Oponnent;

        // if player still have cheer, we attach, else, we skip
        if (!playerCannotDrawFromCheer)
            AttachCardToTarget(target);

        //if the player who is not the player is here, we return, he one assigning energy since his holomem died, we do not need to assign again
        if (!ISMYTURN)
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
        ActionItem.Add("ShowMessage", DuelField_ActionLibrary.ShowMessage("Cheer Step"));
        if (CUR_DA.cardList[0].cardNumber == "Empty")
        {
            playerCannotDrawFromCheer = true;
            if (DUELFIELDDATA.currentGamePhase == GAMEPHASE.HolomemDefeatedEnergyChoose)
                _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "CheerChooseRequestHolomemDown", "", null);

            if (DUELFIELDDATA.currentGamePhase == GAMEPHASE.CheerStepChoose)
                if (ISMYTURN)
                    _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "CheerChooseRequest", "", null);
        }
        {
            DrawCard(CUR_DA);
        }
        if (ISMYTURN)
            isServerActionLocked = true;

        DUELFIELDDATA.currentGamePhase = GAMEPHASE.CheerStepChoose;

        
        yield break;
    }
    IEnumerator HandleCheerStepEnd()
    {

        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? TargetPlayer.Player : TargetPlayer.Oponnent;

        AttachCardToTarget(CUR_DA_PLAYER);

        DUELFIELDDATA.currentGamePhase = GAMEPHASE.MainStep;

        if (ISMYTURN)
            _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "MainStartRequest", "CalledAt:CheerStepEnd", null);

        startofmain = true;
        yield break;
    }
    IEnumerator HandleMainPhase()
    {
        if (startofmain)
        {
            ActionItem.Add("ShowMessage", DuelField_ActionLibrary.ShowMessage("Main Step"));
        }

        startofmain = false;

        if (DUELFIELDDATA.currentGamePhase != GAMEPHASE.MainStep)
        {
            throw new Exception("not in the right gamephase, we're at " + CUR_DA_TYPE + " and tried to enter at" + DUELFIELDDATA.currentGamePhase.GetType());
        }

        if (ISMYTURN)
            DuelField_UI_MAP.INSTANCE.WS_PassTurnButton.SetActive(true);

        DUELFIELDDATA.currentGamePhase = GAMEPHASE.MainStep;
        yield break;
    }
    IEnumerator HandleMainPhaseDoAction()
    {
        yield break;
    }
    IEnumerator HandleEndturn()
    {
        ActionItem.Add("AwaitForActionsThenEndDuel", AwaitForActionsThenEndDuel());
        IEnumerator AwaitForActionsThenEndDuel() 
        {
            DUELFIELDDATA.currentPlayerTurn = CUR_DA.playerID;
            ISMYTURN = DUELFIELDDATA.currentPlayerTurn == PlayerInfo.INSTANCE.PlayerID;
            DUELFIELDDATA.currentGamePhase = GAMEPHASE.ResetStep;

            ActionItem.Add("ShowMessage", DuelField_ActionLibrary.ShowMessage("End Step"));
            DuelField.INSTANCE.hasAlreadyCollabed = false;
            startofmain = false;
            centerStageArtUsed = false;
            collabStageArtUsed = false;
            usedOshiSkill = false;
            DUELFIELDDATA.playerLimiteCardPlayed.Clear();

            if (ISMYTURN)
            {
                _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "ResetRequest", "CalledAt:Endturn", null);
                ActionItem.Add("GetUsableCards", GetUsableCards());
            }
            currentTurn++;
            ClearLogConsole();
            yield break;
        }
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
        if (CUR_DA == null && CUR_DA.playerID == null)
            yield break;

        if (PlayerInfo.INSTANCE.PlayerID.Equals(CUR_DA.playerID))
            DuelField_UI_MAP.INSTANCE.DisableAllOther().SetPanel(true, PanelType.SS_UI_General).SetPanel(true, PanelType.SS_WinPanel);
        else
            DuelField_UI_MAP.INSTANCE.DisableAllOther().SetPanel(true, PanelType.SS_UI_General).SetPanel(true, PanelType.SS_LosePanel);
    }
    IEnumerator HandleAttachSupportItem()
    {
        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? TargetPlayer.Player : TargetPlayer.Oponnent;

        AttachCardToTarget(CUR_DA_PLAYER);
        yield break;
    }
    IEnumerator HandlePlayHolomem()
    {

        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? TargetPlayer.Player : TargetPlayer.Oponnent;

        var zone = GetZone(CUR_DA.targetZone, CUR_DA_PLAYER);
        AddOrMoveCardToGameZone(cardsToBeCreated: new List<CardData>() { CUR_DA.usedCard }, null, CUR_DA_PLAYER, false, false, playedThisTurn: true);

        if (!ISMYTURN)
        {
            RemoveCardFromZone(cardHolderOponnent.gameObject, 1);
        }
        yield break;
    }
    IEnumerator HandleBloomHolomem()
    {

        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? TargetPlayer.Player : TargetPlayer.Oponnent;

        GameObject newHolder = GetZone(CUR_DA.usedCard.curZone, CUR_DA_PLAYER);
        GameObject oldHolder = GetZone(CUR_DA.usedCard.lastZone, CUR_DA_PLAYER);

        Card card = InstantiateAndPrepareCardsToMove(new List<CardData> { CUR_DA.usedCard }, null, CUR_DA_PLAYER, CUR_DA.toBottom, true).First().GetComponent<Card>();

        GameObject childObj = null;
        Card childCard = null;

        foreach (Card c in newHolder.GetComponentsInChildren<Card>())
        {
            if (c.cardNumber.Equals(CUR_DA.targetCard.cardNumber))
            {
                childObj = c.gameObject;
                childCard = c;
                card.suspended = childCard.suspended;
                if(card.suspended)
                    card.transform.Rotate(0, 0, 90);
            }
        }

        card.gameObject.transform.SetSiblingIndex(newHolder.transform.childCount - 1);

        card.bloomChild.Add(childObj);
        card.attachedEnergy = childCard.attachedEnergy;
        card.attachedEquipe = childCard.attachedEquipe;
        card.playedThisTurn = true;
        card.UpdateHP();
        childCard.attachedEnergy = null;
        childCard.attachedEquipe = null;

        bool isPlayedFromHand = card.lastZone.Equals(Lib.GameZone.Hand) || card.lastZone.Equals(Lib.GameZone.na);
        ActionItem.Add("MoveCard", DuelField_ActionLibrary.MoveCard(new () { card.gameObject }, oldHolder.transform, newHolder.transform, 0.2f, isPlayedFromHand, ISMYACTION));
        ActionItem.Add("BloomEffect", DuelField_ActionLibrary.BloomEffect(card.gameObject, 0.6f));

        if (!ISMYTURN)
        {
            RemoveCardFromZone(cardHolderOponnent.gameObject, 1);
        }
        else
        {
            EffectController.INSTANCE.ResolveOnBloomEffect(CUR_DA);
        }
        yield break;
    }
    IEnumerator HandleDoCollab()
    {
        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? TargetPlayer.Player : TargetPlayer.Oponnent;

        var oldHolder = GetZone(CUR_DA.usedCard.lastZone, CUR_DA_PLAYER);
        var newHolder = GetZone(CUR_DA.usedCard.curZone, CUR_DA_PLAYER);

        var cards = oldHolder.transform.GetComponentsInChildren<Card>().Where(item => item.cardNumber.Equals(CUR_DA.usedCard.cardNumber) ? item.Init(CUR_DA.usedCard).PlayedThisTurn(true) : item).Select(item => item.gameObject).ToList();

        foreach(GameObject obj in cards)
            obj.transform.SetParent(newHolder.transform);

        AddOrMoveCardToGameZone(null, cardsToBeMoved: cards, CUR_DA_PLAYER, false, false);

        if (DUELFIELDDATA.currentPlayerTurn == PlayerInfo.INSTANCE.PlayerID)
            EffectController.INSTANCE.ResolveOnCollabEffect(CUR_DA);
        yield break;
    }
    IEnumerator HandleUnDoCollab()
    {
        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? TargetPlayer.Player : TargetPlayer.Oponnent;

        if (!string.IsNullOrEmpty(CUR_DA.usedCard?.cardNumber))
        {
            var cards = GetZone(CUR_DA.usedCard.lastZone, CUR_DA_PLAYER).GetComponentsInChildren<Card>();

            List<GameObject> cardsToBeMoved = new();
            foreach (var cardComp in cards)
            {
                cardComp.Init(CUR_DA.usedCard);
                cardsToBeMoved.Add(cardComp.gameObject);
            }
            AddOrMoveCardToGameZone(null, cardsToBeMoved, CUR_DA_PLAYER, false, false, SuspendAfter: true);
        }
        yield break;
    }
    IEnumerator HandleRemoveEnergyFrom()
    {
        RemoveCardFromPosition(CUR_DA);
        yield break;
    }
    IEnumerator HandleAttachEnergyResponse()
    {
        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? TargetPlayer.Player : TargetPlayer.Oponnent;

        if (!playerCannotDrawFromCheer)
            AttachCardToTarget(CUR_DA_PLAYER);

        EffectController.INSTANCE.isServerResponseArrive = true;
        yield break;
    }
    IEnumerator HandlePayHoloPowerCost()
    {
        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? TargetPlayer.Player : TargetPlayer.Oponnent;

        GameObject zone = GetZone(Lib.GameZone.HoloPower, CUR_DA_PLAYER);
        for (int n = 0; n < CUR_DA.cardList.Count; n++)
        {
            foreach (Transform obj in zone.transform.GetComponentsInChildren<Transform>())
            {
                if (obj.name.Equals("Card"))
                {
                    Destroy(obj);
                    AddOrMoveCardToGameZone(new List<CardData>() { CUR_DA.cardList[n] }, null, CUR_DA_PLAYER, false, false);
                    break;
                }
            }
        }
        yield break;

    }
    IEnumerator HandleMoveCardToZone()
    {
        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? TargetPlayer.Player : TargetPlayer.Oponnent;

        GameObject oldHolder = GetZone(CUR_DA.usedCard.lastZone, CUR_DA_PLAYER);
        GameObject targetObj = GetZone(CUR_DA.usedCard.curZone, CUR_DA_PLAYER);

        List<RectTransform> cardsAtTarget = oldHolder.GetComponentsInChildren<RectTransform>().Where(rt => rt.name == "Card").ToList();
        List<GameObject> cardsToBeMoved = new();

        foreach (RectTransform card in cardsAtTarget)
        {
            Card cardComp = card.GetComponent<Card>();
            if (cardComp.cardNumber.Equals(CUR_DA.usedCard.cardNumber))
            {
                cardsToBeMoved.Add(card.gameObject);
                cardComp.Init(CUR_DA.usedCard);
                card.SetParent(targetObj.transform);
                break;
            }
        }

        ActionItem.Add("MoveMultipleCards", DuelField_ActionLibrary.MoveCard(cardsToBeMoved, oldHolder.transform, targetObj.transform, 0.2f, false, ISMYACTION));
        yield break;
    }
    IEnumerator HandleDisposeUsedSupport()
    {
        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? TargetPlayer.Player : TargetPlayer.Oponnent;

        var oldzone = GetZone(CUR_DA.usedCard.curZone, CUR_DA_PLAYER);
        AddOrMoveCardToGameZone(new List<CardData>() { CUR_DA.usedCard }, null, CUR_DA_PLAYER, false, false);

        if (!ISMYTURN)
            RemoveCardFromZone((ISMYTURN ? cardHolderPlayer : cardHolderOponnent).gameObject, 1);

        yield break;
    }
    IEnumerator HandleResolveOnEffect()
    {
        if (ISMYTURN)
            isServerActionLocked = true;

        EffectController.INSTANCE.isServerResponseArrive = true;

        yield break;
    }
    IEnumerator HandleActiveArtEffect()
    {
        if (ISMYTURN)
        {
            isServerActionLocked = true;
            EffectController.INSTANCE.ResolveOnArtEffect(CUR_DA);
        }
        yield break;
    }
    IEnumerator HandlePickFromListThenGiveBackFromHandDone()
    {
        if (ISMYTURN)
        {
            //we remove only one card from the pllayer hand, bacause we're using draw so the hands match
            RemoveCardFromZone(cardHolderOponnent.gameObject, 1);
            // add card to holopower since we are using draw which removes a card from the zone
            AddOrMoveCardToGameZone(new List<CardData>() { new CardData() { curZone = Lib.GameZone.HoloPower, lastZone = Lib.GameZone.Hand } }, null, TargetPlayer.Oponnent, false, false);
            //just making the card empty so the player dont see in the oponent hand holder, we can check in the log
            CUR_DA.cardList[0].cardNumber = "";
            DrawCard(CUR_DA);
        }
        else
        {
            //removing from the player hand the picked card to add with the draw the one from the holopower
            int n = -1;

            for (int contadorCardHand = 0; contadorCardHand < cardHolderPlayer.childCount; contadorCardHand++)
            {
                Card cardInHand = cardHolderPlayer.GetComponentsInChildren<RectTransform>()[contadorCardHand].GetComponent<Card>();
                if (cardInHand.cardNumber.Equals(CUR_DA.targetCard.cardNumber))
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
                DrawCard(CUR_DA);
            }
            // add card to holopower since we are using draw which removes a card from the zone
            // /\ saporra é gambiarra, ctz
            AddOrMoveCardToGameZone(new List<CardData>() { new CardData() { curZone = Lib.GameZone.HoloPower, lastZone = Lib.GameZone.Hand } }, null, TargetPlayer.Player, false, false);
        }
        yield break;
    }
    IEnumerator HandleRemoveCardsFromArquive()
    {
        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? TargetPlayer.Player : TargetPlayer.Oponnent;

        List<Card> canSelect = GetZone(Lib.GameZone.Arquive, CUR_DA_PLAYER).GetComponentsInChildren<Card>().ToList();
        for (int i = 0; i < CUR_DA.cardList.Count; i++)
        {
            bool match = false;
            int j = 0;
            for (; j < canSelect.Count; j++)
            {
                if (CUR_DA.cardList[i].cardNumber.Equals(canSelect[j].cardNumber))
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
        if (ISMYTURN)
        {
            var canSelect = cardHolderPlayer.GetComponentsInChildren<Card>().ToList();

            for (int i = 0; i < CUR_DA.cardList.Count; i++)
            {
                bool match = false;
                int j = 0;
                for (; j < canSelect.Count; j++)
                {
                    if (CUR_DA.cardList[i].cardNumber.Equals(canSelect[j].cardNumber))
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
            RemoveCardFromZone(cardHolderOponnent.gameObject, CUR_DA.cardList.Count);
        }
        yield break;
    }
    IEnumerator HandleDrawOshiEffect()
    {
        DrawCard(CUR_DA);
        yield break;
    }
    IEnumerator HandleDrawByEffect()
    {
        DrawCard(CUR_DA);
        yield break;
    }
    IEnumerator HandleShowCard()
    {
        EffectController.INSTANCE.EffectInformation.Add(CUR_DA);
        EffectController.INSTANCE.isServerResponseArrive = true;
        yield break;
    }
    IEnumerator HandleOnlyDiceRoll()
    {
        EffectController.INSTANCE.EffectInformation.Add(CUR_DA);
        yield break;
    }
    IEnumerator HandleRecoverHolomem()
    {
        GameObject zone = GetZone(CUR_DA.targetCard.curZone, (ISMYTURN ? TargetPlayer.Player : TargetPlayer.Oponnent));
        Card targetedCard = zone.GetComponentInChildren<Card>();
        targetedCard.currentHp = Math.Min(targetedCard.currentHp + CUR_DA.hpAmount, int.Parse(targetedCard.hp));
        targetedCard.UpdateHP();

        EffectController.INSTANCE.ResolveOnRecoveryEffect(targetedCard.ToCardData());
        yield break;
    }
    IEnumerator HandleInflicArtDamageToHolomem()
    {
        EffectController.INSTANCE.ResolveOnDamageResolveEffect(CUR_DA);
        yield break;
    }
    IEnumerator HandleInflicDamageToHolomem()
    {
        EffectController.INSTANCE.ResolveOnDamageResolveEffect(CUR_DA);
        yield break;
    }
    IEnumerator HandleInflicRecoilDamageToHolomem()
    {
        EffectController.INSTANCE.ResolveOnDamageResolveEffect(CUR_DA);
        yield break;
    }
    IEnumerator HandleResolveDamageToHolomem()
    {

        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? TargetPlayer.Player : TargetPlayer.Oponnent;

        var zoneArt = GetZone(CUR_DA.targetCard.curZone, CUR_DA_PLAYER);

        Card card = zoneArt.GetComponentInChildren<Card>();

        if (CUR_DA_TYPE.Equals("SetHPToFixedValue"))
            card.currentHp = CUR_DA.hpAmount;

        card.currentHp -= CUR_DA.hpAmount;

        card.UpdateHP();

        if (CUR_DA_TYPE.Equals("UsedArt"))
        {
            if (CUR_DA.usedCard.curZone.Equals(Lib.GameZone.Stage))
            {
                centerStageArtUsed = true;
            }
            else if (CUR_DA.usedCard.curZone.Equals(Lib.GameZone.Collaboration))
            {
                collabStageArtUsed = true;
            }

            if (ISMYTURN)
                if (centerStageArtUsed && collabStageArtUsed)
                    GenericActionCallBack(null, "MainEndturnRequest");
        }
        yield break;
    }
    IEnumerator HandleSwitchStageCard()
    {
        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? TargetPlayer.Player : TargetPlayer.Oponnent;

        //if is a retreat using the skill
        if (CUR_DA_TYPE.Equals("SwitchStageCardByRetreat"))
        {
            centerStageArtUsed = true;
        }

        SwitchCard(CUR_DA_PLAYER, Lib.GameZone.Stage, CUR_DA.targetCard.curZone);
        EffectController.INSTANCE.isServerResponseArrive = true;
        yield break;
    }
    IEnumerator HandleSwitchOpponentStageCard()
    {
        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? TargetPlayer.Player : TargetPlayer.Oponnent;

        SwitchCard(CUR_DA_PLAYER, Lib.GameZone.Stage, CUR_DA.targetCard.curZone);
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
        AddOrMoveCardToGameZone(null, cardsToBeMoved, player, false, false);

    }
    IEnumerator HandleRemoveEnergyAtAndSendToArquive()
    {
        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? TargetPlayer.Player : TargetPlayer.Oponnent;

        GameObject TargetZone = GetZone(CUR_DA.usedCard.curZone, CUR_DA_PLAYER);
        Card targetCard = TargetZone.transform.GetChild(TargetZone.transform.childCount - 1).GetComponent<Card>();
        int nn = 0;
        int jj = 0;

        foreach (GameObject attachEnergy in targetCard.attachedEnergy)
        {
            Card energyInfo = attachEnergy.GetComponent<Card>();
            if (energyInfo.cardNumber.Equals(CUR_DA.usedCard.cardNumber))
            {
                energyInfo.Init(CUR_DA.usedCard);
                nn = jj;
                break;
            }
            jj++;
        }
        if (CUR_DA_TYPE.Equals("RemoveEnergyAtAndSendToArquive"))
        {
            AddOrMoveCardToGameZone(null, cardsToBeMoved: new List<GameObject> { targetCard.attachedEnergy[nn] }, CUR_DA_PLAYER, false, false);
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
        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? TargetPlayer.Player : TargetPlayer.Oponnent;

        GameObject TargetZone = GetZone(CUR_DA.usedCard.curZone, CUR_DA_PLAYER);
        var targetCard = TargetZone.transform.GetChild(TargetZone.transform.childCount - 1).GetComponent<Card>();
        int i = 0;
        int j = 0;

        foreach (GameObject attachEnergy in targetCard.attachedEquipe)
        {
            Card energyInfo = attachEnergy.GetComponent<Card>();
            if (energyInfo.cardNumber.Equals(CUR_DA.usedCard.cardNumber))
            {
                energyInfo.Init(CUR_DA.usedCard);
                i = j;
                break;
            }
            j++;
        }
        AddOrMoveCardToGameZone(null, cardsToBeMoved: new List<GameObject> { targetCard.attachedEquipe[i] }, CUR_DA_PLAYER, false, false);
        targetCard.attachedEquipe[i].gameObject.SetActive(true);

        targetCard.attachedEquipe.RemoveAt(i);

        yield break;
    }
    IEnumerator HandleSuffleDeck()
    {
        yield return DuelField_ActionLibrary.ShuffleDeck(GetZone(Lib.GameZone.Deck, TargetPlayer.Player));
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

    public bool CanActivateOshiSkill(string cardNumber)
    {
        GameObject HoloPower = DuelField.INSTANCE.GetZone(Lib.GameZone.HoloPower, DuelField.TargetPlayer.Player);
        int holoPowerCount = DuelField.INSTANCE.GetZone(Lib.GameZone.HoloPower, DuelField.TargetPlayer.Player).transform.childCount - 1;

        if (holoPowerCount < HoloPowerCost(cardNumber, false))
            return false;

        switch (cardNumber)
        {
            case "hSD01-001":
                //check if theres another holomem to replace energy
                int backstagecount = DuelField.INSTANCE.CountBackStageTotal(false, DuelField.TargetPlayer.Player);
                if (DuelField.INSTANCE.GetZone(Lib.GameZone.Stage, DuelField.TargetPlayer.Player).GetComponentInChildren<Card>().attachedEnergy.Count < 1) return false;
                return (backstagecount > 0);
                break;
            case "xxx":
                break;
        }
        return true;
    }
    public bool CanActivateSPOshiSkill(string cardNumber)
    {
        int holoPowerCount = DuelField.INSTANCE.GetZone(Lib.GameZone.HoloPower, DuelField.TargetPlayer.Player).transform.childCount - 1;

        if (holoPowerCount < HoloPowerCost(cardNumber, true))
            return false;

        switch (cardNumber)
        {
            case "hSD01-001":
                //check if opponent has another holomem to switch for the center
                int backstagecount = DuelField.INSTANCE.CountBackStageTotal(true, DuelField.TargetPlayer.Oponnent);
                return (backstagecount > 0);
            case "hYS01-003":
                foreach (Card card in DuelField.INSTANCE.GetZone(Lib.GameZone.Arquive, DuelField.TargetPlayer.Player).GetComponentsInChildren<Card>())
                    if (card.cardType.Equals("ホロメン") || card.cardType.Equals("Buzzホロメン"))
                        return true;
                return false;
            case "hSD01-002":
                foreach (Card card in DuelField.INSTANCE.GetZone(Lib.GameZone.Arquive, DuelField.TargetPlayer.Player).GetComponentsInChildren<Card>())
                    if (card.cardType.Equals("エール") && new EffectController().GetAreasThatContainsCardWithColorOrTagOrName(color: "緑").Length > 0)
                        return true;
                return false;
        }
        return true;
    }
    private int HoloPowerCost(string cardNumber, bool SP = false)
    {
        if (SP)
            switch (cardNumber)
            {
                case "hSD01-001":
                    return 2;
            }
        if (!SP)
            switch (cardNumber)
            {
                case "hSD01-001":
                    return 1;
            }
        return 0;
    }
    public Dictionary<string, Func<IEnumerator>> MapActions()
    {
        return new Dictionary<string, Func<IEnumerator>> {
                    { "StartDuel", HandleStartDuel },
                    { "InitialDraw", HandleInitialDraw },
                    { "PAMulligan", HandleMulligan },
                    { "PBMulligan", HandleMulligan },
                    { "PANoMulligan", HandleMulligan },
                    { "PBNoMulligan", HandleMulligan },
                    { "PAMulliganF", HandleMulligan },
                    { "PBMulliganF", HandleMulligan },
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
}
