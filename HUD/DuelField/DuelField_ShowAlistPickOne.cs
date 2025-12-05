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
    DuelAction _DuelAction;
    private EffectController effectController;

    public IEnumerator SetupSelectableItems(DuelAction da, List<CardData> SelectableCards, List<CardData> avaliableForSelect)
    {

        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(FinishSelection);
        selectedItems.Clear();
        effectController.isSelectionCompleted = false;

        this._DuelAction = da;

        // Clean up previous items before loading the panel

            foreach (GameObject gm in SelectableItems)
            {
                if (gm != null)
                {
                    Destroy(gm); 
                }
            }
            SelectableItems.Clear();  
        


        foreach (CardData item in SelectableCards)
        {
            bool canSelect = false;
            GameObject newItem = Instantiate(itemPrefab, contentPanel);
            newItem.name = InstantiatedObjIndex.ToString();
            Card newC = newItem.GetComponent<Card>().Init(item);

            SelectableItems.Add(newItem);  

            // Check if this card is selectable
            foreach (CardData availableCard in avaliableForSelect)
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

        yield return new WaitUntil(() => effectController.isSelectionCompleted);
        effectController.isSelectionCompleted = false;
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

    public void Start()
    {
        effectController = FindAnyObjectByType<EffectController>();
    }

    void FinishSelection()
    {
        if (ClickedCounter < MustClickCounter)
            return;

        // Prepare information to send to the server
        effectController.EffectInformation.Add(new DuelAction {cardList = selectedItems });

        // Clear selection list
        InstantiatedObjIndex = 1;
        MustClickCounter = 1;
        MaxClickCounter = 1;
        ClickedCounter = 0;
        _DuelAction = null;

        // Hide panel
        contentPanel.transform.parent.parent.parent.gameObject.SetActive(false);

        effectController.isSelectionCompleted = true;
    }

    private void OnDisable()
    {
    }

    private void OnEnable()
    {
    }

}
