using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class HandClick : MonoBehaviour, IPointerClickHandler
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

        DuelAction duelAction = new()
        {
            player = DuelField.INSTANCE.playersType[PlayerInfo.INSTANCE.PlayerID],
            used = this.GetComponent<Card>().ToCardData(),
        };

        if (!DuelField.INSTANCE.isViewMode && !DuelField.INSTANCE.hasAlreadyCollabed && ISMYTURN && ISMYCARD && DuelField.DEFAULTBACKSTAGE.Contains(ActiveCard.curZone)
            && ISMAINPHASE && !ActiveCard.suspended)
        {
            MatchConnection.INSTANCE.SendRequest(duelAction, "DoCollab");
            DuelField.INSTANCE.hasAlreadyCollabed = true;
            clickAction = ClickAction.Collab;
            return;
        }
        else if (!DuelField.INSTANCE.isViewMode && ISMYTURN && ISMYCARD && DuelField.DEFAULTBACKSTAGE.Contains(ActiveCard.curZone)
            && DuelField.INSTANCE.GamePhase.Equals(GAMEPHASE.SetHolomemStep))
        {
            MatchConnection.INSTANCE.SendRequest(duelAction, "ReSetCardAtStage");
            clickAction = ClickAction.ReSETStage;
            return;
        }
        else if ((ActiveCard.cardType == CardType.ホロメン || ActiveCard.cardType == CardType.Buzzホロメン)
            && ActiveCard.curZone.Equals(Lib.GameZone.Collaboration) || ActiveCard.curZone.Equals(Lib.GameZone.Stage)
            && ActiveCard.usable)
        {
            MatchConnection.INSTANCE.SendRequest(duelAction, "CheckArt");
            clickAction = ClickAction.ViewAndUseArt;
            return;
        }
        else if (ActiveCard.cardType == CardType.推しホロメン && ActiveCard.usable)
        {
            MatchConnection.INSTANCE.SendRequest(duelAction, "CheckOshiSkill");
            clickAction = ClickAction.ViewAndUseOshiBothSkills;
            return;
        }

        int n = 0;
        for (; n < CardsInTheZone.Count; n++)
            if (CardsInTheZone[n].gameObject == targetCardGameObject)
                break;

        DuelfField_CardDetailViewer.INSTANCE.SetCardListToBeDisplayed(ref CardsInTheZone, n, clickAction, new());
    }
}
