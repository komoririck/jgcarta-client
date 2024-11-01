using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using static DuelField;
using System.Threading.Tasks;
using System.Collections;
using System.Linq;

public class DuelField_HandClick : MonoBehaviour, IPointerClickHandler
{
    static public bool isViewMode = false;
    static public bool isPainelActive = true;

    private int cardCount = 0;
    private int currentCard = 0;
    private Button backButton;
    private Button nextButton;
    private Card[] cards = null;
    private Transform childTransform;
    private DuelField _DuelField;
    private Card PanelCard;
    private CardPanel CardInfoPanel;
    static readonly List<GameObject> skillsToDestroy = new();
    DuelField_TargetForAttackMenu _DuelField_TargetForAttackMenu;

    void Start()
    {
        _DuelField_TargetForAttackMenu = FindAnyObjectByType<DuelField_TargetForAttackMenu>();
        _DuelField = GameObject.FindAnyObjectByType<DuelField>();
        if (gameObject.GetComponent<Card>().cardNumber.Equals(0))
        {
            this.enabled = false;
        }

        Transform parentTransform = GameObject.Find("MatchField").transform;
        childTransform = parentTransform.Find("CardPanel");

        backButton = childTransform.Find("PreviousCard").GetComponent<Button>();
        backButton.onClick.AddListener(PreviousCardButton);
        nextButton = childTransform.Find("NextCard").GetComponent<Button>();
        nextButton.onClick.AddListener(NextCardButton);
        CardInfoPanel = GameObject.Find("MatchField").transform.Find("CardPanel").GetComponent<CardPanel>();
        PanelCard = CardInfoPanel.CardPanelInfoObject;
    }

    private IEnumerator GetCardPanelInfo()
    {
        if (this.transform.parent.name.Equals("PlayerHand"))
        {
            cards = new Card[] { this.transform.parent.GetComponentsInChildren<Card>(true)[this.transform.GetSiblingIndex()] };
        }
        else
        {
            cards = this.transform.parent.GetComponentsInChildren<Card>(true);
            Array.Reverse(cards);
        }

        cardCount = cards.Length;

        if (currentCard >= cardCount || currentCard < 0)
        {
            currentCard = 0;
        }

        if (cardCount > 0)
        {
            PanelCard.cardNumber = cards[currentCard].cardNumber;
            PanelCard.GetCardInfo();

            yield return UpdateCardPanel();
        }

        UpdateButtonVisibility();
    }
    // Assuming this is inside an async method
    private async Task UpdateCardPanel()
    {
        // Clear existing items in the ArtPanel
        GameObjectExtensions.DestroyAllChildren(CardInfoPanel.ArtPanelContent.transform.gameObject);

        if ((PanelCard.cardType.Equals("ホロメン") || PanelCard.cardType.Equals("Buzzホロメン"))
            && this.transform.parent.name.Equals("Collaboration") || this.transform.parent.name.Equals("Stage")
            && _DuelField._MatchConnection._DuelFieldData.currentGamePhase == DuelFieldData.GAMEPHASE.MainStep
            && this.GetComponent<Card>().playedThisTurn == false)
        {
            childTransform.Find("ArtPanel").gameObject.SetActive(true);
            childTransform.Find("Panel").gameObject.SetActive(false);


            foreach (Art currentArt in PanelCard.Arts)
            {
                GameObject newItem = Instantiate(CardInfoPanel.ArtPanelContentPrefab, CardInfoPanel.ArtPanelContent.transform);
                skillsToDestroy.Add(newItem);

                string translatedName = currentArt.Name;
                string translatedEffect = await GoogleTranslateAPI.TranslateTextHandle(currentArt.Effect);

                newItem.transform.Find("ArtButton").Find("Name").GetComponent<TMP_Text>().text = translatedName;
                newItem.transform.Find("ArtButton").Find("Effect").GetComponent<TMP_Text>().text = translatedEffect;
                newItem.transform.Find("ArtButton").Find("Damage").GetComponent<TMP_Text>().text = currentArt.Damage.Amount.ToString() + "{" + currentArt.ExtraColorDamage.Color + currentArt.ExtraColorDamage.Amount + "}";

                string costString = "";
                foreach ((string Color, int Amount) c in currentArt.Cost)
                    costString += c.Amount + c.Color;

                newItem.transform.Find("ArtButton").Find("Cost").GetComponent<TMP_Text>().text = costString;

                Card thisCard = GetComponent<Card>();
                DuelAction duelaction = new() { usedCard = CardData.CreateCardDataFromCard(thisCard) };
                Button itemButton = newItem.GetComponent<Button>();
                duelaction.usedCard.cardPosition = thisCard.cardPosition;

                //get count of cards, 
                Dictionary<string, List<GameObject>> energyAmount = CountCardAvaliableEnergy(thisCard);

                if (IsCostCovered(currentArt.Cost, energyAmount))
                {
                    itemButton.onClick.AddListener(() => OnItemClick(duelaction, itemButton));
                }
                else
                {
                    CanvasGroup canvasGroup = itemButton.GetComponent<CanvasGroup>();
                    itemButton.interactable = false;
                    canvasGroup.alpha = 0.5f; // Makes the button look semi-transparent
                    canvasGroup.blocksRaycasts = false; // Prevents interactions
                }
            }
        }
        else
        {
            childTransform.Find("ArtPanel").gameObject.SetActive(false);
            childTransform.Find("Panel").gameObject.SetActive(true);

            string translatedArts = await GoogleTranslateAPI.TranslateTextHandle(PanelCard.arts);
            string translatedOshiSkill = await GoogleTranslateAPI.TranslateTextHandle(PanelCard.oshiSkill);
            string translatedSpOshiSkill = await GoogleTranslateAPI.TranslateTextHandle(PanelCard.spOshiSkill);
            string translatedAbilityText = await GoogleTranslateAPI.TranslateTextHandle(PanelCard.abilityText);

            TMP_Text textComponent = childTransform.GetComponentInChildren<TMP_Text>();
            textComponent.text =
                $"{(translatedArts ?? string.Empty)}" +
                $"{(translatedOshiSkill != null ? "\n" + translatedOshiSkill : string.Empty)}" +
                $"{(translatedSpOshiSkill != null ? "\n" + translatedSpOshiSkill : string.Empty)}" +
                $"{(translatedAbilityText != null ? "\n" + translatedAbilityText : string.Empty)}" +
                $"{(PanelCard.cardNumber != null ? "\n" + PanelCard.cardNumber : string.Empty)}";
        }
    }

    private Dictionary<string, List<GameObject>> CountCardAvaliableEnergy(Card card)
    {
        Dictionary<string, List<GameObject>> energyAmount = new();
        foreach (GameObject energy in card.attachedCards)
        {
            Card cardEnergy = energy.GetComponent<Card>();
            if (cardEnergy.cardType.Equals("エール"))
            {
                if (!energyAmount.ContainsKey(cardEnergy.color))
                {
                    energyAmount[cardEnergy.color] = new List<GameObject>();
                }
                energyAmount[cardEnergy.color].Add(energy);
            }
        }
        return energyAmount;
    }
    public bool IsCostCovered(List<(string Color, int Amount)> cost, Dictionary<string, List<GameObject>> energyAmount)
    {
        foreach (var (color, amount) in cost)
        {
            if (color == "無色")
            {
                // Total count of all energies to satisfy "◇" cost
                int totalEnergyCount = energyAmount.Values.Sum(list => list.Count);
                if (totalEnergyCount < amount)
                    return false; // Not enough total energy to satisfy the "◇" cost
            }
            else
            {
                // Check if there's enough energy of the specific color
                int availableCount = energyAmount.ContainsKey(color) ? energyAmount[color].Count : 0;
                if (availableCount < amount)
                    return false; // Not enough energy of the specified color
            }
        }
        return true;
    }

    private void OnItemClick(DuelAction duelaction, Button thisButton)
    {
        if (duelaction.usedCard.cardPosition.Equals("Stage"))
        {
            if (_DuelField.centerStageArtUsed)
                return;
        }
        else if (duelaction.usedCard.cardPosition.Equals("Collaboration"))
        {
            if (_DuelField.collabStageArtUsed)
                return;
        }

        GameObject.Find("MatchField").transform.Find("CardPanel").gameObject.SetActive(false);
        duelaction.selectedSkill = thisButton.transform.Find("ArtButton").Find("Name").GetComponent<TMP_Text>().text;
        duelaction.actionType = "doArt";
        _DuelField_TargetForAttackMenu.SetupSelectableItems(duelaction, TargetPlayer.Oponnent);

    }

    void UpdateButtonVisibility()
    {
        backButton.gameObject.SetActive(currentCard > 0);
        nextButton.gameObject.SetActive(currentCard < cardCount - 1);
    }

    void PreviousCardButton()
    {
        if (currentCard > 0)
        {
            currentCard--;
            StartCoroutine(GetCardPanelInfo());
        }
    }

    void NextCardButton()
    {
        if (currentCard < cardCount - 1)
        {
            currentCard++;
            StartCoroutine(GetCardPanelInfo());
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {

        Transform parentTransform = GameObject.Find("MatchField").transform;
        Transform childTransform = parentTransform.Find("CardPanel");

        if (childTransform.gameObject.activeInHierarchy)
            return;

        bool actionDone = false;

        if (isViewMode == false && _DuelField._MatchConnection._DuelFieldData.currentPlayerTurn == _DuelField.PlayerInfo.PlayerID)
        {

            //if no clicling in a dropzone which is assigned to all field zones
            if (transform.parent.TryGetComponent<DropZone>(out var pointedZoneFather))
            {

                switch (pointedZoneFather.zoneType)
                {
                    case "Stage":
                        break;
                    case "Collaboration":
                        break;
                    case "BackStage1":
                    case "BackStage2":
                    case "BackStage3":
                    case "BackStage4":
                    case "BackStage5":
                        switch (_DuelField._MatchConnection._DuelFieldData.currentGamePhase)
                        {
                            case DuelFieldData.GAMEPHASE.MainStep:

                                if (this.GetComponent<Card>().suspended == true) { break; }

                                if (_DuelField.GetZone("Collaboration", TargetPlayer.Player).GetComponentInChildren<Card>() == null)
                                {
                                    if (_DuelField.GetZone("Deck", TargetPlayer.Player).transform.childCount == 0)
                                        return;
                                    //we are saving the parent name, because when we move to another position we lost it, and we need to send to the server as playerdfrom
                                    string parent = this.transform.parent.name;
                                    MoveCardsToZone(this.transform.parent, _DuelField.GetZone("Collaboration", TargetPlayer.Player).transform);
                                    //getting a card from the deck to send to holopower because of the collab
                                    _DuelField.SendCardToZone(_DuelField.GetZone("Deck", TargetPlayer.Player).transform.GetChild(0).gameObject, "HoloPower", TargetPlayer.Player);
                                    DuelAction duelAction = new()
                                    {
                                        playerID = _DuelField.PlayerInfo.PlayerID,
                                        usedCard = CardData.CreateCardDataFromCard(this.GetComponent<Card>()),
                                        playedFrom = parent,
                                        local = "Collaboration",
                                        actionType = "DoCollab"
                                    };
                                    _DuelField.GenericActionCallBack(duelAction);
                                }
                                this.GetComponent<Card>().cardPosition = "Collaboration";
                                actionDone = true;
                                break;
                            case DuelFieldData.GAMEPHASE.ResetStepReSetStage:
                                if ((_DuelField.GetZone("Stage", TargetPlayer.Player).transform.childCount > 0))
                                    return;

                                //we are saving the parent name, because when we move to another position we lost it, and we need to send to the server as playerdfrom
                                string parentt = this.transform.parent.name;
                                MoveCardsToZone(this.transform.parent, _DuelField.GetZone("Stage", TargetPlayer.Player).transform);
                                this.GetComponent<Card>().cardPosition = "Stage";
                                //getting a card from the deck to send to holopower because of the collab
                                DuelAction duelActionn = new()
                                {
                                    playerID = _DuelField.PlayerInfo.PlayerID,
                                    usedCard = CardData.CreateCardDataFromCard(this.GetComponent<Card>()),
                                    playedFrom = parentt,
                                    local = "Stage",
                                };
                                _DuelField.GenericActionCallBack(duelActionn, "ReSetCardAtStage");
                                actionDone = true;
                                break;
                        }
                        break;
                }
            }
        }

        if (actionDone == false || _DuelField._MatchConnection._DuelFieldData.currentPlayerTurn != _DuelField.PlayerInfo.PlayerID)
        {
            //if in the clicked location theres no card number, return since must be a facedown card
            if (string.IsNullOrEmpty(GetComponentInChildren<Card>().cardNumber))
                return;

            GameObjectExtensions.DestroyAllChildren(CardInfoPanel.transform.Find("ArtPanel").Find("Viewport").Find("Content").gameObject);

            currentCard = 0;
            StartCoroutine(GetCardPanelInfo());
            childTransform.gameObject.SetActive(true);
        }
    }

    static public void MoveCardsToZone(Transform fromParent, Transform toParent)
    {
        var children = new List<Transform>();
        foreach (Transform child in fromParent)
        {
            children.Add(child);
        }
        foreach (Transform child in children)
        {
            child.SetParent(toParent, false);
        }
    }
}
