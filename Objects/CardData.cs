using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using static Card;
using static DuelField;

[Serializable]
public class CardData
{
    public string cardNumber { get; set; }
    public Lib.GameZone curZone { get; set; } 
    public Lib.GameZone lastZone { get; set; }
    public Player owner { get; set; }

    [JsonIgnore]
    public string cardName;
    [JsonIgnore]
    public string cardLimit;
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
    public string cardTag;
    [JsonIgnore]
    public List<Art> Arts = new List<Art>();

    public void Initialize()
    {
        GetCardInfo();
    }

    public CardData GetCardInfo(bool forceUpdate = false)
    {
        if (!string.IsNullOrEmpty(cardType) && !forceUpdate)
            return null;

        if (string.IsNullOrEmpty(cardNumber))
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
        return this;
    }
}
