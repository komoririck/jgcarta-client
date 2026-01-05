using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static DuelFieldData;

public class DuelfField_CardDetailViewer : MonoBehaviour
{
    public static DuelfField_CardDetailViewer INSTANCE;

    private List<Card> CarditemList = new ();
    private int currentIndex = 0;

    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;
    private bool isSwipe;

    static readonly List<GameObject> skillsToDestroy = new();

    DuelField_TargetForAttackMenu _DuelField_TargetForAttackMenu;

    [SerializeField] private GameObject ArtPrefab = null;

    private void Awake()
    {
        INSTANCE = this;
    }
    private void Start()
    {
        _DuelField_TargetForAttackMenu = FindAnyObjectByType<DuelField_TargetForAttackMenu>();
        DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel.transform.Find("Button").GetComponent<Button>().onClick.AddListener(() => { CloseDisplayed(); });
    }

    void Update()
    {
        DetectSwipe();
    }

    public void CloseDisplayed() 
    {
        DuelField_UI_MAP.INSTANCE.LoadAllPanelStatus().SetPanel(true, DuelField_UI_MAP.PanelType.SS_UI_General).SetPanel(false, DuelField_UI_MAP.PanelType.SS_BlockView);

        if (DuelField.INSTANCE.DUELFIELDDATA.currentGamePhase == GAMEPHASE.Mulligan) 
            DuelField_UI_MAP.INSTANCE.SetPanel(true, DuelField_UI_MAP.PanelType.SS_BlockView);
    }
    public void SetCardListToBeDisplayed(ref List<Card> _CarditemList, Card clickedCard, DuelField_HandClick.ClickAction _clickAction)
    {
        if (DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel.gameObject.activeInHierarchy == true)
            return;

        DuelField_UI_MAP.INSTANCE.SaveAllPanelStatus().DisableAllOther().SetPanel(true, DuelField_UI_MAP.PanelType.SS_EffectBoxes_CardPanel);

        if (_CarditemList == null || _CarditemList.Count() == 0)
        {
            Debug.LogWarning("Item list is null or empty.");
            return;
        }

        CarditemList = _CarditemList;
        currentIndex = _CarditemList.IndexOf(clickedCard);

        UpdateDisplayAsync(CarditemList,  clickedCard,  _clickAction);
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
            UpdateDisplayAsync(CarditemList, CarditemList[currentIndex], 0);
        }
    }

    public void SlidePrevious()
    {
        if (CarditemList.Count > 1)
        {
            currentIndex = (currentIndex - 1 + CarditemList.Count) % CarditemList.Count;
            UpdateDisplayAsync(CarditemList, CarditemList[currentIndex], 0);
        }
    }

    private async Task UpdateDisplayAsync(List<Card> _CarditemList, Card clickedCard, DuelField_HandClick.ClickAction clickAction)
    {
        Card thisCard = CarditemList[currentIndex];

        if (CarditemList[currentIndex] != null)
        {
            Card CurrentDisplayingCard = DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel.transform.Find("CardPanelInfo").GetComponent<Card>();
            CurrentDisplayingCard.Init(CarditemList[currentIndex].ToCardData());

            GameObject ArtPanel_Content = DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel_ArtPanel.transform.Find("Viewport").Find("Content").gameObject;
            // Clear existing items in the ArtPanel
            GameObjectExtensions.DestroyAllChildren(ArtPanel_Content);

            if (clickAction.Equals(DuelField_HandClick.ClickAction.ViewAndUseArt))
            {
                DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel_ArtPanel.SetActive(true);
                DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel_OshiPowerPanel.SetActive(false);
                DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel_CardEffectPanel.SetActive(false);

                Dictionary<string, List<GameObject>> energyAmount = thisCard.CountCardAvaliableEnergy();

                //for each art that the current card(CarditemList[currentIndex]) have, we need to instantiate a new card
                foreach (Art currentArt in CarditemList[currentIndex].Arts)
                {
                    if (currentArt.Name.Equals("Retreat") && !CarditemList[currentIndex].curZone.Equals(Lib.GameZone.Stage))
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
                    DuelAction duelaction = new() {playerID = DuelField.INSTANCE.DUELFIELDDATA.playersType[PlayerInfo.INSTANCE.PlayerID] };
                    duelaction.usedCard = thisCard.ToCardData();
                    
                    if (thisCard.IsCostCovered(currentArt.Cost, energyAmount)
                        && ((thisCard.curZone.Equals(Lib.GameZone.Stage) && !DuelField.INSTANCE.centerStageArtUsed)
                        || (thisCard.curZone.Equals(Lib.GameZone.Collaboration) && !DuelField.INSTANCE.collabStageArtUsed))
                        && thisCard.PassSpecialDeclareAttackCondition(currentArt)
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
            if (clickAction.Equals(DuelField_HandClick.ClickAction.ViewAndUseOshiBothSkills) || clickAction.Equals(DuelField_HandClick.ClickAction.ViewAndUseSPOshiSkill) || clickAction.Equals(DuelField_HandClick.ClickAction.ViewAndUseOshiBothSkills) )
            {
                DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel_ArtPanel.SetActive(false);
                DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel_OshiPowerPanel.SetActive(true);
                DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel_CardEffectPanel.SetActive(false);

                Transform contentHolder = DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel_OshiPowerPanel.transform.Find("Viewport").Find("Content");

                string translatedOshiSkill = await GoogleTranslateAPI.TranslateTextHandle(CarditemList[currentIndex].oshiSkill);
                string translatedSpOshiSkill = await GoogleTranslateAPI.TranslateTextHandle(CarditemList[currentIndex].spOshiSkill);

                contentHolder.GetChild(0).transform.Find("ArtButton").Find("Effect").GetComponent<TMP_Text>().text = translatedOshiSkill;
                contentHolder.GetChild(1).transform.Find("ArtButton").Find("Effect").GetComponent<TMP_Text>().text = translatedSpOshiSkill;

                DuelAction duelaction = new() { usedCard = thisCard.ToCardData() };
                duelaction.usedCard.curZone = thisCard.curZone;

                if (clickAction.Equals(DuelField_HandClick.ClickAction.ViewAndUseOshiBothSkills) || clickAction.Equals(DuelField_HandClick.ClickAction.ViewAndUseOshiBothSkills))
                {
                    contentHolder.GetChild(0).GetComponent<Button>().onClick.AddListener(() => OnItemClickOshiSkill(duelaction));
                    contentHolder.GetChild(0).transform.Find("ArtButton").GetComponent<Image>().color = Color.white;
                }
                else
                {
                    contentHolder.GetChild(0).GetComponent<Button>().onClick.RemoveAllListeners();
                    contentHolder.GetChild(0).transform.Find("ArtButton").GetComponent<Image>().color = new Color32(0x33, 0x33, 0x33, 0xFF);
                }
                if (clickAction.Equals(DuelField_HandClick.ClickAction.ViewAndUseSPOshiSkill) || clickAction.Equals(DuelField_HandClick.ClickAction.ViewAndUseOshiBothSkills))
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
            if (clickAction.Equals(DuelField_HandClick.ClickAction.OnlyView))
            {
                DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel_ArtPanel.SetActive(false);
                DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel_OshiPowerPanel.SetActive(false);
                DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel_CardEffectPanel.SetActive(true);

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
    }
    private void OnItemClickRetrat(DuelAction duelaction)
    {
        var list = CardLib.GetAndFilterCards(gameZones: DuelField.DEFAULTBACKSTAGE, player: DuelField.Player.Player);
        if (list == null || list.Count == 0)
            return;

        CloseDisplayed();
        DuelField.INSTANCE.centerStageArtUsed = true;
        DuelField.INSTANCE.GenericActionCallBack(duelaction, "Retreat");
    }
    private void OnItemClickOshiSkill(DuelAction duelaction)
    {
        CloseDisplayed();
        DuelField.INSTANCE.GenericActionCallBack(duelaction, "ResolveOnOshiEffect");
        GameObject HoloPower = DuelField.INSTANCE.GetZone(Lib.GameZone.HoloPower, DuelField.Player.Player);
        DuelField.INSTANCE.usedOshiSkill = true;

    }
    private void OnItemClickSPOshiSkill(DuelAction duelaction)
    {
        CloseDisplayed();
        DuelField.INSTANCE.GenericActionCallBack(duelaction, "ResolveOnOshiSPEffect");
        GameObject HoloPower = DuelField.INSTANCE.GetZone(Lib.GameZone.HoloPower, DuelField.Player.Player);
        DuelField.INSTANCE.usedSPOshiSkill = true;
    }
    private void OnItemClickDeclareAttack(DuelAction duelaction, Button thisButton)
    {
        CloseDisplayed();
        if (duelaction.usedCard.curZone.Equals(Lib.GameZone.Stage))
        {
            if (DuelField.INSTANCE.centerStageArtUsed)
                return;
            DuelField.INSTANCE.centerStageArtUsed = true;
        }
        else if (duelaction.usedCard.curZone.Equals(Lib.GameZone.Collaboration))
        {
            if (DuelField.INSTANCE.collabStageArtUsed)
                return;
            DuelField.INSTANCE.collabStageArtUsed = true;
        }

        duelaction.selectedSkill = thisButton.transform.Find("ArtButton").Find("Name").GetComponent<TMP_Text>().text;
        StartCoroutine(_DuelField_TargetForAttackMenu.SetupSelectableItems(duelaction, DuelField.Player.Oponnent, performArt: true));

    }
    public static string RemoveEmptyLines(string input)
    {
        return string.Join("\n", input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                                      .Where(line => !string.IsNullOrWhiteSpace(line)));
    }

}
