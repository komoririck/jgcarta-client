using Assets.Scripts.HUD.DuelField;
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

public class DuelField : MonoBehaviour
{
    public static DuelField INSTANCE;
    public static Lib.GameZone[] DEFAULTHOLOMEMZONE = new Lib.GameZone[] { Lib.GameZone.Stage, Lib.GameZone.Collaboration, Lib.GameZone.BackStage1, Lib.GameZone.BackStage2, Lib.GameZone.BackStage3, Lib.GameZone.BackStage4, Lib.GameZone.BackStage5 };
    public static Lib.GameZone[] DEFAULTBACKSTAGE = new Lib.GameZone[] { Lib.GameZone.BackStage1, Lib.GameZone.BackStage2, Lib.GameZone.BackStage3, Lib.GameZone.BackStage4, Lib.GameZone.BackStage5 };
    [SerializeField] public List<GameObject> GameZones = new();
    public List<GameObject> GetGameZones() { return GameZones; }
    public Dictionary<Player, string>? players { get; set; }
    public Dictionary<string, Player>? playersType { get; set; }
    public List<GameObject> changedZones { get; internal set; } = new();

    public Player? turnPlayer;

    public GAMEPHASE GamePhase = GAMEPHASE.StartMatch;

    private const int TURN_TIMER_SECONDS = 120;
    private int playerTimers;
    private CancellationTokenSource countdownTokenSource;
    [SerializeField] private TMP_Text TimmerText;
    [SerializeField] private TMP_Text TurnCounterText;

    [SerializeField] public GameObject cardPrefab;

    public RectTransform cardHolderPlayer;
    public RectTransform cardHolderOponnent;

    public RectTransform cardLifeHolderA;
    public RectTransform cardLifeHolderB;

    [SerializeField] private GameObject EffectConfirmationTab = null;
    [SerializeField] private GameObject EffectConfirmationYesButton = null;
    [SerializeField] private GameObject EffectConfirmationNoButton = null;

    private int currentTurn;
    private int playerMulligan = 0;

    public bool hasAlreadyCollabed = false;
    public bool centerStageArtUsed = true;
    public bool collabStageArtUsed = true;

    public bool usedSPOshiSkill = false;
    public bool usedOshiSkill = false;

    [SerializeField] Sprite viewTypeActionImg;
    [SerializeField] Sprite viewTypeViewImg;
    public bool isViewMode = true;
    Button isViewModeButton = null;


    public List<Card> playerLimiteCardPlayed = null;

    [Flags]
    public enum Player : byte
    {
        na = 0,
        PlayerA = 1,
        PlayerB = 2,
        Player = 10,
        Oponnent = 11,
        activationOwner = 100,
    }

    Dictionary<string, Func<IEnumerator>> serverActionHandlers;
    internal bool isSelectionCompleted;
    private int requestIndex = 0;
    public ActionQueue actionQueue = null;

    public bool IsMyTurn()
    {
        if (players[(Player)turnPlayer].Equals(PlayerInfo.INSTANCE.PlayerID))
            return true;
        return false;
    }
    public bool IsMyAction(Player? player = null)
    {
        if (players[(Player)player].Equals(PlayerInfo.INSTANCE.PlayerID))
            return true;
        return false;
    }
    IEnumerator Start()
    {
        actionQueue = new();

        INSTANCE = this;
        ClearLogConsole();

        DuelField_UI_MAP.INSTANCE = FindAnyObjectByType<DuelField_UI_MAP>();
        SetActionToggleMode(ToggleStatus.on);

        DuelField_UI_MAP.INSTANCE.SS_MulliganPanelYes.GetComponent<Button>().onClick.AddListener(() => { MulliganBoxAwnser(true); });
        DuelField_UI_MAP.INSTANCE.SS_MulliganPanelNo.GetComponent<Button>().onClick.AddListener(() => { MulliganBoxAwnser(false); });
        DuelField_UI_MAP.INSTANCE.WS_PassTurnButton.SetActive(false);
        playerLimiteCardPlayed.Add(new Card() { });

        serverActionHandlers = MapActions();

        isViewModeButton ??= DuelField_UI_MAP.INSTANCE.SS_UI_ActionToggleButton.GetComponent<Button>();
        isViewModeButton.onClick.AddListener(() => { SetActionToggleMode(ToggleStatus.flip); });

        requestIndex = 0;
        actionQueue.CancelAll();
        StopAllCoroutines();
        GameLifecycle.Set(GameState.Ready);
        yield return null;
    }

    void Update()
    {
        if (!GameLifecycle.IsReady)
            return;

        while (requestIndex < MatchConnection.INSTANCE.PendingActions.Count)
        {
            if (MatchConnection.INSTANCE.PendingActions[requestIndex].type == "Waitingforopponent" || MatchConnection.INSTANCE.PendingActions[requestIndex].description == "mt")
            {
                requestIndex++;
                continue;
            }

            var request = MatchConnection.INSTANCE.PendingActions[requestIndex];

            if (serverActionHandlers.TryGetValue(request.description, out var handler))
                actionQueue.Enqueue(request.description, handler(), request.duelAction);

            requestIndex++;
        }

        if (GameLifecycle.IsReady && !actionQueue.IsRunning)
        {
            StartCoroutine(actionQueue.Run());
        }

    }

    //FUNCTIONS ASSIGNED IN THE INSPECTOR
    public void ReturnButton()
    {
        MatchConnection.INSTANCE.SendRequest(null, "EndDuel");
        MatchConnection.INSTANCE.CloseConnection();

        GameLifecycle.Set(GameState.Boot);
        actionQueue.CancelAll();
        StopAllCoroutines();

        Destroy(GameObject.Find("HUD DuelField"));
        Destroy(this);
        SceneManager.LoadScene("Match");
    }
    public void DuelReadyButton()
    {
        if (GameZones[6].GetComponentInChildren<Card>() == null)
            return;

        DuelField_UI_MAP.INSTANCE.WS_ReadyButton.SetActive(false);
    }
    public void EndTurnHUDButton()
    {
        DuelField_UI_MAP.INSTANCE.WS_PassTurnButton.SetActive(false);
        MatchConnection.INSTANCE.SendRequest(null, "MainEndturnRequest");
    }
    public void MulliganBoxAwnser(bool awnser)
    {
        DuelField_UI_MAP.INSTANCE.SetPanel(false, DuelField_UI_MAP.PanelType.SS_MulliganPanel);
        DuelField_UI_MAP.INSTANCE.SetPanel(true, DuelField_UI_MAP.PanelType.SS_OponentHand);
        MatchConnection.INSTANCE.SendRequest(new() { yesOrNo = awnser }, "AskForMulligan");
    }
    //FUNCTIONS ASSIGNED IN THE INSPECTOR - END
    public IEnumerator GetUsableCards(List<CardData> usableInput)
    {
        if (usableInput == null || usableInput.Count == 0)
            yield break;

        var cards = CardLib.GetAndFilterCards();
        cards.Where(item => item.GetComponent<HandDragDrop>().enabled = false);

        List<Card> usable = new();
        foreach (CardData cardx in usableInput)
        {
            usable.AddRange(CardLib.GetAndFilterCards(cardNumber: new() { cardx.cardNumber }, gameZones: new[] { cardx.curZone }, player: cardx.owner, onlyVisible: true));
        }

        foreach (Card card in usable)
        {
            HandDragDrop handDragDrop = card.GetComponent<HandDragDrop>() ?? card.gameObject.AddComponent<HandDragDrop>();
            handDragDrop.enabled = true;
            card.Glow();
        }
        yield break;
    }
    IEnumerator HandleMoveCardToZone()
    {
        var CUR_DA_PLAYER = IsMyAction() ? Player.Player : Player.Oponnent;

        if (actionQueue.CUR_DA.players != null)
            CUR_DA_PLAYER = IsMyAction(actionQueue.CUR_DA.players.First().Key) ? Player.Player : Player.Oponnent;

        if (!IsMyAction() && GAMEPHASE.SettingUpBoard == actionQueue.CUR_DA.gamePhase)
            yield break;

        if (actionQueue.CUR_DA.used.curZone == Lib.GameZone.Favourite)
        {
            if (!IsMyAction())
            {
                RemoveCardFromZone(GetZone(Lib.GameZone.Favourite, Player.Oponnent), 1);
                AddOrMoveCardToGameZone(new() { actionQueue.CUR_DA.used }, null, CUR_DA_PLAYER);
            }
        }
        else if ((actionQueue.CUR_DA.used.lastZone.Equals(Lib.GameZone.Hand) && !IsMyTurn()) || actionQueue.CUR_DA_TYPE.Equals("DisposeCard") || actionQueue.CUR_DA_TYPE.Equals("DisposeCard"))
        {
            bool onPlay = actionQueue.CUR_DA_TYPE.Equals("PlayHolomem");
            bool onBloom = actionQueue.CUR_DA_TYPE.Equals("BloomHolomem");

            var playedThisTurn = false;
            if (onPlay || onBloom)
                playedThisTurn = true;

            var list = AddOrMoveCardToGameZone(new() { actionQueue.CUR_DA.used }, null, CUR_DA_PLAYER, false, false, playedThisTurn: playedThisTurn);
            if (!IsMyTurn())
                RemoveCardFromZone(GetZone(Lib.GameZone.Hand, Player.Oponnent), 1, findEnergy: actionQueue.CUR_DA.used.cardType.Equals("エール"));

            var attachmentTypes = new List<string>() { "サポート・マスコット", "サポート・アイテム", "サポート・アイテム・LIMITED", "エール" };
            var isEquipable = attachmentTypes.Contains(actionQueue.CUR_DA.used.cardType);

            if (!onPlay && isEquipable)
            {
                var curZone = actionQueue.CUR_DA.lookLastZone ? new[] { actionQueue.CUR_DA.used.lastZone } : new[] { actionQueue.CUR_DA.used.curZone };
                Card target = CardLib.GetAndFilterCards(player: CUR_DA_PLAYER, gameZones: curZone, onlyVisible: true).FirstOrDefault();

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
            var curZone = actionQueue.CUR_DA.lookLastZone ? new[] { actionQueue.CUR_DA.used.lastZone } : new[] { actionQueue.CUR_DA.used.curZone };
            Card card = CardLib.GetAndFilterCards(player: CUR_DA_PLAYER, gameZones: curZone, cardNumber: new() { actionQueue.CUR_DA.used.cardNumber }).FirstOrDefault();//note to futre, this may lead to problemns if two holomens with same number, one visible and another not
            if (actionQueue.CUR_DA.used != null && string.IsNullOrEmpty(actionQueue.CUR_DA.used.cardNumber)) // this should happen only to invisible zones
                card = CardLib.GetAndFilterCards(player: CUR_DA_PLAYER, gameZones: curZone).FirstOrDefault();
            card.Init(actionQueue.CUR_DA.used);
            card.Detach();
            AddOrMoveCardToGameZone(null, new() { card.gameObject }, CUR_DA_PLAYER, false, false);

            if (actionQueue.CUR_DA.target != null)
            {
                Card father = CardLib.GetAndFilterCards(player: CUR_DA_PLAYER, CardToBeFound: actionQueue.CUR_DA.target, OnlyWithAttachment: CardLib.attachType.all).FirstOrDefault();
                if (father == null)
                    father = CardLib.GetAndFilterCards(player: CUR_DA_PLAYER, CardToBeFound: actionQueue.CUR_DA.target).FirstOrDefault();

                if (father != null)
                {
                    card.AttachTo(father);
                }
                yield break;
            }
        }

        actionQueue.EnqueueNext("SetVisibility", SetVisibility(actionQueue.CUR_DA.used.curZone, CUR_DA_PLAYER));

        IEnumerator SetVisibility(Lib.GameZone zone, Player player)
        {
            yield return 0;
            var cards = CardLib.GetAndFilterCards(player: player, gameZones: new[] { zone });
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
        List<GameObject> allCardsToMove = new();

        if (cardsToBeCreated != null)
        {
            foreach (CardData cardDataGeneric in cardsToBeCreated)
            {
                GameObject newHolder = GetZone(cardDataGeneric.curZone, player);
                GameObject oldHolder = GetZone(cardDataGeneric.lastZone, player);
                GameObject obj = Instantiate(cardPrefab, newHolder.transform);
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
            GameObject newHolder = GetZone(card.curZone, card.owner);
            GameObject oldHolder = GetZone(card.lastZone, card.owner);
            bool isPlayedFromHand = card.lastZone.Equals(Lib.GameZone.Hand) || card.lastZone.Equals(Lib.GameZone.na);

            if (MOVEALLATONCE)
            {
                actionQueue.EnqueueNext("MoveCard", DuelField_ActionLibrary.MoveCard(allCardsToMove, oldHolder.transform, newHolder.transform, 0.2f, isPlayedFromHand, IsMyTurn(), SuspendAfter));
                break;
            }
            else
            {
                actionQueue.EnqueueNext("MoveCard", DuelField_ActionLibrary.MoveCard(new List<GameObject> { cardObj }, oldHolder.transform, newHolder.transform, 0.2f, isPlayedFromHand, IsMyTurn(), SuspendAfter));
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
        if (draw.suffleHandBackToDeck)
        {
            var handZone = GetZone(Lib.GameZone.Hand, actionQueue.CUR_DA.playerID);
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

            AddOrMoveCardToGameZone(cardsFakeData, null, actionQueue.CUR_DA.playerID, false, false, MOVEALLATONCE: false);

            if (draw.suffle)
                actionQueue.EnqueueNext("ShuffleDeck", DuelField_ActionLibrary.ShuffleDeck(GetZone(Lib.GameZone.Deck, actionQueue.CUR_DA.playerID)));
        }

        if (!IsMyAction())
            foreach (CardData cardData in draw.cards)
            {
                cardData.cardNumber = "";
            }

        AddOrMoveCardToGameZone(draw.cards, (List<GameObject>)null, actionQueue.CUR_DA.playerID, false, draw.suffle, MOVEALLATONCE: false);
        RemoveCardFromZone(GetZone(draw.cards[0].lastZone, actionQueue.CUR_DA.playerID), draw.cards.Count);
    }
    public void RemoveCardFromZone(GameObject father, int amount, bool findEnergy = false)
    {
        actionQueue.EnqueueNext("RemoveCardFromZone", DuelField_ActionLibrary.RemoveCardFromZone(father, amount, findEnergy));
    }
    public GameObject GetZone(Lib.GameZone s, Player player)
    {
        if (player == Player.PlayerA)
            if (DuelField.INSTANCE.players[Player.PlayerA].Equals(PlayerInfo.INSTANCE.PlayerID))
                player = Player.Player;
            else
                player = Player.Oponnent;

        if (player == Player.PlayerB)
            if (DuelField.INSTANCE.players[Player.PlayerB].Equals(PlayerInfo.INSTANCE.PlayerID))
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
    IEnumerator HandleStartDuel()
    {
        players = actionQueue.CUR_DA.players;
        turnPlayer = actionQueue.CUR_DA.playerID;
        playersType = new();
        foreach (var playerss in actionQueue.CUR_DA.players)
            playersType.Add(playerss.Value, playerss.Key);

        Player playerA = Player.Player;
        Player playerB = Player.Oponnent;

        TurnCounterText.text = currentTurn.ToString();
        yield return 0;
    }
    IEnumerator HandleBuildBoard()
    {
        AddOrMoveCardToGameZone(actionQueue.CUR_DA.cards, null, actionQueue.CUR_DA.playerID, shuffle: true, MOVEALLATONCE: true);
        yield return 0;
    }
    IEnumerator HandleMulligan()
    {

        if (actionQueue.CUR_DA.yesOrNo)
            DrawCard(actionQueue.CUR_DA);

        playerMulligan++;

        if (playerMulligan == 2)
            if (GamePhase != GAMEPHASE.MulliganForced)
            {
                playerMulligan = 0;
            }
        yield break;
    }
    IEnumerator HandleResetStep()
    {
        var CUR_DA_PLAYER = IsMyTurn() ? Player.Player : Player.Oponnent;
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
        yield break;
    }
    IEnumerator HandleReSetStage()
    {
        if (IsMyTurn())
        {
            if (actionQueue.CUR_DA.used != null)
            {
                var cards = GetZone(actionQueue.CUR_DA.used.curZone, actionQueue.CUR_DA.playerID).GetComponentsInChildren<RectTransform>().Select(item => item.gameObject);

                List<GameObject> cardsToBeMoved = new();
                foreach (var card in cards)
                {
                    var cardComp = card.GetComponent<Card>().Init(actionQueue.CUR_DA.used);
                    card.transform.Rotate(0, 0, 0);
                    cardComp.suspended = false;
                    cardsToBeMoved.Add(card);
                }
                AddOrMoveCardToGameZone(null, cardsToBeMoved, actionQueue.CUR_DA.playerID, false, false);
            }

            hasAlreadyCollabed = false;
        }
        yield break;
    }
    IEnumerator HandleDrawPhase()
    {
        DrawCard(actionQueue.CUR_DA);
        yield break;
    }
    IEnumerator HandleEndturn()
    {
        DuelField.INSTANCE.hasAlreadyCollabed = false;
        centerStageArtUsed = false;
        collabStageArtUsed = false;
        usedOshiSkill = false;
        playerLimiteCardPlayed.Clear();

        currentTurn++;
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
        if (PlayerInfo.INSTANCE.PlayerID.Equals(actionQueue.CUR_DA.playerID))
            DuelField_UI_MAP.INSTANCE.DisableAllOther().SetPanel(true, PanelType.SS_UI_General).SetPanel(true, PanelType.SS_WinPanel);
        else
            DuelField_UI_MAP.INSTANCE.DisableAllOther().SetPanel(true, PanelType.SS_UI_General).SetPanel(true, PanelType.SS_LosePanel);

        DontDestroyManager.DestroyAllDontDestroyOnLoadObjects();
        yield break;
    }
    IEnumerator HandleDoCollab()
    {
        var oldHolder = GetZone(actionQueue.CUR_DA.used.lastZone, actionQueue.CUR_DA.playerID);
        var newHolder = GetZone(actionQueue.CUR_DA.used.curZone, actionQueue.CUR_DA.playerID);

        var cards = oldHolder.transform.GetComponentsInChildren<Card>().Where(item => item.cardNumber.Equals(actionQueue.CUR_DA.used.cardNumber) ? item.Init(actionQueue.CUR_DA.used).PlayedThisTurn(true) : item).Select(item => item.gameObject).ToList();

        foreach (GameObject obj in cards)
            obj.transform.SetParent(newHolder.transform);

        AddOrMoveCardToGameZone(null, cardsToBeMoved: cards, actionQueue.CUR_DA.playerID, false, false);
        yield break;
    }
    IEnumerator HandleUnDoCollab()
    {
        if (!string.IsNullOrEmpty(actionQueue.CUR_DA.used?.cardNumber))
        {
            var cards = GetZone(actionQueue.CUR_DA.used.lastZone, actionQueue.CUR_DA.playerID).GetComponentsInChildren<Card>();

            List<GameObject> cardsToBeMoved = new();
            foreach (var cardComp in cards)
            {
                cardComp.Init(actionQueue.CUR_DA.used);
                cardsToBeMoved.Add(cardComp.gameObject);
            }
            AddOrMoveCardToGameZone(null, cardsToBeMoved, actionQueue.CUR_DA.playerID, false, false, SuspendAfter: true);
        }
        yield break;
    }
    IEnumerator HandleRecoverHolomem()
    {
        GameObject zone = GetZone(actionQueue.CUR_DA.target.curZone, (IsMyTurn() ? Player.Player : Player.Oponnent));
        Card targetedCard = zone.GetComponentInChildren<Card>();
        targetedCard.currentHp = Math.Min(targetedCard.currentHp + actionQueue.CUR_DA.hpAmount, int.Parse(targetedCard.hp));
        targetedCard.UpdateHP();
        yield break;
    }
    IEnumerator HandleResolveDamage()
    {
        Card card = CardLib.GetAndFilterCards(gameZones: new[] { actionQueue.CUR_DA.target.curZone }, player: actionQueue.CUR_DA.target.owner, onlyVisible: true, GetOnlyHolomem: true).First();

        if (actionQueue.CUR_DA.hpFixedValue > 0)
            card.currentHp = actionQueue.CUR_DA.hpFixedValue;

        if (actionQueue.CUR_DA.hpAmount > 0)
            card.currentHp -= actionQueue.CUR_DA.hpAmount;

        card.UpdateHP();
        yield break;
    }
    IEnumerator HandleSuffleDeck()
    {
        foreach (Lib.GameZone zone in actionQueue.CUR_DA.targetedZones)
        {
            yield return DuelField_ActionLibrary.ShuffleDeck(GetZone(zone, actionQueue.CUR_DA.playerID));
        }
    }
    public Dictionary<string, Func<IEnumerator>> MapActions()
    {
        return new Dictionary<string, Func<IEnumerator>>
        {
                    //Starting Duel
                    { "StartDuel", HandleStartDuel },
                    { "SetDecks", HandleBuildBoard },
                    { "InitialDraw", HandleDrawPhase },
                    { "Mulligan", HandleMulligan },
                    //Duelphase Flow
                    { "ResetStep", HandleResetStep },
                    { "ReSetStage", HandleReSetStage },
                    { "DrawPhase", HandleDrawPhase },
                    { "CheerStep", HandleDrawPhase },
                    { "Endturn", HandleEndturn },
                    { "Endduel", HandleEndduel },
                    //General
                    { "DoCollab", HandleDoCollab },
                    { "UnDoCollab", HandleUnDoCollab },
                    { "DrawByEffect", HandleDrawPhase },
                    { "RollDice", HandleShowDiceRoll },
                    { "SuffleDeck", HandleSuffleDeck },
                    { "RecoverHolomem", HandleRecoverHolomem },
                    { "SetGamePhase", HandleSetGamePhase },
                    { "InflicDamageToHolomem", HandleResolveDamage },
                    { "GetUsableCards", HandleGetUsableCards },
                    { "UseArt", HandleUseArt },
                    //Card Moviment
                    { "BloomHolomem", HandleMoveCardToZone },
                    { "PlayHolomem", HandleMoveCardToZone },
                    { "DisposeCard", HandleMoveCardToZone },
                    { "MoveCardToZone", HandleMoveCardToZone },
                    //Trigger Effect
                    { "PrepareTrigger", HandlePrepareTrigger },
                    { "StackResponse", HandleResolveOnEffect },
                };
    }

    private IEnumerator HandleUseArt()
    {
        if (actionQueue.CUR_DA.used.curZone.Equals(Lib.GameZone.Stage))
            DuelField.INSTANCE.centerStageArtUsed = true;
        else
            DuelField.INSTANCE.collabStageArtUsed = true;
        yield break;
    }

    private IEnumerator HandleGetUsableCards()
    {
        actionQueue.EnqueueNext("GetUsableCards", GetUsableCards(actionQueue.CUR_DA.cards));
        yield break;
    }

    private IEnumerator HandlePrepareTrigger()
    {
        yield return DuelField_ShowListPickThenReorder.INSTANCE.SetupSelectableItems(new DuelAction() { }, Lib.ConvertToCard(actionQueue.CUR_DA.cards), Lib.ConvertToCard(actionQueue.CUR_DA.MapSelectable()), actionQueue.CUR_DA.reSelect, actionQueue.CUR_DA.maxPick, canClosePanel: actionQueue.CUR_DA.canClosePanel);
        DuelAction da = DuelField_ShowListPickThenReorder.GetDA();
        if (!(da.cards == null || da.cards.Count == 0))
            MatchConnection.INSTANCE.SendRequest(DuelField_ShowListPickThenReorder.GetDA(), "selectCardsToActivate");
        else
           if (IsMyAction())
            MatchConnection.INSTANCE.SendRequest(null, "StackResponseContinue");
    }
    private IEnumerator HandleResolveOnEffect()
    {
        Player target = actionQueue.CUR_DA.players.First().Key;

        if (players[actionQueue.CUR_DA.playerID].Equals(PlayerInfo.INSTANCE.PlayerID))
            switch (actionQueue.CUR_DA.displayType)
            {
                case DuelAction.Display.Detach:
                    yield return DuelField_DetachCardMenu.INSTANCE.SetupSelectableItems(new DuelAction() { }, actionQueue.CUR_DA.targetedZones.ToArray(), IsACheer: actionQueue.CUR_DA.targetType, target, canClosePanel: actionQueue.CUR_DA.canClosePanel);
                    MatchConnection.INSTANCE.SendRequest(DuelField_DetachCardMenu.GetDA(), actionQueue.CUR_DA_TYPE);
                    break;
                case DuelAction.Display.Target:
                    yield return DuelField_TargetForEffectMenu.INSTANCE.SetupSelectableItems(new DuelAction() { }, target, actionQueue.CUR_DA.targetedZones.ToArray(), Lib.ConvertToCard(actionQueue.CUR_DA.cards), canClosePanel: actionQueue.CUR_DA.canClosePanel);
                    MatchConnection.INSTANCE.SendRequest(DuelField_TargetForEffectMenu.GetDA(), actionQueue.CUR_DA_TYPE);
                    break;
                case DuelAction.Display.ListPickAndReorder:
                    yield return DuelField_ShowListPickThenReorder.INSTANCE.SetupSelectableItems(new DuelAction() { }, Lib.ConvertToCard(actionQueue.CUR_DA.cards), Lib.ConvertToCard(actionQueue.CUR_DA.MapSelectable()), actionQueue.CUR_DA.reSelect, actionQueue.CUR_DA.maxPick, canClosePanel: actionQueue.CUR_DA.canClosePanel);
                    MatchConnection.INSTANCE.SendRequest(DuelField_ShowListPickThenReorder.GetDA(), actionQueue.CUR_DA_TYPE);
                    break;
                case DuelAction.Display.Number:
                    yield return DuelField_ShowANumberList.INSTANCE.SetupSelectableItems(actionQueue.CUR_DA.indexes[0], actionQueue.CUR_DA.indexes[1], new DuelAction() { });
                    MatchConnection.INSTANCE.SendRequest(DuelField_ShowANumberList.GetDA(), actionQueue.CUR_DA_TYPE);
                    break;
                case DuelAction.Display.ListPickOne:
                    yield return DuelField_ShowAlistPickOne.INSTANCE.SetupSelectableItems(new DuelAction() { }, Lib.ConvertToCard(actionQueue.CUR_DA.cards), Lib.ConvertToCard(actionQueue.CUR_DA.MapSelectable()), canClosePanel: actionQueue.CUR_DA.canClosePanel);
                    MatchConnection.INSTANCE.SendRequest(DuelField_ShowAlistPickOne.GetDA(), actionQueue.CUR_DA_TYPE);
                    break;
                case DuelAction.Display.YesOrNo:
                    yield return DuelField_YesOrNoMenu.INSTANCE.ShowYesOrNoMenu(new DuelAction() { }, actionQueue.CUR_DA.message.ToString());
                    MatchConnection.INSTANCE.SendRequest(DuelField_YesOrNoMenu.GetDA(), actionQueue.CUR_DA_TYPE);
                    break;
            }
        yield break;
    }

    private IEnumerator HandleSetGamePhase()
    {
        GamePhase = actionQueue.CUR_DA.gamePhase;

        if (turnPlayer != actionQueue.CUR_DA.playerID)
            StartTurnCounter();

        turnPlayer = actionQueue.CUR_DA.playerID;

        if (GAMEPHASE.Mulligan == actionQueue.CUR_DA.gamePhase)
        {
            DuelField_UI_MAP.INSTANCE.SetPanel(true, DuelField_UI_MAP.PanelType.SS_MulliganPanel);
            DuelField_UI_MAP.INSTANCE.SetPanel(false, DuelField_UI_MAP.PanelType.SS_OponentHand);
        }

        if (GAMEPHASE.CheerStepChoose == actionQueue.CUR_DA.gamePhase || GAMEPHASE.Mulligan == actionQueue.CUR_DA.gamePhase || GAMEPHASE.MulliganForced == actionQueue.CUR_DA.gamePhase)
            yield break;

        if (GAMEPHASE.MainStep == actionQueue.CUR_DA.gamePhase && IsMyTurn())
            DuelField_UI_MAP.INSTANCE.WS_PassTurnButton.SetActive(true);


        if (GAMEPHASE.SettingUpBoard == actionQueue.CUR_DA.gamePhase)
            DuelField_UI_MAP.INSTANCE.WS_ReadyButton.SetActive(true);

        if (GAMEPHASE.EndStep == actionQueue.CUR_DA.gamePhase)
            StartTurnCounter();

        actionQueue.EnqueueNext("ShowMessage", DuelField_ActionLibrary.ShowMessage(actionQueue.CUR_DA.gamePhase.ToString()));
        yield break;
    }
    private IEnumerator HandleShowDiceRoll()
    {
        yield break;
    }
}
