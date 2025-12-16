using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DuelField_ShowAlistPickOne : MonoBehaviour
{
    [SerializeField] private Button confirmButton;
    [SerializeField] private Transform contentPanel;
    [SerializeField] private GameObject itemPrefab;
    private List<GameObject> SelectableItems = new();
    public List<CardData> selectedItems = new ();
    private int InstantiatedObjIndex = 1;
    int MustClickCounter = 1;
    int MaxClickCounter = 1;
    int ClickedCounter = 0;

    public IEnumerator SetupSelectableItems(List<Card> SelectableCards, List<Card> avaliableForSelect = null)
    {
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(FinishSelection);
        selectedItems.Clear();
        EffectController.INSTANCE.isSelectionCompleted = false;

        avaliableForSelect ??= SelectableCards;

        foreach (GameObject gm in SelectableItems)
        {
            if (gm != null)
            {
                Destroy(gm); 
            }
        }
        SelectableItems.Clear();  
        
        foreach (Card item in SelectableCards)
        {
            bool canSelect = false;
            GameObject newItem = Instantiate(itemPrefab, contentPanel);
            newItem.name = InstantiatedObjIndex.ToString();
            Card newC = newItem.GetComponent<Card>().Init(item.ToCardData());

            SelectableItems.Add(newItem);  

            foreach (Card availableCard in avaliableForSelect)
            {
                if (availableCard.cardNumber.Equals(newC.cardNumber))
                    canSelect = true;
            }

            TMP_Text itemText = newItem.GetComponentInChildren<TMP_Text>();
            itemText.text = "";

            Button itemButton = newItem.GetComponent<Button>();
            itemButton.onClick.AddListener(() => OnItemClick(newItem, canSelect));
            InstantiatedObjIndex++;
        }

        contentPanel.transform.parent.parent.parent.gameObject.SetActive(true);

        yield return new WaitUntil(() => EffectController.INSTANCE.isSelectionCompleted);
        EffectController.INSTANCE.isSelectionCompleted = false;
    }

    void OnItemClick(GameObject itemObject, bool canSelect)
    {
        if (!canSelect || itemObject == null)
            return;

        if (ClickedCounter >= MaxClickCounter)
            return;

        selectedItems.Add(itemObject.GetComponent<Card>().ToCardData());

        TMP_Text orderText = itemObject.transform.Find("OrderText").GetComponent<TMP_Text>();
        orderText.text = (ClickedCounter + 1).ToString();  
        orderText.gameObject.SetActive(true);

        ClickedCounter++;
    }
    void FinishSelection()
    {
        if (ClickedCounter < MustClickCounter)
            return;

        // Clear selection list
        InstantiatedObjIndex = 1;
        MustClickCounter = 1;
        MaxClickCounter = 1;
        ClickedCounter = 0;

        contentPanel.transform.parent.parent.parent.gameObject.SetActive(false);
        EffectController.INSTANCE.CurrentContext.Register(new DuelAction {cardList = selectedItems });
        EffectController.INSTANCE.isSelectionCompleted = true;
    }
}
