using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ZoneEnergyCounter : MonoBehaviour
{
    private int previousChildCount = 0;
    private GameObject anotherPrefab;
    private Dictionary<string, GameObject> colorPrefabs = new Dictionary<string, GameObject>();

    private void Awake()
    {
        anotherPrefab = Resources.Load<GameObject>("Presets/EnergyItem");
    }

    void Update()
    {
        if (transform.parent == null)
            return;

        int currentChildCount = transform.parent.childCount;
        if (currentChildCount != previousChildCount)
        {
            UpdateColorPrefabs();
            previousChildCount = currentChildCount;
        }
    }

    public void UpdateColorPrefabs()
    {
        //count colors
        Dictionary<string, int> colorCounts = new Dictionary<string, int>();
        foreach (Transform child in transform.parent.transform)
        {
            Card card = child.GetComponent<Card>();
            if (card == null) continue;

            List<string> toremoved = new ();
            foreach (KeyValuePair<string, GameObject> s in colorPrefabs)
            {
                Destroy(s.Value);
                toremoved.Add(s.Key);
            }

            foreach (string s in toremoved)
                colorPrefabs.Remove(s);

            if (string.IsNullOrEmpty(card.cardNumber)) continue;

            if (!(card.cardType == CardType.エール)) continue;

            string color = card.color.ToString();
            if (colorCounts.ContainsKey(color))
            {
                colorCounts[color]++;
            }
            else
            {
                colorCounts[color] = 1;
            }
        }
        //instantie
        foreach (var entry in colorCounts)
        {
            string color = entry.Key;
            int count = entry.Value;

            if (colorPrefabs.ContainsKey(color))
            {
                GameObject existingPrefab = colorPrefabs[color];
                TMP_Text countText = existingPrefab.transform.Find("Counter").GetComponent<TMP_Text>();
                countText.text = count.ToString();
            }
            else
            {
                GameObject newPrefab = Instantiate(anotherPrefab, transform.Find("EnergyList")); 
                colorPrefabs[color] = newPrefab;

                Image colorImage = newPrefab.transform.Find("Image").GetComponent<Image>();
                colorImage.sprite = GetColorSprite(color);

                TMP_Text countText = newPrefab.transform.Find("Counter").GetComponent<TMP_Text>();
                countText.text = count.ToString();
            }
        }
    }

    private Sprite GetColorSprite(string color)
    {
        switch (color.ToString())
        {
            case "無":
                return Resources.Load<Sprite>("Colors/arts_null");
            case "無色":
                return Resources.Load<Sprite>("Colors/arts_null");
            case "青":
                return Resources.Load<Sprite>("Colors/arts_blue");
            case "緑":
                return Resources.Load<Sprite>("Colors/arts_green");
            case "紫":
                return Resources.Load<Sprite>("Colors/arts_purple");
            case "白":
                return Resources.Load<Sprite>("Colors/arts_white");
            case "赤":
                return Resources.Load<Sprite>("Colors/arts_red");
            case "黄色":
                return Resources.Load<Sprite>("Colors/arts_yellow");
        }

        return null;
    }
}
