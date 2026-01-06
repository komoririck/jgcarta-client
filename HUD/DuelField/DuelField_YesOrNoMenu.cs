using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DuelField_YesOrNoMenu : MonoBehaviour
{
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private Transform CardListContent;
    static DuelAction _DaToReturn;


    public static DuelField_YesOrNoMenu INSTANCE;
    private void Awake()
    {
        INSTANCE = this;
    }
    public IEnumerator ShowYesOrNoMenu(DuelAction DaToReturn, string text = "")
    {
        _DaToReturn = DaToReturn;
        DuelField.INSTANCE.isSelectionCompleted = false;

        DuelField_UI_MAP.INSTANCE.SaveAllPanelStatus().DisableAllOther().SetPanel(true, DuelField_UI_MAP.PanelType.SS_EffectBoxes_ActivateEffectPanel);

        if (!string.IsNullOrEmpty(text))
            CardListContent.GetComponentInChildren<TMP_Text>().text = text;

        yield return new WaitUntil(() => DuelField.INSTANCE.isSelectionCompleted);
        DuelField.INSTANCE.isSelectionCompleted = false;
    }
    public void Start()
    {
        yesButton.onClick.AddListener(YesButton);
        noButton.onClick.AddListener(NoButton);
    }
    void FinishSelection()
    {
        DuelField_UI_MAP.INSTANCE.LoadAllPanelStatus().SetPanel(true, DuelField_UI_MAP.PanelType.SS_UI_General);
        DuelField.INSTANCE.isSelectionCompleted = true;
    }

    public void YesButton()
    {
        _DaToReturn ??= new();
        _DaToReturn.yesOrNo = true;
        FinishSelection();
    }
    public void NoButton()
    {
        _DaToReturn ??= new();
        _DaToReturn.yesOrNo = false;
        FinishSelection();
    }

    public static DuelAction GetDA()
    {
        return _DaToReturn;
    }
}
