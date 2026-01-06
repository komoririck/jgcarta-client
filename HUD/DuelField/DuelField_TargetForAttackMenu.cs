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

    public static DuelField_TargetForAttackMenu INSTANCE;

    private GameObject selectedItem;
    private int clickObjects = 1;

    List<Card> SelectableCards = new();
    DuelAction _DuelAction;
    bool _performArt;

    List<GameObject> instantiatedItem = new();
    Player _target;

    public IEnumerator SetupSelectableItems(DuelAction DuelAction, Player target = Player.Oponnent, bool performArt = false)
    {
        DuelField.INSTANCE.isSelectionCompleted = false;
        _DuelAction = DuelAction;
        _performArt = performArt;
        _target = target;

        switch (_DuelAction.used.cardNumber)
        {
            case "hBP01-009":
                if (DuelField.INSTANCE.GetZone(Lib.GameZone.Stage, target).GetComponentInChildren<Card>() == null)
                {
                    CardListContent.transform.parent.parent.parent.gameObject.SetActive(false);
                    yield return null;
                }
                GameObjectExtensions.DestroyAllChildren(CardListContent.gameObject);
                SelectableCards = CardLib.GetAndFilterCards(gameZones: new[] { Lib.GameZone.Stage }, player: target, onlyVisible: true, GetOnlyHolomem: true);
                break;

            default:
                GameObjectExtensions.DestroyAllChildren(CardListContent.gameObject);
                SelectableCards = CardLib.GetAndFilterCards(gameZones: new[] { Lib.GameZone.Stage, Lib.GameZone.Collaboration }, player: target, onlyVisible: true, GetOnlyHolomem: true);
                break;
        }

        var card = DuelField.INSTANCE.GetZone(Lib.GameZone.Collaboration, target).GetComponent<Card>();

        if (card != null && card.cardNumber.Equals("hBP01-050"))
        {
            GameObjectExtensions.DestroyAllChildren(CardListContent.gameObject);
            SelectableCards = CardLib.GetAndFilterCards(gameZones: new[] { Lib.GameZone.Stage, Lib.GameZone.Collaboration }, player: target, onlyVisible: true, GetOnlyHolomem: true);
        }

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

            int subclickObjects = clickObjects;
            clickObjects++;
            x++;
        }

        CardListContent.transform.parent.parent.parent.gameObject.SetActive(true);

        yield return new WaitUntil(() => DuelField.INSTANCE.isSelectionCompleted);
        DuelField.INSTANCE.isSelectionCompleted = false;
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
        INSTANCE = this;
        confirmButton.onClick.AddListener(FinishSelection);
    }
    void FinishSelection()
    {
        if (selectedItem == null)
            return;

        var pos = _DuelAction.used.curZone;
        Card returnCard = selectedItem.GetComponent<Card>();

        _DuelAction.target = returnCard.ToCardData();
        _DuelAction.players = new();
        _DuelAction.players.Add(_target, "x");

        CardData card = _DuelAction.used;

        _DuelAction.used.curZone = pos;
        if (_performArt)
        {
            MatchConnection.INSTANCE.SendRequest(_DuelAction, "doArt");
        }

        CardListContent.transform.parent.parent.parent.gameObject.SetActive(false);
        DuelField.INSTANCE.isSelectionCompleted = true;
    }
}

