using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class Card : MonoBehaviour
{
    public string cardNumber;
    public string cardPosition;
    public string cardName;

    [JsonIgnore]
    public int currentHp = 0;
    [JsonIgnore]
    public int effectDamageRecieved = 0;
    [JsonIgnore]
    public int normalDamageRecieved = 0;
    [JsonIgnore]
    public string cardLimit;
    [JsonIgnore]
    public string playedFrom;
    [JsonIgnore]
    public string cardType;
    [JsonIgnore]
    public string rarity;
    [JsonIgnore]
    public string product;
    [JsonIgnore]
    public string color;
    [JsonIgnore]
    public string hp;
    [JsonIgnore]
    public string bloomLevel;
    [JsonIgnore]
    public string arts;
    [JsonIgnore]
    public string oshiSkill;
    [JsonIgnore]
    public string spOshiSkill;
    [JsonIgnore]
    public string abilityText;
    [JsonIgnore]
    public string illustrator;
    [JsonIgnore]
    public string life;
    [JsonIgnore]
    public bool playedThisTurn = false;
    [JsonIgnore]
    public bool suspended = false;

    [JsonIgnore]
    public List<CardEffect> cardEffects  = new List<CardEffect>();
    [JsonIgnore]
    public List<GameObject> attachedCards = new();
    [JsonIgnore]
    public List<GameObject> bloomChild = new List<GameObject>();
    [JsonIgnore]
    public List<Art> Arts = new List<Art>();


    [Flags]
    public enum CardFoil : byte
    {
        Normal = 0,
        Glossy = 1,
        Prismatic = 2,
    }

    [Flags]
    public enum CardRarity : byte
    {
        NNMaterial = 0,
        RRMaterial = 1,
        SRMaterial = 2,
        URMaterial = 3,
    }
    public Card(string number) {
        this.cardNumber = number;
        if(!string.IsNullOrEmpty(cardNumber))
            GetCardInfo();
    }

    static public Card CreateFromData(CardData _cardData, string number) {
        Card returnC = new Card(number);
        returnC.playedFrom = _cardData.playerdFrom;
        returnC.cardPosition = _cardData.cardPosition;
        return returnC;
    }

    public void GetCardInfo() {
        if (cardNumber.Equals("0") || string.IsNullOrEmpty(cardNumber))
            return;
        
        foreach (Record record in FileReader.result) {
            if (record.CardNumber == cardNumber)
            {
                this.cardNumber = record.CardNumber;
                cardName = record.Name;
                cardType = record.CardType;
                rarity = record.Rarity;
                product = record.Product;
                color = record.Color;
                hp = record.HP;
                bloomLevel = record.BloomLevel;
                arts = record.Arts;
                oshiSkill = record.OshiSkill;
                spOshiSkill = record.SPOshiSkill;
                abilityText = record.AbilityText;
                illustrator = record.Illustrator;
                life = record.Life;

                try { gameObject.transform.Find("CardImage").GetComponent<Image>().sprite = Resources.Load<Sprite>("CardImages/" + record.CardNumber + "_" + record.Rarity); } catch (Exception e) { Debug.Log($"Sprite Problem: {record.CardNumber}"); }

                List<string> words = arts.Split('-').ToList();
                Arts = new();
                foreach (string art in words)
                {
                    if ((cardType.Equals("ホロメン") || cardType.Equals("Buzzホロメン")))
                    Arts.Add(Art.ParseArtFromString(art));
                }
            }
        }
        if (this.currentHp == 0 && (cardType.Equals("ホロメン") || cardType.Equals("Buzzホロメン"))) {
            currentHp = int.Parse(hp);
        }
    }

    public static bool ContainsCard(List<Card> c, string name) {
        foreach (Card card in c) {
            if (card.cardNumber.Equals(name)) {
                return true;
            }
        }
        return false;
    }

    private void Start()
    {
        if (this.cardNumber.Equals("0"))
        {
            Destroy(GetComponent<DuelField_HandDragDrop>());
            Destroy(GetComponent<DuelField_HandClick>());
        }
    }

    void OnEnable()
    { 

    }

}
[Serializable]
public class CardEffect
{
    public string effectTrigger { get; set; } = "";
    public string text { get; set; } = "";
    public int usageLimit { get; set; } = 0;

    [JsonIgnore] // Ignore this during serialization
    public Card target { get; set; }

    public int continuousEffect { get; set; } = 0;
    public string responseType { get; set; } = "";
    public string activationPhase { get; set; } = "";
}
