using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Lib : MonoBehaviour
{
    [Flags]
    public enum GameZone : byte
    {
        na = 0,
        Hand = 1,
        Deck = 2,
        Arquive = 3,
        Life = 4,
        CardCheer = 5,
        Stage = 6,
        BackStage1 = 7,
        BackStage2 = 8,
        BackStage3 = 9,
        BackStage4 = 10,
        BackStage5 = 11,
        Collaboration = 12,
        Favourite = 13,
        HoloPower = 18,
    }
    public static List<Card> temp = new();
    public static List<Card> ConvertToCard(List<CardData> CardDataList)
    {
        var CardList = new List<Card>();
        foreach (CardData cd in CardDataList)
        {
            GameObject obj = Instantiate(DuelField.INSTANCE.cardPrefab);
            Card card = obj.GetComponent<Card>();
            card.Init(cd);
            obj.gameObject.SetActive(false);
            CardList.Add(card);
            temp.Add(card);
        }
        return CardList;
    }
}
