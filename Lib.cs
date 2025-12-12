using System;
using UnityEngine;

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
}
