using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static DuelField;

public class DuelField_HandDragDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static bool IsDragging = false;

    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] public RectTransformData defaultValues;

    private Vector3 screenPoint;
    private Vector3 offset;

    private Dictionary<DuelFieldData.GAMEPHASE, Dictionary<string, Func<bool>>> handlers;

    GameObject targetZone;
    Card targetCard;
    Lib.GameZone TargetZoneEnum = 0;

    Card thisCard;
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        InitializeHandlers();
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        IsDragging = true;
        GetComponent<BoxCollider>().enabled = false;
        canvasGroup.blocksRaycasts = false;

        screenPoint = Camera.main.WorldToScreenPoint(transform.position);
        offset = transform.position - Camera.main.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, screenPoint.z));

        defaultValues = new RectTransformData(rectTransform);
        rectTransform.localScale = new Vector3(rectTransform.localScale.x * 1.1f, rectTransform.localScale.y * 1.1f, 1f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 currentScreenPoint = new Vector3(eventData.position.x, eventData.position.y, screenPoint.z);
        Vector3 currentWorldPos = Camera.main.ScreenToWorldPoint(currentScreenPoint) + offset;
        transform.position = currentWorldPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        float radius = 0.5f;
        RaycastHit[] hits = Physics.SphereCastAll(ray, radius);

        foreach (var h in hits) {
            targetZone = DuelField.INSTANCE.GetGameZones().Contains(h.transform.gameObject) ? h.transform.gameObject : null;

            if (targetZone == null) {
                var parent = h.transform?.parent?.gameObject;
                targetZone = (parent != null && DuelField.INSTANCE.GetGameZones().Contains(parent)) ? parent : null;
            }

            if (targetZone != null) { 
                if (DoAction())
                {
                    this.transform.SetParent(GameObject.Find("HUD").transform);
                    this.gameObject.SetActive(false);
                    Destroy(this.gameObject);
                }
                break;
            }
        }

        targetCard = thisCard = null;
        targetZone = null; 

        rectTransform = defaultValues.ApplyToRectTransform(rectTransform);
        GetComponent<BoxCollider>().enabled = true;
        IsDragging = false;
        canvasGroup.blocksRaycasts = true;
    }
    void InitializeHandlers()
    {
        handlers = new()
        {
            {
                DuelFieldData.GAMEPHASE.SettingUpBoard, new()
                {
                    { "DEFAULT", Handle_SettingUpBoard }
                }
            },
            {
                DuelFieldData.GAMEPHASE.HolomemDefeatedEnergyChoose, new()
                {
                    { "エール", Handle_CheerStep }
                }
            },
            {
                DuelFieldData.GAMEPHASE.CheerStepChoose, new()
                {
                    { "エール", Handle_CheerStep }
                }
            },
            {
                DuelFieldData.GAMEPHASE.MainStep, new()
                {
                    { "サポート・イベント", Handle_SupportEvent },
                    { "サポート・アイテム", Handle_SupportEvent },
                    { "サポート・スタッフ・LIMITED", Handle_SupportEventLimited },
                    { "サポート・イベント・LIMITED", Handle_SupportEventLimited },
                    { "サポート・アイテム・LIMITED", Handle_SupportEventLimited },
                    { "サポート・ツール", Handle_EquipSupport },
                    { "サポート・マスコット", Handle_EquipSupport },
                    { "サポート・ファン", Handle_EquipSupport },
                    { "ホロメン", Handle_HolomemOrBuzz },
                    { "Buzzホロメン", Handle_HolomemOrBuzz },
                }
            }
        };
    }
    public bool DoAction()
    {
        thisCard = GetComponent<Card>().Init(GetComponent<Card>().ToCardData());
        targetCard = targetZone.GetComponentInChildren<Card>();
        TargetZoneEnum = DuelField.INSTANCE.GetZoneByString(targetZone.name);

        var phase = DuelField.INSTANCE.DUELFIELDDATA.currentGamePhase;
        string cardType = thisCard?.cardType ?? "UNKNOWN";

        if (!handlers.TryGetValue(phase, out var cardTypeMap))
            return false;

        if (cardTypeMap.TryGetValue(cardType, out var action))
            return action();
        

        if (cardTypeMap.TryGetValue("DEFAULT", out var defaultAction))
            return defaultAction();

        return false;
    }
    private bool Handle_SettingUpBoard()
    {
        if (targetCard != null)
            return false;

        var allowedZones = new[]
        {
            Lib.GameZone.Stage,
            Lib.GameZone.BackStage1,
            Lib.GameZone.BackStage2,
            Lib.GameZone.BackStage3,
            Lib.GameZone.BackStage4,
            Lib.GameZone.BackStage5
        };

        if ((DuelField.INSTANCE.GetZone(Lib.GameZone.Stage, TargetPlayer.Player)?.GetComponentInChildren<Card>() == null && TargetZoneEnum != Lib.GameZone.Stage) 
            || !allowedZones.Contains(TargetZoneEnum) 
            || targetZone.transform.parent.name.Equals("OponenteGeneral"))
            return false;

        thisCard.curZone = TargetZoneEnum;
        thisCard.lastZone = Lib.GameZone.Hand;

        this.transform.SetParent(GameObject.Find("HUD").transform);

        var card = new CardData() { curZone = thisCard.curZone, lastZone = thisCard.lastZone, cardNumber = thisCard.cardNumber };

        DuelField.INSTANCE.AddOrMoveCardToGameZone(new List<CardData> { card }, null, TargetPlayer.Player, false, false);

        return true;
    }
    private bool Handle_CheerStep()
    {
        if (targetCard == null)
            return false;

        if (targetCard.cardType != "ホロメン" && targetCard.cardType != "Buzzホロメン")
                return false;

        DuelAction _DuelAction = new();

        _DuelAction.playerID = DuelField.INSTANCE.DUELFIELDDATA.currentPlayerTurn;
        _DuelAction.usedCard = thisCard.ToCardData();
        _DuelAction.usedCard.curZone = Lib.GameZone.Hand;
        _DuelAction.targetZone = (Lib.GameZone)Enum.Parse(
            typeof(Lib.GameZone),
            targetCard.transform.parent.name);
        _DuelAction.targetCard = targetCard.ToCardData();

        DuelField.INSTANCE.GenericActionCallBack(_DuelAction, "CheerChooseRequest");

        return true;
    }
    private bool Handle_SupportEvent()
    {
        if (!DuelField.INSTANCE.CheckForPlayRestrictions(thisCard.cardNumber))
            return false;

        DuelAction _DuelAction = new DuelAction()
        {
            usedCard = thisCard.ToCardData(),
            playerID = DuelField.INSTANCE.DUELFIELDDATA.currentPlayerTurn,
            targetZone = targetCard != null ? TargetZoneEnum : Lib.GameZone.na,
            targetCard = targetCard?.ToCardData()
        };

        FindAnyObjectByType<EffectController>().ResolveSuportEffect(_DuelAction);
        rectTransform.anchoredPosition = Vector2.zero;

        return true;
    }
    private bool Handle_SupportEventLimited()
    {
        if (DuelField.INSTANCE.DUELFIELDDATA.playerLimiteCardPlayed.Count > 0)
            return false;

        DuelField.INSTANCE.DUELFIELDDATA.playerLimiteCardPlayed.Add(thisCard);

        Handle_SupportEvent();


        ActionItem.Add("GetUsableCards", DuelField.INSTANCE.GetUsableCards());

        return true;
    }
    private bool Handle_EquipSupport()
    {
        if (DuelField.INSTANCE.HasRestrictionsToPlayEquip(thisCard, targetCard))
            return false;

        DuelAction _DuelAction = new DuelAction()
        {
            usedCard = thisCard.ToCardData(),
            playerID = DuelField.INSTANCE.DUELFIELDDATA.currentPlayerTurn,
            targetZone = targetCard != null ? TargetZoneEnum : Lib.GameZone.na,
            targetCard = targetCard?.ToCardData()
        };

        DuelField.INSTANCE.GenericActionCallBack(_DuelAction, "AttachEquipamentToHolomem");

        return true;
    }
    private bool Handle_HolomemOrBuzz()
    {
        if (targetZone == null)
            return false;

        Card pointedCard = null;

        foreach (Card card in targetZone.GetComponentsInChildren<Card>()) {
            if (card.transform.gameObject.activeInHierarchy) { 
                pointedCard = card;
                break;
            }
        }

        DuelAction _DuelAction = new();

        // PLAY new holomem
        if (pointedCard == null)
        {
            if (!DuelField.INSTANCE.CanSummonHolomemHere(thisCard, TargetZoneEnum))
                return false; 


            _DuelAction.usedCard = thisCard.ToCardData();
            _DuelAction.targetZone = TargetZoneEnum;

            DuelField.INSTANCE.GenericActionCallBack(_DuelAction, "PlayHolomem");

            return true;
        }

        // BLOOM
        if (!DuelField.INSTANCE.CanBloomHolomem(pointedCard, thisCard))
            return true;

        _DuelAction.playerID = DuelField.INSTANCE.DUELFIELDDATA.currentPlayerTurn;
        _DuelAction.usedCard = thisCard.ToCardData();
        _DuelAction.targetZone = DuelField.INSTANCE.GetZoneByString(pointedCard.transform.parent.name);
        _DuelAction.targetCard = pointedCard.ToCardData();

        DuelField.INSTANCE.GenericActionCallBack(_DuelAction, "BloomHolomem");
        return true;
    }
    void AttachCardToCard(GameObject FatherZone, GameObject card)
    {

        card.transform.SetParent(FatherZone.transform, true);

        card.transform.SetSiblingIndex(0);

        card.transform.localPosition = Vector3.zero;
        card.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);

        card.GetComponentInChildren<Card>().curZone = Lib.GameZone.Hand;
		Destroy(this);
    }
    void BloomCard(GameObject FatherZone, GameObject card)
    {

        GameObject FatherZoneActiveCard = FatherZone.transform.GetChild(FatherZone.transform.childCount - 1).gameObject;

        //set this card dropped card as child of the zone
        card.transform.SetParent(FatherZone.transform, true);

        card.transform.SetSiblingIndex(FatherZone.transform.childCount - 1);

        card.transform.localPosition = Vector3.zero;
        card.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);

        //get both cards information
        Card CardComponent = card.GetComponent<Card>();
        Card FatherZoneActiveCardComponent = card.GetComponent<Card>();

        CardComponent.suspended = FatherZoneActiveCardComponent.suspended;
        //if the card targeted is suspended, lets suspend
        if (FatherZoneActiveCardComponent.suspended)
        {
            this.gameObject.transform.Rotate(0, 0, 90);
        }

        //passing the hp to the new card
        CardComponent.currentHp = FatherZoneActiveCardComponent.currentHp;
        CardComponent.effectDamageRecieved = FatherZoneActiveCardComponent.effectDamageRecieved;
        CardComponent.normalDamageRecieved = FatherZoneActiveCardComponent.normalDamageRecieved;
        CardComponent.UpdateHP();
        //end of passing the hp to the new card

        //getting the name of the father gameobject the the position of the new card
        card.GetComponentInChildren<Card>().curZone = (Lib.GameZone)Enum.Parse(typeof(Lib.GameZone), FatherZoneActiveCard.transform.parent.name);
        //adding all the bloomed childs as reference to the new card
        card.GetComponentInChildren<Card>().bloomChild.Add(FatherZoneActiveCard);
        //adding of attachs from the bloomed card to the new card
        card.GetComponentInChildren<Card>().attachedEnergy = FatherZoneActiveCard.GetComponentInChildren<Card>().attachedEnergy;
        //removing the reference from the past form card
        FatherZoneActiveCard.GetComponentInChildren<Card>().attachedEnergy = null;
        card.GetComponentInChildren<Card>().curZone = Lib.GameZone.Hand;

		//make the older last position card invisible 
		FatherZoneActiveCard.SetActive(false);
        Destroy(this);
    }
    public void EffectQuestionDispalyMenuButton(DuelAction duelAction)
    {
        DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_ActivateEffectPanel.SetActive(true);
        DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_ActivateEffectPanelYES.GetComponent<Button>().onClick.AddListener(() => EffectQuestionYesButton(duelAction));
        DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_ActivateEffectPanelNO.GetComponent<Button>().onClick.AddListener(() => EffectQuestionNoButton(duelAction));
    }

    public void EffectQuestionYesButton(DuelAction duelAction)
    {
        DuelField.INSTANCE.GenericActionCallBack(duelAction, "BloomHolomemWithEffect");
        DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_ActivateEffectPanel.SetActive(false);
    }
    public void EffectQuestionNoButton(DuelAction duelAction)
    {
        DuelField.INSTANCE.GenericActionCallBack(duelAction, "standart");
        DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_ActivateEffectPanel.SetActive(false);
    }
}