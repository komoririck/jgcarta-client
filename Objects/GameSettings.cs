using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSettings : MonoBehaviour
{
    public string OnlineMode = "S";
    public string Language = "EN";
    public int GameBGMVolume = 100;
    public int GameEffectVolume = 100;
    public string GameFullScreen = "N";
    public int GameResolution = 1;
    public List<string> GameUpdates = new();
    public List<Card> ForbbidenCardList = new();
}
