using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DeckData
{
    public string deckId { get; set; }
    public string deckName { get; set; }
    public string main { get; set; }
    public string energy { get; set; }
    public string oshi { get; set; }
    public string status { get; set; }
}