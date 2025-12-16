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
    [SerializeField] private Button confirmButton;
    [SerializeField] private Transform CardListContent;
    [SerializeField] private GameObject CardAttachItemHolder;
    [SerializeField] private GameObject AttachedCardItem;

    private GameObject selectedItem;
    private int clickObjects = 1;
    List<GameObject> instantiatedItem = new();
    Player _target;

    public IEnumerator SetupSelectableItems(Lib.GameZone[] zonesThatPlayerCanSelect = null, bool IsACheer = true, Player player = Player.Player)
    {
        _target = player;

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

			List<GameObject> ListToSelectFrom = new();
            if (IsACheer)
                ListToSelectFrom = item.attachedEnergy;
            else
                ListToSelectFrom = item.attachedEquipe;
            

            if (ListToSelectFrom.Count > 0)
                for (int i = 0; i < ListToSelectFrom.Count; i++) {
                    GameObject attachedCardItem = Instantiate(AttachedCardItem, newItem.GetComponentInChildren<GridLayoutGroup>().transform);
                    Destroy(attachedCardItem.GetComponent<DuelField_HandClick>());
                    Card attachedCard = attachedCardItem.GetComponent<Card>().Init(ListToSelectFrom[i].GetComponent<Card>().ToCardData());

                    TMP_Text itemText = attachedCardItem.GetComponentInChildren<TMP_Text>();
                    itemText.text = "";

                    Button itemButton = attachedCardItem.GetComponent<Button>();
                    itemButton.onClick.AddListener(() => OnItemClick(attachedCardItem, i, canSelect));
                }
            int subclickObjects = clickObjects;
            clickObjects++;
            x++;
        }
        CardListContent.transform.parent.parent.parent.gameObject.SetActive(true);
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
        da.targetPlayer = _target;

        EffectController.INSTANCE.CurrentContext.Register(da);

        CardListContent.transform.parent.parent.parent.gameObject.SetActive(false);
        EffectController.INSTANCE.isSelectionCompleted = true;
    }
}
