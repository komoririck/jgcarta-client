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

    DuelAction _DuelAction;
    Player _target;

    public IEnumerator SetupSelectableItems(DuelAction da, Player target = Player.Player, Lib.GameZone[] zonesThatPlayerCanSelect = null)
    {
        _DuelAction = da;
        _target = target; 

        EffectController.INSTANCE.isSelectionCompleted = false;

        zonesThatPlayerCanSelect ??= DuelField.DEFAULTHOLOMEMZONE;
        GameObjectExtensions.DestroyAllChildren(CardListContent.gameObject);
        SelectableCards = CardLib.GetAndFilterCards(gameZones: zonesThatPlayerCanSelect, player: target, onlyVisible: true);

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

        CardListContent.transform.parent.parent.parent.gameObject.SetActive(true);

        yield return new WaitUntil(() => EffectController.INSTANCE.isSelectionCompleted);
        EffectController.INSTANCE.isSelectionCompleted = false;
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

    public void Start()
    {
        confirmButton.onClick.AddListener(FinishSelection);
    }
    void FinishSelection()
    {
        if (selectedItem == null )
            return;

        Card returnCard = selectedItem.GetComponent<Card>();

        if (returnCard == null)
            return;

        _DuelAction.targetCard = returnCard.ToCardData();
        _DuelAction.usedCard = _DuelAction.usedCard;
        _DuelAction.targetPlayer = _target;

        EffectController.INSTANCE.CurrentContext.Register(_DuelAction);
        CardListContent.transform.parent.parent.parent.gameObject.SetActive(false);
        EffectController.INSTANCE.isSelectionCompleted = true;
    }
}
