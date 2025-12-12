using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static DuelField_UI_MAP;

public class DuelField_UI_MAP : MonoBehaviour
{
    public static DuelField_UI_MAP INSTANCE;

    [Space]
     public GameObject LogContent = null;
     public GameObject LogPrefab = null;
     public GameObject LogBar = null;

     private Dictionary<PanelType, bool> SavedStatus = new();

    private void Start()
    {
        INSTANCE = this;
    }

    public enum PanelType {
        SS_LosePanel,
        SS_WinPanel,
        SS_LogPanel,
        SS_OponentHand,
        SS_PlayerHand,
        SS_BlockView,
        SS_UI_General,
        SS_UI_TurnNumber,
        SS_UI_TurnText,
        SS_UI_Timmer,
        SS_UI_ReturnButton,
        SS_UI_DuelPhaseText,
        SS_UI_ActionToggleButton,
        SS_UI_LogToggleButton,
        SS_MulliganPanel,
        SS_EffectBoxes_General,
        SS_EffectBoxes_SelectionPanel,
        SS_EffectBoxes_SelectionDetachEnergyPanel,
        SS_EffectBoxes_CardPanel,
        SS_EffectBoxes_ActivateEffectPanel,
        SS_EffectBoxes_NumberListPanell,
        WS_Oponent_General,
        WS_Player_General,
        WS_ReadyButton,
        WS_PassTurnButton
    }

    public 
    GameObject
        SS_LosePanel,
        SS_WinPanel,
        SS_LogPanel,
        SS_OponentHand,
        SS_PlayerHand,
        SS_BlockView,
        SS_UI_General,
        SS_UI_TurnNumber,
        SS_UI_TurnText,
        SS_UI_Timmer,
        SS_UI_ReturnButton,
        SS_UI_DuelPhaseText,
        SS_UI_ActionToggleButton,
        SS_UI_LogToggleButton,
        SS_MulliganPanel,
        SS_MulliganPanelYes,
        SS_MulliganPanelNo,
        SS_EffectBoxes_General,
        SS_EffectBoxes_SelectionPanel,
        SS_EffectBoxes_SelectionDetachEnergyPanel,
        SS_EffectBoxes_CardPanel,
        SS_EffectBoxes_ActivateEffectPanel,
        SS_EffectBoxes_ActivateEffectPanelYES,
        SS_EffectBoxes_ActivateEffectPanelNO,
        SS_EffectBoxes_NumberListPanell,
        WS_Oponent_General,
        WS_Player_General,
        WS_ReadyButton,
        WS_PassTurnButton;

    public void SetHandActiveStatus(bool status) {
        SetPanelActiveStatus(status, new List<PanelType> {PanelType.SS_OponentHand,PanelType.SS_PlayerHand});
    }
    public void SetPanelActiveStatus(bool status, List<PanelType> panelTypeList) 
    {
        foreach (PanelType type in panelTypeList) {
            SetPanel(status, type);
        } 
    }
    public DuelField_UI_MAP SetPanel(bool status, PanelType panelType)
    {
        switch (panelType)
        {
            case PanelType.SS_LosePanel:
                SS_LosePanel.SetActive(status);
                SetPanel(status, PanelType.SS_BlockView);
                break;
            case PanelType.SS_WinPanel:
                SS_WinPanel.SetActive(status);
                SetPanel(status, PanelType.SS_BlockView);
                break;
            case PanelType.SS_LogPanel:
                SS_LogPanel.SetActive(status);
                SetPanel(status, PanelType.SS_BlockView);
                break;
            case PanelType.SS_OponentHand:
                SS_OponentHand.SetActive(status);
                break;
            case PanelType.SS_PlayerHand:
                SS_PlayerHand.SetActive(status);
                break;
            case PanelType.SS_BlockView:
                SS_BlockView.SetActive(status);
                break;
            case PanelType.SS_UI_General:
                SS_UI_General.SetActive(status);
                break;
            case PanelType.SS_UI_TurnNumber:
                SS_UI_TurnNumber.SetActive(status);
                break;
            case PanelType.SS_UI_TurnText:
                SS_UI_TurnText.SetActive(status);
                break;
            case PanelType.SS_UI_Timmer:
                SS_UI_Timmer.SetActive(status);
                break;
            case PanelType.SS_UI_ReturnButton:
                SS_UI_ReturnButton.SetActive(status);
                break;
            case PanelType.SS_UI_DuelPhaseText:
                SS_UI_DuelPhaseText.SetActive(status);
                break;
            case PanelType.SS_UI_ActionToggleButton:
                SS_UI_ActionToggleButton.SetActive(status);
                break;
            case PanelType.SS_UI_LogToggleButton:
                SS_UI_LogToggleButton.SetActive(status);
                break;
            case PanelType.SS_MulliganPanel:
                SS_MulliganPanel.SetActive(status);
                SetPanel(status, PanelType.SS_BlockView);
                break;
            case PanelType.SS_EffectBoxes_General:
                break;
            default:
                if (status)
                    SetPanel(false, PanelType.SS_BlockView);
                else
                    SetPanel(true, PanelType.SS_BlockView);

                switch (panelType)
                    {
                        case PanelType.SS_EffectBoxes_SelectionPanel:
                            SS_EffectBoxes_SelectionPanel.SetActive(status);
                            SS_EffectBoxes_General.SetActive(status);
                            SetPanel(!status, PanelType.SS_UI_General);
                            break;
                        case PanelType.SS_EffectBoxes_SelectionDetachEnergyPanel:
                            SS_EffectBoxes_SelectionDetachEnergyPanel.SetActive(status);
                            SS_EffectBoxes_General.SetActive(status);
                            SetPanel(!status, PanelType.SS_UI_General);
                            break;
                        case PanelType.SS_EffectBoxes_CardPanel:
                            SS_EffectBoxes_CardPanel.SetActive(status);
                            SS_EffectBoxes_General.SetActive(status);
                            SetPanel(!status, PanelType.SS_UI_General);
                            break;
                        case PanelType.SS_EffectBoxes_ActivateEffectPanel:
                            SS_EffectBoxes_ActivateEffectPanel.SetActive(status);
                            SS_EffectBoxes_General.SetActive(status);
                        SS_EffectBoxes_ActivateEffectPanelYES.SetActive(status);
                        SS_EffectBoxes_ActivateEffectPanelNO.SetActive(status);
                        SetPanel(!status, PanelType.SS_UI_General);
                            break;
                        case PanelType.SS_EffectBoxes_NumberListPanell:
                            SS_EffectBoxes_NumberListPanell.SetActive(status);
                            SS_EffectBoxes_General.SetActive(status);
                            SetPanel(!status, PanelType.SS_UI_General);
                            break;
                        case PanelType.WS_Oponent_General:
                            WS_Oponent_General.SetActive(status);
                            SS_EffectBoxes_General.SetActive(status);
                            SetPanel(!status, PanelType.SS_UI_General);
                            break;
                        case PanelType.WS_Player_General:
                            WS_Player_General.SetActive(status);
                            SS_EffectBoxes_General.SetActive(status);
                            SetPanel(!status, PanelType.SS_UI_General);
                            break;
                        case PanelType.WS_ReadyButton:
                            WS_ReadyButton.SetActive(status);
                            SS_EffectBoxes_General.SetActive(status);
                            SetPanel(!status, PanelType.SS_UI_General);
                            break;
                        case PanelType.WS_PassTurnButton:
                            WS_PassTurnButton.SetActive(status);
                            SS_EffectBoxes_General.SetActive(status);
                            SetPanel(!status, PanelType.SS_UI_General);
                            break;
                    }
                break;
        }
        return this;
    }
    public GameObject GetPanelObject(PanelType panelType)
    {
        switch (panelType)
        {
            case PanelType.SS_LosePanel:
                return SS_LosePanel;
            case PanelType.SS_WinPanel:
                return SS_WinPanel;
            case PanelType.SS_LogPanel:
                return SS_LogPanel;
            case PanelType.SS_OponentHand:
                return SS_OponentHand;
            case PanelType.SS_PlayerHand:
                return SS_PlayerHand;
            case PanelType.SS_BlockView:
                return SS_BlockView;
            case PanelType.SS_UI_General:
                return SS_UI_General;
            case PanelType.SS_UI_TurnNumber:
                return SS_UI_TurnNumber;
            case PanelType.SS_UI_TurnText:
                return SS_UI_TurnText;
            case PanelType.SS_UI_Timmer:
                return SS_UI_Timmer;
            case PanelType.SS_UI_ReturnButton:
                return SS_UI_ReturnButton;
            case PanelType.SS_UI_DuelPhaseText:
                return SS_UI_DuelPhaseText;
            case PanelType.SS_UI_ActionToggleButton:
                return SS_UI_ActionToggleButton;
            case PanelType.SS_UI_LogToggleButton:
                return SS_UI_LogToggleButton;
            case PanelType.SS_MulliganPanel:
                return SS_MulliganPanel;

            case PanelType.SS_EffectBoxes_General:
                return SS_EffectBoxes_General;
            case PanelType.SS_EffectBoxes_SelectionPanel:
                return SS_EffectBoxes_SelectionPanel;
            case PanelType.SS_EffectBoxes_SelectionDetachEnergyPanel:
                return SS_EffectBoxes_SelectionDetachEnergyPanel;
            case PanelType.SS_EffectBoxes_CardPanel:
                return SS_EffectBoxes_CardPanel;
            case PanelType.SS_EffectBoxes_ActivateEffectPanel:
                return SS_EffectBoxes_ActivateEffectPanel;
            case PanelType.SS_EffectBoxes_NumberListPanell:
                return SS_EffectBoxes_NumberListPanell;

            case PanelType.WS_Oponent_General:
                return WS_Oponent_General;
            case PanelType.WS_Player_General:
                return WS_Player_General;
            case PanelType.WS_ReadyButton:
                return WS_ReadyButton;
            case PanelType.WS_PassTurnButton:
                return WS_PassTurnButton;

            default:
                Debug.LogWarning("Unhandled PanelType in GetPanelObject: " + panelType);
                return null;
        }
    }

    internal DuelField_UI_MAP DisableAllOther()
    {
        SS_LosePanel.SetActive(false);
        SS_WinPanel.SetActive(false);
        SS_LogPanel.SetActive(false);
        SS_OponentHand.SetActive(false);
        SS_PlayerHand.SetActive(false);
        SS_BlockView.SetActive(false);
        SS_UI_General.SetActive(false);
        SS_UI_TurnNumber.SetActive(false);
        SS_UI_TurnText.SetActive(false);
        SS_UI_Timmer.SetActive(false);
        SS_UI_ReturnButton.SetActive(false);
        SS_UI_DuelPhaseText.SetActive(false);
        SS_UI_ActionToggleButton.SetActive(false);
        SS_UI_LogToggleButton.SetActive(false);
        SS_MulliganPanel.SetActive(false);
        SS_EffectBoxes_General.SetActive(false);
        SS_EffectBoxes_SelectionPanel.SetActive(false);
        SS_EffectBoxes_SelectionDetachEnergyPanel.SetActive(false);
        SS_EffectBoxes_CardPanel.SetActive(false);
        SS_EffectBoxes_ActivateEffectPanel.SetActive(false);
        SS_EffectBoxes_ActivateEffectPanelYES.SetActive(false);
        SS_EffectBoxes_ActivateEffectPanelNO.SetActive(false);
        SS_EffectBoxes_NumberListPanell.SetActive(false);
        WS_Oponent_General.SetActive(false);
        WS_Player_General.SetActive(false);
        WS_ReadyButton.SetActive(false);
        WS_PassTurnButton.SetActive(false);
        return this;
    }

    internal DuelField_UI_MAP SaveAllPanelStatus()
    {
        foreach (PanelType s in Enum.GetValues(typeof(PanelType)))
        {
            SavedStatus[s] = GetPanelObject(s).activeSelf;
        }
        return this;
    }
    internal DuelField_UI_MAP LoadAllPanelStatus()
    {
        foreach (KeyValuePair<PanelType, bool> s in SavedStatus)
        {
            SetPanel(s.Value, s.Key);
        }
        return this;
    }
}
