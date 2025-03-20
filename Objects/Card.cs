using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
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
    public string artEffect;
    [JsonIgnore]
    public bool playedThisTurn = false;
    [JsonIgnore]
    public bool suspended = false;
    [JsonIgnore]
    public string cardTag;
    [JsonIgnore]
    public List<CardEffect> cardEffects = new List<CardEffect>();
    [JsonIgnore]
    public List<GameObject> attachedEnergy = new();
    [JsonIgnore]
    public List<GameObject> attachedEquipe = new();
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
    public Card(string number, string position = "")
    {
        this.cardNumber = number;
        if (!string.IsNullOrEmpty(position))
            this.cardPosition = position;
        if (!string.IsNullOrEmpty(cardNumber))
            GetCardInfo();
    }

    public Card GetCardInfo(bool forceUpdate = false)
    {
        //sometimes we are creating cards without a gameobject, this cause problem with this part
        try
        {
            if (transform.parent != null)
                if (transform.parent.name.Equals("Life") || transform.parent.name.Equals("CardCheer"))
                {
                    transform.Find("Background").transform.localScale = new Vector3(63.7f, 63.7f, 1);
                    transform.Find("Background").GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("CardImages/001");
                }
        }
        catch (Exception e) {
            Debug.Log("A card was created without a gameoject :" + e);
        }

        if (!string.IsNullOrEmpty(cardType) && !forceUpdate)
            return null;

        if (cardNumber.Equals("0") || string.IsNullOrEmpty(cardNumber))
            return null;

        Record record = FileReader.result[cardNumber];

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
            artEffect = record.ArtEffect;
            cardTag = record.Tag;

            transform.Find("FrontView").GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("CardImages/" + record.CardNumber + "_" + record.Rarity);
            
            //try { gameObject.transform.Find("CardImage").GetComponent<Image>().sprite = Resources.Load<Sprite>("CardImages/" + record.CardNumber + "_" + record.Rarity); } catch (Exception e) { Debug.Log($"Sprite Problem: {record.CardNumber}"); }

            List<string> eachArtText = arts.Split(';').ToList();
            List<string> eachArtEffectText = artEffect.Split(';').ToList();
            eachArtText.Add("");

            if ((cardType.Equals("ホロメン") || cardType.Equals("Buzzホロメン")))
            {
                if (Arts != null)
                    Arts.Clear();
                else
                    Arts = new();
                for (int n = 0; n < eachArtText.Count; n++)
                {
                    if (string.IsNullOrEmpty(eachArtText[n]))
                        continue;

                    string eachArtEffectTextValidText = "";
                    if (n >= 0 && n < eachArtEffectText.Count)
                    {
                        if (!string.IsNullOrEmpty(eachArtEffectText[n]) || eachArtEffectText != null)
                        {
                            eachArtEffectTextValidText = eachArtEffectText[n];
                        }
                    }
                    Arts.Add(Art.ParseArtFromString(eachArtText[n], eachArtEffectTextValidText));
                }
                //adding the retreat to holomemns
                Arts.Add(new Art { Name = "Retreat", Cost = new List<(string Color, int Amount)>() { ("無色", 1) }, Effect = "Return this card o the backstage" });
            }
        }

        if (this.currentHp == 0 && (cardType.Equals("ホロメン") || cardType.Equals("Buzzホロメン")))
        {
            currentHp = int.Parse(hp);
        }

        return this;
    }

    public Card SetCardNumber(string numnber)
    {
        this.cardNumber = numnber;
        return this;
    }

    public static bool ContainsCard(List<Card> c, string name)
    {
        foreach (Card card in c)
        {
            if (card.cardNumber.Equals(name))
            {
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

    public List<Card> StringListToCardList(List<string> cards)
    {
        List<Card> returnCards = new();
        foreach (string s in cards)
        {
            Card card = new(s);
            returnCards.Add(card);
        }
        return returnCards;
    }

    public List<string> CardListToStringList(List<Card> cards)
    {
        List<string> returnCards = new();
        foreach (Card c in cards)
        {
            returnCards.Add(c.cardNumber);
        }
        return returnCards;
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
