using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DuelfField_CardDetailViewer : MonoBehaviour
{
    public static DuelfField_CardDetailViewer INSTANCE;

    private List<Card> _carditemList = new();
    private int _currentIndex = 0;
    private List<int> _ArtIndex = new();

    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;
    private bool isSwipe;

    static readonly List<GameObject> skillsToDestroy = new();

    [SerializeField] private GameObject ArtPrefab = null;

    private void Awake()
    {
        INSTANCE = this;
    }
    private void Start()
    {
        DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel.transform.Find("Button").GetComponent<Button>().onClick.AddListener(() => { CloseDisplayed(); });
    }

    void Update()
    {
        DetectSwipe();
    }

    public void CloseDisplayed()
    {
        DuelField_UI_MAP.INSTANCE.LoadAllPanelStatus().SetPanel(true, DuelField_UI_MAP.PanelType.SS_UI_General).SetPanel(false, DuelField_UI_MAP.PanelType.SS_BlockView);

        if (DuelField.INSTANCE.GamePhase == GAMEPHASE.Mulligan)
            DuelField_UI_MAP.INSTANCE.SetPanel(true, DuelField_UI_MAP.PanelType.SS_BlockView);
    }
    public void SetCardListToBeDisplayed(ref List<Card> carditemList, int index, HandClick.ClickAction clickAction, List<int> ArtIndex)
    {
        if (DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel.gameObject.activeInHierarchy == true)
            return;

        DuelField_UI_MAP.INSTANCE.SaveAllPanelStatus().DisableAllOther().SetPanel(true, DuelField_UI_MAP.PanelType.SS_EffectBoxes_CardPanel);

        _carditemList = carditemList;
        _currentIndex = index;
        _ArtIndex = ArtIndex;

        UpdateDisplayAsync(clickAction);
    }
    private async Task UpdateDisplayAsync(HandClick.ClickAction clickAction)
    {
        DuelAction duelaction = new() { player = DuelField.INSTANCE.playersType[PlayerInfo.INSTANCE.PlayerID], used = _carditemList[_currentIndex].ToCardData() };

        if (_carditemList[_currentIndex] != null)
        {
            Card CurrentDisplayingCard = DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel.transform.Find("CardPanelInfo").GetComponent<Card>();
            CurrentDisplayingCard.Init(_carditemList[_currentIndex].ToCardData());

            GameObject ArtPanel_Content = DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel_ArtPanel.transform.Find("Viewport").Find("Content").gameObject;
            // Clear existing items in the ArtPanel
            GameObjectExtensions.DestroyAllChildren(ArtPanel_Content);

            if (clickAction.Equals(HandClick.ClickAction.ViewAndUseArt))
            {
                DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel_ArtPanel.SetActive(true);
                DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel_OshiPowerPanel.SetActive(false);
                DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel_CardEffectPanel.SetActive(false);

                int counter = 0;
                foreach (int n in _ArtIndex)
                {
                    GameObject newItem = Instantiate(ArtPrefab, ArtPanel_Content.transform);
                    skillsToDestroy.Add(newItem);

                    var currentArt = _carditemList[_currentIndex].Arts[counter];

                    newItem.transform.Find("ArtButton").Find("Name").GetComponent<TMP_Text>().text = currentArt.Name;
                    newItem.transform.Find("ArtButton").Find("Effect").GetComponent<TMP_Text>().text = await GoogleTranslateAPI.TranslateTextHandle(currentArt.Effect);
                    newItem.transform.Find("ArtButton").Find("Damage").GetComponent<TMP_Text>().text = $"{currentArt.Damage} <{(currentArt.Tokkou != null ? currentArt.Tokkou.Color.ToString() : string.Empty)} {(currentArt.Tokkou != null ? currentArt.Tokkou.Amount : string.Empty)}>";

                    string costString = string.Empty;
                    foreach (var c in currentArt.Cost)
                        costString += c.Amount + c.Color.ToString();
                    newItem.transform.Find("ArtButton").Find("Cost").GetComponent<TMP_Text>().text = costString;


                    Button itemButton = newItem.GetComponent<Button>();
                    if (n == 1)
                    {
                        if (currentArt.Name.Equals("Retreat"))
                            itemButton.onClick.AddListener(() =>
                            {
                                CloseDisplayed();
                                MatchConnection.INSTANCE.SendRequest(duelaction, "Retreat");
                            });
                        else
                            itemButton.onClick.AddListener(() =>
                            {
                                CloseDisplayed();
                                duelaction.selectedSkill = _carditemList[_currentIndex].Arts[counter].Name;
                                StartCoroutine(DuelField_TargetForAttackMenu.INSTANCE.SetupSelectableItems(duelaction, DuelField.Player.Oponnent, performArt: true));
                            });
                    }
                    else
                    {
                        newItem.transform.Find("ArtButton").GetComponent<Image>().color = new Color32(0x33, 0x33, 0x33, 0xFF);
                        itemButton.interactable = false;
                    }
                    counter++;
                }
            }
            else if (clickAction.Equals(HandClick.ClickAction.ViewAndUseOshiBothSkills))
            {
                DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel_ArtPanel.SetActive(false);
                DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel_OshiPowerPanel.SetActive(true);
                DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel_CardEffectPanel.SetActive(false);

                Transform contentHolder = DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel_OshiPowerPanel.transform.Find("Viewport").Find("Content");

                string translatedOshiSkill = "";
                string translatedSpOshiSkill = "";

                if (_carditemList[_currentIndex].oshiSkill != null)
                    translatedOshiSkill = await GoogleTranslateAPI.TranslateTextHandle(_carditemList[_currentIndex].oshiSkill.effect_text);

                if (_carditemList[_currentIndex].spOshiSkill != null)
                    translatedSpOshiSkill = await GoogleTranslateAPI.TranslateTextHandle(_carditemList[_currentIndex].spOshiSkill.effect_text);

                contentHolder.GetChild(0).transform.Find("ArtButton").Find("Effect").GetComponent<TMP_Text>().text = $"{_carditemList[_currentIndex].oshiSkill.cost}\n{_carditemList[_currentIndex].oshiSkill.name}\n{translatedOshiSkill}";
                contentHolder.GetChild(1).transform.Find("ArtButton").Find("Effect").GetComponent<TMP_Text>().text = $"{_carditemList[_currentIndex].oshiSkill.cost}\n{_carditemList[_currentIndex].oshiSkill.name}\n{translatedOshiSkill}";

                contentHolder.GetChild(0).GetComponent<Button>().onClick.RemoveAllListeners();
                contentHolder.GetChild(0).transform.Find("ArtButton").GetComponent<Image>().color = new Color32(0x33, 0x33, 0x33, 0xFF);

                contentHolder.GetChild(1).transform.Find("ArtButton").GetComponent<Image>().color = new Color32(0x33, 0x33, 0x33, 0xFF);
                contentHolder.GetChild(1).GetComponent<Button>().onClick.RemoveAllListeners();


                if (_ArtIndex[0] == 1)
                {
                    contentHolder.GetChild(0).GetComponent<Button>().onClick.AddListener(() =>
                    {
                        CloseDisplayed();
                        MatchConnection.INSTANCE.SendRequest(duelaction, "ResolveOnOshiEffect");
                    });
                    contentHolder.GetChild(0).transform.Find("ArtButton").GetComponent<Image>().color = Color.white;
                }

                if (_ArtIndex[1] == 1)
                {
                    contentHolder.GetChild(1).GetComponent<Button>().onClick.AddListener(() =>
                    {
                        CloseDisplayed();
                        MatchConnection.INSTANCE.SendRequest(duelaction, "ResolveOnOshiSPEffect");

                    });
                    contentHolder.GetChild(1).transform.Find("ArtButton").GetComponent<Image>().color = Color.white;
                }
            }
            else if (clickAction.Equals(HandClick.ClickAction.OnlyView))
            {
                DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel_ArtPanel.SetActive(false);
                DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel_OshiPowerPanel.SetActive(false);
                DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel_CardEffectPanel.SetActive(true);


                string translatedArts = "";
                string translatedOshiSkill = "";
                string translatedSpOshiSkill = "";
                string translatedAbilityText = "";
                string translatedGiftText = "";
                string translatedExtraText = "";

                //need to make this better later, get each card attack and match with its text effect
                if (_carditemList[_currentIndex].Arts != null)
                    foreach (var art in _carditemList[_currentIndex].Arts) 
                    {
                        string cost = "";
                        foreach (var cst in art.Cost)
                            cost += $"{cst.Color}+{cst.Amount}";
                        translatedArts += $"{art.Name}-{await GoogleTranslateAPI.TranslateTextHandle(art.Effect)}-{cost}-{art.Damage}\n";
                    }

                if (_carditemList[_currentIndex].oshiSkill != null)
                    translatedOshiSkill = $"Cost:{_carditemList[_currentIndex].oshiSkill.cost}\n{_carditemList[_currentIndex].oshiSkill.name}\n{await GoogleTranslateAPI.TranslateTextHandle(_carditemList[_currentIndex].oshiSkill.effect_text)}";

                if (_carditemList[_currentIndex].spOshiSkill != null)
                    translatedSpOshiSkill = $"Cost:{_carditemList[_currentIndex].spOshiSkill.cost}\n{_carditemList[_currentIndex].spOshiSkill.name}\n{await GoogleTranslateAPI.TranslateTextHandle(_carditemList[_currentIndex].spOshiSkill.effect_text)}";

                if (_carditemList[_currentIndex].AbilityText != null)
                    foreach (var effect in _carditemList[_currentIndex].AbilityText)
                        translatedAbilityText += $"{await GoogleTranslateAPI.TranslateTextHandle(effect)}\n";

                if (_carditemList[_currentIndex].Gift != null)
                    translatedGiftText = $"{_carditemList[_currentIndex].Gift.Type}\n{_carditemList[_currentIndex].Gift.Name}\n{await GoogleTranslateAPI.TranslateTextHandle(_carditemList[_currentIndex].Gift.Text)}";

                if (_carditemList[_currentIndex].Extra != null)
                    foreach (var effect in _carditemList[_currentIndex].Extra)
                        translatedExtraText += $"{await GoogleTranslateAPI.TranslateTextHandle(effect)}\n";

                TMP_Text textComponent = DuelField_UI_MAP.INSTANCE.SS_EffectBoxes_CardPanel.GetComponentInChildren<TMP_Text>();
                textComponent.text = RemoveEmptyLines(
                    $"{(translatedArts ?? string.Empty)}" +
                    $"{(translatedOshiSkill != null ? "\n" + translatedOshiSkill : string.Empty)}" +
                    $"{(translatedSpOshiSkill != null ? "\n" + translatedSpOshiSkill : string.Empty)}" +
                    $"{(translatedAbilityText != null ? "\n" + translatedAbilityText : string.Empty)}" +
                    $"{(translatedGiftText != null ? "\n" + translatedGiftText : string.Empty)}" +
                    $"{(translatedExtraText != null ? "\n" + translatedExtraText : string.Empty)}" +
                    $"{(_carditemList[_currentIndex].cardNumber != null ? "\n" + _carditemList[_currentIndex].cardNumber : string.Empty)}"
                    );
            }
        }
    }
    public static string RemoveEmptyLines(string input)
    {
        return string.Join("\n", input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Where(line => !string.IsNullOrWhiteSpace(line)));
    }
    //-----------------------------------------DetectSwipe-----------------------------------------//
    private void DetectSwipe()
    {
        if (Input.GetMouseButtonDown(0))
        {
            startTouchPosition = Input.mousePosition;
            isSwipe = true;
        }

        if (Input.GetMouseButtonUp(0) && isSwipe)
        {
            endTouchPosition = Input.mousePosition;
            Vector2 swipeDelta = endTouchPosition - startTouchPosition;

            if (swipeDelta.magnitude > 50) // Adjust threshold as needed
            {
                if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
                {
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
            isSwipe = false;
        }
    }
    public void SlideNext()
    {
        if (_carditemList.Count > 1)
        {
            _currentIndex = (_currentIndex + 1) % _carditemList.Count;
            UpdateDisplayAsync(0);
        }
    }
    public void SlidePrevious()
    {
        if (_carditemList.Count > 1)
        {
            _currentIndex = (_currentIndex - 1 + _carditemList.Count) % _carditemList.Count;
            UpdateDisplayAsync(0);
        }
    }
}
