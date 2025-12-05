using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class DuelField_ShowANumberList : MonoBehaviour
{
    [SerializeField] private Transform NumberPanel;
    private EffectController effectController;

    public TMP_Dropdown dropdown;

    public IEnumerator SetupSelectableNumbers(int min, int max)
    {
        effectController.isSelectionCompleted = false;
        dropdown.ClearOptions();

        List<string> options = new List<string>();
        for (int i = min; i <= max; i++)
        {
            options.Add(i.ToString());
        }
        dropdown.AddOptions(options);

        NumberPanel.gameObject.SetActive(true);

        yield return new WaitUntil(() => effectController.isSelectionCompleted);
        effectController.isSelectionCompleted = false;
    }

    public void Start()
    {
        effectController = FindAnyObjectByType<EffectController>();
    }
    public void FinishSelection()
    {
        NumberPanel.gameObject.SetActive(false);
        effectController.EffectInformation.Add(new DuelAction{ yesOrNo = dropdown.options[dropdown.value].text.Equals("Yes") ? true: false });
        effectController.isSelectionCompleted = true;
    }

}
