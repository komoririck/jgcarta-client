using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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

        if (string.IsNullOrEmpty(cardNumber))
        {
            Destroy(GetComponent<DuelField_HandClick>());
            Destroy(GetComponent<DuelField_HandDragDrop>());
        }
        if (!curZone.Equals(Lib.GameZone.Hand))
        {
            Destroy(GetComponent<DuelField_HandDragDrop>());
        }
        if (curZone.Equals(Lib.GameZone.Hand) && transform.parent != null)
        {
            if (transform.parent.name.Equals("OponentHand"))
                Destroy(GetComponent<DuelField_HandDragDrop>());
        }

        //SetActiveVisual(false);

        return this;
    }

    public Card PlayedThisTurn(bool sts) {
        playedThisTurn = sts;
        return this;
    }

    public Card SetCardArt()
    {
        try
        {
            Transform FrontView = transform.Find("FrontView");
            //3d layout
            if (FrontView != null)
            {
                FrontView.GetComponent<Image>().sprite = Resources.Load<Sprite>("CardImages/000");
                transform.Find("Background").GetComponent<Image>().sprite = Resources.Load<Sprite>("CardImages/000");

                if (curZone.Equals(Lib.GameZone.CardCheer) || curZone.Equals(Lib.GameZone.Life))
                {
                    FrontView.GetComponent<Image>().sprite = Resources.Load<Sprite>("CardImages/001");
                    transform.Find("Background").GetComponent<Image>().sprite = Resources.Load<Sprite>("CardImages/001");
                }

                if (!string.IsNullOrEmpty(cardNumber))
                {
                    FrontView.GetComponent<Image>().sprite = Resources.Load<Sprite>("CardImages/" + cardNumber + "_" + rarity);
                }
            }
            //ui layout
            else
            {
                transform.Find("CardImage").GetComponent<Image>().sprite = Resources.Load<Sprite>("CardImages/000");

                if (curZone.Equals(Lib.GameZone.Hand) && lastZone.Equals(Lib.GameZone.CardCheer) && string.IsNullOrEmpty(cardNumber))
                {
                    transform.Find("CardImage").GetComponent<Image>().sprite = Resources.Load<Sprite>("CardImages/001");
                }
                else if (!string.IsNullOrEmpty(cardNumber))
                {
                    transform.Find("CardImage").GetComponent<Image>().sprite = Resources.Load<Sprite>("CardImages/" + cardNumber + "_" + rarity);
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log("Error loading image :" + e);
        }
        return this;
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
    public void UpdateHP()
    {
        Card card = this;

        if (card.transform == null)
            return;

        if (card.transform.parent == null)
            return;

        if (card.transform.parent.name.Equals(Lib.GameZone.Favourite.ToString()) || card.transform.parent.name.Equals(Lib.GameZone.Deck.ToString())
            || card.transform.parent.name.Equals(Lib.GameZone.CardCheer.ToString()) || card.transform.parent.name.Equals(Lib.GameZone.Life.ToString())
            || card.transform.parent.name.Equals(Lib.GameZone.HoloPower.ToString()) || card.transform.parent.name.Equals(Lib.GameZone.Arquive.ToString()))
            return;

        var hpbar = card.transform.Find("HPBAR");
        if (hpbar == null)
            return;

        hpbar.gameObject.SetActive(true);
        hpbar.Find("HPCurrent").GetComponent<TMP_Text>().text = card.currentHp.ToString();
        hpbar.Find("HPMax").GetComponent<TMP_Text>().text = card.hp.ToString();

        if (card.transform.parent.parent.name.Equals("Oponente") || card.transform.parent.name.Equals("Oponente"))
        {
            hpbar.Find("HPCurrent").localEulerAngles = new Vector3(0, 0, 180);
            hpbar.Find("HPBar").localEulerAngles = new Vector3(0, 0, 180);
            hpbar.Find("HPMax").localEulerAngles = new Vector3(0, 0, 180);
        }
    }
    public Card UpdateZone(Lib.GameZone zone)
    {
        this.lastZone = curZone;
        this.curZone = zone;
        return this;
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
