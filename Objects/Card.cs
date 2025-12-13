using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static DuelField_HandClick;

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
    public void Glow()
    {
        var glowObj = transform.Find("CardGlow");
        if (glowObj == null)
            return;

        bool isGlowing = false;
        bool isRed = false;

        bool ISMYTURN = DuelField.INSTANCE.DUELFIELDDATA.currentPlayerTurn == PlayerInfo.INSTANCE.PlayerID; 
        bool ISMYCARD = transform.parent?.name == "PlayerGeneral" || transform.parent?.parent?.name == "PlayerGeneral" || transform.parent?.parent?.parent?.name == "PlayerGeneral";
        bool ISMAINPHASE = DuelField.INSTANCE.DUELFIELDDATA.currentGamePhase.Equals(DuelFieldData.GAMEPHASE.MainStep);

        var backRoll = new[] {
                Lib.GameZone.BackStage1,
                Lib.GameZone.BackStage2,
                Lib.GameZone.BackStage3,
                Lib.GameZone.BackStage4,
                Lib.GameZone.BackStage5
            };

        var ActiveCard = this;

        if (!DuelField.INSTANCE.isViewMode && !DuelField.INSTANCE.hasAlreadyCollabed && ISMYTURN && ISMYCARD && backRoll.Contains(ActiveCard.curZone)
        && ISMAINPHASE && !ActiveCard.suspended)
            isGlowing = true;

        if ((ActiveCard.cardType.Equals("ホロメン") || ActiveCard.cardType.Equals("Buzzホロメン"))
            && ActiveCard.curZone.Equals(Lib.GameZone.Collaboration) || ActiveCard.curZone.Equals(Lib.GameZone.Stage)
            && ISMAINPHASE && ISMYTURN && ISMYCARD)
        {
            Dictionary<string, List<GameObject>> energyAmount = CountCardAvaliableEnergy();

            foreach (Art currentArt in ActiveCard.Arts)
            {
                if (currentArt.Name.Equals("Retreat") && !ActiveCard.transform.parent.name.Equals(Lib.GameZone.Stage))
                    continue;

                if (IsCostCovered(currentArt.Cost, energyAmount)
                    && ((ActiveCard.curZone.Equals(Lib.GameZone.Stage) && !DuelField.INSTANCE.centerStageArtUsed)
                    || (ActiveCard.curZone.Equals(Lib.GameZone.Collaboration) && !DuelField.INSTANCE.collabStageArtUsed))
                    && PassSpecialDeclareAttackCondition(currentArt)
                    )
                {
                    isGlowing = true;
                    isRed = true;
                }
            }
        }

        if (ActiveCard.cardType.Equals("推しホロメン")
        && ISMAINPHASE && ISMYTURN && ISMYCARD)
        {
            bool conditionA = (!DuelField.INSTANCE.usedOshiSkill && DuelField.INSTANCE.CanActivateOshiSkill(ActiveCard.cardNumber));
            bool conditionB = (!DuelField.INSTANCE.usedSPOshiSkill && DuelField.INSTANCE.CanActivateSPOshiSkill(ActiveCard.cardNumber));

            if (conditionA || conditionB)
            {
                isGlowing = true;
                isRed = true;
            }
        }
        var dragdrop = GetComponent<DuelField_HandDragDrop>();
        if (dragdrop != null && dragdrop.isActiveAndEnabled)
            isGlowing = true;

        glowObj.gameObject.SetActive(isGlowing);

        var glowImage = glowObj.GetComponent<Image>();
        if (glowImage != null)
        {
            glowImage.color = !isRed ? Color.green : Color.red;
        }
    }
    public bool PassSpecialDeclareAttackCondition(Art currentArt)
    {
        Card card = this;
        switch (card.cardNumber + "+" + currentArt.Name)
        {
            case "hBP01-070+共依存":
                foreach (GameObject _card in card.attachedEquipe)
                    if (_card.GetComponent<Card>().cardName.Equals("座員"))
                        return true;
                return false;
        }
        return true;
    }
    public Dictionary<string, List<GameObject>> CountCardAvaliableEnergy()
    {
        Card card = this;
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
    public Card PlayedThisTurn(bool sts)
    {
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
    public bool IsCostCovered(List<(string Color, int Amount)> cost, Dictionary<string, List<GameObject>> energyAmount)
    {
        Card SkillOwnerCard = this;
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
                    payment[index] = (payment[index].Color, (payment[index].Amount + 1));
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
