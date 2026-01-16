using Assets.Scripts.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using static DuelField;


[Serializable]
public class Card : MonoBehaviour
{
	public Player owner;
	public string cardNumber;
	public Lib.GameZone lastZone;
	public Lib.GameZone curZone;

	//unity
	public int currentHp = 0;

	public bool playedThisTurn = false;
	public bool suspended = false;

	public List<Card> attachedEnergy = new();
	public List<Card> attachedEquipe = new();
	public List<Card> bloomChild = new();

	public Card father = null;

	public bool usable = false;

	//record
	private Record record = null;

	public string? cardName => record.Name;
	public CardType? cardType => (CardType)ConvertCardType();
	public string? rarity => record.Rarity;
	public List<string>? product => record.Product;
	public ColorCard? color => ConvertColor();
	public string? hp => record.HP;
	public BloomLevel? bloomLevel => ConvertBloomLevel();
	public string? illustrator => record.Illustrator;
	public string? life => record.Life;
	public List<string>? cardTag => record.Tag;
    public ColorCount BatonTouchCost => ConvertBatomPass(record.BatonTouchCost);
    public List<string>? Extra => record.Extra;
	public Gift? Gift => record.Gift;
	public List<Art>? Arts => ConvertArt();
	public OshiSkill? oshiSkill => record.OshiSkill;
	public OshiSkill? spOshiSkill => record.SPOshiSkill;
	public List<string>? AbilityText => ConvertAbility();


	public Card Init(CardData data)
    {
        this.owner = data.owner;
        this.curZone = data.curZone;
        this.lastZone = data.lastZone;

        if (!string.IsNullOrEmpty(data.cardNumber) && FileReader.result.TryGetValue(data.cardNumber, out var serverRecord))
        {
            this.cardNumber = data.cardNumber;
            this.record = serverRecord;
        }

        ResolveOwner();
        SetCardArt();

        if (cardType != null && this.currentHp == 0 && (cardType == CardType.Buzzホロメン || cardType == CardType.ホロメン))
			currentHp = int.Parse(hp);

		if (string.IsNullOrEmpty(cardNumber))
		{
			Destroy(GetComponent<HandClick>());
			Destroy(GetComponent<HandDragDrop>());
		}
		if (!curZone.Equals(Lib.GameZone.Hand))
		{
			Destroy(GetComponent<HandDragDrop>());
		}
		if (curZone.Equals(Lib.GameZone.Hand) && transform.parent != null)
		{
			if (transform.parent.name.Equals("OponentHand"))
				Destroy(GetComponent<HandDragDrop>());
		}
		return this;
	}
	public Card NeedEnergyCounter()
	{
		var energyList = transform.Find("EnergyList");
		ZoneEnergyCounter zoneEnergyCounter = GetComponentInChildren<ZoneEnergyCounter>();

		if (!DuelField.DEFAULTHOLOMEMZONE.Contains(curZone))
		{
			zoneEnergyCounter.enabled = false;
			energyList.gameObject.SetActive(false);
		}
		else
		{
			zoneEnergyCounter.enabled = true;
			energyList.gameObject.SetActive(true);
		}
		return this;
	}
	public void AttachTo(Card newFather)
	{
		if (newFather == null || newFather == this)
			return;

		Detach();

		father = newFather;

		switch (cardType)
		{
			case CardType.エール:
				father.attachedEnergy ??= new();
				father.attachedEnergy.Add(this);
				break;

			case CardType.ホロメン:
			case CardType.Buzzホロメン:
				father.bloomChild ??= new();
				father.bloomChild.Add(this);
				break;

			default:
				father.attachedEquipe ??= new();
				father.attachedEquipe.Add(this);
				break;
		}
	}
	public void Detach()
	{
		if (father == null)
			return;

		father.attachedEnergy?.Remove(this);
		father.attachedEquipe?.Remove(this);
		father.bloomChild?.Remove(this);

		father = null;
	}
	public void BloomFrom(Card baseCard)
	{
		if (baseCard == null || baseCard == this)
			return;

		Detach();

		if (father != null)
		{
			baseCard.father.bloomChild?.Remove(baseCard);
			baseCard.father.bloomChild?.Add(this);
		}

		attachedEnergy = baseCard.attachedEnergy ?? new();
		attachedEquipe = baseCard.attachedEquipe ?? new();
		bloomChild = baseCard.bloomChild ?? new();
		bloomChild.Add(baseCard);

		baseCard.Detach();
		baseCard.attachedEnergy = new();
		baseCard.attachedEquipe = new();
		baseCard.bloomChild = new();

		foreach (var go in attachedEnergy.Concat(attachedEquipe.Concat(bloomChild)))
		{
			if (go.TryGetComponent<Card>(out var child))
				child.father = this;
		}
		playedThisTurn = true;
	}

	public void Glow(Color? ForceColor = null, bool? ForceGlow = null)
	{
		var glowObj = transform.Find("CardGlow");
		if (glowObj == null)
			return;

		bool isGlowing = false;
		bool isRed = false;

		bool ISMYTURN = DuelField.INSTANCE.IsMyTurn();
		bool ISMYCARD = transform.parent?.name == "PlayerGeneral" || transform.parent?.parent?.name == "PlayerGeneral" || transform.parent?.parent?.parent?.name == "PlayerGeneral";
		bool ISMAINPHASE = DuelField.INSTANCE.GamePhase.Equals(GAMEPHASE.MainStep);

		var backRoll = new[] {
				Lib.GameZone.BackStage1,
				Lib.GameZone.BackStage2,
				Lib.GameZone.BackStage3,
				Lib.GameZone.BackStage4,
				Lib.GameZone.BackStage5
			};

		var ActiveCard = this;

		if (usable)
			isGlowing = true;

		if (ActiveCard.curZone.Equals(Lib.GameZone.Collaboration) || ActiveCard.curZone.Equals(Lib.GameZone.Stage) || ActiveCard.curZone.Equals(Lib.GameZone.Favourite))
			isRed = true;

		if (ForceGlow != null && (bool)ForceGlow)
			isGlowing = true;
		else if (ForceGlow != null && !(bool)ForceGlow)
			isGlowing = false;


		glowObj.gameObject.SetActive(isGlowing);

		var glowImage = glowObj.GetComponent<Image>();
		if (glowImage != null)
			if (ForceColor != null)
				glowImage.color = (Color)ForceColor;
			else
				glowImage.color = !isRed ? Color.green : Color.red;
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

				if (curZone.Equals(Lib.GameZone.CardCheer) || curZone.Equals(Lib.GameZone.Life) || lastZone.Equals(Lib.GameZone.CardCheer) || lastZone.Equals(Lib.GameZone.Life))
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
	internal CardData ToCardData()
	{
		return new CardData
		{
			cardNumber = this.cardNumber,
			curZone = this.curZone,
			lastZone = this.lastZone,
			owner = this.owner,
		};
	}
	public void UpdateHP()
	{
		Card card = this;

		if (!IsHolomem() || record == null || card.transform == null || card.transform.parent == null)
			return;

		var hpbar = card.transform.Find("HPBAR");
		if (hpbar == null)
			return;

		hpbar.gameObject.SetActive(true);
		hpbar.Find("HPCurrent").GetComponent<TMP_Text>().text = card.currentHp.ToString();
		hpbar.Find("HPMax").GetComponent<TMP_Text>().text = card.hp.ToString();

		if (!DuelField.DEFAULTHOLOMEMZONE.Contains(curZone))
		{
			hpbar.gameObject.SetActive(false);
			return;
		}

		if (card.transform.parent.parent.name.Equals("Oponente") || card.transform.parent.name.Equals("Oponente"))
		{
			hpbar.Find("HPCurrent").localEulerAngles = new Vector3(0, 0, 180);
			hpbar.Find("HPBar").localEulerAngles = new Vector3(0, 0, 180);
			hpbar.Find("HPMax").localEulerAngles = new Vector3(0, 0, 180);
		}
	}
	public void ResolveOwner()
	{
		if (owner == Player.Player || owner == Player.Oponnent) return;
		if (DuelField.INSTANCE == null) return;
		owner = (DuelField.INSTANCE.players[owner] == PlayerInfo.INSTANCE.PlayerID) ? Player.Player : Player.Oponnent;
	}



	private CardType? ConvertCardType()
	{
		if (this.record == null)
			return CardType.na;

		return (CardType)Enum.Parse(typeof(CardType), this.record.CardType.Replace("・", null));
	}
	private BloomLevel ConvertBloomLevel()
	{
		switch (this.record.BloomLevel)
		{
			case "1st":
				return BloomLevel.st1;
			case "Debut":
				return BloomLevel.debut;
			case "2nd":
				return BloomLevel.nd2;
			default:
				return BloomLevel.na;
		}
	}
	private ColorCard ConvertColor()
	{
		return (ColorCard)Enum.Parse(typeof(ColorCard), this.record.Color);
	}
	private List<Art> ConvertArt()
	{
		var Arts = this.record.Arts.Select(item => JsonArt.Convert(item)).ToList();
		foreach (var item in Arts)
			item.Name = item.Name.Split(" ").FirstOrDefault();

		Arts.Add(new Art { Name = "Retreat", Cost = new() { new() { Color = BatonTouchCost.Color, Amount = BatonTouchCost.Amount } }, Effect = "Return this card o the backstage" });
		return Arts;
	}
	private List<string> ConvertAbility()
	{
		if (this.record.AbilityText == null)
			return null;

		return this.record.AbilityText.FirstOrDefault().Split("\n").ToList();
    }
    public ColorCount ConvertBatomPass(List<string> count)
    {
        ColorCount colorCount = new();
        colorCount.Color = ColorCard.無;
        colorCount.Amount = count.Count;
        return colorCount;
    }
    public bool IsHolomem()
	{
		return cardType == CardType.ホロメン || cardType == CardType.Buzzホロメン;
	}
	public bool IsLimited()
	{
		return cardType == CardType.サポートアイテムLIMITED || cardType == CardType.サポートイベントLIMITED || cardType == CardType.サポートスタッフLIMITED;
	}
	public bool HasAttachments()
	{
		return attachedEnergy != null || attachedEquipe != null || bloomChild != null;
	}
}
