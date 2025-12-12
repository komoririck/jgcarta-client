using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using static DuelField;

public class DuelField_HandClick : MonoBehaviour, IPointerClickHandler
{
    static public bool isPainelActive = true;

    private DuelField _DuelField;
    private DuelfField_CardDetailViewer _DuelfField_CardDetailViewer;

    private Vector3 screenPoint;
    private Vector3 offset;

    private Camera mainCamera;

    void Start()
    {
        _DuelField = GameObject.FindAnyObjectByType<DuelField>();
        if (string.IsNullOrEmpty(gameObject.GetComponent<Card>().cardNumber))
        {
            this.enabled = false;
        }

        mainCamera = Camera.main;
    }

    bool DoAction(GameObject targetCardGameObject)
    {
        var duelFieldData = DuelField.INSTANCE.DUELFIELDDATA;

        bool actionDone = false;

        if (targetCardGameObject == null)
            return actionDone;

        if (!DuelField.INSTANCE.isViewMode && duelFieldData.currentPlayerTurn == PlayerInfo.INSTANCE.PlayerID)
        {
            switch ((Lib.GameZone)Enum.Parse(typeof(Lib.GameZone), targetCardGameObject.name)  )
            {
                case Lib.GameZone.BackStage1:
                case Lib.GameZone.BackStage2:
                case Lib.GameZone.BackStage3:
                case Lib.GameZone.BackStage4:
                case Lib.GameZone.BackStage5:
                    switch (duelFieldData.currentGamePhase)
                    {
                        case DuelFieldData.GAMEPHASE.MainStep:

                            if (DuelField.INSTANCE.hasAlreadyCollabed) { break; }

                            if (this.GetComponent<Card>().suspended == true) { break; }

                            if (_DuelField.GetZone(Lib.GameZone.Collaboration, TargetPlayer.Player).GetComponentInChildren<Card>() == null)
                            {
                                //if (_DuelField.GetZone("HoloP", TargetPlayer.Player).transform.childCount == 0)
                                //    return;

                                DuelAction duelAction = new()
                                {
                                    playerID = PlayerInfo.INSTANCE.PlayerID,
                                    usedCard = this.GetComponent<Card>().ToCardData(),
                                    activationZone = Lib.GameZone.Collaboration,
                                };
                                _DuelField.GenericActionCallBack(duelAction, "DoCollab");
                                DuelField.INSTANCE.hasAlreadyCollabed = true;
                            }
                            actionDone = true;
                            break;
                        case DuelFieldData.GAMEPHASE.ResetStepReSetStage:
                            if ((_DuelField.GetZone(Lib.GameZone.Stage, TargetPlayer.Player).transform.GetComponentsInChildren<Card>().Count() > 0))
                                return false;

                            DuelAction duelActionn = new()
                            {
                                playerID = PlayerInfo.INSTANCE.PlayerID,
                                usedCard = this.GetComponent<Card>().ToCardData(),
                                activationZone = Lib.GameZone.Stage,
                            };
                            _DuelField.GenericActionCallBack(duelActionn, "ReSetCardAtStage");
                            actionDone = true;
                            break;
                    }
                    break;
            }
        } else
        {
            //if in the clicked location theres no card number, return since must be a facedown card
            if (string.IsNullOrEmpty(GetComponentInChildren<Card>().cardNumber))
                return actionDone;

            _DuelfField_CardDetailViewer ??= FindAnyObjectByType<DuelfField_CardDetailViewer>(FindObjectsInactive.Include);

            List<Card> Cards = this.transform.parent.GetComponentsInChildren<Card>(true).ToList();
            bool targetIsHand = (transform.parent.name.Equals("PlayerHand") || transform.parent.name.Equals("OponentHand"));

            if (targetIsHand)
                Cards = Cards.ToArray().Reverse().ToList(); //kkkkkkkkkkkkkkkkkkkk segue o jogo

            _DuelfField_CardDetailViewer.SetCardListToBeDisplayed(ref Cards, DuelField.INSTANCE.isViewMode, GetComponent<Card>());
        }
        return actionDone;
    }

    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (DuelField_UI_MAP.INSTANCE.SS_LosePanel.gameObject.activeInHierarchy || DuelField_UI_MAP.INSTANCE.SS_WinPanel.gameObject.activeInHierarchy || DuelField_UI_MAP.INSTANCE.SS_LogPanel.gameObject.activeInHierarchy
            || DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_ActivateEffectPanel.gameObject.activeInHierarchy || DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel.gameObject.activeInHierarchy
            || DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_SelectionDetachEnergyPanel.gameObject.activeInHierarchy || DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_SelectionPanel.gameObject.activeInHierarchy)
            return;

        Ray ray = mainCamera.ScreenPointToRay(eventData.position);
        float radius = 0.5f;
        RaycastHit[] hits = Physics.SphereCastAll(ray, radius);

        foreach (var h in hits)
        {
            var targetZone = DuelField.INSTANCE.GetGameZones().Contains(h.transform.gameObject) ? h.transform.gameObject : null;

            if (targetZone == null)
            {
                var parent = h.transform?.parent?.gameObject;
                targetZone = (parent != null && DuelField.INSTANCE.GetGameZones().Contains(parent)) ? parent : null;
            }

            if (targetZone != null)
            {
                DoAction(targetZone);
            }
        }
    }
}
