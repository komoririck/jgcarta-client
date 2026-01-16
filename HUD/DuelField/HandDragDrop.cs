using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using static DuelField;

public class HandDragDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static bool IsDragging_Global = false;

    [SerializeField] private RectTransform rectTransform;
    [SerializeField] public RectTransformData defaultValues;

    private Vector3 screenPoint;
    private Vector3 offset;

    public bool? IsDragging = null;
    PointerEventData dragWatcher = null;

    Card thisCard;
    Card targetCard;
    GameObject targetZone;
    Lib.GameZone TargetZoneEnum = 0;
    DuelAction da;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }
    public void Update()
    {
        if (IsDragging == null)
            return;

        if (IsDragging == true)
        {
            Vector3 currentScreenPoint = new Vector3(dragWatcher.position.x, dragWatcher.position.y, screenPoint.z);
            Vector3 currentWorldPos = Camera.main.ScreenToWorldPoint(currentScreenPoint) + offset;
            transform.position = currentWorldPos;
        }
        else
        {
            targetCard = thisCard = null;
            targetZone = null;

            GetComponent<BoxCollider>().enabled = true;
            rectTransform = defaultValues.ApplyToRectTransform(rectTransform);
            IsDragging_Global = false;
            IsDragging = null;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        GetComponent<BoxCollider>().enabled = false;
        defaultValues = new RectTransformData(rectTransform);

        screenPoint = Camera.main.WorldToScreenPoint(transform.position);
        offset = transform.position - Camera.main.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, screenPoint.z));
        rectTransform.localScale = new Vector3(rectTransform.localScale.x * 1.1f, rectTransform.localScale.y * 1.1f, 1f);

        IsDragging = IsDragging_Global = true;
    }
    public void OnDrag(PointerEventData eventData)
    {
        dragWatcher = eventData;
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        IsDragging = false;

        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        RaycastHit[] hits = Physics.SphereCastAll(ray, 0.5f);

        foreach (var h in hits)
        {
            targetZone = DuelField.INSTANCE.GetGameZones().Contains(h.transform.gameObject) ? h.transform.gameObject : null;
            if (targetZone != null)
                break;
        }

        if (targetZone == null)
            return;

        thisCard = GetComponent<Card>();
        TargetZoneEnum = DuelField.INSTANCE.GetZoneByString(targetZone.name);
        targetCard = CardLib.GetAndFilterCards(player: Player.Player, onlyVisible: true, gameZones: new[] { TargetZoneEnum }).FirstOrDefault();

        da = new()
        {
            used = thisCard.ToCardData(),
            target = targetCard != null ? targetCard.ToCardData() : null,
            targetedZones = new() { TargetZoneEnum },
        };

        switch (DuelField.INSTANCE.GamePhase)
        {
            case GAMEPHASE.SettingUpBoard:
                var stage = DuelField.INSTANCE.GetZone(Lib.GameZone.Stage, Player.Player).GetComponentInChildren<Card>();
                if (targetCard != null || targetZone.transform.parent.name.Equals("OponenteGeneral") || (stage == null && TargetZoneEnum != Lib.GameZone.Stage) || !DEFAULTHOLOMEMZONE.Contains(TargetZoneEnum) || TargetZoneEnum == Lib.GameZone.Collaboration)
                    break;

                MatchConnection.INSTANCE.SendRequest(da, "InicialBoardSetup");
                break;

            case GAMEPHASE.CheerStepChoose:
                if (targetCard == null || !(targetCard.cardType == CardType.ホロメン || targetCard.cardType == CardType.Buzzホロメン))
                    break;

                MatchConnection.INSTANCE.SendRequest(da, "CheerChooseRequest");
                break;

            case GAMEPHASE.MainStep:
                Handle_MainStep();
                break;
        }
    }
    private void Handle_MainStep()
    {
        switch (thisCard.cardType)
        {
            case CardType.サポートイベント:
            case CardType.サポートアイテム:
                MatchConnection.INSTANCE.SendRequest(da, "ResolveOnSupportEffect");
                break;
            case CardType.サポートスタッフLIMITED:
            case CardType.サポートイベントLIMITED:
            case CardType.サポートアイテムLIMITED:
                if (DuelField.INSTANCE.playerLimiteCardPlayed.Count > 0)
                    return;
                DuelField.INSTANCE.playerLimiteCardPlayed.Add(thisCard);
                MatchConnection.INSTANCE.SendRequest(da, "ResolveOnSupportEffect");
                break;
            case CardType.サポートツール:
            case CardType.サポートマスコット:
            case CardType.サポートファン:
                MatchConnection.INSTANCE.SendRequest(da, "AttachEquipamentToHolomem");
                break;
            case CardType.ホロメン:
            case CardType.Buzzホロメン:
                if (!DuelField.DEFAULTHOLOMEMZONE.Contains(TargetZoneEnum) || targetZone == null)
                    return;

                if (targetCard == null && CardLib.CountPlayerActiveHolomem() < 5 && DEFAULTBACKSTAGE.Contains(TargetZoneEnum))
                    MatchConnection.INSTANCE.SendRequest(da, "PlayHolomem");
                else
                    MatchConnection.INSTANCE.SendRequest(da, "BloomHolomem");
                break;
        }
    }
}