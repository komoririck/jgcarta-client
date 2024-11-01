using System.Linq;
using UnityEngine;
using System.Collections.Generic;

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
}


public class FileReader : MonoBehaviour
{
    public string fileName = "CardList"; // Ensure file is named CardList.txt in the Resources folder
    public static List<Record> result = new();

    void Start()
    {
        List<Record> records = null;
        if (result.Count == 0) { 
           records = ReadTextFile(fileName);
        }
        if (records != null)
        {
            Debug.Log($"Successfully read {records.Count} records.");
        }
    }

    public static List<Record> ReadTextFile(string fileName)
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
                Tag = parts[14]
            };

            //Debug.Log($"Loaded Record: {record.Name}");

            result.Add(record);
        }

        return result;
    }

    public static List<Record> QueryRecords(string name = null, string type = null)
    {
        var query = result.AsQueryable();

        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(r => r.Name == name);
        }

        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(r => r.CardType == type);
        }

        return query.ToList();
    }


    public static List<Record> QueryRecordsByNameAndBloom(List<string> numbers, string type)
    {
        var query = result.Where(r => numbers.Contains(r.CardNumber) && r.BloomLevel == type);

        return query.ToList();
    }
    public static List<Record> QueryRecordsByNames(List<string> names)
    {
        var query = result.Where(r => names.Contains(r.Name));

        return query.ToList();
    }

    public static List<Record> QueryRecordsByType(List<string> type)
    {
        List<Record> record = new List<Record>();
        foreach (string tp in type) {
            record.AddRange(result.Where(r => r.CardType == tp));
        }

        return record.ToList();
    }
    public static List<Record> QueryBloomableCard(string name, string bloomLevel)
    {
        string nextBloomLevel = "";
        switch (bloomLevel)
        {
            case "Debut":
                nextBloomLevel = "1st";
                break;
            case "1st":
                nextBloomLevel = "2nd";
                break;
        }

        var query = result.Where(r => r.Name == name && r.BloomLevel == nextBloomLevel);
        return query.ToList();
    }
    public static List<Record> QueryBloomablePreviousCard(string name, string bloomLevel)
    {
        string nextBloomLevel = "";
        switch (bloomLevel)
        {
            case "1st":
                nextBloomLevel = "Debut";
                break; 
            case "2nd":
                nextBloomLevel = "1st";
                break;
        }

        var query = result.Where(r => r.Name == name && r.BloomLevel == nextBloomLevel);
        return query.ToList();
    }

    public static List<Card> ConvertRecordToCardList(List<Record> list)
    {
        List<Card> cards = new();
        foreach (Record r in list) {
            Card cnew = new Card(r.CardNumber);
            cards.Add(cnew);
        }
        return cards;
    }

}

