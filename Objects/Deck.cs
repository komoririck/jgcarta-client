using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour
{

    public int DeckID = 0;
    public string DeckName = "";
    public string DeckOwner = "";
    public Card OshiCard = null;
    public List<Card> MainDeck = new();
    public List<Card> ExtraDeck = new();
    public List<Card> GraveYardDeck = new();
    public List<Card> BanishedDeck = new();

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
