using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static DuelField;
using static UnityEngine.GraphicsBuffer;

public class DuelField_TargetForEffectMenu : MonoBehaviour
{
    [SerializeField] private Button confirmButton;
    [SerializeField] private Transform CardListContent;
    [SerializeField] private GameObject CardAttachItemHolder;
    [SerializeField] private GameObject AttachedCardItem;


    private GameObject selectedItem;
    private int clickObjects = 1;
    List<Card> SelectableCards = new();
    List<GameObject> instantiatedItem = new();

    static DuelAction _DaToReturn;
    Player _target;
    [SerializeField] private Button closeButton;
    bool _canClosePanel = false;

    public static DuelField_TargetForEffectMenu INSTANCE;

    void Start()
    {
        INSTANCE = this;
        closeButton = DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_General_PanelCloseButton.GetComponent<Button>();
        closeButton.onClick.AddListener(() => {
            if (_canClosePanel) { 
                _DaToReturn = new DuelAction();
                DuelField_UI_MAP.INSTANCE.LoadAllPanelStatus().SetPanel(true, DuelField_UI_MAP.PanelType.SS_UI_General);
            }
        });
        confirmButton.onClick.AddListener(FinishSelection);
    }

    public IEnumerator SetupSelectableItems(DuelAction da, Player target = Player.Player, Lib.GameZone[] zonesThatPlayerCanSelect = null, List<Card> specificList = null , bool canClosePanel = false)
    {
        _DaToReturn = da;
        _target = target;
        _canClosePanel = canClosePanel;

        DuelField.INSTANCE.isSelectionCompleted = false;

        zonesThatPlayerCanSelect ??= DuelField.DEFAULTHOLOMEMZONE;
        GameObjectExtensions.DestroyAllChildren(CardListContent.gameObject);
        SelectableCards = CardLib.GetAndFilterCards(gameZones: zonesThatPlayerCanSelect, player: target, onlyVisible: true);

        if (specificList != null)
            SelectableCards = SelectableCards.Where(sc => specificList.Any(sl => sl.cardNumber == sc.cardNumber && sl.curZone == sc.curZone)).ToList();

        int x = 0; 
        foreach (Card item in SelectableCards)
        {
            bool canSelect = true;

            GameObject newItem = Instantiate(CardAttachItemHolder, CardListContent);
            newItem.name = clickObjects.ToString();
            instantiatedItem.Add(newItem);
            Card newC = newItem.GetComponent<Card>().Init(item.ToCardData());

            TMP_Text itemText = newItem.GetComponentInChildren<TMP_Text>();
            itemText.text = "";

            Button itemButton = newItem.GetComponent<Button>();
            itemButton.onClick.AddListener(() => OnItemClick(newItem, x, canSelect));


            for (int i = 0; i <  item.attachedEnergy.Count; i++) {
                GameObject attachedCardItem = Instantiate(AttachedCardItem, newItem.GetComponentInChildren<GridLayoutGroup>().transform);
                attachedCardItem.GetComponent<Card>().Init(item.attachedEnergy[i].GetComponent<Card>().ToCardData());
            }
            int subclickObjects = clickObjects;
            clickObjects++;
            x++;
        }

        DuelField_UI_MAP.INSTANCE.SaveAllPanelStatus().DisableAllOther().SetPanel(true, DuelField_UI_MAP.PanelType.SS_EffectBoxes_SelectionDetachEnergyPanel);
        DuelField_UI_MAP.INSTANCE.SetPanel(_canClosePanel, DuelField_UI_MAP.PanelType.SS_EffectBoxes_General_PanelCloseButton);

        yield return new WaitUntil(() => DuelField.INSTANCE.isSelectionCompleted);
        DuelField.INSTANCE.isSelectionCompleted = false;
        zonesThatPlayerCanSelect = null;
    }

    void OnItemClick(GameObject itemObject, int itemName, bool canSelect)
    {
        if (canSelect == false)
            return;

            selectedItem = itemObject;

            TMP_Text orderText = itemObject.transform.Find("Selected_Text").GetComponent<TMP_Text>();
            orderText.text = "X";
            orderText.gameObject.SetActive(true);
    }
    void FinishSelection()
    {
        if (selectedItem == null )
            return;

        Card returnCard = selectedItem.GetComponent<Card>();

        if (returnCard == null)
            return;

        _DaToReturn.target = returnCard.ToCardData();
        _DaToReturn.used = _DaToReturn.used;
        _DaToReturn.players = new();
        _DaToReturn.players.Add(_target, "x");

        DuelField_UI_MAP.INSTANCE.LoadAllPanelStatus().SetPanel(true, DuelField_UI_MAP.PanelType.SS_UI_General).SetPanel(false, DuelField_UI_MAP.PanelType.SS_BlockView);
        DuelField.INSTANCE.isSelectionCompleted = true;
    }
    public static DuelAction GetDA()
    {
        return _DaToReturn;
    }
}
