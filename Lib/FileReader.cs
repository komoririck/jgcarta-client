using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using System;

public class Record
{


    public string Name { get; set; }
    public string CardType { get; set; }
    public string Rarity { get; set; }
    public string Product { get; set; }
    public string Color { get; set; }
    public string HP { get; set; }
    public string BloomLevel { get; set; }
    public string Arts { get; set; }
    public string OshiSkill { get; set; }
    public string SPOshiSkill { get; set; }
    public string AbilityText { get; set; }
    public string Illustrator { get; set; }
    public string CardNumber { get; set; }
    public string Life { get; set; }
    public string Tag { get; set; }
    public string ArtEffect { get; set; }
}


public class FileReader : MonoBehaviour
{
    public string fileName = "CardList"; // Ensure file is named CardList.txt in the Resources folder
    public static Dictionary<string, Record> result = new();

    void Start()
    {
        if (result.Count == 0) {
            ReadTextFile(fileName);
        }
    }

    public static Dictionary<string, Record> ReadTextFile(string fileName)
    {
        // Load the .txt file as a TextAsset from the Resources folder
        TextAsset textFile = Resources.Load<TextAsset>(fileName);

        // Check if the file was found
        if (textFile == null)
        {
            Debug.LogError($"File not found: {fileName}.txt in Resources folder.");
            return null;
        }

        // Split the file content into lines
        string[] lines = textFile.text.Split('\n');

        // Skip the header line and process the rest
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrEmpty(line))
                continue; // Skip empty lines

            // Split line into parts, expecting comma separation
            string[] parts = line.Split(','); // Adjust based on actual data delimiter

            // Check for correct data length
            if (parts.Length < 14)
            {
                Debug.LogError($"Invalid data format in line {i + 1}: {line}");
                continue;
            }

            // Create a new Record from the line data
            Record record = new()
            {
                Name = parts[0],
                CardType = parts[1],
                Rarity = parts[2],
                Product = parts[3],
                Color = parts[4],
                HP = parts[5],
                BloomLevel = parts[6],
                Arts = parts[7],
                OshiSkill = parts[8],
                SPOshiSkill = parts[9],
                AbilityText = parts[10],
                Illustrator = parts[11],
                CardNumber = parts[12],
                Life = parts[13],
                Tag = parts[14],
                ArtEffect = parts[17]
            };

            try {
                result.TryAdd(parts[12], record);
            } 
            catch (Exception e) 
            { 
            }
        }

        return result;
    }
}

