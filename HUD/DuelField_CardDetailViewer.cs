using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Assets.Scripts.Lib;
using System.Threading.Tasks;
using System.Linq;
using static DuelField;
using System;
using System.Collections;
using Unity.VisualScripting;

public class DuelfField_CardDetailViewer : MonoBehaviour
{
    public const bool TESTEMODE = false;

    private List<Card> CarditemList = new List<Card>(); // Internal list of GameObjects with Image components
    private int currentIndex = 0;

    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;
    private bool isSwipe;

    private DuelField _DuelField;
    static readonly List<GameObject> skillsToDestroy = new();
    bool isViewMode = true;

    EffectController _EffectController;
    DuelField_TargetForAttackMenu _DuelField_TargetForAttackMenu;

    [SerializeField] private GameObject ArtPrefab = null;
    [SerializeField] private GameObject CardPanel = null;

    private void Start()
    {
        _DuelField = FindAnyObjectByType<DuelField>();
        _EffectController = FindAnyObjectByType<EffectController>();
        _DuelField_TargetForAttackMenu = FindAnyObjectByType<DuelField_TargetForAttackMenu>();
    }

    void Update()
    {
        DetectSwipe();
    }

    public void SetItemList(Transform[] newItemList, bool viewmode)
    {
        isViewMode = viewmode;

        if (CardPanel.gameObject.activeInHierarchy == true)
            return;

        CardPanel.SetActive(true);

        Array.Reverse(newItemList);

        if (newItemList == null || newItemList.Count() == 0)
        {
            Debug.LogWarning("Item list is null or empty.");
            return;
        }

        CarditemList = new List<Card>();
        foreach (Transform cardObj in newItemList)
        {
            Card card = cardObj.GetComponent<Card>();
            if (card != null)
            {
                CarditemList.Add(card);
            }
        }
        currentIndex = 0; // Reset to the first item
        UpdateDisplayAsync();
    }

    private void DetectSwipe()
    {
        // Detect touch or mouse down
        if (Input.GetMouseButtonDown(0))
        {
            startTouchPosition = Input.mousePosition;
            isSwipe = true;
        }

        // Detect touch or mouse up
        if (Input.GetMouseButtonUp(0) && isSwipe)
        {
            endTouchPosition = Input.mousePosition;
            Vector2 swipeDelta = endTouchPosition - startTouchPosition;

            // Ensure the swipe is significant enough (preventing accidental small movements)
            if (swipeDelta.magnitude > 50) // Adjust threshold as needed
            {
                if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
                {
                    // Horizontal swipe detected
                    if (swipeDelta.x > 0)
                    {
                        SlidePrevious(); // Swipe right, move back
                    }
                    else
                    {
                        SlideNext(); // Swipe left, move forward
                    }
                }
            }
            isSwipe = false; // Reset swipe flag
        }
    }

    public void SlideNext()
    {
        if (CarditemList.Count > 1)
        {
            currentIndex = (currentIndex + 1) % CarditemList.Count;
            UpdateDisplayAsync();
        }
    }

    public void SlidePrevious()
    {
        if (CarditemList.Count > 1)
        {
            currentIndex = (currentIndex - 1 + CarditemList.Count) % CarditemList.Count;
            UpdateDisplayAsync();
        }
    }

    private async Task UpdateDisplayAsync()
    {

        Card thisCard = CarditemList[currentIndex];

        if (CarditemList[currentIndex] != null)
        {
            Card CurrentDisplayingCard = CardPanel.transform.Find("CardPanelInfo").GetComponent<Card>();

            CurrentDisplayingCard.cardNumber = CarditemList[currentIndex].cardNumber;
            CurrentDisplayingCard.GetCardInfo(forceUpdate: true);

            GameObject ArtPanel_Content = _DuelField.ArtPanel.transform.Find("Viewport").Find("Content").gameObject;
            // Clear existing items in the ArtPanel
            GameObjectExtensions.DestroyAllChildren(ArtPanel_Content);

            if ((CarditemList[currentIndex].cardType.Equals("ホロメン") || CarditemList[currentIndex].cardType.Equals("Buzzホロメン"))
                && CarditemList[currentIndex].cardPosition.Equals("Collaboration") || CarditemList[currentIndex].cardPosition.Equals("Stage")
                && _DuelField._MatchConnection._DuelFieldData.currentGamePhase == DuelFieldData.GAMEPHASE.MainStep
                && CarditemList[currentIndex].playedThisTurn == false
                && _DuelField._MatchConnection._DuelFieldData.currentPlayerTurn.Equals(_DuelField.PlayerInfo.PlayerID)
                && !isViewMode
                && CarditemList[currentIndex].transform.parent.parent.name.Equals("Oponente"))
            {
                _DuelField.ArtPanel.SetActive(true);
                _DuelField.OshiPowerPanel.SetActive(false);
                _DuelField.CardEffectPanel.SetActive(false);

                Dictionary<string, List<GameObject>> energyAmount = CountCardAvaliableEnergy(thisCard);

                //for each art that the current card(CarditemList[currentIndex]) have, we need to instantiate a new card
                foreach (Art currentArt in CarditemList[currentIndex].Arts)
                {
                    if (currentArt.Name.Equals("Retreat") && !CarditemList[currentIndex].transform.parent.name.Equals("Stage"))
                        continue;

                    GameObject newItem = Instantiate(ArtPrefab, ArtPanel_Content.transform);
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


                    Button itemButton = newItem.GetComponent<Button>();
                    DuelAction duelaction = new() { usedCard = CardData.CreateCardDataFromCard(thisCard) };
                    duelaction.usedCard.cardPosition = thisCard.cardPosition;
                    if (IsCostCovered(currentArt.Cost, energyAmount, CarditemList[currentIndex])
                        && ((thisCard.cardPosition.Equals("Stage") && !_DuelField.centerStageArtUsed)
                        || (thisCard.cardPosition.Equals("Collaboration") && !_DuelField.collabStageArtUsed))
                        )
                    {
                        if (currentArt.Name.Equals("Retreat"))
                            itemButton.onClick.AddListener(() => OnItemClickRetrat(duelaction));
                        else
                            itemButton.onClick.AddListener(() => OnItemClickDeclareAttack(duelaction, itemButton));
                    }
                    else
                    {
                        newItem.transform.Find("ArtButton").GetComponent<Image>().color = new Color32(0x33, 0x33, 0x33, 0xFF);
                        itemButton.interactable = false;
                    }
                }
            }
            else if (CarditemList[currentIndex].cardType.Equals("推しホロメン") 
                && !isViewMode 
                && _DuelField._MatchConnection._DuelFieldData.currentGamePhase == DuelFieldData.GAMEPHASE.MainStep 
                && _DuelField._MatchConnection._DuelFieldData.currentPlayerTurn.Equals(_DuelField.PlayerInfo.PlayerID))
            {
                _DuelField.OshiPowerPanel.SetActive(true);
                _DuelField.CardEffectPanel.SetActive(false);
                _DuelField.ArtPanel.SetActive(false);

                if (CarditemList[currentIndex].transform.parent.parent.name.Equals("Oponente"))
                    return;

                Transform contentHolder = _DuelField.OshiPowerPanel.transform.Find("Viewport").Find("Content");

                string translatedOshiSkill = await GoogleTranslateAPI.TranslateTextHandle(CarditemList[currentIndex].oshiSkill);
                string translatedSpOshiSkill = await GoogleTranslateAPI.TranslateTextHandle(CarditemList[currentIndex].spOshiSkill);

                contentHolder.GetChild(0).transform.Find("ArtButton").Find("Effect").GetComponent<TMP_Text>().text = translatedOshiSkill;
                contentHolder.GetChild(1).transform.Find("ArtButton").Find("Effect").GetComponent<TMP_Text>().text = translatedSpOshiSkill;

                DuelAction duelaction = new() { usedCard = CardData.CreateCardDataFromCard(thisCard) };
                duelaction.usedCard.cardPosition = thisCard.cardPosition;



                if (!_DuelField.usedOshiSkill && CanActivateOshiSkill(duelaction.usedCard.cardNumber))
                {
                    contentHolder.GetChild(0).GetComponent<Button>().onClick.AddListener(() => OnItemClickOshiSkill(duelaction));
                    contentHolder.GetChild(0).transform.Find("ArtButton").GetComponent<Image>().color = Color.white;
                }
                else
                {
                    contentHolder.GetChild(0).GetComponent<Button>().onClick.RemoveAllListeners();
                    contentHolder.GetChild(0).transform.Find("ArtButton").GetComponent<Image>().color = new Color32(0x33, 0x33, 0x33, 0xFF);
                }

                if (!_DuelField.usedSPOshiSkill && CanActivateSPOshiSkill(duelaction.usedCard.cardNumber))
                {
                    contentHolder.GetChild(1).GetComponent<Button>().onClick.AddListener(() => OnItemClickSPOshiSkill(duelaction));
                    contentHolder.GetChild(1).transform.Find("ArtButton").GetComponent<Image>().color = Color.white;
                }
                else
                {
                    contentHolder.GetChild(1).transform.Find("ArtButton").GetComponent<Image>().color = new Color32(0x33, 0x33, 0x33, 0xFF);
                    contentHolder.GetChild(1).GetComponent<Button>().onClick.RemoveAllListeners();
                }
            }
            else
            {
                _DuelField.ArtPanel.SetActive(false);
                _DuelField.OshiPowerPanel.SetActive(false);
                _DuelField.CardEffectPanel.SetActive(true);

                CarditemList[currentIndex].GetCardInfo();

                //need to make this better later, get each card attack and match with its text effect
                string translatedArts = await GoogleTranslateAPI.TranslateTextHandle(CarditemList[currentIndex].artEffect.Replace(";", "\n"));
                string translatedOshiSkill = await GoogleTranslateAPI.TranslateTextHandle(CarditemList[currentIndex].oshiSkill);
                string translatedSpOshiSkill = await GoogleTranslateAPI.TranslateTextHandle(CarditemList[currentIndex].spOshiSkill);
                string translatedAbilityText = await GoogleTranslateAPI.TranslateTextHandle(CarditemList[currentIndex].abilityText);

                TMP_Text textComponent = CardPanel.GetComponentInChildren<TMP_Text>();
                textComponent.text = RemoveEmptyLines(
                    $"{(translatedArts ?? string.Empty)}" +
                    $"{(translatedOshiSkill != null ? "\n" + translatedOshiSkill : string.Empty)}" +
                    $"{(translatedSpOshiSkill != null ? "\n" + translatedSpOshiSkill : string.Empty)}" +
                    $"{(translatedAbilityText != null ? "\n" + translatedAbilityText : string.Empty)}" +
                    $"{(CarditemList[currentIndex].cardNumber != null ? "\n" + CarditemList[currentIndex].cardNumber : string.Empty)}"
                    );
            }
        }
        else
        {
            Debug.LogWarning("Current item is null.");
        }
    }
    private void OnItemClickRetrat(DuelAction duelaction)
    {
        StartCoroutine(_EffectController.RetreatArt(duelaction));
        CardPanel.gameObject.SetActive(false);
    }
    private void OnItemClickOshiSkill(DuelAction duelaction)
    {
        CardPanel.SetActive(false);
        StartCoroutine(_EffectController.OshiSkill(duelaction));
        GameObject HoloPower = _DuelField.GetZone("HoloPower", TargetPlayer.Player);
        /*for (int n = 0; n < HoloPowerCost(duelaction.usedCard.cardNumber, false); n++)
            _DuelField.SendCardToZone(HoloPower.transform.GetChild(HoloPower.transform.childCount - 1).gameObject, "Arquive", TargetPlayer.Player);*/
        _DuelField.usedOshiSkill = true;
    }
    private bool CanActivateOshiSkill(string cardNumber)
    {
        GameObject HoloPower = _DuelField.GetZone("HoloPower", TargetPlayer.Player);
        int holoPowerCount = _DuelField.GetZone("HoloPower", TargetPlayer.Player).transform.childCount -1;

        if (holoPowerCount < HoloPowerCost(cardNumber, false))
            return false;

        switch (cardNumber)
        {
            case "hSD01-001":
                //check if theres another holomem to replace energy
                int backstagecount = _DuelField.CountBackStageTotal(false, TargetPlayer.Player);
                if (_DuelField.GetZone("Stage", TargetPlayer.Player).GetComponentInChildren<Card>().attachedEnergy.Count < 1) return false;
                return (backstagecount > 0);
                break;
            case "xxx":
                break;
        }
        return true;
    }
    private void OnItemClickSPOshiSkill(DuelAction duelaction)
    {
        CardPanel.SetActive(false);
        StartCoroutine(_EffectController.SPOshiSkill(duelaction));
        GameObject HoloPower = _DuelField.GetZone("HoloPower", TargetPlayer.Player);
        /*for (int n = 0; n < HoloPowerCost(duelaction.usedCard.cardNumber, true); n++)
            _DuelField.SendCardToZone(HoloPower.transform.GetChild(HoloPower.transform.childCount - 1).gameObject, "Arquive", TargetPlayer.Player);*/
        _DuelField.usedSPOshiSkill = true;
    }
    private bool CanActivateSPOshiSkill(string cardNumber)
    {
        int holoPowerCount = _DuelField.GetZone("HoloPower", TargetPlayer.Player).transform.childCount - 1;

        if (holoPowerCount < HoloPowerCost(cardNumber, true))
            return false;

        switch (cardNumber)
        {
            case "hSD01-001":
                //check if opponent has another holomem to switch for the center
                int backstagecount = _DuelField.CountBackStageTotal(true, TargetPlayer.Oponnent);
                return (backstagecount > 0);
                break;
            case "xxx":
                break;
        }
        return true;
    }
    private void OnItemClickDeclareAttack(DuelAction duelaction, Button thisButton)
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

        duelaction.selectedSkill = thisButton.transform.Find("ArtButton").Find("Name").GetComponent<TMP_Text>().text;
        duelaction.actionType = "doArt";
        StartCoroutine(_DuelField_TargetForAttackMenu.SetupSelectableItems(duelaction, TargetPlayer.Oponnent));
        CardPanel.gameObject.SetActive(false);
    }
    private int HoloPowerCost(string cardNumber, bool SP = false)
    {
        if (TESTEMODE)
            return 0;

        if (SP)
            switch (cardNumber)
            {
                case "hSD01-001":
                    return 2;
            }
        if (!SP)
            switch (cardNumber)
            {
                case "hSD01-001":
                    return 1;
            }
        return 0;
    }

    private Dictionary<string, List<GameObject>> CountCardAvaliableEnergy(Card card)
    {
        Dictionary<string, List<GameObject>> energyAmount = new();
        foreach (GameObject energy in card.attachedEnergy)
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
    public bool IsCostCovered(List<(string Color, int Amount)> cost, Dictionary<string, List<GameObject>> energyAmount, Card SkillOwnerCard)
    {
        // Convert energyAmount to a payment list with color and total available amount.
        List<(string Color, int Amount)> payment = energyAmount
            .Select(entry => (Color: entry.Key, Amount: entry.Value.Count))
            .ToList();


        //check for equipaments effects
        foreach (GameObject cardObj in SkillOwnerCard.attachedEquipe)
        {
            Card card = cardObj.GetComponent<Card>();
            switch (card.cardNumber)
            {
                case "hBP01-126":
                    int index = payment.FindIndex(p => p.Color == "赤");
                    payment[index] = (payment[index].Color, (payment[index].Amount +1));
                    break;
                case "hBP01-118":
                    index = payment.FindIndex(p => p.Color == "白");
                    payment[index] = (payment[index].Color, (payment[index].Amount + 1));
                    break;
            }
        }

        foreach (var (Color, Amount) in cost.ToList())
        {
            int remainingAmount = Amount;

            // Step 1: Try to reduce cost using exact color match first.
            for (int i = 0; i < payment.Count && remainingAmount > 0; i++)
            {
                if (payment[i].Color.Equals(Color))
                {
                    int deductAmount = Math.Min(payment[i].Amount, remainingAmount);
                    payment[i] = (payment[i].Color, payment[i].Amount - deductAmount);
                    remainingAmount -= deductAmount;

                    // Remove exhausted payments.
                    if (payment[i].Amount == 0)
                        payment.RemoveAt(i--);
                }
            }

            // Step 2: If exact match did not fully cover the cost, use colorless payments as a fallback.
            if (remainingAmount > 0)
            {
                for (int i = 0; i < payment.Count && remainingAmount > 0; i++)
                {
                    if (Color.Equals("無色"))
                    {
                        int deductAmount = Math.Min(payment[i].Amount, remainingAmount);
                        payment[i] = (payment[i].Color, payment[i].Amount - deductAmount);
                        remainingAmount -= deductAmount;

                        // Remove exhausted payments.
                        if (payment[i].Amount == 0)
                            payment.RemoveAt(i--);
                    }
                }
            }

            // If we couldn't cover the entire cost, return false.
            if (remainingAmount > 0)
                return false;

            // Remove the fully covered cost from the list.
            cost.Remove((Color, Amount));
        }

        // If all costs are covered, return true.
        return true;
    }
    public static string RemoveEmptyLines(string input)
    {
        return string.Join("\n", input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                                      .Where(line => !string.IsNullOrWhiteSpace(line)));
    }
}
