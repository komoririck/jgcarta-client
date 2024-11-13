using Assets.Scripts.Lib;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using TMPro;
using Unity.VisualScripting;
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
    List<Card> SelectableCards = new();
    Card usedCard = new("");
    DuelAction duelAction;

    List<GameObject> instantiatedItem = new();
    private EffectController effectController;

    public IEnumerator SetupSelectableItems(DuelAction _DuelAction, TargetPlayer target = TargetPlayer.Player)
    {
        effectController.isSelectionCompleted = false;

        duelAction = _DuelAction;
        this.usedCard.cardNumber = _DuelAction.usedCard.cardNumber;
        usedCard.GetCardInfo();

        //some cards have limitations for target
        switch (_DuelAction.usedCard.cardNumber)
        {
            case "hBP01-009":
                if (_DuelField.GetZone("Stage", TargetPlayer.Oponnent).GetComponentInChildren<Card>() == null) { 
                    CardListContent.transform.parent.parent.parent.gameObject.SetActive(false);
                    yield return null;
                }

                _DuelField.PopulateSelectableCards(target, new string[] { "Stage" }, CardListContent.gameObject, SelectableCards);
                break;
                default:
                _DuelField.PopulateSelectableCards(target, new string[] { "Stage", "Collaboration" }, CardListContent.gameObject, SelectableCards);
                break;
        }

        _DuelField.PopulateSelectableCards(target, new string[] { "Stage", "Collaboration"}, CardListContent.gameObject, SelectableCards);

        int x = 0;  // Variable to track order
        foreach (Card item in SelectableCards)
        {
            bool canSelect = true;

            GameObject newItem = Instantiate(CardAttachItemHolder, CardListContent);
            Destroy(newItem.GetComponent<DuelField_HandClick>());
            newItem.name = clickObjects.ToString();
            instantiatedItem.Add(newItem);

            Card newC = newItem.GetComponent<Card>();
            newC.cardNumber = item.cardNumber;
            newC.cardPosition = item.transform.parent.name;
            newC.GetCardInfo();
            newC.attachedEnergy = item.attachedEnergy;


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

        string pos = duelAction.usedCard.cardPosition;
        Card returnCard = selectedItem.GetComponent<Card>();

        duelAction.targetCard = CardData.CreateCardDataFromCard(returnCard);

        Card card = new(duelAction.usedCard.cardNumber);
        card.GetCardInfo();

        duelAction.usedCard.cardPosition = pos;
        if (duelAction.actionType.Equals("doArt")) {
            _DuelField.GenericActionCallBack(duelAction, "doArt");
        }

        CardListContent.transform.parent.parent.parent.gameObject.SetActive(false);
        effectController.isSelectionCompleted = true;
    }


}

