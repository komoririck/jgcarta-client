using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using static DuelField;
using static Lib;

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
    public static List<Card>? GetAndFilterCards(CardFilter? filter = null, List<Card>? CardList = null, List<CardType>? cardType = null, List<BloomLevel>? bloomLevel = null, List<string>? cardNumber = null, List<ColorCard>? color = null,
        List<string>? name = null, bool? onlyVisible = null, GameZone[]? gameZones = null, Player? player = null, bool? OnlyHolomem = null, bool? OnlyLimited = null, bool? CardThatAllowReRoll = null,
        List<string>? ContainTags = null, bool? OnlyBlommable = null, bool? OnlyEquipable = null, bool? isMascot = null, CardData? CardToBeFound = null, bool? Suspended = null, string? resolutionState = null,
        GameZone? matchZoneColor = null, int? last = null, bool? playedThisTurn = null, List<string>? NameExcluded = null, GameZone[]? playedFrom = null, bool? Attachments = null, int? minHp = null, int? missingHpAtLeast = null,
        int? exactHp = null, string? effectTiming = null, bool? CheckActivation = null, bool? IsEnergy = null, bool? NameInEffect = null, bool? IsKnocked = null, Card? TriggerSource = null, bool? isTool = null, bool? isSuport = null,
        bool? isToolMascotSuport = null)
    {

        if (CardToBeFound != null)
        {
            gameZones = new[] { CardToBeFound.curZone };
            cardNumber = new() { CardToBeFound.cardNumber };
        }

        if (OnlyHolomem != null)
            cardType = new() { CardType.ホロメン, CardType.Buzzホロメン };

        if (IsEnergy != null)
            cardType = new() { CardType.エール };

        if (OnlyLimited != null)
            cardType = new() { CardType.サポートアイテムLIMITED, CardType.サポートイベントLIMITED, CardType.サポートスタッフLIMITED };

        if (isToolMascotSuport != null)
            cardType = new() { CardType.サポートツール, CardType.サポートファン, CardType.サポートアイテム, CardType.サポートアイテムLIMITED, CardType.サポートマスコット };

        if (isSuport != null)
            cardType = new() { CardType.サポートツール, CardType.サポートファン };

        if (isTool != null)
            cardType = new() { CardType.サポートアイテム, CardType.サポートアイテムLIMITED };

        if (isMascot != null)
            cardType = new() { CardType.サポートマスコット };

        if (CardList == null && gameZones == null)
            gameZones = DuelField.DEFAULTALL;

        //If no CardList is provided, get all cards from the specified zones
        if (CardList == null && gameZones != null)
        {
            if (player == null || player.Equals(Player.na))
            {
                CardList = gameZones.SelectMany(zone => DuelField.INSTANCE.GetZone(zone, Player.Player).GetComponentsInChildren<Card>(includeInactive: true)).ToList();
                CardList.AddRange(gameZones.SelectMany(zone => DuelField.INSTANCE.GetZone(zone, Player.Oponnent).GetComponentsInChildren<Card>(includeInactive: true)).ToList());
            }
            else
            {
                CardList = gameZones.SelectMany(zone => DuelField.INSTANCE.GetZone(zone, (Player)player).GetComponentsInChildren<Card>(includeInactive: true)).ToList();
            }
        }
        else if (gameZones != null)
        {
            CardList = CardList.FindAll(card => gameZones.Contains(card.curZone));
        }

        //Filter by required
        if (Attachments != null)
        {
            List<Card> finalList = new();
            foreach (Card card in CardList)
            {
                finalList.AddRange(card.attachedEnergy);
                finalList.AddRange(card.attachedEquipe);
                finalList.AddRange(card.bloomChild);
            }
            CardList = finalList;
        }
        if (playedFrom != null)
        {
            CardList = CardList.FindAll(card => playedFrom.Contains(card.lastZone));
        }
        if (matchZoneColor != null)
        {
            color ??= new();
            color.Add((ColorCard)GetAndFilterCards(player: player, gameZones: new[] { (GameZone)matchZoneColor }, onlyVisible: true).FirstOrDefault().color);
        }
        if (cardNumber != null)
        {
            CardList = CardList.FindAll(card => cardNumber.Contains(card.cardNumber));
        }
        if (onlyVisible != null)
        {
            CardList = CardList.FindAll(card => card.gameObject.activeSelf == true);
        }
        if (cardType != null)
        {
            CardList = CardList.FindAll(card => cardType.Contains((CardType)card.cardType));
        }
        if (bloomLevel != null)
        {
            CardList = CardList.FindAll(card => bloomLevel.Contains((BloomLevel)card.bloomLevel));
        }
        if (color != null)
        {
            CardList = CardList.FindAll(card => color.Contains((ColorCard)card.color));
        }
        if (ContainTags != null && ContainTags.Count > 0)
        {
            CardList = CardList.FindAll(card => ContainTags.All(tag => card.cardTag.Contains(tag)));
        }
        if (Suspended != null)
        {
            CardList = CardList.FindAll(card => card.suspended == Suspended);
        }
        if (minHp != null)
        {
            CardList = CardList.FindAll(card => card.currentHp > minHp);
        }
        if (missingHpAtLeast != null)
        {
            CardList = CardList.FindAll(card =>
                (int.Parse(card.hp) - card.currentHp) >= missingHpAtLeast);
        }
        if (exactHp != null)
        {
            CardList = CardList.FindAll(card => int.Parse(card.hp) == exactHp);
        }
        if (OnlyBlommable != null)
        {
            List<Card> checkedCards = new();
            foreach (Card cardk in CardList)
            {
                var level = cardk.bloomLevel.Equals(BloomLevel.debut) ? BloomLevel.st1 : BloomLevel.nd2;
                if (GetAndFilterCards(player: player, OnlyHolomem: true, playedThisTurn: false, name: new() { cardk.cardName }, bloomLevel: new() { level }).FirstOrDefault() != null)
                    checkedCards.Add(cardk);
            }
            CardList = checkedCards;
        }
        if (playedThisTurn != null)
        {
            CardList = CardList.FindAll(card => card.playedThisTurn);
        }
        //EXCLUSIONS
        if (NameExcluded != null)
        {
            CardList = CardList.FindAll(card => !NameExcluded.Contains(card.cardName));
        }
        if (last != null && last > 0 && CardList.Count > last)
        {
            CardList = CardList.TakeLast((int)last).ToList();
        }
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
    public static int CountPlayerActiveHolomem(bool onlyBackstage = false, Player target = Player.Player)
    {
        List<Lib.GameZone> x = new();
        x.AddRange(DuelField.DEFAULTBACKSTAGE);
        x.Add(Lib.GameZone.Favourite);

        return GetAndFilterCards(player: target, gameZones: x.ToArray(), OnlyHolomem: true, onlyVisible: true).Count;
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
    internal static Lib.GameZone[] ZonesForList(List<Card> cards)
    {
        if (cards == null || cards.Count == 0)
            return Array.Empty<Lib.GameZone>();

        return cards
            .Where(c => c != null)
            .Select(c => c.curZone)
            .ToArray();
    }

    internal static List<Tuple<Card, bool>> GetActiveParent(List<Card> cards)
    {
        var result = new List<Tuple<Card, bool>>();

        if (cards == null || cards.Count == 0)
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

