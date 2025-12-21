using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static DuelField;
using static DuelField_UI_MAP;
using static DuelFieldData;
using static UnityEngine.Rendering.BoolParameter;

public class DuelField : MonoBehaviour
{
    public static DuelField INSTANCE;
    public static Lib.GameZone[] DEFAULTHOLOMEMZONE = new Lib.GameZone[] { Lib.GameZone.Stage, Lib.GameZone.Collaboration, Lib.GameZone.BackStage1, Lib.GameZone.BackStage2, Lib.GameZone.BackStage3, Lib.GameZone.BackStage4, Lib.GameZone.BackStage5 };
    public static Lib.GameZone[] DEFAULTBACKSTAGE = new Lib.GameZone[] { Lib.GameZone.BackStage1, Lib.GameZone.BackStage2, Lib.GameZone.BackStage3, Lib.GameZone.BackStage4, Lib.GameZone.BackStage5 };
    [SerializeField] private List<GameObject> GameZones = new();
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
    public bool centerStageArtUsed = true;
    public bool collabStageArtUsed = true;

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
    public enum Player : byte
    {
        Player = 10,
        Oponnent = 11,
        FirstPlayer = 0,
        SecondPlayer = 1,
        PlayerA = 0,
        PlayerB = 1,
        TurnPlayer = 3,
        na = 99,
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
        //fake data for first turn
        DUELFIELDDATA.playerLimiteCardPlayed.Add(new Card() { });
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

        DuelField_TargetForEffectMenu.INSTANCE.enabled = true;
        DuelField_DetachCardMenu.INSTANCE.enabled = true;
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
            yield return 0;

            foreach (Card cds in Lib.temp)
                try
                {
                    Destroy(cds.gameObject);
                }
                catch (Exception e) { }


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
                            enableDrag = CardLib.CheckForPlayRestrictions(cardComponent.cardNumber);
                        }
                        else if (cardComponent.cardType.Contains("ツール") || cardComponent.cardType.Contains("マスコット") || cardComponent.cardType.Contains("ファン"))
                        {
                            enableDrag = CardLib.GetAndFilterCards(CheckFieldForHasRestrictionsToPlayEquip: true, CardList: new() { cardComponent }).Count > 1;
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
                            if (CardLib.GetAndFilterCards(WhoCanBloom: true, bloomLevel: new() { "Debut" }).Count > 0)
                                enableDrag = true;
                        }

                        if (cardComponent.bloomLevel.Equals("2nd") || cardComponent.cardNumber.Equals("hBP01-045"))
                        {
                            if (CardLib.GetAndFilterCards(WhoCanBloom: true, bloomLevel: new() { "1st" }).Count > 0)
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

        var childlist = CardLib.GetAndFilterCards(GetOnlyHolomem: true, player: Player.Player, onlyVisible: true);
        foreach (Card card in childlist)
            card.Glow();

        yield break;
    }
    IEnumerator HandleMoveCardToZone()
    {
        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? Player.Player : Player.Oponnent;

        if (CUR_DA.actionTarget == Player.na)
            CUR_DA_PLAYER = CUR_DA.GetClientSideType(CUR_DA.actionTarget);
        
        if (CUR_DA.usedCard.lastZone.Equals(Lib.GameZone.Hand) || CUR_DA_TYPE.Equals("DisposeCard"))
        {
            bool onPlay = CUR_DA_TYPE.Equals("PlayHolomem");
            bool onBloom = CUR_DA_TYPE.Equals("BloomHolomem");

            var playedThisTurn = false;
            if (onPlay || onBloom)
                playedThisTurn = true;

            var list = AddOrMoveCardToGameZone(new() { CUR_DA.usedCard }, null, CUR_DA_PLAYER, false, false, playedThisTurn: playedThisTurn);

            if (!ISMYTURN)
                RemoveCardFromZone(GetZone(Lib.GameZone.Hand, Player.Oponnent), 1, findEnergy: CUR_DA.usedCard.cardType.Equals("エール"));

            var attachmentTypes = new List<string>() { "サポート・マスコット", "サポート・アイテム", "サポート・アイテム・LIMITED", "エール" };
            var isEquipable = attachmentTypes.Contains(CUR_DA.usedCard.cardType);

            if (!onPlay && isEquipable)
            {
                Card target = CardLib.GetAndFilterCards(player: CUR_DA_PLAYER, CardToBeFound: CUR_DA.targetCard, OnlyWithAttachment: CardLib.attachType.all).FirstOrDefault();
                if (target == null)
                    target = CardLib.GetAndFilterCards(player: CUR_DA_PLAYER, CardToBeFound: CUR_DA.targetCard).FirstOrDefault();

                var createdCard = list.FirstOrDefault().GetComponent<Card>();
                if (createdCard != null)
                    if (onBloom)
                        createdCard.BloomFrom(target);
                    else
                        createdCard.AttachTo(target);
            }
        }
        else
        {
            var curZone = CUR_DA.lookLastZone ? new[] { CUR_DA.usedCard.lastZone } : new[] {  CUR_DA.usedCard.curZone };
            Card card = CardLib.GetAndFilterCards(player: CUR_DA_PLAYER, gameZones: curZone, cardNumber: new() { CUR_DA.usedCard.cardNumber }).FirstOrDefault();//note to futre, this may lead to problemns if two holomens with same number, one visible and another not
            card.Init(CUR_DA.usedCard);
            card.Detach();
            AddOrMoveCardToGameZone(null, new() { card.gameObject }, CUR_DA_PLAYER, false, false);

            if (CUR_DA.targetCard != null)
            {
                Card father = CardLib.GetAndFilterCards(player: CUR_DA_PLAYER, CardToBeFound: CUR_DA.targetCard, OnlyWithAttachment: CardLib.attachType.all).FirstOrDefault();
                if (father == null)
                    father = CardLib.GetAndFilterCards(player: CUR_DA_PLAYER, CardToBeFound: CUR_DA.targetCard).FirstOrDefault();

                if (father != null)
                {
                    card.AttachTo(father);
                }
                yield break;
            }
        }

        ActionItem.Add("SetVisibility", SetVisibility(CUR_DA.usedCard.curZone, CUR_DA_PLAYER));

        IEnumerator SetVisibility(Lib.GameZone zone, Player player)
        {
            var cards = GetZone(zone, player).GetComponentsInChildren<Card>();
            List<Tuple<Card, bool>> returned = CardLib.GetActiveParent(cards);
            foreach (var each in returned)
            {
                each.Item1.gameObject.SetActive(each.Item2);
            }
            yield break;
        }
    }

    public List<GameObject> InstantiateAndPrepareCardsToMove(List<CardData> cardsToBeCreated, List<GameObject> cardsToBeMoved, Player player, bool toBottom = false, bool playedThisTurn = false)
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
                allCardsToMove.Add(obj);

                List<Card> list = new();

                list.AddRange(card.attachedEnergy);
                list.AddRange(card.attachedEquipe);
                list.AddRange(card.bloomChild);

                foreach (Card objchild in list)
                {
                    objchild.lastZone = objchild.curZone;
                    objchild.curZone = card.curZone;
                }
                allCardsToMove.AddRange(list.Select(item => item.gameObject));
            }

            foreach (GameObject obj in allCardsToMove)
            {
                Card card = obj.GetComponent<Card>();
                card.NeedEnergyCounter();
                obj.transform.SetParent(GetZone(card.curZone, player).transform);
            }
        }
        return allCardsToMove;
    }
    public List<GameObject> AddOrMoveCardToGameZone(List<CardData> cardsToBeCreated, List<GameObject> cardsToBeMoved, Player player, bool shuffle = false, bool toBottom = false, bool playedThisTurn = false, bool MOVEALLATONCE = true, bool SuspendAfter = false)
    {
        var allCardsToMove = InstantiateAndPrepareCardsToMove(cardsToBeCreated, cardsToBeMoved, player, toBottom, playedThisTurn);
        foreach (GameObject cardObj in allCardsToMove)
        {

            Card card = cardObj.GetComponent<Card>();
            GameObject newHolder = GetZone(card.curZone, player);
            GameObject oldHolder = GetZone(card.lastZone, player);
            bool isPlayedFromHand = card.lastZone.Equals(Lib.GameZone.Hand) || card.lastZone.Equals(Lib.GameZone.na);

            if (MOVEALLATONCE)
            {
                ActionItem.Add("MoveCard", DuelField_ActionLibrary.MoveCard(allCardsToMove, oldHolder.transform, newHolder.transform, 0.2f, isPlayedFromHand, ISMYTURN, SuspendAfter));
                break;
            }
            else
            {
                ActionItem.Add("MoveCard", DuelField_ActionLibrary.MoveCard(new List<GameObject> { cardObj }, oldHolder.transform, newHolder.transform, 0.2f, isPlayedFromHand, ISMYTURN, SuspendAfter));
            }
        }
        return allCardsToMove;
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
        var CUR_DA_PLAYER = ISMYACTION ? Player.Player : Player.Oponnent;

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
                {
                    cardNumber = "",
                    lastZone = Lib.GameZone.Hand,
                    curZone = Lib.GameZone.Deck
                });
            }

            AddOrMoveCardToGameZone(cardsFakeData, null, CUR_DA_PLAYER, false, false, MOVEALLATONCE: false);

            if (draw.suffle)
                ActionItem.Add("ShuffleDeck", DuelField_ActionLibrary.ShuffleDeck(GetZone(Lib.GameZone.Deck, CUR_DA_PLAYER)));
        }

        if (!ISMYACTION)
        foreach (CardData cardData in draw.cardList) 
        { 
          cardData.cardNumber = "";
        }

        AddOrMoveCardToGameZone(draw.cardList, (List<GameObject>)null, CUR_DA_PLAYER, draw.toBottom, draw.suffle, MOVEALLATONCE: false);
        RemoveCardFromZone(GetZone(draw.cardList[0].lastZone, CUR_DA_PLAYER), draw.cardList.Count);
    }
    public void RemoveCardFromZone(GameObject father, int amount, bool findEnergy = false)
    {
        ActionItem.Add("RemoveCardFromZone", DuelField_ActionLibrary.RemoveCardFromZone(father, amount, findEnergy));
    }
    public GameObject GetZone(Lib.GameZone s, Player player)
    {
        if (player == Player.FirstPlayer)
            if (DuelField.INSTANCE.DUELFIELDDATA.firstPlayer.Equals(PlayerInfo.INSTANCE.PlayerID))
                player = Player.Player;
            else
                player = Player.Oponnent;

        if (player == Player.SecondPlayer)
            if (DuelField.INSTANCE.DUELFIELDDATA.secondPlayer.Equals(PlayerInfo.INSTANCE.PlayerID))
                player = Player.Player;
            else
                player = Player.Oponnent;

        if (s.Equals(Lib.GameZone.Hand))
        {
            return (Player.Player == player ? cardHolderPlayer.gameObject : cardHolderOponnent.gameObject);
        }

        if (s.Equals(Lib.GameZone.na))
            s = Lib.GameZone.Deck;


        int maxZones = GameZones.Count;
        int nZones = 0;

        if (Player.Oponnent == player)
            nZones = GameZones.Count / 2;

        if (Player.Player == player)
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
    public GameObject GetCardPrefab(string target)
    {
        if (target.Equals("PlayerHand") || target.Equals("OponentHand"))
            return oldCardPrefab;
        else
            return cardPrefab;
    }
    IEnumerator HandleStartDuel()
    {
        ActionItem.Add("ShowMessage", DuelField_ActionLibrary.ShowMessage("Starting Duel"));

        var data = CUR_DA.duelFieldData;

        Player playerA = Player.Player;
        Player playerB = Player.Oponnent;

        if (!DUELFIELDDATA.firstPlayer.Equals(PlayerInfo.INSTANCE.PlayerID))
        {
            playerA = Player.Oponnent;
            playerB = Player.Player;
        }

        AddOrMoveCardToGameZone(data.playerADeck, null, playerA, false, false);
        AddOrMoveCardToGameZone(data.playerBDeck, null, playerB, false, false);

        AddOrMoveCardToGameZone(data.playerACardCheer, null, playerA, false, false);
        AddOrMoveCardToGameZone(data.playerBCardCheer, null, playerB, false, false);

        AddOrMoveCardToGameZone(new List<CardData>() { data.playerAFavourite }, null, playerA, false, false);
        AddOrMoveCardToGameZone(new List<CardData>() { data.playerBFavourite }, null, playerB, false, false);

        IEnumerator ShuffleAll()
        {
            StartCoroutine(DuelField_ActionLibrary.ShuffleDeck(GetZone(Lib.GameZone.Deck, Player.Player)));
            StartCoroutine(DuelField_ActionLibrary.ShuffleDeck(GetZone(Lib.GameZone.Deck, Player.Oponnent)));
            StartCoroutine(DuelField_ActionLibrary.ShuffleDeck(GetZone(Lib.GameZone.CardCheer, Player.Player)));
            StartCoroutine(DuelField_ActionLibrary.ShuffleDeck(GetZone(Lib.GameZone.CardCheer, Player.Oponnent)));
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

        GetZone(Lib.GameZone.Favourite, Player.Oponnent).GetComponentInChildren<Card>().Init(oppFav);

        AddOrMoveCardToGameZone(myLife, (List<GameObject>)null, Player.Player, false, false, MOVEALLATONCE: false);
        RemoveCardFromZone(GetZone(Lib.GameZone.CardCheer, Player.Player), myLife.Count);

        AddOrMoveCardToGameZone(oppLife, (List<GameObject>)null, Player.Oponnent, false, false, MOVEALLATONCE: false);
        RemoveCardFromZone(GetZone(Lib.GameZone.CardCheer, Player.Oponnent), oppLife.Count);

        AddOrMoveCardToGameZone(new List<CardData>() { oppStage }, null, Player.Oponnent, false, false);
        RemoveCardFromZone(GetZone(Lib.GameZone.Hand, Player.Oponnent), 1);

        AddOrMoveCardToGameZone(oppBack, null, Player.Oponnent, false, false, MOVEALLATONCE: false);
        RemoveCardFromZone(GetZone(Lib.GameZone.Hand, Player.Oponnent), oppBack.Count);

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
        var CUR_DA_PLAYER = ISMYTURN ? Player.Player : Player.Oponnent;

        ActionItem.Add("ShowMessage", DuelField_ActionLibrary.ShowMessage("Reset Step"));

        StartTurnCounter();
        TurnCounterText.text = currentTurn++.ToString();

        //unsuspend all holomems
        var childlist = CardLib.GetAndFilterCards(player: CUR_DA_PLAYER, OnlyBackStage: true);
        foreach (Card c in childlist)
        {
            if (c.suspended)
            {
                c.suspended = false;
                c.transform.rotation = Quaternion.Euler(0, 0, 0);
                c.Flip(true);
            }
            c.playedThisTurn = false;
        }

        yield return HandleUnDoCollab();

        if (ISMYTURN)
        {
            if (GetZone(Lib.GameZone.Stage, Player.Player).transform.GetComponentInChildren<Card>() != null)
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
            DUELFIELDDATA.currentGamePhase = (GetZone(Lib.GameZone.Stage, Player.Player).transform.GetComponentInChildren<Card>() != null) ? GAMEPHASE.DrawStep : GAMEPHASE.ResetStepReSetStage;
        }


        yield break;
    }
    IEnumerator HandleReSetStage()
    {

        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? Player.Player : Player.Oponnent;

        if (ISMYTURN)
        {
            if (CUR_DA.usedCard != null)
            {
                var cards = GetZone(CUR_DA.usedCard.curZone, CUR_DA_PLAYER).GetComponentsInChildren<RectTransform>().Select(item => item.gameObject);

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
        var CUR_DA_PLAYER = ISMYACTION ? Player.Player : Player.Oponnent;

        ActionItem.Add("ShowMessage", DuelField_ActionLibrary.ShowMessage("Draw Step"));
        DrawCard(CUR_DA);

        if (ISMYTURN)
            _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "CheerRequest", "AskNextPhase", null);
        DUELFIELDDATA.currentGamePhase = GAMEPHASE.CheerStep;


        yield break;
    }
    IEnumerator HandleDefeatedHoloMember()
    {
        DUELFIELDDATA.currentGamePhase = GAMEPHASE.HolomemDefeated;

        if (ISMYTURN)
            EndTurnButton.SetActive(false);
        else
            _ = MatchConnection.INSTANCE.SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "CheerRequestHolomemDown", "", null);

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

        var target = ISMYTURN ? Player.Player : Player.Oponnent;

        // if player still have cheer, we attach, else, we skip
        if (!playerCannotDrawFromCheer)
            yield return HandleMoveCardToZone();

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
        {
            DuelField_UI_MAP.INSTANCE.WS_PassTurnButton.SetActive(true);
            ActionItem.Add("GetUsableCards", GetUsableCards());
        }
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
    IEnumerator HandleDoCollab()
    {
        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? Player.Player : Player.Oponnent;

        var oldHolder = GetZone(CUR_DA.usedCard.lastZone, CUR_DA_PLAYER);
        var newHolder = GetZone(CUR_DA.usedCard.curZone, CUR_DA_PLAYER);

        var cards = oldHolder.transform.GetComponentsInChildren<Card>().Where(item => item.cardNumber.Equals(CUR_DA.usedCard.cardNumber) ? item.Init(CUR_DA.usedCard).PlayedThisTurn(true) : item).Select(item => item.gameObject).ToList();

        foreach (GameObject obj in cards)
            obj.transform.SetParent(newHolder.transform);

        AddOrMoveCardToGameZone(null, cardsToBeMoved: cards, CUR_DA_PLAYER, false, false);
        yield break;
    }
    IEnumerator HandleUnDoCollab()
    {
        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? Player.Player : Player.Oponnent;

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
    IEnumerator HandleDrawByEffect()
    {
        DrawCard(CUR_DA);
        yield break;
    }
    IEnumerator HandleRecoverHolomem()
    {
        GameObject zone = GetZone(CUR_DA.targetCard.curZone, (ISMYTURN ? Player.Player : Player.Oponnent));
        Card targetedCard = zone.GetComponentInChildren<Card>();
        targetedCard.currentHp = Math.Min(targetedCard.currentHp + CUR_DA.hpAmount, int.Parse(targetedCard.hp));
        targetedCard.UpdateHP();
        yield break;
    }
    IEnumerator HandleResolveDamage()
    {
        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? Player.Player : Player.Oponnent;

        Card card = CardLib.GetAndFilterCards(gameZones: new[] { CUR_DA.targetCard.curZone }, onlyVisible: true, GetOnlyHolomem: true).First();

        if (CUR_DA.hpFixedValue > 0)
            card.currentHp = CUR_DA.hpFixedValue;

        if (CUR_DA.hpAmount > 0)
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
                GenericActionCallBack(null, "ResolveAfterDamageEffects");
        }
        yield break;
    }
    IEnumerator HandleTriggerAfterDamageResolution()
    {
        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        if (ISMYTURN)
            if (centerStageArtUsed && collabStageArtUsed)
                GenericActionCallBack(null, "MainEndturnRequest");

        yield break;
    }
    IEnumerator HandleSuffleDeck()
    {
        var ISMYACTION = PlayerInfo.INSTANCE.PlayerID == CUR_DA.playerID;
        var CUR_DA_PLAYER = ISMYACTION ? Player.Player : Player.Oponnent;

        foreach (Lib.GameZone zone in CUR_DA.targetedZones)
        {
            yield return DuelField_ActionLibrary.ShuffleDeck(GetZone(zone, CUR_DA_PLAYER));
        }
    }
    internal bool CanSummonHolomemHere(Card cardBeingChecked, Lib.GameZone targetZoneEnum)
    {
        bool canContinue = false;
        if (cardBeingChecked.bloomLevel.Equals("Debut") || cardBeingChecked.bloomLevel.Equals("Spot")) canContinue = true;

        if (!canContinue) return false;

        int count = CardLib.CountPlayerActiveHolomem();

        if (targetZoneEnum.Equals(Lib.GameZone.BackStage1) || targetZoneEnum.Equals(Lib.GameZone.BackStage2) || targetZoneEnum.Equals(Lib.GameZone.BackStage3) || targetZoneEnum.Equals(Lib.GameZone.BackStage4) || targetZoneEnum.Equals(Lib.GameZone.BackStage5))
        {
            canContinue = false;
            if (count < 5)
                canContinue = true;
        }
        return canContinue;
    }
    public Dictionary<string, Func<IEnumerator>> MapActions()
    {
        return new Dictionary<string, Func<IEnumerator>> 
        {
                    //Starting Duel
                    { "StartDuel", HandleStartDuel },
                    { "InitialDraw", HandleInitialDraw },
                    { "PAMulligan", HandleMulligan },
                    { "PBMulligan", HandleMulligan },
                    { "PANoMulligan", HandleMulligan },
                    { "PBNoMulligan", HandleMulligan },
                    { "PAMulliganF", HandleMulligan },
                    { "PBMulliganF", HandleMulligan },
                    { "DuelUpdate", HandleDuelUpdate},
                    //Duelphase Flow
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
                    //General
                    { "DoCollab", HandleDoCollab },
                    { "UnDoCollab", HandleUnDoCollab },
                    { "DrawByEffect", HandleDrawByEffect },
                    { "ShowCard", HandleShowCard },
                    { "RollDice", HandleShowDiceRoll },
                    { "OnlyDiceRoll", HandleShowDiceRoll },
                    { "SuffleDeck", HandleSuffleDeck },
                    { "RecoverHolomem", HandleRecoverHolomem },
                    //Card Moviment
                    { "BloomHolomem", HandleMoveCardToZone },
                    { "PlayHolomem", HandleMoveCardToZone },
                    { "DisposeCard", HandleMoveCardToZone },
                    { "MoveCardToZone", HandleMoveCardToZone },
                    //Inflict Damage
                    { "InflicArtDamageToHolomem", HandleAskServerToInflictDamage },
                    { "InflicDamageToHolomem", HandleAskServerToInflictDamage },
                    { "InflicRecoilDamageToHolomem", HandleAskServerToInflictDamage },
                    { "AskToBlockDamage", HandleAskBlockDamage },
                    { "SetHPToFixedValue", HandleResolveDamage },
                    { "ResolveDamageToHolomem", HandleResolveDamage },
                    { "TriggerAfterDamageEffect", HandleTriggerAfterDamageEffect },
                    //Trigger Effect
                    { "ClearEffects", HandleClearEffects },
                    { "OnCollabEffect", HandleResolveOnEffect },
                    { "OnArtEffect", HandleResolveOnEffect },
                    { "ResolveOnAttachEffect", HandleResolveOnEffect },
                    { "ResolveOnSupportEffect", HandleResolveOnEffect },
                    { "ResolveOnOshiEffect", HandleResolveOnEffect },
                    { "ResolveOnOshiSPEffect", HandleResolveOnEffect },
                    { "ActiveArtEffect", HandleResolveOnEffect },
                    { "RetreatAction", HandleResolveOnEffect },
                };
    }

    private IEnumerator HandleTriggerAfterDamageEffect()
    {
        if (CUR_DA.cardList != null && CUR_DA.cardList.Count > 0)
            yield return HandleResolveOnEffect();
        else
            DuelField.INSTANCE.GenericActionCallBack(new DuelAction() {playerID = PlayerInfo.INSTANCE.PlayerID}, "SendHolomemDefeteadToArquive");
    }

    private IEnumerator HandleAskBlockDamage()
    {
        if (CUR_DA.cardList != null && CUR_DA.cardList.Count > 0)
            yield return HandleResolveOnEffect();
        else
            yield return HandleAskServerToInflictDamage();
    }

    private IEnumerator HandleResolveOnEffect()
    {
        Player target = CUR_DA.GetClientSideType(CUR_DA.actionTarget);

        if (CUR_DA.playerID.Equals(PlayerInfo.INSTANCE.PlayerID))
            switch (CUR_DA.displayType)
            {
                case DuelAction.Display.Detach:
                    yield return DuelField_DetachCardMenu.INSTANCE.SetupSelectableItems(new DuelAction() { }, CUR_DA.targetedZones, IsACheer: CUR_DA.targetType, target, canClosePanel: CUR_DA.canClosePanel);
                    DuelField.INSTANCE.GenericActionCallBack(DuelField_DetachCardMenu.GetDA(), CUR_DA_TYPE);
                    break;
                case DuelAction.Display.Target:
                    yield return DuelField_TargetForEffectMenu.INSTANCE.SetupSelectableItems(new DuelAction() { }, target, CUR_DA.targetedZones, Lib.ConvertToCard(CUR_DA.cardList), canClosePanel: CUR_DA.canClosePanel);
                    DuelField.INSTANCE.GenericActionCallBack(DuelField_TargetForEffectMenu.GetDA(), CUR_DA_TYPE);
                    break;
                case DuelAction.Display.ListPickAndReorder:
                    yield return DuelField_ShowListPickThenReorder.INSTANCE.SetupSelectableItems(new DuelAction() { }, Lib.ConvertToCard(CUR_DA.cardList), Lib.ConvertToCard(CUR_DA.MapSelectable()), CUR_DA.reSelect, CUR_DA.maxPick, canClosePanel: CUR_DA.canClosePanel);
                    DuelField.INSTANCE.GenericActionCallBack(DuelField_ShowListPickThenReorder.GetDA(), CUR_DA_TYPE);
                    break;
                case DuelAction.Display.Number:
                    yield return DuelField_ShowANumberList.INSTANCE.SetupSelectableItems(CUR_DA.selectableList[0], CUR_DA.selectableList[1], new DuelAction() { });
                    DuelField.INSTANCE.GenericActionCallBack(DuelField_ShowANumberList.GetDA(), CUR_DA_TYPE);
                    break;
                case DuelAction.Display.ListPickOne:
                    yield return DuelField_ShowAlistPickOne.INSTANCE.SetupSelectableItems(new DuelAction() { }, Lib.ConvertToCard(CUR_DA.cardList), Lib.ConvertToCard(CUR_DA.MapSelectable()), canClosePanel: CUR_DA.canClosePanel);
                    DuelField.INSTANCE.GenericActionCallBack(DuelField_ShowAlistPickOne.GetDA(), CUR_DA_TYPE);
                    break;
                case DuelAction.Display.YesOrNo:
                    yield return DuelField_YesOrNoMenu.INSTANCE.ShowYesOrNoMenu(new DuelAction() { }, CUR_DA.message.ToString());
                    DuelField.INSTANCE.GenericActionCallBack(DuelField_YesOrNoMenu.GetDA(), CUR_DA_TYPE);
                    break;
            }
        yield break;
    }

    private IEnumerator HandleClearEffects()
    {
        yield break;
    }
    private IEnumerator HandleShowCard()
    {
        yield break;
    }
    private IEnumerator HandleShowDiceRoll()
    {
        yield break;
    }
    IEnumerator HandleAskServerToInflictDamage()
    {
        DuelField.INSTANCE.GenericActionCallBack(CUR_DA, "AskServerToResolveDamageToHolomem");
        yield break;
    }
}
