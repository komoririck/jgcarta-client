using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static DuelField;

public class DuelField_TargetForAttackMenu : MonoBehaviour
{
    [SerializeField] private Button confirmButton;
    [SerializeField] private Transform CardListContent;
    [SerializeField] private GameObject CardAttachItemHolder;
    [SerializeField] private GameObject AttachedCardItem;
    private GameObject selectedItem;
    private int clickObjects = 1;
    private DuelField _DuelField;
	List<CardData> SelectableCards = new();
	CardData usedCard = new();
    DuelAction duelAction;

    List<GameObject> instantiatedItem = new();
    private EffectController effectController;

    public IEnumerator SetupSelectableItems(DuelAction _DuelAction, TargetPlayer target = TargetPlayer.Player)
    {
        effectController.isSelectionCompleted = false;

        duelAction = _DuelAction;
        this.usedCard.cardNumber = _DuelAction.usedCard.cardNumber;

        _DuelField.PopulateSelectableCards(target, new Lib.GameZone[] { Lib.GameZone.Stage, Lib.GameZone.Collaboration }, CardListContent.gameObject, SelectableCards);

        //some cards have limitations for target
        switch (_DuelAction.usedCard.cardNumber)
        {
            case "hBP01-009":
                if (_DuelField.GetZone(Lib.GameZone.Stage, TargetPlayer.Oponnent).GetComponentInChildren<Card>() == null) { 
                    CardListContent.transform.parent.parent.parent.gameObject.SetActive(false);
                    yield return null;
                }

                _DuelField.PopulateSelectableCards(target, new Lib.GameZone[] { Lib.GameZone.Stage }, CardListContent.gameObject, SelectableCards);
                break;
                default:
                _DuelField.PopulateSelectableCards(target, new Lib.GameZone[] { Lib.GameZone.Stage, Lib.GameZone.Collaboration }, CardListContent.gameObject, SelectableCards);
                break;
        }

        var card = _DuelField.GetZone(Lib.GameZone.Collaboration, TargetPlayer.Oponnent).GetComponent<Card>();
        if (card != null && card.cardNumber.Equals("hBP01-050")) {
        //GIFT: Bodyguard
            _DuelField.PopulateSelectableCards(target, new Lib.GameZone[] {Lib.GameZone.Collaboration}, CardListContent.gameObject, SelectableCards);
        }

        int x = 0;  // Variable to track order
        foreach (CardData item in SelectableCards)
        {
            bool canSelect = true;

            GameObject newItem = Instantiate(CardAttachItemHolder, CardListContent);
            Destroy(newItem.GetComponent<DuelField_HandClick>());
            newItem.name = clickObjects.ToString();
            instantiatedItem.Add(newItem);

            Card newC = newItem.GetComponent<Card>();
            newC.Init(item);
			newC.curZone = DuelField.INSTANCE.GetZoneByString(CardAttachItemHolder.transform.parent.name);

			TMP_Text itemText = newItem.GetComponentInChildren<TMP_Text>();
            itemText.text = "";

            Button itemButton = newItem.GetComponent<Button>();
            itemButton.onClick.AddListener(() => OnItemClick(newItem, x, canSelect));

            int subclickObjects = clickObjects;
            clickObjects++;
            x++;
        }

        CardListContent.transform.parent.parent.parent.gameObject.SetActive(true);

        yield return new WaitUntil(() => effectController.isSelectionCompleted);
        effectController.isSelectionCompleted = false;
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
        _DuelField = GameObject.FindAnyObjectByType<DuelField>();
        confirmButton.onClick.AddListener(FinishSelection);
        effectController = FindAnyObjectByType<EffectController>();
    }
    void FinishSelection()
    {
        if (selectedItem == null)
            return;

        var pos = duelAction.usedCard.curZone;
        Card returnCard = selectedItem.GetComponent<Card>();

        duelAction.targetCard = returnCard.ToCardData();

        CardData card = duelAction.usedCard;

        duelAction.usedCard.curZone = pos;
        if (duelAction.actionType.Equals("doArt")) {
            _DuelField.GenericActionCallBack(duelAction, "doArt");
        }

        CardListContent.transform.parent.parent.parent.gameObject.SetActive(false);
        effectController.isSelectionCompleted = true;
    }


}

