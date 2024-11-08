using Assets.Scripts.Lib;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static DuelField;

public class DuelField_TargetForEffectMenu : MonoBehaviour
{
    [SerializeField] private Button confirmButton;
    [SerializeField] private Transform CardListContent;
    [SerializeField] private GameObject CardAttachItemHolder;
    [SerializeField] private GameObject AttachedCardItem;
    private GameObject selectedItem;
    private int clickObjects = 1;
    private DuelField _DuelField;
    List<Card> SelectableCards = new();
    Card usedCard;
    DuelAction duelAction;
    private EffectController effectController;

    List<GameObject> instantiatedItem = new();

    public IEnumerator SetupSelectableItems(DuelAction da, TargetPlayer target = TargetPlayer.Player, string[] zonesThatPlayerCanSelect = null)
    {
        effectController.isSelectionCompleted = false;

        duelAction = da;
        usedCard = new Card(duelAction.usedCard.cardNumber);
        usedCard.GetCardInfo();

        //assign the positions where we need to get the cards for selection, if stars null, we pass the values bellow
        if (zonesThatPlayerCanSelect == null)
            zonesThatPlayerCanSelect = new string[] { "Stage", "Collaboration", "BackStage1", "BackStage2", "BackStage3", "BackStage4", "BackStage5" };

        //we call PopulateSelectableCards to clear the recycable menu and add to  SelectableCards the last card in each position 
        _DuelField.PopulateSelectableCards(target, zonesThatPlayerCanSelect, CardListContent.gameObject, SelectableCards);

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



            for (int i = 0; i <  newC.attachedEnergy.Count; i++) {
                GameObject attachedCardItem = Instantiate(AttachedCardItem, newItem.GetComponentInChildren<GridLayoutGroup>().transform);
                Destroy(attachedCardItem.GetComponent<DuelField_HandClick>());
                Card attachedCard = attachedCardItem.GetComponent<Card>();
                attachedCard.cardPosition = newC.cardPosition;
                attachedCard.cardNumber = newC.attachedEnergy[i].GetComponent<Card>().cardNumber;
                attachedCard.GetCardInfo();

            }
            int subclickObjects = clickObjects;
            clickObjects++;
            x++;
        }

        CardListContent.transform.parent.parent.parent.gameObject.SetActive(true);

        yield return new WaitUntil(() => effectController.isSelectionCompleted);
        effectController.isSelectionCompleted = false;
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
        effectController = FindAnyObjectByType<EffectController>();
        _DuelField = GameObject.FindAnyObjectByType<DuelField>();
        confirmButton.onClick.AddListener(FinishSelection);
    }
    void FinishSelection()
    {
        if (selectedItem == null )
            return;

        Card returnCard = selectedItem.GetComponent<Card>();

        if (returnCard == null)
            return;

        duelAction.targetCard = CardData.CreateCardDataFromCard(returnCard);
        duelAction.usedCard.cardPosition = duelAction.usedCard.cardPosition;

        effectController.EffectInformation.Add(duelAction);

        // Hide panel
        CardListContent.transform.parent.parent.parent.gameObject.SetActive(false);

        effectController.isSelectionCompleted = true;
    }
}
