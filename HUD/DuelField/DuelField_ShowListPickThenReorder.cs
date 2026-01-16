using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DuelField_ShowListPickThenReorder : MonoBehaviour
{
    public static DuelField_ShowListPickThenReorder INSTANCE;

    [SerializeField] private Button confirmButton;
    [SerializeField] private Transform contentPanel;
    [SerializeField] private GameObject itemPrefab;
    private List<GameObject> SelectableItems = new();
    public List<GameObject> selectedItems = new();
    public List<Card> selectedItemsReturn = new();
    public List<int> selectedItemsReturnOrder = new();
    private List<GameObject> notSelectedItems = new();
    public List<int> selectedItemsPos = new List<int>();
    public List<int> notSelectedItemsPos = new List<int>();
    private int clickOrder = 1;
    private int clickObjects = 1;
    private bool doubleSelect = false;
    List<CardData> returnList = new();
    int clickcounter = 0;
    int _MinimunCanPick = -1;
    int _MaximunCanPick = -1;
    static DuelAction _DaToReturn;

    [SerializeField] private Button closeButton;
    bool _canClosePanel = false;

    void Start()
    {
        INSTANCE = this;

        closeButton = DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_General_PanelCloseButton.GetComponent<Button>();
        closeButton.onClick.AddListener(() => {
            if (_canClosePanel)
            {
                _DaToReturn = new DuelAction();
                DuelField_UI_MAP.INSTANCE.LoadAllPanelStatus().SetPanel(true, DuelField_UI_MAP.PanelType.SS_UI_General);
            }
        });
    }

    public IEnumerator SetupSelectableItems(DuelAction DuelAction, List<Card> SelectableCards, List<Card> avaliableForSelect = null, bool doubleselect = false, int MinimunCanPick = -1, int MaximunCanPick = -1, bool canClosePanel = false)
    {
        _canClosePanel = canClosePanel;

        avaliableForSelect ??= SelectableCards;

        DuelField.INSTANCE.isSelectionCompleted = false;
        DuelField_UI_MAP.INSTANCE.SaveAllPanelStatus().DisableAllOther().SetPanel(true, DuelField_UI_MAP.PanelType.SS_EffectBoxes_SelectionPanel);
        DuelField_UI_MAP.INSTANCE.SetPanel(_canClosePanel, DuelField_UI_MAP.PanelType.SS_EffectBoxes_General_PanelCloseButton);
        FillMenu(DuelAction, SelectableCards, avaliableForSelect, doubleselect, MinimunCanPick, MaximunCanPick);

        yield return new WaitUntil(() => DuelField.INSTANCE.isSelectionCompleted);
        DuelField.INSTANCE.isSelectionCompleted = false;
    }
    public void FillMenu(DuelAction DuelAction, List<Card> SelectableCards, List<Card> avaliableForSelect, bool doubleselect = false, int MinimunCanPick = -1, int MaximunCanPick = -1)
    {
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(FinishSelection);
        DuelField.INSTANCE.isSelectionCompleted = false;
        _DaToReturn = DuelAction;

        this._MinimunCanPick = MinimunCanPick;
        this._MaximunCanPick = MaximunCanPick;
        this.doubleSelect = doubleselect;

        int x = 0;
        foreach (Card item in SelectableCards)
        {
            bool canSelect = false;
            GameObject newItem = Instantiate(itemPrefab, contentPanel);
            newItem.name = clickObjects.ToString();
            Card newC = newItem.GetComponent<Card>().Init(item.ToCardData()) ;

            SelectableItems.Add(newItem);

            foreach (Card avalibleCard in avaliableForSelect)
            {
                if (avalibleCard.cardNumber.Equals(newC.cardNumber)) {
                    newC.Glow(ForceColor: Color.blue, ForceGlow: true);
                    canSelect = true;
                }
            }

            TMP_Text itemText = newItem.GetComponentInChildren<TMP_Text>();
            itemText.text = null;

            int subclickObjects = clickObjects;

            Button itemButton = newItem.GetComponent<Button>();
            itemButton.onClick.AddListener(() => OnItemClick(newItem, subclickObjects, canSelect));
            clickObjects++;
            x++;
        }

    }

    void OnItemClick(GameObject itemObject, int itemName, bool canSelect)
    {
        if (canSelect == false)
            return;

        if (clickcounter == _MaximunCanPick)
            return;

        if (!selectedItems.Contains(itemObject))
        {
            selectedItemsPos.Add(clickOrder);
            selectedItems.Add(itemObject);

            Card card = itemObject.GetComponent<Card>();
            card.Glow(ForceColor: Color.green, ForceGlow: true);

            TMP_Text orderText = itemObject.transform.Find("OrderText").GetComponent<TMP_Text>();
            orderText.text = clickOrder.ToString();
            orderText.gameObject.SetActive(true);

            clickOrder++;
            clickcounter++;
        }
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
                Card card = gms.GetComponent<Card>();
                returnList.Add(card.ToCardData());
            }

            selectedItems = new();
            FillMenu(_DaToReturn, SecondSelectableItemsCard, SecondSelectableItemsCard, false, SecondSelectableItemsCard.Count);
        }
        else
        {
            if (clickcounter < _MinimunCanPick)
                return;

            ///////////////////////////////////////////////////////////////////////
            if (!doubleSelect)
                foreach (GameObject gms in selectedItems)
                    returnList.Add(gms.GetComponent<Card>().ToCardData());

            _DaToReturn.cards = returnList;
            _DaToReturn.indexes = selectedItemsPos;

            foreach (GameObject gm in SelectableItems)
            {
                Destroy(gm);
                clickOrder = 1;
            }

            selectedItems = new();
            SelectableItems = new();
            selectedItemsPos = new();
            DuelField_UI_MAP.INSTANCE.LoadAllPanelStatus().SetPanel(true, DuelField_UI_MAP.PanelType.SS_UI_General).SetPanel(false, DuelField_UI_MAP.PanelType.SS_BlockView);

            DuelField.INSTANCE.isSelectionCompleted = true;
        }
    }
    public static DuelAction GetDA() {
        return _DaToReturn;
    }
}
