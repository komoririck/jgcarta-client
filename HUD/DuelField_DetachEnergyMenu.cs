using Assets.Scripts.Lib;
using System.Collections;
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
   

    private EffectController effectController;
    bool _AddCostToEffectInformation;
    bool _DestroyCostWhenDeAtach;

    public IEnumerator SetupSelectableItems(DuelAction _DuelAction, bool AddCostToEffectInformation = false, string[] zonesThatPlayerCanSelect = null, bool DestroyCostWhenDeAtach = false)
    {
        _AddCostToEffectInformation = AddCostToEffectInformation;
        _DestroyCostWhenDeAtach = DestroyCostWhenDeAtach;

        duelAction = _DuelAction;
        this.usedCard.cardNumber = _DuelAction.usedCard.cardNumber;
        usedCard.GetCardInfo();

        //assign the positions where we need to get the cards for selection, if stars null, we pass the values bellow
        if (zonesThatPlayerCanSelect == null)
            zonesThatPlayerCanSelect = new string[] { "Stage", "Collaboration", "BackStage1", "BackStage2", "BackStage3", "BackStage4", "BackStage5" };

        _DuelField.PopulateSelectableCards(TargetPlayer.Player, zonesThatPlayerCanSelect, CardListContent.gameObject, SelectableCards);

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

            if (newC.attachedEnergy.Count > 0)
                for (int i = 0; i <  newC.attachedEnergy.Count; i++) {
                    GameObject attachedCardItem = Instantiate(AttachedCardItem, newItem.GetComponentInChildren<GridLayoutGroup>().transform);
                    Destroy(attachedCardItem.GetComponent<DuelField_HandClick>());
                    Card attachedCard = attachedCardItem.GetComponent<Card>();
                    attachedCard.cardPosition = newC.cardPosition;
                    attachedCard.cardNumber = newC.attachedEnergy[i].GetComponent<Card>().cardNumber;
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
        effectController.isSelectionCompleted = false;
        yield return new WaitUntil(() => effectController.isSelectionCompleted);
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
        _DuelField_TargetForEffectMenu = FindAnyObjectByType<DuelField_TargetForEffectMenu>();
        _DuelField = GameObject.FindAnyObjectByType<DuelField>();
        confirmButton.onClick.AddListener(FinishSelection);
    }
    void FinishSelection()
    {
        if (selectedItem == null)
            return;

        Card returnCard = selectedItem.GetComponent<Card>();

        duelAction.cheerCostCard = CardData.CreateCardDataFromCard(returnCard);
        duelAction.usedCard.cardPosition = duelAction.cheerCostCard.cardPosition;
        duelAction.local = duelAction.cheerCostCard.cardPosition;

        GameObject fatherObj = _DuelField.GetZone(duelAction.local, DuelField.TargetPlayer.Player);
        Card FatherCard = fatherObj.transform.GetChild(fatherObj.transform.childCount - 1).GetComponent<Card>();

        if (FatherCard.attachedEnergy.Count < 0)
            return;

        int j = -1;
        for (int i = 0; i < FatherCard.attachedEnergy.Count; i++) {
            Card childCard = FatherCard.attachedEnergy[i].GetComponent<Card>();
            if (childCard.cardNumber.Equals(duelAction.cheerCostCard.cardNumber)) { 
                j = i;
                break;
            }
        }

        if (_AddCostToEffectInformation)
        {
            effectController.EffectInformation.Add(new Card(returnCard.cardNumber, returnCard.cardPosition));
        }
        else
        {
            effectController.EffectInformation.Add(duelAction);
        }

        if (_DestroyCostWhenDeAtach)
        {
            Destroy(FatherCard.attachedEnergy[j]);
        }
        else
        {
            _DuelField.SendCardToZone(FatherCard.attachedEnergy[j], "Arquive", DuelField.TargetPlayer.Player);
            FatherCard.attachedEnergy[j].SetActive(true);
        }

        FatherCard.attachedEnergy.RemoveAt(j);


        CardListContent.transform.parent.parent.parent.gameObject.SetActive(false);

        effectController.isSelectionCompleted = true;
    }
}
