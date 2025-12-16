using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DuelField_ShowANumberList : MonoBehaviour
{
    [SerializeField] private Transform NumberPanel;
    public TMP_Dropdown dropdown;

    public IEnumerator SetupSelectableNumbers(int min, int max)
    {
        EffectController.INSTANCE.isSelectionCompleted = false;
        dropdown.ClearOptions();

        List<string> options = new List<string>();
        for (int i = min; i <= max; i++)
        {
            options.Add(i.ToString());
        }
        dropdown.AddOptions(options);

        NumberPanel.gameObject.SetActive(true);

        yield return new WaitUntil(() => EffectController.INSTANCE.isSelectionCompleted);
        EffectController.INSTANCE.isSelectionCompleted = false;
    }
    public void FinishSelection()
    {
        NumberPanel.gameObject.SetActive(false);
        EffectController.INSTANCE.CurrentContext.Register(new DuelAction{ yesOrNo = dropdown.options[dropdown.value].text.Equals("Yes") ? true: false });
        EffectController.INSTANCE.isSelectionCompleted = true;
    }
}
