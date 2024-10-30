using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Card;

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
    [SerializeField] string DeckName = "";

    void Start()
    {
        cardPool = FindObjectOfType<CardPool>(); // Ensure you have a CardPool in the scene
        textBox.onValueChanged.AddListener(OnSearchInputChanged);
        ClearTextButton();
    }

    private void UpdateDisplay(string filter)
    {
        // Check if cardPool is null
        if (cardPool == null)
        {
            Debug.LogError("CardPool is not initialized. Make sure the CardPool script is attached to a GameObject in the scene.");
            return; // Exit the method if cardPool is null
        }

        // Clear existing cards from the content area
        foreach (Transform child in CardViewContent.transform)
        {
            if (child != null)
            {
                cardPool.ReturnCard(child.gameObject); // Return cards to pool instead of destroying
            }
        }

        List<Record> query;
        if (string.IsNullOrEmpty(filter))
            query = FileReader.result;
        else
            query = FileReader.result.Where(r => r.CardNumber.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (Record record in query)
        {
            GameObject newCardGameObject = cardPool.GetCard();
            if (newCardGameObject == null)
                continue;

            newCardGameObject.transform.SetParent(CardViewContent.transform, false);
            Card newCard = newCardGameObject.GetComponent<Card>();
            newCard.cardNumber = record.CardNumber;
            newCard.GetCardInfo();
            Button button = newCardGameObject.GetComponent<Button>();
            if (button == null)
                button = newCardGameObject.AddComponent<Button>();
            
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => AddCardToDeck(newCardGameObject));
        }
    }


    private void OnSearchInputChanged(string value)
    {
        UpdateDisplay(value);
    }

    public void ClearTextButton()
    {
        textBox.text = "";
        UpdateDisplay("");
    }

    void AddCardToDeck(GameObject thisCard)
    {
        Card ClickedCard = thisCard.GetComponent<Card>();
        GameObject newCard = Instantiate(cardPrefab, DeckContent.transform);
        Card CardToAdd = newCard.GetComponent<Card>();
        Button button = newCard.AddComponent<Button>();
        button.onClick.AddListener(() => RemoveCardFromDeck(newCard));

        CardToAdd.cardNumber = ClickedCard.cardNumber;
        CardToAdd.GetCardInfo();
        if (CardToAdd.cardType.Equals("エール"))
        {
            newCard.transform.SetParent(EnergyContent.transform);
            if (EnergyCardList.Count > 19)
                Destroy(newCard);
            else
                EnergyCardList.Add(CardToAdd);
        }
        else if (CardToAdd.cardType.Equals("推しホロメン"))
        {
            OshiCard.cardNumber = CardToAdd.cardNumber;
            Destroy(newCard);
            OshiCard.GetCardInfo();
        }
        else
        {
            if (DeckCardList.Count > 49)
                Destroy(newCard);
            else
                DeckCardList.Add(CardToAdd);
        }
    }

    void RemoveCardFromDeck(GameObject thisCard)
    {
        Card card = thisCard.GetComponent<Card>();
        DeckCardList.Remove(card);
        Destroy(thisCard);
    }

    public void ImportDeckFromClipBoard()
    {
        string clipboardContent = GUIUtility.systemCopyBuffer;

        string[] lines = clipboardContent.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        string currentTag = "";

        // Define valid tags
        HashSet<string> validTags = new HashSet<string> { "#created by", "#main", "#energy", "#oshi" };


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
                button.onClick.AddListener(() => RemoveCardFromDeck(newCardGameObject));
            }

            switch (currentTag)
            {
                case "#created by":
                    DeckName = line.Trim();
                    break;
                case "#main":
                    CardToAdd.cardNumber = line.Trim();
                    CardToAdd.GetCardInfo();
                    if (DeckCardList.Count > 49 && !string.IsNullOrEmpty(CardToAdd.name))
                        Destroy(newCardGameObject);
                    else
                        DeckCardList.Add(CardToAdd);
                    break;
                case "#energy":
                    CardToAdd.cardNumber = line.Trim();
                    CardToAdd.GetCardInfo();

                    CardToAdd.transform.SetParent(EnergyContent.transform);
                    if (EnergyCardList.Count > 19 && !string.IsNullOrEmpty(CardToAdd.name))
                        Destroy(newCardGameObject);
                    else
                        EnergyCardList.Add(CardToAdd);
                    break;
                case "#oshi":
                    OshiCard.cardNumber = line.Trim();
                    Destroy(newCardGameObject);
                    OshiCard.GetCardInfo();
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

    void SaveDeckInformation()
    {
        string DeckText = "";
        string EnergyText = "";

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

        DeckData _DeckData = new()
        {
            deckName = DeckName,
            main = DeckText,
            energy = EnergyText,
            oshi = OshiCard.cardNumber
        };

        WaitingResponsePainel.SetActive(true);

        if (true) {
            GenericButton.DisplayPopUp(MessageBox, MessageBoxText, "Deck Updated");
        }
        else
        {
            GenericButton.DisplayPopUp(MessageBox, MessageBoxText, "Error");
        }
    }
}
