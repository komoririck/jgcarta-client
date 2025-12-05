using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

public class DuelfField_CardDetailViewer : MonoBehaviour
{
    public const bool TESTEMODE = false;

    private List<Card> CarditemList = new (); // Internal list of GameObjects with Image components
    private int currentIndex = 0;

    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;
    private bool isSwipe;

    static readonly List<GameObject> skillsToDestroy = new();
    bool isViewMode = true;

    DuelField_TargetForAttackMenu _DuelField_TargetForAttackMenu;

    [SerializeField] private GameObject ArtPrefab = null;

    private void Start()
    {
        _DuelField_TargetForAttackMenu = FindAnyObjectByType<DuelField_TargetForAttackMenu>();
        DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel.transform.Find("Button").GetComponent<Button>().onClick.AddListener(() => { CloseDisplayed(); });
    }

    void Update()
    {
        DetectSwipe();
    }

    public void CloseDisplayed() {
        DuelField_UI_MAP.INSTANCE.LoadAllPanelStatus().SetPanel(true, DuelField_UI_MAP.PanelType.SS_UI_General).SetPanel(false, DuelField_UI_MAP.PanelType.SS_BlockView);
    }

    public void SetCardListToBeDisplayed(ref List<Card> _CarditemList, bool viewmode, Card clickedCard)
    {

        if (_CarditemList[0].transform.parent.name.Equals("PlayerHand") || _CarditemList[0].transform.parent.name.Equals("Content"))
        {
            int n = 0;
            foreach (Card card in _CarditemList)
            {
                if (card == clickedCard)
                    break;
                n++;
            }
            currentIndex = n;

        }
        else {
            _CarditemList.Reverse();
            currentIndex = 0;
        }

        isViewMode = viewmode;

        if (DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel.gameObject.activeInHierarchy == true)
            return;

        DuelField_UI_MAP.INSTANCE.SaveAllPanelStatus().DisableAllOther().SetPanel(true, DuelField_UI_MAP.PanelType.SS_EffectBoxes_CardPanel);

        if (_CarditemList == null || _CarditemList.Count() == 0)
        {
            Debug.LogWarning("Item list is null or empty.");
            return;
        }

        CarditemList = _CarditemList;
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
            Card CurrentDisplayingCard = DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel.transform.Find("CardPanelInfo").GetComponent<Card>();
            CurrentDisplayingCard.Init(CarditemList[currentIndex].ToCardData()).Flip();

            GameObject ArtPanel_Content = DuelField.INSTANCE.ArtPanel.transform.Find("Viewport").Find("Content").gameObject;
            // Clear existing items in the ArtPanel
            GameObjectExtensions.DestroyAllChildren(ArtPanel_Content);

            if ((CarditemList[currentIndex].cardType.Equals("ホロメン") || CarditemList[currentIndex].cardType.Equals("Buzzホロメン"))
                && CarditemList[currentIndex].curZone.Equals(Lib.GameZone.Collaboration) || CarditemList[currentIndex].curZone.Equals(Lib.GameZone.Stage)
                && DuelField.INSTANCE.duelFieldData.currentGamePhase == DuelFieldData.GAMEPHASE.MainStep
                && DuelField.INSTANCE.duelFieldData.currentPlayerTurn.Equals(PlayerInfo.INSTANCE.PlayerID)
                && !isViewMode
                && CarditemList[currentIndex].transform.parent.parent.name.Equals("PlayerGeneral")
                )
            {
                DuelField.INSTANCE.ArtPanel.SetActive(true);
                DuelField.INSTANCE.OshiPowerPanel.SetActive(false);
                DuelField.INSTANCE.CardEffectPanel.SetActive(false);

                Dictionary<string, List<GameObject>> energyAmount = CountCardAvaliableEnergy(thisCard);

                //for each art that the current card(CarditemList[currentIndex]) have, we need to instantiate a new card
                foreach (Art currentArt in CarditemList[currentIndex].Arts)
                {
                    if (currentArt.Name.Equals("Retreat") && !CarditemList[currentIndex].transform.parent.name.Equals(Lib.GameZone.Stage))
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
                    DuelAction duelaction = new() { usedCard = thisCard.ToCardData() };
                    duelaction.usedCard.curZone = thisCard.curZone;
                    if (IsCostCovered(currentArt.Cost, energyAmount, CarditemList[currentIndex])
                        && ((thisCard.curZone.Equals(Lib.GameZone.Stage) && !DuelField.INSTANCE.centerStageArtUsed)
                        || (thisCard.curZone.Equals(Lib.GameZone.Collaboration) && !DuelField.INSTANCE.collabStageArtUsed))
                        && PassSpecialDeclareAttackCondition(CarditemList[currentIndex], currentArt)
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
                && DuelField.INSTANCE.duelFieldData.currentGamePhase == DuelFieldData.GAMEPHASE.MainStep 
                && DuelField.INSTANCE.duelFieldData.currentPlayerTurn.Equals(PlayerInfo.INSTANCE.PlayerID))
            {
                DuelField.INSTANCE.OshiPowerPanel.SetActive(true);
                DuelField.INSTANCE.CardEffectPanel.SetActive(false);
                DuelField.INSTANCE.ArtPanel.SetActive(false);

                if (CarditemList[currentIndex].transform.parent.parent.name.Equals("Oponente"))
                    return;

                Transform contentHolder = DuelField.INSTANCE.OshiPowerPanel.transform.Find("Viewport").Find("Content");

                string translatedOshiSkill = await GoogleTranslateAPI.TranslateTextHandle(CarditemList[currentIndex].oshiSkill);
                string translatedSpOshiSkill = await GoogleTranslateAPI.TranslateTextHandle(CarditemList[currentIndex].spOshiSkill);

                contentHolder.GetChild(0).transform.Find("ArtButton").Find("Effect").GetComponent<TMP_Text>().text = translatedOshiSkill;
                contentHolder.GetChild(1).transform.Find("ArtButton").Find("Effect").GetComponent<TMP_Text>().text = translatedSpOshiSkill;

                DuelAction duelaction = new() { usedCard = thisCard.ToCardData() };
                duelaction.usedCard.curZone = thisCard.curZone;



                if (!DuelField.INSTANCE.usedOshiSkill && CanActivateOshiSkill(duelaction.usedCard.cardNumber))
                {
                    contentHolder.GetChild(0).GetComponent<Button>().onClick.AddListener(() => OnItemClickOshiSkill(duelaction));
                    contentHolder.GetChild(0).transform.Find("ArtButton").GetComponent<Image>().color = Color.white;
                }
                else
                {
                    contentHolder.GetChild(0).GetComponent<Button>().onClick.RemoveAllListeners();
                    contentHolder.GetChild(0).transform.Find("ArtButton").GetComponent<Image>().color = new Color32(0x33, 0x33, 0x33, 0xFF);
                }

                if (!DuelField.INSTANCE.usedSPOshiSkill && CanActivateSPOshiSkill(duelaction.usedCard.cardNumber))
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
                DuelField.INSTANCE.ArtPanel.SetActive(false);
                DuelField.INSTANCE.OshiPowerPanel.SetActive(false);
                DuelField.INSTANCE.CardEffectPanel.SetActive(true);

                CarditemList[currentIndex].Init(CarditemList[currentIndex].ToCardData());

                //need to make this better later, get each card attack and match with its text effect
                string translatedArts = await GoogleTranslateAPI.TranslateTextHandle(CarditemList[currentIndex].artEffect.Replace(";", "\n"));
                string translatedOshiSkill = await GoogleTranslateAPI.TranslateTextHandle(CarditemList[currentIndex].oshiSkill);
                string translatedSpOshiSkill = await GoogleTranslateAPI.TranslateTextHandle(CarditemList[currentIndex].spOshiSkill);
                string translatedAbilityText = await GoogleTranslateAPI.TranslateTextHandle(CarditemList[currentIndex].abilityText);

                TMP_Text textComponent = DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel.GetComponentInChildren<TMP_Text>();
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
    private bool PassSpecialDeclareAttackCondition(Card card, Art currentArt)
    {
        switch (card.cardNumber + "+" + currentArt.Name) {
            case "hBP01-070+共依存":
                foreach (GameObject _card in card.attachedEquipe)
                    if (_card.GetComponent<Card>().cardName.Equals("座員"))
                        return true;
                return false;
        }
        return true;
    }
    private void OnItemClickRetrat(DuelAction duelaction)
    {
        CloseDisplayed();
        StartCoroutine(EffectController.INSTANCE.RetreatArt(duelaction));
    }
    private void OnItemClickOshiSkill(DuelAction duelaction)
    {
        CloseDisplayed();
        StartCoroutine(EffectController.INSTANCE.OshiSkill(duelaction));
        GameObject HoloPower = DuelField.INSTANCE.GetZone(Lib.GameZone.HoloPower, DuelField.TargetPlayer.Player);
        /*for (int n = 0; n < HoloPowerCost(duelaction.usedCard.cardNumber, false); n++)
            DuelField.INSTANCE.SendCardToZone(HoloPower.transform.GetChild(HoloPower.transform.childCount - 1).gameObject, Lib.GameZone.Arquive, DuelField.TargetPlayer.Player);*/
        DuelField.INSTANCE.usedOshiSkill = true;

    }
    private bool CanActivateOshiSkill(string cardNumber)
    {
        GameObject HoloPower = DuelField.INSTANCE.GetZone(Lib.GameZone.HoloPower, DuelField.TargetPlayer.Player);
        int holoPowerCount = DuelField.INSTANCE.GetZone(Lib.GameZone.HoloPower, DuelField.TargetPlayer.Player).transform.childCount -1;

        if (holoPowerCount < HoloPowerCost(cardNumber, false))
            return false;

        switch (cardNumber)
        {
            case "hSD01-001":
                //check if theres another holomem to replace energy
                int backstagecount = DuelField.INSTANCE.CountBackStageTotal(false, DuelField.TargetPlayer.Player);
                if (DuelField.INSTANCE.GetZone(Lib.GameZone.Stage, DuelField.TargetPlayer.Player).GetComponentInChildren<Card>().attachedEnergy.Count < 1) return false;
                return (backstagecount > 0);
                break;
            case "xxx":
                break;
        }
        return true;
    }
    private void OnItemClickSPOshiSkill(DuelAction duelaction)
    {
        CloseDisplayed();
        StartCoroutine(EffectController.INSTANCE.SPOshiSkill(duelaction));
        GameObject HoloPower = DuelField.INSTANCE.GetZone(Lib.GameZone.HoloPower, DuelField.TargetPlayer.Player);
        /*for (int n = 0; n < HoloPowerCost(duelaction.usedCard.cardNumber, true); n++)
            DuelField.INSTANCE.SendCardToZone(HoloPower.transform.GetChild(HoloPower.transform.childCount - 1).gameObject, Lib.GameZone.Arquive, DuelField.TargetPlayer.Player);*/
        DuelField.INSTANCE.usedSPOshiSkill = true;
    }
    private bool CanActivateSPOshiSkill(string cardNumber)
    {
        int holoPowerCount = DuelField.INSTANCE.GetZone(Lib.GameZone.HoloPower, DuelField.TargetPlayer.Player).transform.childCount - 1;

        if (holoPowerCount < HoloPowerCost(cardNumber, true))
            return false;

        switch (cardNumber)
        {
            case "hSD01-001":
                //check if opponent has another holomem to switch for the center
                int backstagecount = DuelField.INSTANCE.CountBackStageTotal(true, DuelField.TargetPlayer.Oponnent);
                return (backstagecount > 0);
            case "hYS01-003":
                foreach (Card card in DuelField.INSTANCE.GetZone(Lib.GameZone.Arquive, DuelField.TargetPlayer.Player).GetComponentsInChildren<Card>())
                    if (card.cardType.Equals("ホロメン") || card.cardType.Equals("Buzzホロメン"))
                        return true;
                    return false;
            case "hSD01-002":
                foreach (Card card in DuelField.INSTANCE.GetZone(Lib.GameZone.Arquive, DuelField.TargetPlayer.Player).GetComponentsInChildren<Card>())
                    if (card.cardType.Equals("エール") && new EffectController().GetAreasThatContainsCardWithColorOrTagOrName(color: "緑").Length > 0)
                        return true;
                return false;
        }
        return true;
    }
    private void OnItemClickDeclareAttack(DuelAction duelaction, Button thisButton)
    {
        CloseDisplayed();
        if (duelaction.usedCard.curZone.Equals(Lib.GameZone.Stage))
        {
            if (DuelField.INSTANCE.centerStageArtUsed)
                return;
        }
        else if (duelaction.usedCard.curZone.Equals(Lib.GameZone.Collaboration))
        {
            if (DuelField.INSTANCE.collabStageArtUsed)
                return;
        }

        duelaction.selectedSkill = thisButton.transform.Find("ArtButton").Find("Name").GetComponent<TMP_Text>().text;
        duelaction.actionType = "doArt";
        StartCoroutine(_DuelField_TargetForAttackMenu.SetupSelectableItems(duelaction, DuelField.TargetPlayer.Oponnent));

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
