using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ZoneEnergyCounter : MonoBehaviour
{
    private int previousChildCount = 0;
    public GameObject anotherPrefab; // Prefab to instantiate
    private Dictionary<string, GameObject> colorPrefabs = new Dictionary<string, GameObject>();

    void Update()
    {
        int currentChildCount = transform.parent.childCount;
        if (currentChildCount != previousChildCount)
        {
            UpdateColorPrefabs();
            previousChildCount = currentChildCount;
        }
    }

    // Call this method whenever cards are added or removed
    public void UpdateColorPrefabs()
    {
        // Clear counts for existing colors
        Dictionary<string, int> colorCounts = new Dictionary<string, int>();

        // Step 1: Count Cards by Color
        foreach (Transform child in transform.parent.transform)
        {
            Card card = child.GetComponent<Card>();
            if (card == null || !card.cardType.Equals("エール")) continue;

            string color = card.color;
            if (colorCounts.ContainsKey(color))
            {
                colorCounts[color]++;
            }
            else
            {
                colorCounts[color] = 1;
            }
        }

        // Step 2: Update or Instantiate Prefabs based on the counts
        foreach (var entry in colorCounts)
        {
            string color = entry.Key;
            int count = entry.Value;

            if (colorPrefabs.ContainsKey(color))
            {
                // Update the count if the prefab already exists
                GameObject existingPrefab = colorPrefabs[color];
                TMP_Text countText = existingPrefab.transform.Find("Counter").GetComponent<TMP_Text>();
                countText.text = count.ToString();
            }
            else
            {
                // Instantiate new prefab for this color
                GameObject newPrefab = Instantiate(anotherPrefab, transform); // Assuming the parent is set as needed
                colorPrefabs[color] = newPrefab;

                // Set the color-specific sprite
                Image colorImage = newPrefab.transform.Find("Image").GetComponent<Image>();
                colorImage.sprite = GetColorSprite(color); // Replace with actual method to get sprite by color

                // Set the initial count
                TMP_Text countText = newPrefab.transform.Find("Counter").GetComponent<TMP_Text>();
                countText.text = count.ToString();
            }
        }

        // Step 3: Remove Prefabs for Colors No Longer Present
        List<string> colorsToRemove = new List<string>();
        foreach (var color in colorPrefabs.Keys)
        {
            if (!colorCounts.ContainsKey(color))
            {
                colorsToRemove.Add(color);
            }
        }
        foreach (string color in colorsToRemove)
        {
            Destroy(colorPrefabs[color]);
            colorPrefabs.Remove(color);
        }
    }

    private Sprite GetColorSprite(string color)
    {
        switch (color)
        {
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
