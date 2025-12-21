using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static DuelField;
using static UnityEngine.GraphicsBuffer;

public class DuelField_DetachCardMenu : MonoBehaviour
{
    public static DuelField_DetachCardMenu INSTANCE;

    [SerializeField] private Button confirmButton;
    [SerializeField] private Transform CardListContent;
    [SerializeField] private GameObject CardAttachItemHolder;
    [SerializeField] private GameObject AttachedCardItem;

    private GameObject selectedItem;
    private int clickObjects = 1;
    List<GameObject> instantiatedItem = new();
    Player _target;

    [SerializeField] private Button closeButton;
    bool _canClosePanel = false;

    static DuelAction _DaToReturn;

    private void Awake()
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
    public IEnumerator SetupSelectableItems(DuelAction DaToReturn, Lib.GameZone[] zonesThatPlayerCanSelect = null, bool IsACheer = true, Player player = Player.Player, bool canClosePanel = false)
    {
        _DaToReturn = DaToReturn;
        _target = player;
        _canClosePanel = canClosePanel;

        zonesThatPlayerCanSelect ??= DuelField.DEFAULTHOLOMEMZONE;
        GameObjectExtensions.DestroyAllChildren(CardListContent.gameObject);
        List<Card> SelectableCards = CardLib.GetAndFilterCards(gameZones: zonesThatPlayerCanSelect, player: player, onlyVisible: true);

        int x = 0;
        foreach (Card item in SelectableCards)
        {
            bool canSelect = true;

            GameObject newItem = Instantiate(CardAttachItemHolder, CardListContent);
            Card newC = newItem.GetComponent<Card>().Init(item.ToCardData());
            Destroy(newItem.GetComponent<DuelField_HandClick>());
            newItem.name = clickObjects.ToString();
            instantiatedItem.Add(newItem);

			List<Card> ListToSelectFrom = new();
            if (IsACheer)
                ListToSelectFrom = item.attachedEnergy;
            else
                ListToSelectFrom = item.attachedEquipe;
            

            if (ListToSelectFrom.Count > 0)
                for (int i = 0; i < ListToSelectFrom.Count; i++) {
                    GameObject attachedCardItem = Instantiate(AttachedCardItem, newItem.GetComponentInChildren<GridLayoutGroup>().transform);
                    Destroy(attachedCardItem.GetComponent<DuelField_HandClick>());
                    Card attachedCard = attachedCardItem.GetComponent<Card>().Init(ListToSelectFrom[i].ToCardData());

                    TMP_Text itemText = attachedCardItem.GetComponentInChildren<TMP_Text>();
                    itemText.text = "";

                    Button itemButton = attachedCardItem.GetComponent<Button>();
                    itemButton.onClick.AddListener(() => OnItemClick(attachedCardItem, i, canSelect));
                }
            int subclickObjects = clickObjects;
            clickObjects++;
            x++;
        }
        DuelField_UI_MAP.INSTANCE.SaveAllPanelStatus().DisableAllOther().SetPanel(true, DuelField_UI_MAP.PanelType.SS_EffectBoxes_SelectionDetachEnergyPanel);
        DuelField_UI_MAP.INSTANCE.SetPanel(_canClosePanel, DuelField_UI_MAP.PanelType.SS_EffectBoxes_General_PanelCloseButton);

        EffectController.INSTANCE.isSelectionCompleted = false;
        yield return new WaitUntil(() => EffectController.INSTANCE.isSelectionCompleted);
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

    public void Start()
    {
        confirmButton.onClick.AddListener(FinishSelection);
    }
    void FinishSelection()
    {
        if (selectedItem == null)
            return;

        Card returnCard = selectedItem.GetComponent<Card>();

        DuelAction da = new DuelAction();
        da.attachmentCost = new() { returnCard.ToCardData() };
        da.activationZone = da.attachmentCost.First().curZone;
        da.actionTarget = _target;

        _DaToReturn = da;

        DuelField_UI_MAP.INSTANCE.LoadAllPanelStatus().SetPanel(true, DuelField_UI_MAP.PanelType.SS_UI_General).SetPanel(false, DuelField_UI_MAP.PanelType.SS_BlockView);
        EffectController.INSTANCE.isSelectionCompleted = true;
    }
    public static DuelAction GetDA()
    {
        return _DaToReturn;
    }
}
