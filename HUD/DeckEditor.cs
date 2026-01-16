using Assets.Scripts.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Card;
using static Deck;

public class DeckEditor : MonoBehaviour
{
    [SerializeField] private GameObject DeckContent = null;
    [SerializeField] private GameObject EnergyContent = null;
    [SerializeField] private GameObject CardViewContent = null;
    [SerializeField] private GameObject cardPrefab = null;
    [SerializeField] private TMP_InputField textBox = null;

    [SerializeField] private GameObject WaitingResponsePainel = null;

    [SerializeField] private GameObject MessageBox = null;
    [SerializeField] private TMP_Text MessageBoxText = null;


    private CardPool cardPool; // Reference to your CardPool
    List<Card> DeckCardList = new();
    List<Card> EnergyCardList = new();
    [SerializeField] Card OshiCard;
    [SerializeField] string DeckName = null;


    public Transform displayArea;  // Area where cards will be displayed
    private int maxVisibleCards = 10;  // Example max visible count

    private HTTPSMaker _HTTPSMaker;

    public float doubleClickTime = 0.3f; // Max time between clicks to register a double-click
    private float lastClickTime;
    private bool isOneClick = false;

    [SerializeField] public GameObject CardDetailPanel;
    Card clickedCard;

    DeckInfo _DeckData;

    [SerializeField] public GameObject ActiveDeckButton;

    private void Start()
    {
        Button btn = OshiCard.AddComponent<Button>();
        btn.onClick.AddListener(() => HandleCardClick(OshiCard.gameObject));

        _HTTPSMaker = FindAnyObjectByType<HTTPSMaker>();

        // Initialize card pool if not assigned and ensure a CardPool exists in the scene
        if (cardPool == null)
        {
            cardPool = FindObjectOfType<CardPool>();
            if (cardPool == null)
            {
                Debug.LogError("CardPool is not initialized. Make sure the CardPool script is attached to a GameObject in the scene.");
                return;
            }
        }

        // Set up search box input and clear button functionality
        textBox.onValueChanged.AddListener(OnSearchInputChanged);

        if ((_DeckData = FindAnyObjectByType<DeckInfo>()) != null)
            GetDeckInfo(_DeckData);

        ActiveDeckButton.AddComponent<Button>().onClick.AddListener(() => SetDeckAsActiveRequest());
        ActiveDeckButton.active = false;

        if (string.IsNullOrEmpty(_DeckData.status)) {
            ActiveDeckButton.active = true;
        }

        UpdateDisplay(null);
    }
    private void UpdateDisplay(string filter)
    {
        if (cardPool == null)
        {
            Debug.LogError("CardPool is not initialized.");
            return;
        }

        // Clear existing cards by returning them to the card pool
        foreach (Transform child in CardViewContent.transform)
        {
            cardPool.ReturnCard(child.gameObject);  // Recycle cards instead of destroying
        }

        List<Record> query = string.IsNullOrEmpty(filter)
            ? FileReader.result.AsQueryable().Select(r => r.Value).ToList()
            : new() { FileReader.result[filter] };

        // Display each matching card
        foreach (Record record in query)
        {
            GameObject newCardGameObject = cardPool.GetCard();
            if (newCardGameObject == null)
                continue;

            newCardGameObject.transform.SetParent(CardViewContent.transform, false);
            Card newCard = newCardGameObject.GetComponent<Card>().Init(newCardGameObject.GetComponent<Card>().ToCardData());

            // Set up button listener for each card to add it to the deck
            Button button = newCardGameObject.GetComponent<Button>() ?? newCardGameObject.AddComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SetDeckAsActiveRequest());
        }
    }
    private void EnsureCardPool(int count)
    {
        while (cardPool.PoolSize < count)  // Use PoolSize or similar property of CardPool to track pool size
        {
            GameObject newCard = Instantiate(cardPrefab);
            newCard.SetActive(false);
            //cardPool.AddCard(newCard);  // Assume AddCard is a method in CardPool to add new cards
        }
    }
    public void ClearDisplay()
    {
        foreach (GameObject cardObj in cardPool.GetAllCards())  // Assuming GetAllCards returns all pooled cards
        {
            cardObj.SetActive(false);  // Hide all pooled cards when clearing the display
        }
    }
    private void OnSearchInputChanged(string value)
    {
        UpdateDisplay(value);
    }
    public void ClearTextButton()
    {
        textBox.text = null;
        UpdateDisplay(null);
    }
    void HandleCardClick(GameObject thisCard)
    {
        if (isOneClick && (Time.time - lastClickTime) < doubleClickTime)
        {
            // Double-click detected
            isOneClick = false;
            lastClickTime = 0;

            if (thisCard.transform.parent.parent.parent.name.Equals("Deck Scroll View") || thisCard.transform.parent.parent.parent.name.Equals("Energy Scroll View"))
            {
                RemoveCardFromDeck(thisCard);
            }
            else
            {
                AddCardToDeck(thisCard);
            }
        }
        else
        {
            clickedCard = thisCard.GetComponentInChildren<Card>();

            // First click detected
            isOneClick = true;
            lastClickTime = Time.time;
            Invoke(nameof(SingleClickConfirmed), doubleClickTime);
        }
    }

    void SingleClickConfirmed()
    {
        if (isOneClick)
        {
            isOneClick = false;
            OpenCardDetailMenu(); // Show the card menu if single-clicked
        }
    }

    void AddCardToDeck(GameObject thisCard)
    {
        //instantite the object to be add
        Card ClickedCard = thisCard.GetComponent<Card>();
        GameObject newCard = Instantiate(cardPrefab, DeckContent.transform);
        Card CardToAdd = newCard.GetComponent<Card>();
        Button button = newCard.AddComponent<Button>();
        button.onClick.AddListener(() => HandleCardClick(newCard));

        CardToAdd.cardNumber = ClickedCard.cardNumber;
        //determine which list it belongs
        if (CardToAdd.cardType.Equals("エール"))
        {
            newCard.transform.SetParent(EnergyContent.transform);
            if (EnergyCardList.Count > 19)
            {
                Destroy(newCard);
                EnergyCardList.Remove(CardToAdd);
            }
            else
            {
                EnergyCardList.Add(CardToAdd);
            }
        }
        else if (CardToAdd.cardType.Equals("推しホロメン"))
        {
            OshiCard.cardNumber = CardToAdd.cardNumber;
            Destroy(newCard);
        }
        else
        {
            if (DeckCardList.Count > 49)
            {
                Destroy(newCard);
                DeckCardList.Remove(CardToAdd);
            }
            else
            {
                DeckCardList.Add(CardToAdd);
            }
        }
    }

    void RemoveCardFromDeck(GameObject thisCard)
    {
        //destroy the click object
        Card card = thisCard.GetComponent<Card>();
        if (thisCard.transform.parent.parent.parent.name.Equals("Deck Scroll View"))
        {
            DeckCardList.Remove(card);
        }
        else
        {
            EnergyCardList.Remove(card);
        }
        Destroy(thisCard);
    }

    void OpenCardDetailMenu()
    {
        CardDetailPanel.SetActive(true);
        Card card = CardDetailPanel.transform.Find("CardPanelInfo").GetComponent<Card>().Init(new () {cardNumber = clickedCard.cardNumber});
    }

    public void ImportDeckFromClipBoard()
    {
        string clipboardContent = GUIUtility.systemCopyBuffer;

        string[] lines = clipboardContent.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        string currentTag = null;

        // Define valid tags
        HashSet<string> validTags = new HashSet<string> { "#created by", "#main", "#energy", "#oshi" };

        foreach (GameObject obj in DeckContent.transform)
        {
            Destroy(obj);
            DeckCardList.Clear();
        }
        foreach (GameObject obj in EnergyContent.transform)
        {
            Destroy(obj);
            EnergyCardList.Clear();
        }

        foreach (string line in lines)
        {
            if (line.StartsWith("#"))
            {
                // Validate the tag
                if (validTags.Contains(line.Trim()))
                {
                    currentTag = line.Trim();
                }
                else
                {
                    GenericButton.DisplayPopUp(MessageBox, MessageBoxText, "invalid text format");
                    return;
                }
                continue;
            }

            Card CardToAdd = null;
            GameObject newCardGameObject = null;
            if (currentTag.Equals("#main") || currentTag.Equals("#energy"))
            {
                newCardGameObject = Instantiate(cardPrefab, DeckContent.transform);
                CardToAdd = newCardGameObject.GetComponent<Card>();
                Button button = newCardGameObject.AddComponent<Button>();
                button.onClick.AddListener(() => HandleCardClick(newCardGameObject));

            }

            switch (currentTag)
            {
                case "#created by":
                    DeckName = line.Trim();
                    break;
                case "#main":
                    CardToAdd.cardNumber = line.Trim();
                    if (DeckCardList.Count > 49 && !string.IsNullOrEmpty(CardToAdd.cardName))
                        Destroy(newCardGameObject);
                    else
                        DeckCardList.Add(CardToAdd);
                    break;
                case "#energy":
                    CardToAdd.cardNumber = line.Trim();

                    CardToAdd.transform.SetParent(EnergyContent.transform);
                    if (EnergyCardList.Count > 19 && !string.IsNullOrEmpty(CardToAdd.cardName))
                        Destroy(newCardGameObject);
                    else
                        EnergyCardList.Add(CardToAdd);
                    break;
                case "#oshi":
                    OshiCard.cardNumber = line.Trim();
                    Destroy(newCardGameObject);
                    break;
            }
        }
        GenericButton.DisplayPopUp(MessageBox, MessageBoxText, "Deck imported from clipboard");
    }
    public void ExportDeckToClipboard()
    {
        StringBuilder exportText = new StringBuilder();

        exportText.AppendLine("#created by");
        exportText.AppendLine(DeckName);

        exportText.AppendLine("#main");
        foreach (Card card in DeckCardList)
        {
            exportText.AppendLine(card.cardNumber);
        }

        exportText.AppendLine("#energy");
        foreach (Card card in EnergyCardList)
        {
            exportText.AppendLine(card.cardNumber);
        }

        if (OshiCard != null)
        {
            exportText.AppendLine("#oshi");
            exportText.AppendLine(OshiCard.cardNumber);
        }
        GUIUtility.systemCopyBuffer = exportText.ToString();
        GenericButton.DisplayPopUp(MessageBox, MessageBoxText, "Deck Copied to clipboard");
    }
    public void SaveDeckInformation()
    {
        string DeckText = null;
        string EnergyText = null;

        foreach (var number in DeckCardList)
        {
            DeckText += $"{number.cardNumber},";
        }
        if (DeckText.Length > 0) DeckText = DeckText.Remove(DeckText.Length - 1);

        foreach (var number in EnergyCardList)
        {
            EnergyText += $"{number.cardNumber},";
        }
        if (EnergyText.Length > 0) EnergyText = EnergyText.Remove(EnergyText.Length - 1);

        DeckData NewDeckToSave = new()
        {
            deckId = _DeckData.deckId,
            deckName = _DeckData.deckName,
            main = DeckText,
            energy = EnergyText,
            oshi = OshiCard.cardNumber,
        };
        
        WaitingResponsePainel.SetActive(true);
        StartCoroutine(SaveDeckRequest(NewDeckToSave));
        WaitingResponsePainel.SetActive(false);
    }
    public IEnumerator SaveDeckRequest(DeckData _DeckData)
    {
        IEnumerator HandleSaveDeckRequest(DeckData _DeckData)
        {
            yield return StartCoroutine(_HTTPSMaker.UpdateDeckRequest(_DeckData));
            string response = _HTTPSMaker.returnMessage.Equals("success") ? "Deck Updated" : "Error";
            _HTTPSMaker.returnMessage = null;
            GenericButton.DisplayPopUp(MessageBox, MessageBoxText, response);
        }
        yield return StartCoroutine(HandleSaveDeckRequest(_DeckData));
    }
    public IEnumerator SetDeckAsActiveRequest()
    {

        IEnumerator HandleSetDeckAsActiveRequest(DeckData _DeckData)
        {
            yield return StartCoroutine(_HTTPSMaker.SetDeckAsActive(_DeckData));
            string response = _HTTPSMaker.returnMessage.Equals("success") ? "Deck Active" : "Error";
            if (response.Equals("Deck Active"))
                ActiveDeckButton.active = false;
            _HTTPSMaker.returnMessage = null;
            GenericButton.DisplayPopUp(MessageBox, MessageBoxText, response);
        }
        yield return StartCoroutine(HandleSetDeckAsActiveRequest(new DeckData() { deckId = _DeckData.deckId }));
    }

    public void GetDeckInfo(DeckInfo _DeckData)
    {
        _HTTPSMaker.returnedObjects.Clear();
        DeckName = _DeckData.deckName;
        var main = _DeckData.main.Split(",");

        foreach (String card in main)
        {
            Card CardToAdd = null;
            GameObject newCardGameObject = null;
            newCardGameObject = Instantiate(cardPrefab, DeckContent.transform);
            CardToAdd = newCardGameObject.GetComponent<Card>().Init(new CardData {cardNumber = card });
            Button button = newCardGameObject.AddComponent<Button>();
            button.onClick.AddListener(() => HandleCardClick(newCardGameObject));

            if (DeckCardList.Count > 49 && !string.IsNullOrEmpty(CardToAdd.cardName))
                Destroy(newCardGameObject);
            else
                DeckCardList.Add(CardToAdd);
        }

        var cheer = _DeckData.energy.Split(",");
        foreach (String card in cheer)
        {
            Card CardToAdd = null;
            GameObject newCardGameObject = null;
            newCardGameObject = Instantiate(cardPrefab, EnergyContent.transform);
            CardToAdd = newCardGameObject.GetComponent<Card>().Init(newCardGameObject.GetComponent<Card>().ToCardData());
            Button button = newCardGameObject.AddComponent<Button>();
            button.onClick.AddListener(() => HandleCardClick(newCardGameObject));

            if (EnergyCardList.Count > 19 && !string.IsNullOrEmpty(CardToAdd.cardName))
                Destroy(newCardGameObject);
            else
                EnergyCardList.Add(CardToAdd);
        }
        OshiCard.cardNumber = _DeckData.oshi;
    }
}
