using Assets.Scripts.Lib;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
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
    [SerializeField] private DropZone PlayerField;
    DuelField_HandClick handClick;

    private int originalSiblingIndex;

    static public GameObject EffectQuestionPainel;
    static public Button EffectQuestionPainelYesButton;
    static public Button EffectQuestionPainelNoButton;


    private Vector3 screenPoint;
    private Vector3 offset;

    private Camera mainCamera;

    private RaycastHit hit;

    private GameObject targetCardGameObject;


    public const bool TESTEMODE = false;

    void Start()
    {
        EffectQuestionPainel = GameObject.Find("EffectBoxes").transform.Find("ActivateEffectPanel").gameObject;
        EffectQuestionPainelYesButton = EffectQuestionPainel.transform.Find("YesButton").GetComponent<Button>();
        EffectQuestionPainelNoButton = EffectQuestionPainel.transform.Find("NoButton").GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        defaultValues = new RectTransformData(rectTransform);
        _DuelField = GameObject.FindAnyObjectByType<DuelField>();
        handClick = GetComponent<DuelField_HandClick>();

        mainCamera = Camera.main;
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
        //GetComponent<BoxCollider>().enabled = false;

        handClick.enabled = false;
        // Store the original sibling index so we can return the card to its original position later
        originalSiblingIndex = rectTransform.GetSiblingIndex();

        // Move the card to the top of the hierarchy to render it on top of others
        rectTransform.SetSiblingIndex(rectTransform.parent.childCount - 1);

        rectTransform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        //canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        
        screenPoint = mainCamera.WorldToScreenPoint(transform.position);
        offset = transform.position - mainCamera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, screenPoint.z));

    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 currentScreenPoint = new Vector3(eventData.position.x, eventData.position.y, screenPoint.z);
        Vector3 currentWorldPos = mainCamera.ScreenToWorldPoint(currentScreenPoint) + offset;
        transform.position = currentWorldPos;

        Ray ray = mainCamera.ScreenPointToRay(eventData.position);

        if (Physics.Raycast(mainCamera.ScreenPointToRay(eventData.position), out hit))
        {
            targetCardGameObject = hit.collider.gameObject;
        }

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        rectTransform = defaultValues.ApplyToRectTransform(rectTransform);

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        rectTransform.SetSiblingIndex(originalSiblingIndex);
        //GetComponent<BoxCollider>().enabled = true;
        handClick.enabled = true;


        DuelAction _DuelAction = new();

        bool validDropZoneFound = false;

        if (targetCardGameObject == null)
            return;

        if (!(targetCardGameObject.transform.parent.name.Equals("Player") || targetCardGameObject.transform.parent.parent.name.Equals("Player")))
            return;

        if (!(targetCardGameObject.name.Equals("Collaboration") || targetCardGameObject.name.Equals("Stage") || targetCardGameObject.name.Equals("BackStage1") || targetCardGameObject.name.Equals("BackStage2") ||
            targetCardGameObject.name.Equals("BackStage3") || targetCardGameObject.name.Equals("BackStage4") || targetCardGameObject.name.Equals("BackStage5")))
            return;

        Card thisCard = GetComponent<Card>();
        Card targetCard = targetCardGameObject.GetComponentInChildren<Card>();

        switch (_DuelField._MatchConnection._DuelFieldData.currentGamePhase)
            {
                case DuelFieldData.GAMEPHASE.SettingUpBoard:
                    switch (targetCardGameObject.name)
                    {
                        case "BackStage1":
                        case "BackStage2":
                        case "BackStage3":
                        case "BackStage4":
                        case "BackStage5":
                            if (_DuelField.GetZone("Stage", TargetPlayer.Player).GetComponentInChildren<Card>() != null)
                            {
                                PerformActionBasedOnDropZone(targetCardGameObject.name);
                                validDropZoneFound = true;
                                _DuelField.ArrangeCards(_DuelField.cardsPlayer, _DuelField.cardHolderPlayer);
                            }
                            break;
                        case "Collaboration":
                            break;
                        case "Stage":
                            if (targetCard == null)
                            {
                                PerformActionBasedOnDropZone(targetCardGameObject.name);
                                validDropZoneFound = true;
                                _DuelField.ArrangeCards(_DuelField.cardsPlayer, _DuelField.cardHolderPlayer);
                            }
                            break;
                    }
                    break;
            case DuelFieldData.GAMEPHASE.HolomemDefeatedEnergyChoose:
            case DuelFieldData.GAMEPHASE.CheerStepChoose:

                if (targetCard == null)
                    break;


                if (thisCard.cardType.Equals("エール"))
                {
                    if (targetCard.cardType.Equals("ホロメン") || targetCard.cardType.Equals("Buzzホロメン"))
                    {
                        _DuelAction.playerID = _DuelField._MatchConnection._DuelFieldData.currentPlayerTurn;
                        _DuelAction.usedCard = CardData.CreateCardDataFromCard(thisCard);
                        _DuelAction.usedCard.playedFrom = "hand";
                        _DuelAction.local = targetCard.gameObject.transform.parent.gameObject.name;
                        _DuelAction.targetCard = CardData.CreateCardDataFromCard(targetCard);

                        _DuelField.GenericActionCallBack(_DuelAction, "CheerChooseRequest");

                        validDropZoneFound = true;
                        break;
                    }
                }
                break;
            case DuelFieldData.GAMEPHASE.MainStep:

                    if (thisCard.cardType.Equals("サポート・イベント") || thisCard.cardType.Equals("サポート・アイテム") || thisCard.cardType.Equals("サポート・スタッフ・LIMITED") || thisCard.cardType.Equals("サポート・イベント・LIMITED") || thisCard.cardType.Equals("サポート・アイテム・LIMITED"))
                    {
                        if (!_DuelField.CheckForPlayRestrictions(thisCard.cardNumber))
                            break;


                        if (thisCard.cardType.Equals("サポート・スタッフ・LIMITED") || thisCard.cardType.Equals("サポート・イベント・LIMITED") || thisCard.cardType.Equals("サポート・アイテム・LIMITED"))
                        {

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

                        _DuelAction = new DuelAction()
                        {
                            usedCard = CardData.CreateCardDataFromCard(thisCard),
                            playerID = _DuelField._MatchConnection._DuelFieldData.currentPlayerTurn,
                            playedFrom = "hand",
                            local = (targetCard != null) ? targetCard.gameObject.transform.parent.gameObject.name : "",
                            targetCard = CardData.CreateCardDataFromCard(targetCard)
                        };
                        FindAnyObjectByType<EffectController>().ResolveSuportEffect(_DuelAction);

                        rectTransform.anchoredPosition = Vector2.zero;

                        validDropZoneFound = true;
                        break;
                    }
                    else if (thisCard.cardType.Equals("サポート・ツール") || thisCard.cardType.Equals("サポート・マスコット") || thisCard.cardType.Equals("サポート・ファン"))
                    {

                        if (_DuelField.HasRestrictionsToPlayEquip(thisCard, targetCard))
                            break;

                        _DuelAction = new DuelAction()
                        {
                            usedCard = CardData.CreateCardDataFromCard(thisCard),
                            playerID = _DuelField._MatchConnection._DuelFieldData.currentPlayerTurn,
                            playedFrom = "hand",
                            local = (targetCard != null) ? targetCard.gameObject.transform.parent.gameObject.name : "",
                            targetCard = CardData.CreateCardDataFromCard(targetCard)
                        };


                        _DuelField.GenericActionCallBack(_DuelAction, "AttachEquipamentToHolomem");

                        validDropZoneFound = true;
                        break;
                    }
                    else if (thisCard.cardType.Equals("ホロメン") || thisCard.cardType.Equals("Buzzホロメン"))
                        // PLAY HOLOOMEM
                        switch (targetCardGameObject.name)
                        {
                            case "BackStage1":
                            case "BackStage2":
                            case "BackStage3":
                            case "BackStage4":
                            case "BackStage5":
                            case "Stage":
                                Transform lastChild = null;
                                if (targetCardGameObject.transform.childCount > 0)
                                    lastChild = targetCardGameObject.transform.GetChild(targetCardGameObject.transform.childCount - 1);

                                Card pointedCard = lastChild.GetComponent<Card>();

                                // se a posição que jogamos a carta está vázia, e a carta jogada e holomem ou buzz, nos tentamos abaixar, senão tentamos bloomar
                                if (pointedCard == null && (thisCard.cardType.Equals("ホロメン") || thisCard.cardType.Equals("Buzzホロメン")) || TESTEMODE)
                                {
                                    if (!TESTEMODE) { 

                                    if (_DuelField.GetZone(targetCardGameObject.name, TargetPlayer.Player).GetComponentInChildren<Card>() != null)
                                        break;

                                    thisCard.GetCardInfo();
                                    bool canContinue = false;

                                    if (thisCard.bloomLevel.Equals("Debut") || thisCard.bloomLevel.Equals("Spot"))
                                        canContinue = true;

                                    if (!canContinue)
                                        break;

                                    //get holomem count
                                    int count = _DuelField.CountBackStageTotal();

                                    if (targetCardGameObject.name.Equals("BackStage1") || targetCardGameObject.name.Equals("BackStage2") || targetCardGameObject.name.Equals("BackStage3") ||
                                        targetCardGameObject.name.Equals("BackStage4") || targetCardGameObject.name.Equals("BackStage5"))
                                    {
                                        canContinue = false;

                                        if (count < 5)
                                            canContinue = true;
                                    }

                                    if (!canContinue)
                                        break;


                                    }
                                    //PerformActionBasedOnDropZone(dropZone.zoneType);

                                    _DuelAction.usedCard.cardNumber = thisCard.cardNumber;
                                    _DuelAction.local = targetCardGameObject.name;
                                    _DuelAction.playedFrom = "hand";

                                    _DuelField.GenericActionCallBack(_DuelAction, "PlayHolomem");

                                    validDropZoneFound = true;
                                    break;
                                }
                                else // BLOOM
                                {
                                    //cards cannot bloom the turn they are played 
                                    if (pointedCard.playedThisTurn == true || pointedCard.cardType.Equals("Buzzホロメン"))
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
                                        }
                                        else if (pointedCard.cardNumber.Equals("hSD01-013") && (thisCard.cardName.Equals("ときのそら") || thisCard.cardName.Equals("AZKi")) && bloomToLevel.Equals("2nd"))
                                        {
                                            canContinue = true;
                                        }
                                        else if (thisCard.cardNumber.Equals("hBP01-045")) {
                                            int lifeCounter = _DuelField.GetZone("Life", TargetPlayer.Player).transform.childCount - 1;
                                            if (lifeCounter < 4 && pointedCard.cardName.Equals("AZKi") || pointedCard.cardName.Equals("SorAZ"))
                                            {
                                                canContinue = true;
                                            }
                                        }
                                        //normal condition to bloom match
                                        else if (thisCard.cardName.Equals(pointedCard.cardName) && thisCard.bloomLevel.Equals(bloomToLevel))
                                        {
                                            canContinue = true;
                                        }

                                        if (!canContinue)
                                            break;

                                        //attaching energys
                                        //BloomCard(pointedCard.gameObject.transform.parent.gameObject, thisCard.gameObject);

                                        //creating the informatio for the server
                                        _DuelAction.playerID = _DuelField._MatchConnection._DuelFieldData.currentPlayerTurn;
                                        _DuelAction.usedCard = CardData.CreateCardDataFromCard(thisCard);
                                        _DuelAction.playedFrom = "hand";
                                        _DuelAction.local = pointedCard.gameObject.transform.parent.gameObject.name;
                                        _DuelAction.targetCard = CardData.CreateCardDataFromCard(pointedCard);

                                        _DuelField.GenericActionCallBack(_DuelAction, "BloomHolomem");

                                        validDropZoneFound = true;
                                        break;
                                    }
                                }
                                break;
                        }
                    break;
            }

        if (validDropZoneFound)
        {
            if (_DuelField._MatchConnection._DuelFieldData.currentGamePhase != DuelFieldData.GAMEPHASE.SettingUpBoard)
            {
                _DuelField.cardsPlayer.Remove(rectTransform);
                this.transform.SetParent(GameObject.Find("HUD").transform);
                this.gameObject.SetActive(false);
                Destroy(this.gameObject);
            }
        }
        targetCardGameObject = null;
    }

    void PerformActionBasedOnDropZone(string zoneType, bool toBack = false)
    {
        Card thisCard = GetComponentInChildren<Card>();

        switch (zoneType)
        {
            case "Collaboration":
            case "Stage":
            case "BackStage1":
            case "BackStage2":
            case "BackStage3":
            case "BackStage4":
            case "BackStage5":
                thisCard.playedFrom = "hand";
                thisCard.cardPosition = zoneType;
                _DuelField.AddOrMoveCardToGameZone(_DuelField.GetZone(zoneType, TargetPlayer.Player), cardsToBeMoved: new List<GameObject> { gameObject }, TOBOTTOMOFTHELIST: toBack);
                break;
        }
        _DuelField.cardsPlayer.Remove(gameObject.GetComponent<RectTransform>());
        thisCard.playedThisTurn = true;
        rectTransform.anchoredPosition = Vector2.zero;
        _DuelField.UpdateHP(thisCard);
        Destroy(this);
    }

    void AttachCardToCard(GameObject FatherZone, GameObject Card)
    {

        Card.transform.SetParent(FatherZone.transform, true);

        Card.transform.SetSiblingIndex(0);

        Card.transform.localPosition = Vector3.zero;
        Card.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);

        _DuelField.cardsPlayer.Remove(Card.GetComponent<RectTransform>());
        Card.GetComponentInChildren<Card>().playedFrom = "hand";
        Destroy(this);
    }
    void BloomCard(GameObject FatherZone, GameObject Card)
    {

        GameObject FatherZoneActiveCard = FatherZone.transform.GetChild(FatherZone.transform.childCount - 1).gameObject;

        //set this card dropped card as child of the zone
        Card.transform.SetParent(FatherZone.transform, true);

        Card.transform.SetSiblingIndex(FatherZone.transform.childCount - 1);

        Card.transform.localPosition = Vector3.zero;
        Card.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);

        //get both cards information
        Card CardComponent = Card.GetComponent<Card>();
        Card FatherZoneActiveCardComponent = Card.GetComponent<Card>();

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
        _DuelField.UpdateHP(CardComponent);
        //end of passing the hp to the new card

        //getting the name of the father gameobject the the position of the new card
        Card.GetComponentInChildren<Card>().cardPosition = FatherZoneActiveCard.transform.parent.name;
        //adding all the bloomed childs as reference to the new card
        Card.GetComponentInChildren<Card>().bloomChild.Add(FatherZoneActiveCard);
        //adding of attachs from the bloomed card to the new card
        Card.GetComponentInChildren<Card>().attachedEnergy = FatherZoneActiveCard.GetComponentInChildren<Card>().attachedEnergy;
        //removing the reference from the past form card
        FatherZoneActiveCard.GetComponentInChildren<Card>().attachedEnergy = null;

        _DuelField.cardsPlayer.Remove(Card.GetComponent<RectTransform>());
        Card.GetComponentInChildren<Card>().playedFrom = "hand";

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
        _DuelField.GenericActionCallBack(duelAction, "BloomHolomemWithEffect");
        EffectQuestionPainel.SetActive(false);
    }
    public void EffectQuestionNoButton(DuelAction duelAction)
    {
        _DuelField.GenericActionCallBack(duelAction, "standart");
        EffectQuestionPainel.SetActive(false);
    }
}