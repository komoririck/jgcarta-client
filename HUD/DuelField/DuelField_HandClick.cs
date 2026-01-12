using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class DuelField_HandClick : MonoBehaviour, IPointerClickHandler
{
    void Start()
    {
        if (string.IsNullOrEmpty(gameObject.GetComponent<Card>().cardNumber))
            this.enabled = false;
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
    public void OnPointerClick(PointerEventData eventData)
    {
        if (DuelField_UI_MAP.INSTANCE.SS_LosePanel.gameObject.activeInHierarchy || DuelField_UI_MAP.INSTANCE.SS_WinPanel.gameObject.activeInHierarchy || DuelField_UI_MAP.INSTANCE.SS_LogPanel.gameObject.activeInHierarchy
            || DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_ActivateEffectPanel.gameObject.activeInHierarchy || DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel.gameObject.activeInHierarchy
            || DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_SelectionDetachEnergyPanel.gameObject.activeInHierarchy || DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_SelectionPanel.gameObject.activeInHierarchy)
            return;

        if (HandDragDrop.IsDragging_Global)
            return;

        DoAction(this.gameObject);
    }
    void DoAction(GameObject targetCardGameObject)
    {
        ClickAction clickAction = ClickAction.OnlyView;

        List<Card> CardsInTheZone = this.transform.parent.GetComponentsInChildren<Card>(true).ToList();
        Card ActiveCard = CardsInTheZone.Where(item => item.gameObject.activeSelf).FirstOrDefault();

        if (ActiveCard == null) 
            return;

        bool ISMYTURN = DuelField.INSTANCE.IsMyTurn();
        bool ISMYCARD = targetCardGameObject.transform.parent.parent.name.Equals("PlayerGeneral");
        bool ISMAINPHASE = DuelField.INSTANCE.GamePhase.Equals(GAMEPHASE.MainStep);

        if (!DuelField.INSTANCE.isViewMode && !DuelField.INSTANCE.hasAlreadyCollabed && ISMYTURN && ISMYCARD && DuelField.DEFAULTBACKSTAGE.Contains(ActiveCard.curZone)
            && ISMAINPHASE && !ActiveCard.suspended)
        {
            clickAction = ClickAction.Collab;
        }
        else if (!DuelField.INSTANCE.isViewMode && ISMYTURN && ISMYCARD && DuelField.DEFAULTBACKSTAGE.Contains(ActiveCard.curZone)
            && DuelField.INSTANCE.GamePhase.Equals(GAMEPHASE.SetHolomemStep))
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
            bool conditionA = (!DuelField.INSTANCE.usedOshiSkill && CardLib.CanActivateOshiSkill(ActiveCard.cardNumber));
            bool conditionB = (!DuelField.INSTANCE.usedSPOshiSkill && CardLib.CanActivateSPOshiSkill(ActiveCard.cardNumber));

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
                playerID = DuelField.INSTANCE.playersType[PlayerInfo.INSTANCE.PlayerID],
                used = this.GetComponent<Card>().ToCardData(),
                targetedZones = new() { Lib.GameZone.Collaboration },
            };

            MatchConnection.INSTANCE.SendRequest(duelAction, "DoCollab");
            DuelField.INSTANCE.hasAlreadyCollabed = true;
        }
        else if (clickAction.Equals(ClickAction.ReSETStage))
        {
            DuelAction duelActionn = new()
            {
                playerID = DuelField.INSTANCE.playersType[PlayerInfo.INSTANCE.PlayerID],
                used = this.GetComponent<Card>().ToCardData(),
                targetedZones = new() { Lib.GameZone.Stage },
            };
            MatchConnection.INSTANCE.SendRequest(duelActionn, "ReSetCardAtStage");
        }
        else
        {
            DuelfField_CardDetailViewer.INSTANCE.SetCardListToBeDisplayed(ref CardsInTheZone, CardsInTheZone.IndexOf(ActiveCard), clickAction);
        }
        ActiveCard.Glow();
    }
}
