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
    public void SetPanelActiveStatus(bool status, List<PanelType> panelType) 
    {
        foreach (PanelType type in panelType) {
            SetPanel(status, type);
        } 
    }
    public void SetPanel(bool status, PanelType panelType)
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
            case PanelType.SS_EffectBoxes_SelectionPanel:
                SS_EffectBoxes_SelectionPanel.SetActive(status);
                SS_EffectBoxes_General.SetActive(status);
                SetPanel(status, PanelType.SS_BlockView);
                SetPanel(!status, PanelType.SS_UI_General);
                break;
            case PanelType.SS_EffectBoxes_SelectionDetachEnergyPanel:
                SS_EffectBoxes_SelectionDetachEnergyPanel.SetActive(status);
                SS_EffectBoxes_General.SetActive(status);
                SetPanel(status, PanelType.SS_BlockView);
                SetPanel(!status, PanelType.SS_UI_General);
                break;
            case PanelType.SS_EffectBoxes_CardPanel:
                SS_EffectBoxes_CardPanel.SetActive(status);
                SS_EffectBoxes_General.SetActive(status);
                SetPanel(status, PanelType.SS_BlockView);
                SetPanel(!status, PanelType.SS_UI_General);
                break;
            case PanelType.SS_EffectBoxes_ActivateEffectPanel:
                SS_EffectBoxes_ActivateEffectPanel.SetActive(status);
                SS_EffectBoxes_General.SetActive(status);
                SetPanel(status, PanelType.SS_BlockView);
                SetPanel(!status, PanelType.SS_UI_General);
                break;
            case PanelType.SS_EffectBoxes_NumberListPanell:
                SS_EffectBoxes_NumberListPanell.SetActive(status);
                SS_EffectBoxes_General.SetActive(status);
                SetPanel(status, PanelType.SS_BlockView);
                SetPanel(!status, PanelType.SS_UI_General);
                break;
            case PanelType.WS_Oponent_General:
                WS_Oponent_General.SetActive(status);
                SS_EffectBoxes_General.SetActive(status);
                SetPanel(status, PanelType.SS_BlockView);
                SetPanel(!status, PanelType.SS_UI_General);
                break;
            case PanelType.WS_Player_General:
                WS_Player_General.SetActive(status);
                SS_EffectBoxes_General.SetActive(status);
                SetPanel(status, PanelType.SS_BlockView);
                SetPanel(!status, PanelType.SS_UI_General);
                break;
            case PanelType.WS_ReadyButton:
                WS_ReadyButton.SetActive(status);
                SS_EffectBoxes_General.SetActive(status);
                SetPanel(status, PanelType.SS_BlockView);
                SetPanel(!status, PanelType.SS_UI_General);
                break;
            case PanelType.WS_PassTurnButton:
                WS_PassTurnButton.SetActive(status);
                SS_EffectBoxes_General.SetActive(status);
                SetPanel(status, PanelType.SS_BlockView);
                SetPanel(!status, PanelType.SS_UI_General);
                break;
            default:
                Debug.LogWarning("Unhandled PanelType: " + panelType);
                break;
        }
    }
}
