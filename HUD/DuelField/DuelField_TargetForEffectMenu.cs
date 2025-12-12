using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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
    List<CardData> SelectableCards = new();
    CardData usedCard;
    DuelAction duelAction;
    private EffectController effectController;

    List<GameObject> instantiatedItem = new();

    public IEnumerator SetupSelectableItems(DuelAction da, TargetPlayer target = TargetPlayer.Player, Lib.GameZone[] zonesThatPlayerCanSelect = null)
    {
        effectController.isSelectionCompleted = false;

        duelAction = da;
        usedCard = duelAction.usedCard;

        //assign the positions where we need to get the cards for selection, if stars null, we pass the values bellow
        if (zonesThatPlayerCanSelect == null)
            zonesThatPlayerCanSelect = new Lib.GameZone[] { Lib.GameZone.Stage, Lib.GameZone.Collaboration, Lib.GameZone.BackStage1, Lib.GameZone.BackStage2, Lib.GameZone.BackStage3, Lib.GameZone.BackStage4, Lib.GameZone.BackStage5 };

        //we call PopulateSelectableCards to clear the recycable menu and add to  SelectableCards the last card in each position 
        _DuelField.PopulateSelectableCards(target, zonesThatPlayerCanSelect, CardListContent.gameObject, SelectableCards);

        int x = 0;  // Variable to track order
        foreach (CardData item in SelectableCards)
        {
            bool canSelect = true;

            GameObject newItem = Instantiate(CardAttachItemHolder, CardListContent);
            newItem.name = clickObjects.ToString();
            instantiatedItem.Add(newItem);

            Card newC = newItem.GetComponent<Card>();
            newC.cardNumber = item.cardNumber;
            newC.curZone = DuelField.INSTANCE.GetZoneByString(CardAttachItemHolder.transform.parent.name);

            TMP_Text itemText = newItem.GetComponentInChildren<TMP_Text>();
            itemText.text = "";

            Button itemButton = newItem.GetComponent<Button>();
            itemButton.onClick.AddListener(() => OnItemClick(newItem, x, canSelect));



            for (int i = 0; i <  newC.attachedEnergy.Count; i++) {
                GameObject attachedCardItem = Instantiate(AttachedCardItem, newItem.GetComponentInChildren<GridLayoutGroup>().transform);
                Card attachedCard = attachedCardItem.GetComponent<Card>();
                attachedCard.Init(attachedCard.ToCardData());
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

        duelAction.targetCard = returnCard.ToCardData();
        duelAction.usedCard.curZone = duelAction.usedCard.curZone;

        effectController.EffectInformation.Add(duelAction);

        // Hide panel
        CardListContent.transform.parent.parent.parent.gameObject.SetActive(false);

        effectController.isSelectionCompleted = true;
    }
}
