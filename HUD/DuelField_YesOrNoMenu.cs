using Assets.Scripts.Lib;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static DuelField;
using static UnityEngine.GraphicsBuffer;

public class DuelField_YesOrNoMenu : MonoBehaviour
{
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private Transform CardListContent;
    private EffectController effectController;

    public IEnumerator ShowYesOrNoMenu(string text = "")
    {
        effectController.isSelectionCompleted = false;

        CardListContent.gameObject.SetActive(true);
        if (!string.IsNullOrEmpty(text))
            CardListContent.GetComponentInChildren<TMP_Text>().text = text;

        yield return new WaitUntil(() => effectController.isSelectionCompleted);
        effectController.isSelectionCompleted = false;
    }

    public void Start()
    {
        yesButton.onClick.AddListener(YesButton);
        noButton.onClick.AddListener(NoButton);
        effectController = FindAnyObjectByType<EffectController>();
    }
    void FinishSelection()
    {
        CardListContent.gameObject.SetActive(false);
        effectController.isSelectionCompleted = true;
    }

    public void YesButton() 
    {
        effectController.EffectInformation.Add("Yes");
        FinishSelection();
    }
    public void NoButton()
    {
        effectController.EffectInformation.Add("No");
        FinishSelection();
    }

}
