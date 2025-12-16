using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DuelField_YesOrNoMenu : MonoBehaviour
{
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private Transform CardListContent;

    public IEnumerator ShowYesOrNoMenu(string text = "")
    {
        EffectController.INSTANCE.isSelectionCompleted = false;

        CardListContent.gameObject.SetActive(true);
        if (!string.IsNullOrEmpty(text))
            CardListContent.GetComponentInChildren<TMP_Text>().text = text;

        yield return new WaitUntil(() => EffectController.INSTANCE.isSelectionCompleted);
        EffectController.INSTANCE.isSelectionCompleted = false;
    }
    public void Start()
    {
        yesButton.onClick.AddListener(YesButton);
        noButton.onClick.AddListener(NoButton);
    }
    void FinishSelection()
    {
        CardListContent.gameObject.SetActive(false);
        EffectController.INSTANCE.isSelectionCompleted = true;
    }

    public void YesButton() 
    {
        EffectController.INSTANCE.CurrentContext.Register(new DuelAction { yesOrNo = true });
        FinishSelection();
    }
    public void NoButton()
    {
        EffectController.INSTANCE.CurrentContext.Register( new DuelAction { yesOrNo = false }  );
        FinishSelection();
    }

}
