using Assets.Scripts.Lib;
using System.Collections.Generic;
using System.Linq.Expressions;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static DuelField;
using static UnityEngine.GraphicsBuffer;

public class DuelField_DetachEnergyMenu : MonoBehaviour
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
    DuelField_TargetForEffectMenu _DuelField_TargetForEffectMenu;
    List<GameObject> instantiatedItem = new();

    public void SetupSelectableItems(DuelAction _DuelAction)
    {
        duelAction = _DuelAction;
        this.usedCard.cardNumber = _DuelAction.usedCard.cardNumber;
        usedCard.GetCardInfo();

        _DuelField.PopulateSelectableCards(TargetPlayer.Player, new string[] { "Stage", "Collaboration", "BackStage1", "BackStage2", "BackStage3", "BackStage4", "BackStage5" }, CardListContent.gameObject, SelectableCards);

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
            newC.attachedCards = item.attachedCards;

            if (newC.attachedCards.Count > 0)
                for (int i = 0; i <  newC.attachedCards.Count; i++) {
                    GameObject attachedCardItem = Instantiate(AttachedCardItem, newItem.GetComponentInChildren<GridLayoutGroup>().transform);
                    Destroy(attachedCardItem.GetComponent<DuelField_HandClick>());
                    Card attachedCard = attachedCardItem.GetComponent<Card>();
                    attachedCard.cardPosition = newC.cardPosition;
                    attachedCard.cardNumber = newC.attachedCards[i].GetComponent<Card>().cardNumber;
                    attachedCard.GetCardInfo();

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
        _DuelField_TargetForEffectMenu = FindAnyObjectByType<DuelField_TargetForEffectMenu>();
        _DuelField = GameObject.FindAnyObjectByType<DuelField>();
        confirmButton.onClick.AddListener(FinishSelection);
    }
    void FinishSelection()
    {
        if (selectedItem == null)
            return;


        Card returnCard = selectedItem.GetComponent<Card>();

        duelAction.cheerCostCard = DataConverter.CreateCardDataFromCard(returnCard);
        duelAction.usedCard.cardPosition = duelAction.cheerCostCard.cardPosition;
        duelAction.local = duelAction.cheerCostCard.cardPosition;

        GameObject fatherObj = _DuelField.GetZone(duelAction.local, DuelField.TargetPlayer.Player);
        Card FatherCard = fatherObj.transform.GetChild(fatherObj.transform.childCount - 1).GetComponent<Card>();

        if (FatherCard.attachedCards.Count < 0)
            return;

        int j = -1;
        for (int i = 0; i < FatherCard.attachedCards.Count; i++) {
            Card childCard = FatherCard.attachedCards[i].GetComponent<Card>();
            if (childCard.cardNumber.Equals(duelAction.cheerCostCard.cardNumber)) { 
                j = i;
                break;
            }
        }

        _DuelField.SendCardToZone(FatherCard.attachedCards[j], "Arquive", DuelField.TargetPlayer.Player);
        FatherCard.attachedCards[j].SetActive(true);

        duelAction.actionType = "UseSuportStaffMember";

        FatherCard.attachedCards.RemoveAt(j);

        FindAnyObjectByType<EffectController>().duelActionOutput = duelAction;

        CardListContent.transform.parent.parent.parent.gameObject.SetActive(false);

        FindAnyObjectByType<EffectController>();
    }

    // Helper method to get the last card in the zone, if any
    private Card GetLastCardInZone(string zoneName, DuelField.TargetPlayer targetPlayer)
    {
        var zone = _DuelField.GetZone(zoneName, targetPlayer);
        if (zone.transform.childCount > 0)
        {
            var card = zone.transform.GetChild(zone.transform.childCount - 1).GetComponent<Card>();
            return card != null ? card : null;
        }
        return null;
    }
}
