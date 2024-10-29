using Assets.Scripts.Lib;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.PlayerLoop;
using UnityEngine.U2D.IK;
using UnityEngine.UI;
using static DuelField;

public class DuelField_HandDragDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Canvas canvas;
    [SerializeField] private DropZone[] dropZones; // Array to store all drop zones
    [SerializeField] public RectTransformData defaultValues;
    [SerializeField] private DuelField _DuelField;
    [SerializeField] private CardPanel cardPanel;
    [SerializeField] private DropZone PlayerField;
    DuelField_HandClick handClick;
    bool validDropZoneFound = false;

    private int originalSiblingIndex;

    static public GameObject EffectQuestionPainel;
    static public Button EffectQuestionPainelYesButton;
    static public Button EffectQuestionPainelNoButton;


    void Start()
    {
        EffectQuestionPainel = GameObject.Find("MatchField").transform.Find("ActivateEffectPanel").gameObject;
        EffectQuestionPainelYesButton = EffectQuestionPainel.transform.Find("YesButton").GetComponent<Button>();
        EffectQuestionPainelNoButton = EffectQuestionPainel.transform.Find("NoButton").GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        dropZones = FindObjectsOfType<DropZone>(); // Find all DropZone components in the scene
        defaultValues = new RectTransformData(rectTransform);
        _DuelField = GameObject.FindAnyObjectByType<DuelField>();
        cardPanel = GameObject.FindAnyObjectByType<CardPanel>();
        handClick = GetComponent<DuelField_HandClick>();
    }

    private void OnEnable()
    {
        this.transform.Find("CardGlow").gameObject.SetActive(true);
    }
    private void OnDisable()
    {
        this.transform.Find("CardGlow").gameObject.SetActive(false);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        handClick.enabled = false;
        // Store the original sibling index so we can return the card to its original position later
        originalSiblingIndex = rectTransform.GetSiblingIndex();

        // Move the card to the top of the hierarchy to render it on top of others
        rectTransform.SetSiblingIndex(rectTransform.parent.childCount - 1);

        rectTransform.localScale = new Vector3(1.2f, 1.2f);
        //canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        dropZones = FindObjectsOfType<DropZone>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint);

        // Set the anchored position to the local point
        rectTransform.anchoredPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        rectTransform.SetSiblingIndex(originalSiblingIndex);
        handClick.enabled = true;

        Card thisCard = GetComponent<Card>();

        DuelAction _DuelAction = new();

        int zoneCounter = 0;
        foreach (DropZone dropZone in dropZones)
        {
            if (!(zoneCounter < dropZones.Length))
                return;

            if (!dropZone.isHovered)
                continue;

            Card targetCard = dropZone.GetComponent<Card>();


            switch (_DuelField._MatchConnection._DuelFieldData.currentGamePhase)
            {
                case DuelFieldData.GAMEPHASE.HolomemDefeatedEnergyChoose:
                case DuelFieldData.GAMEPHASE.CheerStepChoose:
                    if (dropZone.zoneType.Equals("HoloMember"))
                    {
                        if (thisCard.cardType.Equals("エール"))
                        {
                            if (targetCard.cardType.Equals("ホロメン") || targetCard.cardType.Equals("Buzzホロメン"))
                            {
                                AttachCardToCard(targetCard.gameObject.transform.parent.gameObject, thisCard.gameObject);
                                if (targetCard.attachedCards == null)
                                    targetCard.attachedCards = new();
                                targetCard.attachedCards.Add(thisCard.gameObject);
                                thisCard.gameObject.SetActive(false);

                                _DuelAction.playerID = _DuelField._MatchConnection._DuelFieldData.currentPlayerTurn;
                                _DuelAction.usedCard =  DataConverter.CreateCardDataFromCard(thisCard);
                                _DuelAction.playedFrom = "Hand";
                                _DuelAction.local = targetCard.gameObject.transform.parent.gameObject.name;
                                _DuelAction.targetCard = DataConverter.CreateCardDataFromCard(targetCard); 

                                _DuelField.GenericActionCallBack(_DuelAction, "CheerChooseRequest");
                                validDropZoneFound = true;
                            }
                        }
                    }
                    break;
                case DuelFieldData.GAMEPHASE.SettingUpBoard:
                    switch (dropZone.zoneType)
                    {
                        case "BackStage1":
                        case "BackStage2":
                        case "BackStage3":
                        case "BackStage4":
                        case "BackStage5":
                            if (DropZone.GetZoneByName("Stage", dropZones).transform.GetComponentInChildren<Card>() != null)
                            {
                                PerformActionBasedOnDropZone(dropZone.zoneType);
                                validDropZoneFound = true;
                            }
                            break;
                        case "Collaboration":
                            break;
                        case "Stage":
                            if (dropZone.GetComponentInChildren<Card>() == null)
                            {
                                PerformActionBasedOnDropZone(dropZone.zoneType);
                                validDropZoneFound = true;
                            }
                            break;
                    }
                    break;
                case DuelFieldData.GAMEPHASE.MainStep:
                    
                    if (thisCard.cardType.Equals("サポート・イベント") || thisCard.cardType.Equals("サポート・アイテム") || thisCard.cardType.Equals("サポート・スタッフ・LIMITED") || thisCard.cardType.Equals("サポート・イベント・LIMITED") || thisCard.cardType.Equals("サポート・アイテム・LIMITED"))
                    {
                        if (thisCard.cardType.Equals("サポート・スタッフ・LIMITED") || thisCard.cardType.Equals("サポート・イベント・LIMITED") || thisCard.cardType.Equals("サポート・アイテム・LIMITED")) {

                            if (_DuelField._MatchConnection._DuelFieldData.playerLimiteCardPlayed.Count > 0)
                                break;

                            _DuelField._MatchConnection._DuelFieldData.playerLimiteCardPlayed.Add(thisCard);
                            foreach (RectTransform r in _DuelField.cardsPlayer)
                            {
                                Card cardComponent = r.GetComponent<Card>();
                                if (cardComponent.cardType.Equals("サポート・スタッフ・LIMITED") || cardComponent.cardType.Equals("サポート・イベント・LIMITED") || cardComponent.cardType.Equals("サポート・アイテム・LIMITED"))
                                    cardComponent.GetComponent<DuelField_HandDragDrop>().enabled = false;
                            }
                        }

                        string sendThisCardTo = "Arquive";

                        //if (thisCard.cardType.Equals("サポート・スタッフ・LIMITED")) { 
                            _DuelAction = new DuelAction()
                            {
                                usedCard = DataConverter.CreateCardDataFromCard(thisCard),
                                playerID = _DuelField._MatchConnection._DuelFieldData.currentPlayerTurn,
                                playedFrom = "Hand",
                                local = (targetCard != null) ? targetCard.gameObject.transform.parent.gameObject.name : "",
                                targetCard = DataConverter.CreateCardDataFromCard(targetCard)
                            };
                        //}
                        FindAnyObjectByType<EffectController>().ResolveSuportEffect(_DuelAction);

                        _DuelField.cardsPlayer.Remove(this.transform.GetComponent<RectTransform>());
                        _DuelField.ArrangeCards(_DuelField.cardsPlayer, _DuelField.cardHolderPlayer);

                        _DuelField.SendCardToZone(this.gameObject, sendThisCardTo, TargetPlayer.Player, false);
                        rectTransform.anchoredPosition = Vector2.zero;

                        validDropZoneFound = true;
                    } else if (thisCard.cardType.Equals("ホロメン"))
                    switch (dropZone.zoneType)
                    {
                        case "BackStage1":
                        case "BackStage2":
                        case "BackStage3":
                        case "BackStage4":
                        case "BackStage5":
                        case "Stage":
                                // se a posição que jogamos a carta está vázia, e a carta jogada e holomem ou buzz, nos tentamos abaixar, senão tentamos bloomar
                                if (dropZone.GetComponentInChildren<Card>() == null && (thisCard.cardType.Equals("ホロメン") || thisCard.cardType.Equals("Buzzホロメン")))
                                {
                                    if (_DuelField.GetZone(dropZone.zoneType, TargetPlayer.Player).GetComponentInChildren<Card>() != null)
                                        break;

                                    thisCard.GetCardInfo();
                                    bool canContinue = false;
                                    if (thisCard.bloomLevel.Equals("Debut") || thisCard.bloomLevel.Equals("Spot"))
                                        canContinue = true;

                                    //get holomem count
                                    int count = _DuelField.CountBackStageTotal();

                                    if (dropZone.zoneType.Equals("BackStage1") || dropZone.zoneType.Equals("BackStage2") || dropZone.zoneType.Equals("BackStage3") ||
                                        dropZone.zoneType.Equals("BackStage4") || dropZone.zoneType.Equals("BackStage5"))
                                    {
                                        canContinue = false;

                                        if (count < 5)
                                            canContinue = true;
                                    }
                                
                                    if (!canContinue)
                                        break;

                                    PerformActionBasedOnDropZone(dropZone.zoneType);

                                    _DuelAction.actionType = "PlayHolomem";
                                    _DuelAction.usedCard.cardNumber = thisCard.cardNumber;
                                    _DuelAction.local = dropZone.zoneType;
                                    _DuelAction.playedFrom = "Hand";

                                    _DuelField.GenericActionCallBack(_DuelAction);
                                    validDropZoneFound = true;

                                    //if we played the 5º holomem, we need to disable drag and drop for others
                                    if (count == 5) {
                                        foreach (RectTransform r in _DuelField.cardsPlayer)
                                        {
                                            Card cardComponent = r.GetComponent<Card>();
                                            if (cardComponent.cardType.Equals("Debut"))
                                                cardComponent.GetComponent<DuelField_HandDragDrop>().enabled = false;
                                        }
                                    }
                                }
                                else // BLOOM
                                {
                                    Transform lastChild = dropZone.transform.GetChild(dropZone.transform.childCount - 1);
                                    Card pointedCard = lastChild.GetComponent<Card>();

                                    //cards cannot bloom the turn they are played 
                                    if (pointedCard.playedThisTurn == true)
                                        break;

                                    pointedCard.GetCardInfo();
                                    thisCard.GetCardInfo();

                                    bool canContinue = false;
                                    if (thisCard.cardType.Equals("ホロメン") || thisCard.cardType.Equals("Buzzホロメン"))
                                    {
                                        //get level to bloom
                                        string bloomToLevel = pointedCard.bloomLevel.Equals("Debut") ? "1st" : "2nd";
                                        //especial card condition to bloom match
                                        if (thisCard.cardNumber.Equals("hSD01-013") && thisCard.bloomLevel.Equals(bloomToLevel) && (pointedCard.cardName.Equals("ときのそら") || pointedCard.cardName.Equals("AZKi")))
                                        {
                                            canContinue = true;
                                        } else if (pointedCard.cardNumber.Equals("hSD01-013") && (thisCard.cardName.Equals("ときのそら") || thisCard.cardName.Equals("AZKi")) && bloomToLevel.Equals("2nd")) {
                                            canContinue = true;
                                        }
                                        //normal condition to bloom match
                                        else if (thisCard.cardName.Equals(pointedCard.cardName) && pointedCard.bloomLevel.Equals(bloomToLevel))
                                        {
                                            canContinue = true;
                                        }
                                        
                                        if (!canContinue)
                                            break;

                                        //attaching energys
                                        BloomCard(pointedCard.gameObject.transform.parent.gameObject, thisCard.gameObject);

                                        //creating the informatio for the server
                                        _DuelAction.playerID = _DuelField._MatchConnection._DuelFieldData.currentPlayerTurn;
                                        _DuelAction.usedCard = DataConverter.CreateCardDataFromCard(thisCard);
                                        _DuelAction.playedFrom = "Hand";
                                        _DuelAction.local = pointedCard.gameObject.transform.parent.gameObject.name;
                                        _DuelAction.targetCard = DataConverter.CreateCardDataFromCard(pointedCard);
                                        _DuelAction.actionType = "BloomHolomem";

                                        _DuelField.GenericActionCallBack(_DuelAction);
                                        validDropZoneFound = true;
                                        _DuelField.ArrangeCards(_DuelField.cardsPlayer, _DuelField.cardHolderPlayer);
                                    }
                                }
                                break;
                        }
                    break;
            }
            zoneCounter++;
        }
        if(!validDropZoneFound)
        rectTransform = defaultValues.ApplyToRectTransform(rectTransform);
    }

    void PerformActionBasedOnDropZone(string zoneType, bool toBack = false)
    {
        Card thisCard;

        switch (zoneType)
        {
            case "Collaboration":
                _DuelField.SendCardToZone(gameObject, "Collaboration", TargetPlayer.Player, toBack);
                _DuelField.cardsPlayer.Remove(gameObject.GetComponent<RectTransform>());
                thisCard = GetComponentInChildren<Card>();
                thisCard.playedFrom = "hand";
                thisCard.cardPosition = zoneType;
                thisCard.playedThisTurn = true;
                rectTransform.anchoredPosition = Vector2.zero;
                _DuelField.UpdateHP(thisCard);
                Destroy(this);
                break;
            case "Stage":

                this.AddComponent<DropZone>().zoneType = "HoloMember";

                _DuelField.SendCardToZone(gameObject, "Stage", TargetPlayer.Player, toBack);
                _DuelField.cardsPlayer.Remove(gameObject.GetComponent<RectTransform>());
                thisCard = GetComponentInChildren<Card>();
                thisCard.playedFrom = "hand";
                thisCard.cardPosition = zoneType;
                thisCard.playedThisTurn = true;
                rectTransform.anchoredPosition = Vector2.zero;
                _DuelField.UpdateHP(thisCard);
                Destroy(this);
                break;
            case "BackStage1":
            case "BackStage2":
            case "BackStage3":
            case "BackStage4":
            case "BackStage5":
                this.AddComponent<DropZone>().zoneType = "HoloMember";

                _DuelField.SendCardToZone(gameObject, zoneType, TargetPlayer.Player, toBack);
                _DuelField.cardsPlayer.Remove(gameObject.GetComponent<RectTransform>());
                thisCard = GetComponentInChildren<Card>();
                thisCard.playedFrom = "hand";
                thisCard.cardPosition = zoneType;
                thisCard.playedThisTurn = true;
                rectTransform.anchoredPosition = Vector2.zero;
                _DuelField.UpdateHP(thisCard);
                Destroy(this);
                break;
        }
        _DuelField.ArrangeCards(_DuelField.cardsPlayer, _DuelField.cardHolderPlayer);


        //   if (!validDropZoneFound)

        //        rectTransform = defaultValues.ApplyToRectTransform(rectTransform);
    }

    void AttachCardToCard(GameObject FatherZone, GameObject Card) {

        Card.transform.SetParent(FatherZone.transform, true);

        Card.transform.SetSiblingIndex(0); 

        Card.transform.localPosition = Vector3.zero;
        Card.transform.localScale = new Vector3(0.9f, 0.9f);

            _DuelField.cardsPlayer.Remove(Card.GetComponent<RectTransform>());
        Card.GetComponentInChildren<Card>().playedFrom = "hand";
            _DuelField.ArrangeCards(_DuelField.cardsPlayer, _DuelField.cardHolderPlayer);
        Destroy(this);
    }
    void BloomCard(GameObject FatherZone,  GameObject Card)
    {

        GameObject FatherZoneActiveCard = FatherZone.transform.GetChild(FatherZone.transform.childCount -1).gameObject;

        //set this card dropped card as child of the zone
        Card.transform.SetParent(FatherZone.transform, true);
        //add dropzone
        this.AddComponent<DropZone>().zoneType = "HoloMember";

        Card.transform.SetSiblingIndex(FatherZone.transform.childCount - 1);

        Card.transform.localPosition = Vector3.zero;
        Card.transform.localScale = new Vector3(0.9f, 0.9f);

        //get both cards information
        Card CardComponent = Card.GetComponent<Card>();
        Card FatherZoneActiveCardComponent = Card.GetComponent<Card>();

        CardComponent.suspended = FatherZoneActiveCardComponent.suspended;
        //if the card targeted is suspended, lets suspend
        if (FatherZoneActiveCardComponent.suspended) { 
            this.gameObject.transform.Rotate(0, 0, 90);
        }

        //passing the hp to the new card
        CardComponent.currentHp = FatherZoneActiveCardComponent.currentHp;
        CardComponent.effectDamageRecieved = FatherZoneActiveCardComponent.effectDamageRecieved;
        CardComponent.normalDamageRecieved = FatherZoneActiveCardComponent.normalDamageRecieved;
        //end of passing the hp to the new card

        //getting the name of the father gameobject the the position of the new card
        Card.GetComponentInChildren<Card>().cardPosition = FatherZoneActiveCard.transform.parent.name;
        //adding all the bloomed childs as reference to the new card
        Card.GetComponentInChildren<Card>().bloomChild.Add(FatherZoneActiveCard);
        //adding of attachs from the bloomed card to the new card
        Card.GetComponentInChildren<Card>().attachedCards = FatherZoneActiveCard.GetComponentInChildren<Card>().attachedCards;
        //removing the reference from the past form card
        FatherZoneActiveCard.GetComponentInChildren<Card>().attachedCards = null;

        _DuelField.cardsPlayer.Remove(Card.GetComponent<RectTransform>());
        Card.GetComponentInChildren<Card>().playedFrom = "hand";
        _DuelField.ArrangeCards(_DuelField.cardsPlayer, _DuelField.cardHolderPlayer);
        _DuelField.UpdateHP(Card.GetComponent<Card>());

        //make the older last position card invisible 
        FatherZoneActiveCard.SetActive(false);
        Destroy(this);
    }
    public void EffectQuestionDispalyMenuButton(DuelAction duelAction)
    {
        EffectQuestionPainel.SetActive(true);
        EffectQuestionPainelYesButton.onClick.AddListener(() => EffectQuestionYesButton(duelAction));
        EffectQuestionPainelNoButton.onClick.AddListener(() => EffectQuestionNoButton(duelAction));
    }

    public void EffectQuestionYesButton(DuelAction duelAction)
    {
        duelAction.actionType = "BloomHolomemWithEffect";
        _DuelField.GenericActionCallBack(duelAction, "BloomHolomemWithEffect");
        EffectQuestionPainel.SetActive(false);
    }
    public void EffectQuestionNoButton(DuelAction duelAction)
    {
        _DuelField.GenericActionCallBack(duelAction);
        EffectQuestionPainel.SetActive(false);
    }
}