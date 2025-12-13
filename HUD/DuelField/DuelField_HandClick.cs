using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using static DuelField;

public class DuelField_HandClick : MonoBehaviour, IPointerClickHandler
{
    bool VALIDFORACTION = false;
    void Start()
    {
        if (string.IsNullOrEmpty(gameObject.GetComponent<Card>().cardNumber))
        {
            this.enabled = false;
        }
    }
    [Flags]
    public enum ClickAction : byte
    {
        OnlyView = 0,
        Collab = 1,
        ViewAndUseOshiBothSkills = 2,
        ViewAndUseArt = 3,
        ReSETStage = 4,
        ViewAndUseSPOshiSkill = 5,
    }
    void DoAction(GameObject targetCardGameObject)
    {
        ClickAction clickAction = ClickAction.OnlyView;

        List<Card> CardsInTheZone = this.transform.parent.GetComponentsInChildren<Card>(true).ToList();

        Card ActiveCard = null;
        foreach (Card card in CardsInTheZone)
            if (card.gameObject.activeSelf)
                ActiveCard = card;

        if (ActiveCard == null) return;

        bool ISMYTURN = DuelField.INSTANCE.DUELFIELDDATA.currentPlayerTurn == PlayerInfo.INSTANCE.PlayerID;
        bool ISMYCARD = targetCardGameObject.transform.parent.parent.name.Equals("PlayerGeneral") || targetCardGameObject.transform.parent.parent.parent.name.Equals("PlayerGeneral");
        bool ISMAINPHASE = DuelField.INSTANCE.DUELFIELDDATA.currentGamePhase.Equals(DuelFieldData.GAMEPHASE.MainStep);

        var backRoll = new[]
        {
            Lib.GameZone.BackStage1,
            Lib.GameZone.BackStage2,
            Lib.GameZone.BackStage3,
            Lib.GameZone.BackStage4,
            Lib.GameZone.BackStage5
        };

        if (!DuelField.INSTANCE.isViewMode && !DuelField.INSTANCE.hasAlreadyCollabed && ISMYTURN && ISMYCARD && backRoll.Contains(ActiveCard.curZone)
            && ISMAINPHASE && !ActiveCard.suspended)
        {
            clickAction = ClickAction.Collab;
        }
        else if (!DuelField.INSTANCE.isViewMode && ISMYTURN && ISMYCARD && backRoll.Contains(ActiveCard.curZone)
            && DuelField.INSTANCE.DUELFIELDDATA.currentGamePhase.Equals(DuelFieldData.GAMEPHASE.ResetStepReSetStage))
        {
            clickAction = ClickAction.ReSETStage;
        }
        else if ((ActiveCard.cardType.Equals("ホロメン") || ActiveCard.cardType.Equals("Buzzホロメン"))
            && ActiveCard.curZone.Equals(Lib.GameZone.Collaboration) || ActiveCard.curZone.Equals(Lib.GameZone.Stage)
            && !DuelField.INSTANCE.isViewMode
            && ISMAINPHASE && ISMYTURN && ISMYCARD)
        {
            clickAction = ClickAction.ViewAndUseArt;
        }
        else if (ActiveCard.cardType.Equals("推しホロメン")
            && !DuelField.INSTANCE.isViewMode
            && ISMAINPHASE && ISMYTURN && ISMYCARD)
        {
            bool conditionA = (!DuelField.INSTANCE.usedOshiSkill && DuelField.INSTANCE.CanActivateOshiSkill(ActiveCard.cardNumber));
            bool conditionB = (!DuelField.INSTANCE.usedSPOshiSkill && DuelField.INSTANCE.CanActivateSPOshiSkill(ActiveCard.cardNumber));

            if (conditionA && conditionB)
                clickAction = ClickAction.ViewAndUseOshiBothSkills;
            else if (conditionA)
                clickAction = ClickAction.ViewAndUseOshiBothSkills;
            else if (conditionB)
                clickAction = ClickAction.ViewAndUseSPOshiSkill;
        }

        if (clickAction.Equals(ClickAction.Collab))
        {
            DuelAction duelAction = new()
            {
                playerID = PlayerInfo.INSTANCE.PlayerID,
                usedCard = this.GetComponent<Card>().ToCardData(),
                activationZone = Lib.GameZone.Collaboration,
            };

            DuelField.INSTANCE.GenericActionCallBack(duelAction, "DoCollab");
            DuelField.INSTANCE.hasAlreadyCollabed = true;
        }
        else if (clickAction.Equals(ClickAction.ReSETStage))
        {
            DuelAction duelActionn = new()
            {
                playerID = PlayerInfo.INSTANCE.PlayerID,
                usedCard = this.GetComponent<Card>().ToCardData(),
                activationZone = Lib.GameZone.Stage,
            };
            DuelField.INSTANCE.GenericActionCallBack(duelActionn, "ReSetCardAtStage");
        }
        else
        {
            DuelfField_CardDetailViewer.INSTANCE.SetCardListToBeDisplayed(ref CardsInTheZone, ActiveCard, clickAction);
        }
        ActiveCard.Glow();
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (DuelField_UI_MAP.INSTANCE.SS_LosePanel.gameObject.activeInHierarchy || DuelField_UI_MAP.INSTANCE.SS_WinPanel.gameObject.activeInHierarchy || DuelField_UI_MAP.INSTANCE.SS_LogPanel.gameObject.activeInHierarchy
            || DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_ActivateEffectPanel.gameObject.activeInHierarchy || DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel.gameObject.activeInHierarchy
            || DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_SelectionDetachEnergyPanel.gameObject.activeInHierarchy || DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_SelectionPanel.gameObject.activeInHierarchy)
            return;

        if (DuelField_HandDragDrop.IsDragging)
            return;

        DoAction(this.gameObject);
    }
}
