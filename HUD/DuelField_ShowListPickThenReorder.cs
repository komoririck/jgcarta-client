using Assets.Scripts.Lib;
using JetBrains.Annotations;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DuelField_ShowListPickThenReorder : MonoBehaviour
{
    [SerializeField] private Button confirmButton;
    [SerializeField] private Transform contentPanel;
    [SerializeField] private GameObject itemPrefab;
    private List<GameObject> SelectableItems = new();
    public List<GameObject> selectedItems = new();
    public List<Card> selectedItemsReturn = new();
    public List<int> selectedItemsReturnOrder = new();
    private List<GameObject> notSelectedItems = new();
    private List<Card> SelectableItemsCard = new();
    public List<int> selectedItemsPos = new List<int>();
    public List<int> notSelectedItemsPos = new List<int>();
    private int clickOrder = 1;
    private int clickObjects = 1;
    private bool doubleSelect = false;
    private DuelField _DuelField;
    List<string> returnList = new();
    int clickcounter = 0;
    int mustclick = 0;
    int MaximumCanPick = -1;
    DuelAction _DuelAction;

    private EffectController effectController;

    public IEnumerator SetupSelectableItems(DuelAction DuelAction, List<Card> SelectableCards, List<Card> avaliableForSelect, bool doubleselect = false, int MaximumCanPick = -1)
    {


        contentPanel.transform.parent.parent.parent.gameObject.SetActive(true);
        FillMenu(DuelAction, SelectableCards, avaliableForSelect, doubleselect, MaximumCanPick);
        yield return new WaitUntil(() => effectController.isSelectionCompleted);
        effectController.isSelectionCompleted = false;
    }
    public void FillMenu(DuelAction DuelAction, List<Card> SelectableCards, List<Card> avaliableForSelect, bool doubleselect = false, int MaximumCanPick = -1)
    {
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(FinishSelection);
        effectController.isSelectionCompleted = false;
        this._DuelAction = DuelAction;
        this.MaximumCanPick = MaximumCanPick;
        this.doubleSelect = doubleselect;

        if (this.MaximumCanPick == -1)
        {
            mustclick = SelectableCards.Count;
        }
        else
        {
            mustclick = this.MaximumCanPick;
        }

        int x = 0;  // Variable to track order
        foreach (Card item in SelectableCards)
        {
            bool canSelect = false;
            GameObject newItem = Instantiate(itemPrefab, contentPanel);
            newItem.name = clickObjects.ToString();
            Card newC = newItem.GetComponent<Card>();
            newC.cardNumber = item.cardNumber;
            newC.GetCardInfo();
            SelectableItems.Add(newItem);

            foreach (Card avalibleCard in avaliableForSelect)
            {
                if (avalibleCard.cardNumber.Equals(newC.cardNumber))
                    canSelect = true;
            }

            // Clear or set the TMP text as needed
            TMP_Text itemText = newItem.GetComponentInChildren<TMP_Text>();
            itemText.text = "";

            int subclickObjects = clickObjects;

            // Add the click event listener with the local variable
            Button itemButton = newItem.GetComponent<Button>();
            itemButton.onClick.AddListener(() => OnItemClick(newItem, subclickObjects, canSelect));
            clickObjects++;
            x++;  // Increment x for the next item
        }

    }

    void OnItemClick(GameObject itemObject, int itemName, bool canSelect)
    {
        if (canSelect == false)
            return;

        GameObject obj = itemObject;
        if (!selectedItems.Contains(obj))
        {
            selectedItemsPos.Add(clickOrder);
            selectedItems.Add(obj);

            // Update the selection order text
            TMP_Text orderText = itemObject.transform.Find("OrderText").GetComponent<TMP_Text>();
            orderText.text = clickOrder.ToString();
            orderText.gameObject.SetActive(true);

            // Increment click order

            clickOrder++;
            clickcounter++;
        }
    }

    public void Start()
    {
        effectController = FindAnyObjectByType<EffectController>();
        _DuelField = GameObject.FindAnyObjectByType<DuelField>();
    }
    void FinishSelection()
    {
        if (doubleSelect)
        {

            for (int i = 0; i < selectedItemsPos.Count; i++)
            {
                selectedItemsPos[i] = 0;
            }
            foreach (GameObject item in SelectableItems)
            {
                if (!selectedItems.Contains(item))
                    notSelectedItems.Add(item);
            }
            List<Card> SecondSelectableItemsCard = new();
            foreach (GameObject item in notSelectedItems)
            {
                SecondSelectableItemsCard.Add(item.GetComponent<Card>());
            }
            foreach (GameObject gm in SelectableItems)
            {
                Destroy(gm);
            }
            doubleSelect = false;

            foreach (GameObject gms in selectedItems)
            {
                returnList.Add(gms.GetComponent<Card>().cardNumber);
            }

            selectedItems = new();
            FillMenu(_DuelAction, SecondSelectableItemsCard, SecondSelectableItemsCard, false, SecondSelectableItemsCard.Count);
        }
        else
        {


            if (clickcounter < mustclick)
                return;

            ///////////////////////////////////////////////////////////////////////
            if (!doubleSelect)
                foreach (GameObject gms in selectedItems)
                    returnList.Add(gms.GetComponent<Card>().cardNumber);

            _DuelAction.cardList = null;


            _DuelAction.SelectedCards = returnList;
            _DuelAction.Order = selectedItemsPos;

            effectController.EffectInformation.Add(_DuelAction);

            foreach (GameObject gm in SelectableItems)
            {
                Destroy(gm);
                clickOrder = 1;
            }

            selectedItems = new();
            SelectableItems = new();
            selectedItemsPos = new();
            contentPanel.transform.parent.parent.parent.gameObject.SetActive(false);

            effectController.isSelectionCompleted = true;
        }
    }
}
