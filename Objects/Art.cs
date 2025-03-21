﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows;

public class Art : MonoBehaviour
{
    public string Name;
    public List<(string Color, int Amount)> Cost { get; set; } = new List<(string, int)>();
    public (string Color, int Amount) Damage { get; set; }
    public (string Color, int Amount) DamageMultiplier { get; set; }
    public (string Color, int Amount) ExtraColorDamage { get; set; }
    public string Effect;

    public static Art ParseArtFromString(string artString, string artEffect)
    {
        var parts = artString.Split(':');
        if (parts.Length != 5)
            return null;

        // Create a new instance of Art to hold the parsed data
        var art = new Art();

        // Parse costs (example: "1x無色1x白")
        string costString = parts[0];
        var splitedCosts = costString.Split('*');

        foreach (string cost in splitedCosts)
        {
            var splitedcostString = cost.Split('x');
            // Ensure we have two parts
            if (splitedcostString.Length == 2)
            {
                // Try to parse the first part as an integer
                if (int.TryParse(splitedcostString[0], out int number))
                {
                    string text = splitedcostString[1]; // Get the string part
                    art.Cost.Add((text, number));
                }
            }
        }

        // Parse name (example: "あなたの心は…くもりのち晴れ！")
        art.Name = parts[1];
        art.Effect = artEffect;

        // Parse damage (example: "50x1白")
        var damageParts = parts[2].Split('x');
        int damageValue = int.Parse(damageParts[0]);
        string damageColor = damageParts[1];

        // Parse damage multiplier (example: "0x1白")
        var multiplierParts = parts[3].Split('x');
        int multiplierValue = int.Parse(multiplierParts[0]);
        string multiplierColor = multiplierParts[1];

        // Parse extra color damage (example: "青0")
        string extraColor = parts[4].Substring(0, 1);
        int extraColorDamage = 0;
        int numberStartIndex = parts[4].Length - 1;
        while (numberStartIndex > 0 && !char.IsDigit(parts[4][numberStartIndex]))
            numberStartIndex--;
        
        // Extract the color part and parse the damage part
        extraColor = parts[4].Substring(0, numberStartIndex);
        extraColorDamage = int.Parse(parts[4].Substring(numberStartIndex));

        // Set the parsed values to the Art object
        art.Damage = (damageColor, damageValue);
        art.DamageMultiplier = (multiplierColor, multiplierValue);
        art.ExtraColorDamage = (extraColor, extraColorDamage);

        // Return the populated Art object
        return art;
    }
}
public class ArtCalculator
{
    public static int CalculateTotalDamage(Art art, List<Card> costs, string extraColor)
    {
        // Count the occurrences of each color in the list of cost objects
        var colorCount = costs.GroupBy(cost => cost.color).ToDictionary(g => g.Key, g => g.Count());

        // Base damage calculation
        int baseDamage = 0;
        foreach (var cost in art.Cost)
        {
            if (colorCount.ContainsKey(cost.Color))
            {
                baseDamage += art.Damage.Amount * colorCount[cost.Color];
            }
        }
        Console.WriteLine($"Base Damage: {baseDamage}");

        // Multiplier calculation
        int totalDamage = baseDamage;
        if (colorCount.ContainsKey(art.DamageMultiplier.Color))
        {
            int multiplier = art.DamageMultiplier.Amount * colorCount[art.DamageMultiplier.Color];
            totalDamage += multiplier;
            Console.WriteLine($"Multiplier Applied: {multiplier} (Multiplier: {art.DamageMultiplier.Amount})");
        }

        // Extra color damage calculation
        if (art.ExtraColorDamage.Color == extraColor)
        {
            totalDamage += art.ExtraColorDamage.Amount;
            Console.WriteLine($"Extra Color Damage Added: {art.ExtraColorDamage.Amount} (Extra Color: {extraColor})");
        }

        return totalDamage;
    }
}