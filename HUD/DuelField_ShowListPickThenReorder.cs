using Assets.Scripts.Lib;
using Newtonsoft.Json;
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
    public List<GameObject> selectedItems = new ();
    public List<Card> selectedItemsReturn = new();
    public List<int> selectedItemsReturnOrder = new();
    private List<GameObject> notSelectedItems = new();
    private List<Card> SelectableItemsCard = new(); 
    public List<int> selectedItemsPos = new List<int>();
    public List<int> notSelectedItemsPos = new List<int>();
    private int clickOrder = 1;
    private int clickObjects = 1;
    private bool doubleSelect = false;
    private string callback = "";
    private DuelField _DuelField;
    List<string> returnList = new();
    int clickcounter = 0;
    int mustclick = 0;
    int limit = -1;
    private string resolvingCardNumber;
    private bool responseComplete;
    DuelAction _DuelAction;

    public void SetupSelectableItems(DuelAction DuelAction, List<Card> SelectableCards, string _callBack, List<Card> avaliableForSelect, bool doubleselect = false, int minimumToSelect = -1, string resolvingCardNumber = "", bool responseComplete = false)
    {
        this._DuelAction = DuelAction;

        this.limit = minimumToSelect;
        callback = _callBack;
        if (doubleselect == true)
            doubleSelect = true;
        else if (minimumToSelect != -1)
            mustclick = minimumToSelect;
        else
            mustclick = SelectableCards.Count;

        this.resolvingCardNumber = resolvingCardNumber;
        this.responseComplete = responseComplete;


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

            foreach (Card avalibleCard in avaliableForSelect) {
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

        contentPanel.transform.parent.parent.parent.gameObject.SetActive(true);
    }

    void OnItemClick(GameObject itemObject, int itemName, bool canSelect)
    {
        if (canSelect == false)
            return;

        if (clickcounter >= limit)
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
        _DuelField = GameObject.FindAnyObjectByType<DuelField>();
        confirmButton.onClick.AddListener(FinishSelection);
    }
    void FinishSelection()
    {
        if (doubleSelect) {

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

            foreach (GameObject gms in selectedItems) {
                returnList.Add(gms.GetComponent<Card>().cardNumber);
            }

            selectedItems = new();
            SetupSelectableItems(_DuelAction, SecondSelectableItemsCard, callback, SecondSelectableItemsCard, false, -1, resolvingCardNumber, true);
            return;
        }


        if (clickcounter < mustclick)
            return;

        ///////////////////////////////////////////////////////////////////////
        if (!doubleSelect)
            foreach (GameObject gms in selectedItems)
                returnList.Add(gms.GetComponent<Card>().cardNumber);

        //FindAnyObjectByType<EffectController>().EffectInformation.Add(returnList);
        //FindAnyObjectByType<EffectController>().EffectInformation.Add(selectedItemsPos);

        foreach (GameObject gm in SelectableItems)
        {
            Destroy(gm);
            clickOrder = 1;
        }

        FindAnyObjectByType<EffectController>().duelActionOutput = _DuelAction;

        selectedItems = new();
        SelectableItems = new();
        selectedItemsPos = new();
        contentPanel.transform.parent.parent.parent.gameObject.SetActive(false);

        FindAnyObjectByType<EffectController>();
    }
}
