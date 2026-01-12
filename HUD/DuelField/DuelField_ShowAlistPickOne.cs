using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UI;

public class DuelField_ShowAlistPickOne : MonoBehaviour
{
    public static DuelField_ShowAlistPickOne INSTANCE;

    [SerializeField] private Button confirmButton;
    [SerializeField] private Transform contentPanel;
    [SerializeField] private GameObject itemPrefab;
    private List<GameObject> SelectableItems = new();
    public List<CardData> selectedItems = new ();
    private int InstantiatedObjIndex = 1;
    int MustClickCounter = 1;
    int MaxClickCounter = 1;
    int ClickedCounter = 0;
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
    public IEnumerator SetupSelectableItems(DuelAction DaToReturn, List<Card> SelectableCards, List<Card> avaliableForSelect = null, bool canClosePanel = false)
    {
        _DaToReturn = DaToReturn;
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(FinishSelection);
        selectedItems.Clear();
        DuelField.INSTANCE.isSelectionCompleted = false;
        _canClosePanel = canClosePanel;

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
                { 
                    canSelect = true;
                    newC.Glow(ForceColor: Color.blue, ForceGlow: true);
                }
            }

            TMP_Text itemText = newItem.GetComponentInChildren<TMP_Text>();
            itemText.text = "";

            Button itemButton = newItem.GetComponent<Button>();
            itemButton.onClick.AddListener(() => OnItemClick(newItem, canSelect));
            InstantiatedObjIndex++;
        }

        DuelField_UI_MAP.INSTANCE.SaveAllPanelStatus().DisableAllOther().SetPanel(true, DuelField_UI_MAP.PanelType.SS_EffectBoxes_SelectionPanel);
        DuelField_UI_MAP.INSTANCE.SetPanel(_canClosePanel, DuelField_UI_MAP.PanelType.SS_EffectBoxes_General_PanelCloseButton);

        yield return new WaitUntil(() => DuelField.INSTANCE.isSelectionCompleted);
        DuelField.INSTANCE.isSelectionCompleted = false;
    }

    void OnItemClick(GameObject itemObject, bool canSelect)
    {
        if (!canSelect || itemObject == null)
            return;

        if (ClickedCounter >= MaxClickCounter)
            return;

        Card card = itemObject.GetComponent<Card>();
        card.Glow(ForceColor: Color.green, ForceGlow: true);

        selectedItems.Add(card.ToCardData());

        TMP_Text orderText = itemObject.transform.Find("OrderText").GetComponent<TMP_Text>();
        orderText.text = (ClickedCounter + 1).ToString();  
        orderText.gameObject.SetActive(true);

        ClickedCounter++;
    }
    void FinishSelection()
    {
        if (ClickedCounter < MustClickCounter)
            return;

        InstantiatedObjIndex = 1;
        MustClickCounter = 1;
        MaxClickCounter = 1;
        ClickedCounter = 0;

        _DaToReturn ??= new();
        _DaToReturn.cards = selectedItems;

        DuelField_UI_MAP.INSTANCE.LoadAllPanelStatus().SetPanel(true, DuelField_UI_MAP.PanelType.SS_UI_General).SetPanel(false, DuelField_UI_MAP.PanelType.SS_BlockView);
        DuelField.INSTANCE.isSelectionCompleted = true;
    }
    public static DuelAction GetDA()
    {
        return _DaToReturn;
    }
}
