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

public class DuelField_DetachCardMenu : MonoBehaviour
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

    bool CHEER = true;

    public IEnumerator SetupSelectableItems(DuelAction _DuelAction, bool AddCostToEffectInformation = false, string[] zonesThatPlayerCanSelect = null, bool Cheer = true)
    {
        _AddCostToEffectInformation = AddCostToEffectInformation;
        CHEER = Cheer;

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

            Card newC = newItem.GetComponent<Card>().SetCardNumber(item.cardNumber).GetCardInfo();
            newC.cardPosition = item.transform.parent.name;
            newC.attachedEnergy = item.attachedEnergy;
            newC.attachedEquipe = item.attachedEquipe;

            List<GameObject> ListToSelectFrom = new();
            if (Cheer)
            {
                ListToSelectFrom = newC.attachedEnergy;
            }
            else
            {
                ListToSelectFrom = newC.attachedEquipe;
            }

            if (ListToSelectFrom.Count > 0)
                for (int i = 0; i < ListToSelectFrom.Count; i++) {
                    GameObject attachedCardItem = Instantiate(AttachedCardItem, newItem.GetComponentInChildren<GridLayoutGroup>().transform);
                    Destroy(attachedCardItem.GetComponent<DuelField_HandClick>());
                    Card attachedCard = attachedCardItem.GetComponent<Card>();
                    attachedCard.cardPosition = newC.cardPosition;
                    attachedCard.cardNumber = ListToSelectFrom[i].GetComponent<Card>().cardNumber;
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

        List<GameObject> DetachbleList = null;

        if (CHEER)
            DetachbleList = FatherCard.attachedEnergy;
        else
            DetachbleList = FatherCard.attachedEquipe;


        if (DetachbleList.Count < 0)
            return;

        int j = -1;
        for (int i = 0; i < DetachbleList.Count; i++) {
            Card childCard = DetachbleList[i].GetComponent<Card>();
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

        CardListContent.transform.parent.parent.parent.gameObject.SetActive(false);
        effectController.isSelectionCompleted = true;
    }
}
