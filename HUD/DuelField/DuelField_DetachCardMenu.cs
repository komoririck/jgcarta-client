using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static DuelField;

public class DuelField_DetachCardMenu : MonoBehaviour
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
    DuelField_TargetForEffectMenu _DuelField_TargetForEffectMenu;
    List<GameObject> instantiatedItem = new();
   

    private EffectController effectController;
    bool _AddOnlyCostToEffectInformation;

    bool CHEER = true;

    public IEnumerator SetupSelectableItems(DuelAction _DuelAction, bool AddCostToEffectInformation = false, Lib.GameZone[] zonesThatPlayerCanSelect = null, bool IsACheer = true)
    {
        _AddOnlyCostToEffectInformation = AddCostToEffectInformation;
        CHEER = IsACheer;

        duelAction = _DuelAction;
        this.usedCard.cardNumber = _DuelAction.usedCard.cardNumber;

        //assign the positions where we need to get the cards for selection, if stars null, we pass the values bellow
        if (zonesThatPlayerCanSelect == null)
            zonesThatPlayerCanSelect = new Lib.GameZone[] { Lib.GameZone.Stage, Lib.GameZone.Collaboration, Lib.GameZone.BackStage1, Lib.GameZone.BackStage2, Lib.GameZone.BackStage3, Lib.GameZone.BackStage4, Lib.GameZone.BackStage5 };

        _DuelField.PopulateSelectableCards(TargetPlayer.Player, zonesThatPlayerCanSelect, CardListContent.gameObject, SelectableCards);

        int x = 0;  // Variable to track order
        foreach (CardData item in SelectableCards)
        {
            bool canSelect = true;

            GameObject newItem = Instantiate(CardAttachItemHolder, CardListContent);
            Destroy(newItem.GetComponent<DuelField_HandClick>());
            newItem.name = clickObjects.ToString();
            instantiatedItem.Add(newItem);

            Card newC = newItem.GetComponent<Card>().SetCardNumber(item.cardNumber);
            newC.curZone = DuelField.INSTANCE.GetZoneByString(CardAttachItemHolder.transform.parent.name);

			List<GameObject> ListToSelectFrom = new();
            if (IsACheer)
                ListToSelectFrom = newC.attachedEnergy;
            else
                ListToSelectFrom = newC.attachedEquipe;
            

            if (ListToSelectFrom.Count > 0)
                for (int i = 0; i < ListToSelectFrom.Count; i++) {
                    GameObject attachedCardItem = Instantiate(AttachedCardItem, newItem.GetComponentInChildren<GridLayoutGroup>().transform);
                    Destroy(attachedCardItem.GetComponent<DuelField_HandClick>());
                    Card attachedCard = attachedCardItem.GetComponent<Card>();
                    attachedCard.curZone = newC.curZone;
                    attachedCard.cardNumber = ListToSelectFrom[i].GetComponent<Card>().cardNumber;

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

        duelAction.cheerCostCard = returnCard.ToCardData();
        duelAction.usedCard.curZone = duelAction.cheerCostCard.curZone;
        duelAction.activationZone = duelAction.cheerCostCard.curZone;

        GameObject fatherObj = _DuelField.GetZone(duelAction.activationZone, DuelField.TargetPlayer.Player);
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

        if (_AddOnlyCostToEffectInformation)
        {
            effectController.EffectInformation.Add(new DuelAction {usedCard = returnCard.ToCardData() });
        }
        else
        {
            effectController.EffectInformation.Add(duelAction);
        }

        CardListContent.transform.parent.parent.parent.gameObject.SetActive(false);
        effectController.isSelectionCompleted = true;
    }
}
