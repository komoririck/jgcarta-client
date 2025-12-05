using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class Card : MonoBehaviour
{
    public string cardNumber;
    public Lib.GameZone lastZone = 0;
    public Lib.GameZone curZone = 0;
    public string cardName;
    public string cardLimit;
    public string cardType;
    public string rarity;
    public string product;
    public string color;
    public string hp;
    public string bloomLevel;
    public string arts;
    public string oshiSkill;
    public string spOshiSkill;
    public string abilityText;
    public string illustrator;
    public string life;
    public string artEffect;
    public string cardTag;
    public List<Art> Arts = new List<Art>();
    public bool playedThisTurn = false;
    public bool suspended = false;
    public int currentHp = 0;
    public int effectDamageRecieved = 0;
    public int normalDamageRecieved = 0;
    public List<CardEffect> cardEffects = new();
    public List<GameObject> attachedEnergy = new();
    public List<GameObject> attachedEquipe = new();
    public List<GameObject> bloomChild = new();

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

    public Card Init(CardData data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        data.GetCardInfo();

        this.cardNumber = data.cardNumber;
        this.curZone = data.curZone;
        this.lastZone = data.lastZone;

        this.cardName = data.cardName;
        this.cardLimit = data.cardLimit;
        this.cardType = data.cardType;
        this.rarity = data.rarity;
        this.product = data.product;
        this.color = data.color;
        this.hp = data.hp;
        this.bloomLevel = data.bloomLevel;
        this.arts = data.arts;
        this.oshiSkill = data.oshiSkill;
        this.spOshiSkill = data.spOshiSkill;
        this.abilityText = data.abilityText;
        this.illustrator = data.illustrator;
        this.life = data.life;
        this.artEffect = data.artEffect;
        this.cardTag = data.cardTag;

        this.Arts = data.Arts != null ? new List<Art>(data.Arts) : new List<Art>();

        SetCardArt();

        if (cardType != null)
            if (this.currentHp == 0 && (cardType.Equals("ホロメン") || cardType.Equals("Buzzホロメン")))
                currentHp = int.Parse(hp);

        return this;
    }

    public void SetCardArt(Lib.GameZone zone = 0)
    {
        try
        {
            Transform FrontView = transform.Find("FrontView");
            //3d layout
            if (FrontView != null)
            {
                if (string.IsNullOrEmpty(cardNumber))
                {
                    if (zone.Equals(Lib.GameZone.CardCheer))
                    {
                        transform.Find("Background").GetComponent<Image>().sprite = Resources.Load<Sprite>("CardImages/001");
                    }
                    else
                    {
                        transform.Find("Background").GetComponent<Image>().sprite = Resources.Load<Sprite>("CardImages/000");

                    }
                }
                else
                {
                    FrontView.GetComponent<Image>().sprite = Resources.Load<Sprite>("CardImages/" + cardNumber + "_" + rarity);
                }
            }
            //ui layout
            else
            {
                if (string.IsNullOrEmpty(cardNumber))
                {
                    transform.Find("CardImage").GetComponent<Image>().sprite = Resources.Load<Sprite>("CardImages/000");
                }
                else
                {
                    transform.Find("CardImage").GetComponent<Image>().sprite = Resources.Load<Sprite>("CardImages/" + cardNumber + "_" + rarity);
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log("Error loading image :" + e);
        }
    }

    public Card ScaleToFather()
    {
        if (transform.parent == null)
        {
            Debug.LogWarning("Card has no parent to scale to.");
            return this;
        }
        transform.localScale = new Vector3(1f, 1f, 1f);
        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector3(ScaleXFromY(rt.sizeDelta.y), rt.sizeDelta.y, 1f);
        return this;
    }
    float ScaleXFromY(float newY)
    {
        float origX = 53f;
        float origY = 75f;
        return origX * (newY / origY);
    }

    public Card Flip(bool layout3D = true)
    {
        if (string.IsNullOrEmpty(cardName))
            transform.localRotation = Quaternion.Euler(0f, -180f, 0f);
        else
            transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

        if (layout3D)
            if (string.IsNullOrEmpty(cardName))
                transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            else
                transform.localRotation = Quaternion.Euler(0f, -180f, 0f);

        return this;
    }
    public Card SetCardNumber(string numnber)
    {
        this.cardNumber = numnber;
        return this;
    }

    internal CardData ToCardData()
    {
        return new CardData
        {
            cardNumber = this.cardNumber,
            curZone = this.curZone,
            lastZone = this.lastZone,
        };
    }
}

[Serializable]
public class CardEffect
{
    public string effectTrigger { get; set; } = "";
    public string text { get; set; } = "";
    public int usageLimit { get; set; } = 0;

    // Ignore this during serialization
    public Card target { get; set; }

    public int continuousEffect { get; set; } = 0;
    public string responseType { get; set; } = "";
    public string activationPhase { get; set; } = "";
}
