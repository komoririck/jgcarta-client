using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DuelField_ShowANumberList : MonoBehaviour
{
    [SerializeField] private Transform NumberPanel;
    public TMP_Dropdown dropdown;
    static DuelAction _DaToReturn;


    public static DuelField_ShowANumberList INSTANCE;
    private void Awake()
    {
        INSTANCE = this;
    }
    public IEnumerator SetupSelectableItems(int min, int max, DuelAction DaToReturn)
    {
        _DaToReturn = DaToReturn;
        EffectController.INSTANCE.isSelectionCompleted = false;
        dropdown.ClearOptions();

        List<string> options = new List<string>();
        for (int i = min; i <= max; i++)
        {
            options.Add(i.ToString());
        }
        dropdown.AddOptions(options);

        DuelField_UI_MAP.INSTANCE.SaveAllPanelStatus().DisableAllOther().SetPanel(true, DuelField_UI_MAP.PanelType.SS_EffectBoxes_NumberListPanell);

        yield return new WaitUntil(() => EffectController.INSTANCE.isSelectionCompleted);
        EffectController.INSTANCE.isSelectionCompleted = false;
    }
    public void FinishSelection()
    {
        _DaToReturn ??= new();
        _DaToReturn.yesOrNo = dropdown.options[dropdown.value].text.Equals("Yes");

        DuelField_UI_MAP.INSTANCE.LoadAllPanelStatus().SetPanel(true, DuelField_UI_MAP.PanelType.SS_UI_General);
        EffectController.INSTANCE.isSelectionCompleted = true;
    }
    public static DuelAction GetDA()
    {
        return _DaToReturn;
    }
}
