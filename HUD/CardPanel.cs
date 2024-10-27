using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardPanel : MonoBehaviour
{
    public GameObject CardPanelObject;
    public Card CardPanelInfoObject;
    public GameObject ArtPanelContent;
    public GameObject ArtPanelContentPrefab;
    public void hiddePainelButton() {
        CardPanelObject.SetActive(false);
    }
}
