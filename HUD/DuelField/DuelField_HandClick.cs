using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using static DuelField;
using System.Linq;

public class DuelField_HandClick : MonoBehaviour, IPointerClickHandler
{
    static public bool isViewMode = false;
    static public bool isPainelActive = true;

    private Card[] cards = null;
    private DuelField _DuelField;
    private DuelfField_CardDetailViewer _DuelfField_CardDetailViewer;
    static readonly List<GameObject> skillsToDestroy = new();

    void Start()
    {
        _DuelField = GameObject.FindAnyObjectByType<DuelField>();
        if (gameObject.GetComponent<Card>().cardNumber.Equals(0))
        {
            this.enabled = false;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        var _SelectionPanel = DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_SelectionPanel;  
        var _SelectionDetachEnergyPanel = DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_SelectionDetachEnergyPanel;
        var _CardPanelInfo = DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel;
        var _ActivateEffectPanel = DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_ActivateEffectPanel;
        var _LogPanel = DuelField_UI_MAP.INSTANCE.SS_LogPanel;
        var _VictoryPanel = DuelField_UI_MAP.INSTANCE.SS_WinPanel;
        var _LosePanel = DuelField_UI_MAP.INSTANCE.SS_LosePanel;

        if (_LosePanel.gameObject.activeInHierarchy || _VictoryPanel.gameObject.activeInHierarchy || _LogPanel.gameObject.activeInHierarchy
            || _ActivateEffectPanel.gameObject.activeInHierarchy || _CardPanelInfo.gameObject.activeInHierarchy
            || _SelectionDetachEnergyPanel.gameObject.activeInHierarchy || _SelectionPanel.gameObject.activeInHierarchy)
            return;

        bool actionDone = false;

        if (isViewMode == false && MatchConnection.INSTANCE._DuelFieldData.currentPlayerTurn == PlayerInfo.INSTANCE.PlayerID)
        {
            //if no clicling in a dropzone which is assigned to all field zones
            if (transform.parent.TryGetComponent<DropZone>(out var pointedZoneFather))
            {

                switch (pointedZoneFather.zoneType)
                {
                    case "Stage":
                        break;
                    case "Collaboration":
                        break;
                    case "BackStage1":
                    case "BackStage2":
                    case "BackStage3":
                    case "BackStage4":
                    case "BackStage5":
                        switch (MatchConnection.INSTANCE._DuelFieldData.currentGamePhase)
                        {
                            case DuelFieldData.GAMEPHASE.MainStep:

                                if (this.GetComponent<Card>().suspended == true) { break; }

                                if (_DuelField.GetZone("Collaboration", TargetPlayer.Player).GetComponentInChildren<Card>() == null)
                                {
                                    //if (_DuelField.GetZone("HoloP", TargetPlayer.Player).transform.childCount == 0)
                                    //    return;

                                    DuelAction duelAction = new()
                                    {
                                        playerID = PlayerInfo.INSTANCE.PlayerID,
                                        usedCard = CardData.CreateCardDataFromCard(this.GetComponent<Card>()),
                                        playedFrom = this.transform.parent.name,
                                        local = "Collaboration",
                                    };
                                    _DuelField.GenericActionCallBack(duelAction, "DoCollab");
                                }
                                actionDone = true;
                                break;
                            case DuelFieldData.GAMEPHASE.ResetStepReSetStage:
                                if ((_DuelField.GetZone("Stage", TargetPlayer.Player).transform.GetComponentsInChildren<Card>().Count() > 0))
                                    return;

                                DuelAction duelActionn = new()
                                {
                                    playerID = PlayerInfo.INSTANCE.PlayerID,
                                    usedCard = CardData.CreateCardDataFromCard(this.GetComponent<Card>()),
                                    playedFrom = this.transform.parent.name,
                                    local = "Stage",
                                };
                                _DuelField.GenericActionCallBack(duelActionn, "ReSetCardAtStage");
                                actionDone = true;
                                break;
                        }
                        break;
                }
            }
        }

        if (actionDone == false || MatchConnection.INSTANCE._DuelFieldData.currentPlayerTurn != PlayerInfo.INSTANCE.PlayerID)
        {
            //if in the clicked location theres no card number, return since must be a facedown card
            if (string.IsNullOrEmpty(GetComponentInChildren<Card>().cardNumber))
                return;

            _DuelfField_CardDetailViewer??= FindAnyObjectByType<DuelfField_CardDetailViewer>(FindObjectsInactive.Include);

            List<Card> Cards = this.transform.parent.GetComponentsInChildren<Card>(true).ToList();
            _DuelfField_CardDetailViewer.SetCardListToBeDisplayed(ref Cards, isViewMode, GetComponent<Card>());
        }
    }
}
