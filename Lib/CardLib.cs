using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using static DuelField;

class CardLib
{
    [Flags]
    public enum attachType : byte
    {
        na = 0,
        energy,
        mascot,
        equip,
        all,
    }
    public static List<Card> GetAndFilterCards(List<Card> CardList = null, List<string> cardType = null, List<string> bloomLevel = null, List<string> cardNumber = null, List<string> color = null, List<string> cardName = null, bool onlyVisible = false, Lib.GameZone[] gameZones = null, Player player = Player.na, bool GetOnlyHolomem = false, bool GetLimitedCards = false, bool CardThatAllowReRoll = false, bool GetAllSuportTypes = false, List<string> ContainTags = null, bool WhoCanBloom = false, bool OnlyBackStage = false , bool CheckFieldForHasRestrictionsToPlayEquip = false, bool GetOnlyItemMascot = false, CardData CardToBeFound = null, CardFilter filter = null, bool CheckForAttachableTargetsOnTheBoard = false, bool isNotSuspended = false, attachType OnlyWithAttachment = attachType.na )
    {
        //var thisCard = CardLib.GetAndFilterCards(gameZones: new[] { da.usedCard.curZone }, player: TargetPlayer.Player, color: new() { "赤" }, cardType: new() { "エール" });
        //List<string> cardType = IsACheer ? new (){ "エール"} : new() { "サポート・ツール", "サポート・マスコット", "サポート・ファン" };
        //cardType: new() { "ホロメン", "Buzzホロメン" }

        if (CardToBeFound != null)
        {
            gameZones = new[] { CardToBeFound.curZone };
            cardNumber = new() { CardToBeFound.cardNumber };
        }

        if (GetOnlyHolomem)
            cardType = new() { "ホロメン", "Buzzホロメン" };

        if (GetOnlyItemMascot)
            cardType = new() { "サポート・マスコット", "サポート・アイテム", "サポート・アイテム・LIMITED" };

        if (GetAllSuportTypes)
            cardType = new() { "サポート・ツール", "サポート・マスコット", "サポート・ファン" };
        
        if (GetLimitedCards)
        {
            cardType = new() { "サポート・アイテム・LIMITED", "サポート・イベント・LIMITED", "サポート・スタッフ・LIMITED" };
        }

        if (CardList == null && gameZones == null)
        {
            gameZones = DuelField.DEFAULTHOLOMEMZONE;

            if (OnlyBackStage)
                gameZones = DuelField.DEFAULTBACKSTAGE;
        }

        //If no CardList is provided, get all cards from the specified zones
        if (CardList == null && gameZones != null)
        {
            if (player.Equals(Player.na))
            {
                CardList = gameZones.SelectMany(zone => DuelField.INSTANCE.GetZone(zone, Player.Player).GetComponentsInChildren<Card>(includeInactive: true)).ToList();
                CardList.AddRange(gameZones.SelectMany(zone => DuelField.INSTANCE.GetZone(zone, Player.Oponnent).GetComponentsInChildren<Card>(includeInactive: true)).ToList());
            }
            else
            {
                CardList = gameZones.SelectMany(zone => DuelField.INSTANCE.GetZone(zone, player).GetComponentsInChildren<Card>(includeInactive: true)).ToList();
            }
        }
        else if (gameZones != null)
        {
            CardList = CardList.FindAll(card => gameZones.Contains(card.curZone));
        }

        //Filter by required
        if (cardType != null)
        {
            CardList = CardList.FindAll(card => cardType.Contains(card.cardType));
        }
        if (bloomLevel != null) 
        { 
            CardList = CardList.FindAll(card => bloomLevel.Contains(card.bloomLevel));
        }
        if (cardNumber != null) 
        { 
            CardList = CardList.FindAll(card => cardNumber.Contains(card.cardNumber));
        }
        if (color != null) 
        { 
            CardList = CardList.FindAll(card => color.Contains(card.color));
        }
        if (cardName != null) 
        { 
            CardList = CardList.FindAll(card => cardName.Contains(card.cardName));
        }
        if (onlyVisible) 
        {
            List<Tuple<Card, bool>> toBeReturned = CardLib.GetActiveParent(CardList.ToArray());
            CardList = toBeReturned.FindAll(x => x.Item2.Equals(true)).Select(x => x.Item1).ToList();
        }
        if (ContainTags != null && ContainTags.Count > 0)
        {
            CardList = CardList.FindAll(card => ContainTags.All(tag => card.cardTag.Contains(tag)));
        }
        if (CardThatAllowReRoll)
        {
            HashSet<Card> uniqueParents = new HashSet<Card>();
            foreach (Card card in CardList)
            {
                if (card.cardNumber.Equals("hBP01-123"))
                {
                    Card parentCard = card.transform.parent.GetComponent<Card>();
                    uniqueParents.Add(parentCard);
                }
            }
            CardList = uniqueParents.ToList();
        }
        if (WhoCanBloom && bloomLevel != null)
        {   
            List<Card> checkedCards = new();
            foreach (Card card in CardList)
            {
                foreach (string s in bloomLevel)
                    if (NamesThatCanBloom(s).Contains(card.cardName))
                        checkedCards.Add(card);
            }
            CardList = checkedCards;
        }

        if (WhoCanBloom && bloomLevel == null) 
        {
            Debug.Log("wrong way of calling this function");
        }

        if (CheckFieldForHasRestrictionsToPlayEquip) 
        { 
            foreach (Card card in CardList)
                foreach (Card target in GetAndFilterCards(player: Player.Player, onlyVisible: true))
                    if (HasRestrictionsToPlayEquip(card, target))
                        CardList.Remove(card);
        }

        CardList = OnlyWithAttachment switch
        {
            attachType.energy =>
                CardList.FindAll(card => card.attachedEnergy != null && card.attachedEnergy.Count > 0),

            attachType.equip =>
                CardList.FindAll(card => card.attachedEquipe != null && card.attachedEquipe.Count > 0),

            attachType.mascot =>
                CardList.FindAll(card => card.bloomChild != null && card.bloomChild.Count > 0),

            attachType.all =>
                CardList.FindAll(card =>
                    (card.attachedEnergy != null && card.attachedEnergy.Count > 0) ||
                    (card.attachedEquipe != null && card.attachedEquipe.Count > 0) ||
                    (card.bloomChild != null && card.bloomChild.Count > 0)
                ),

            _ => CardList
        };

        return CardList;
    }
    public static List<string> NamesThatCanBloom(string level)
    {
        List<string> validNames = new();

        var cards = GetAndFilterCards(player: Player.Player);

        foreach (Card card in cards ) {
            if (card.bloomLevel.Equals(level) && card.playedThisTurn == false)
            {
                validNames.Add(card.cardName);
                //EXTRA CONDITIONS
                if (card.bloomLevel.Equals("Debut") && card.cardName.Equals("ときのそら") || card.cardName.Equals("AZKi"))
                    validNames.Add("SorAZ");
            }
        }
        return validNames;
    }
    public static List<CardData> ConvertToCardData(List<Card> CardList) 
    {
        var CardDataList = new List<CardData>();
        foreach (Card c in CardList) {
            CardDataList.Add(c.ToCardData());
        }
        return CardDataList;
    }
    public static bool CanBloomHolomem(Card pointedCard, Card thisCard)
    {
        if (pointedCard.playedThisTurn == true || pointedCard.cardType.Equals("Buzzホロメン")) return false;

        bool canContinue = false;
        if (thisCard.cardType.Equals("ホロメン") || thisCard.cardType.Equals("Buzzホロメン"))
        {
            string bloomToLevel = pointedCard.bloomLevel.Equals("Debut") ? "1st" : "2nd";
            // especial card condition to bloom match
            if (thisCard.cardNumber.Equals("hSD01-013") && thisCard.bloomLevel.Equals(bloomToLevel) && (pointedCard.cardName.Equals("ときのそら") || pointedCard.cardName.Equals("AZKi")))
            {
                canContinue = true;
            }
            else if (pointedCard.cardNumber.Equals("hSD01-013") && (thisCard.cardName.Equals("ときのそら") || thisCard.cardName.Equals("AZKi")) && bloomToLevel.Equals("2nd"))
            {
                canContinue = true;
            }
            else if (thisCard.cardNumber.Equals("hBP01-045"))
            {
                int lifeCounter = DuelField.INSTANCE.GetZone(Lib.GameZone.Life, Player.Player).transform.childCount - 1;
                if (lifeCounter < 4 && pointedCard.cardName.Equals("AZKi") || pointedCard.cardName.Equals("SorAZ"))
                {
                    canContinue = true;
                }
            }
            // normal condition to bloom match
            else if (thisCard.cardName.Equals(pointedCard.cardName) && thisCard.bloomLevel.Equals(bloomToLevel))
            {
                canContinue = true;
            }
        }
        return canContinue;
    }
    public static int CountPlayerActiveHolomem(bool onlyBackstage = false, Player target = Player.Player)
    {
        return GetAndFilterCards(player: target, OnlyBackStage: onlyBackstage, GetOnlyHolomem: true, onlyVisible: true).Count;
    }
    public static bool CanActivateOshiSkill(string cardNumber)
    {
        GameObject HoloPower = DuelField.INSTANCE.GetZone(Lib.GameZone.HoloPower, DuelField.Player.Player);
        int holoPowerCount = DuelField.INSTANCE.GetZone(Lib.GameZone.HoloPower, DuelField.Player.Player).transform.childCount - 1;

        if (holoPowerCount < HoloPowerCost(cardNumber, false))
            return false;

        switch (cardNumber)
        {
            case "hSD01-001":
                //check if theres another holomem to replace energy
                int backstagecount = CountPlayerActiveHolomem(false, DuelField.Player.Player);
                if (DuelField.INSTANCE.GetZone(Lib.GameZone.Stage, DuelField.Player.Player).GetComponentInChildren<Card>().attachedEnergy.Count < 1) return false;
                return (backstagecount > 0);
                break;
            case "xxx":
                break;
        }
        return true;
    }
    public static bool CanActivateSPOshiSkill(string cardNumber)
    {
        int holoPowerCount = DuelField.INSTANCE.GetZone(Lib.GameZone.HoloPower, DuelField.Player.Player).transform.childCount - 1;

        if (holoPowerCount < HoloPowerCost(cardNumber, true))
            return false;

        switch (cardNumber)
        {
            case "hSD01-001":
                //check if opponent has another holomem to switch for the center
                int backstagecount = CountPlayerActiveHolomem(true, DuelField.Player.Oponnent);
                return (backstagecount > 0);
            case "hYS01-003":
                foreach (Card card in DuelField.INSTANCE.GetZone(Lib.GameZone.Arquive, DuelField.Player.Player).GetComponentsInChildren<Card>())
                    if (card.cardType.Equals("ホロメン") || card.cardType.Equals("Buzzホロメン"))
                        return true;
                return false;
            case "hSD01-002":
                foreach (Card card in DuelField.INSTANCE.GetZone(Lib.GameZone.Arquive, DuelField.Player.Player).GetComponentsInChildren<Card>())
                    if (card.cardType.Equals("エール") && CardLib.GetAndFilterCards(player: Player.Player, color: new (){ "緑" }).Count > 0)
                        return true;
                return false;
        }
        return true;
    }
    private static int HoloPowerCost(string cardNumber, bool SP = false)
    {
        if (SP)
            switch (cardNumber)
            {
                case "hSD01-001":
                    return 2;
            }
        if (!SP)
            switch (cardNumber)
            {
                case "hSD01-001":
                    return 1;
            }
        return 0;
    }
    public static bool CheckForPlayRestrictions(string cardNumber)
    {
        switch (cardNumber)
        {
            case "hSD01-016":
                var deckCount = DuelField.INSTANCE.GetZone(Lib.GameZone.Deck, Player.Player).transform.childCount - 1;
                if (deckCount < 3)
                    return false;
                break;
            case "hSD01-021":
            case "hSD01-018":
            case "hBP01-111":
            case "hBP01-113":
                deckCount = DuelField.INSTANCE.GetZone(Lib.GameZone.Deck, Player.Player).transform.childCount - 1;
                if (deckCount == 0)
                    return false;
                break;
            case "hBP01-109":
            case "hBP01-102":
                if (DuelField.INSTANCE.GetZone(Lib.GameZone.Hand, Player.Player).transform.childCount > 6)
                    return false;
                break;
            case "hBP01-105":
            case "hSD01-019":
            case "hBP01-103":
                return CardLib.GetAndFilterCards(player: Player.Player, cardType: new() { "エール" }).Count > 0;
            case "hBP01-106":
                //check if we have back holomems to switch
                int backstagecount = CardLib.CountPlayerActiveHolomem(true);
                return (backstagecount > 0);
            case "hBP01-108":
            case "hBP01-112":
                //check if opponent have back holomems to switch
                backstagecount = CardLib.CountPlayerActiveHolomem(true, Player.Oponnent);
                return (backstagecount > 0);
            case "hSD01-020":
            case "hBP01-107":
                var energyList = DuelField.INSTANCE.GetZone(Lib.GameZone.Arquive, Player.Player).GetComponentsInChildren<Card>();
                foreach (Card card in energyList)
                {
                    if (card.cardType.Equals("エール"))
                    {
                        return true;
                    }
                }
                return false;
        }
        return true;
    }
    public static bool HasRestrictionsToPlayEquip(Card card, Card target)
    {
        switch (card.cardNumber)
        {
            case "hBP01-123":
                if (target.name.Equals("兎田ぺこら"))
                    return false;
                break;
            case "hBP01-122":
                if (target.name.Equals("アキ・ローゼンタール"))
                    return false;
                break;
            case "hBP01-126":
                if (target.name.Equals("尾丸ポルカ"))
                    return false;
                break;
            case "hBP01-125":
                if (target.name.Equals("小鳥遊キアラ"))
                    return false;
                break;
            case "hBP01-124":
                if (target.name.Equals("AZKi") || target.name.Equals("SorAZ"))
                    return false;
                break;
            case "hBP01-114":
            case "hBP01-116":
            case "hBP01-117":
            case "hBP01-118":
            case "hBP01-119":
            case "hBP01-120":
            case "hBP01-115":
            case "hBP01-121":
                foreach (Card _Card in card.attachedEquipe)
                {
                    _Card.cardNumber.Equals(card.cardNumber);
                    return false;
                }
                break;
        }
        return true;
    }
    internal static Lib.GameZone[] ZonesForList(List<Card> cards)
    {
        if (cards == null || cards.Count == 0)
            return Array.Empty<Lib.GameZone>();

        return cards
            .Where(c => c != null)
            .Select(c => c.curZone)
            .ToArray();
    }

    internal static List<Tuple<Card, bool>> GetActiveParent(Card[] cards)
    {
        var result = new List<Tuple<Card, bool>>();

        if (cards == null || cards.Length == 0)
            return result;

        var roots = cards.Where(c => c.father == null);

        foreach (var root in roots)
        {
            bool showRoot = true;
            bool showChildren = false;

            result.Add(Tuple.Create(root, showRoot));

            AddChildren(root.attachedEnergy, showChildren, result);
            AddChildren(root.attachedEquipe, showChildren, result);
            AddChildren(root.bloomChild, showChildren, result);
        }

        return result;
    }
    static void AddChildren(List<Card> list,bool active, List<Tuple<Card, bool>> result)
    {
        if (list == null)
            return;

        foreach (var go in list)
        {
            if (go.TryGetComponent<Card>(out var card))
                result.Add(Tuple.Create(card, active));
        }
    }
}

