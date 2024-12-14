using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Card;

public class Deck : MonoBehaviour
{
    private HTTPSMaker _HTTPSMaker;
    [SerializeField] private GameObject ContentHolder;
    [SerializeField] private GameObject DeckPrefab;

    private void Start()
    {
        _HTTPSMaker = FindAnyObjectByType<HTTPSMaker>();
        StartCoroutine(GetDeckRequest());
    }
    public IEnumerator HandleGetDeckRequest()
    {
        yield return StartCoroutine(_HTTPSMaker.GetDeckRequest());
        _HTTPSMaker.returnMessage = "";
    }
    public IEnumerator GetDeckRequest()
    {
        yield return StartCoroutine(HandleGetDeckRequest());
        foreach (object deckObj in _HTTPSMaker.returnedObjects) {
            DeckData DeckData = (DeckData)deckObj;
            GameObject NewDeck = Instantiate(DeckPrefab, ContentHolder.transform);
            DeckInfo DeckComponent = NewDeck.AddComponent<DeckInfo>();

            DeckComponent.deckId = DeckData.deckId;
            DeckComponent.deckName = DeckData.deckName;
            DeckComponent.main = DeckData.main;
            DeckComponent.energy = DeckData.energy;
            DeckComponent.oshi = DeckData.oshi;

            NewDeck.AddComponent<Button>().onClick.AddListener(() => OnItemClick(DeckComponent));
        }

        _HTTPSMaker.returnedObjects.Clear();
    }

    private void OnItemClick(DeckInfo deckComponent)
    {
        GameObject DeckInfoHolder = new GameObject("DeckInfoHolder");
        DeckInfo DeckInfoHolderComponent = DeckInfoHolder.AddComponent<DeckInfo>();

        DeckInfoHolderComponent.deckId = deckComponent.deckId;
        DeckInfoHolderComponent.deckName = deckComponent.deckName;
        DeckInfoHolderComponent.main = deckComponent.main;
        DeckInfoHolderComponent.energy = deckComponent.energy;
        DeckInfoHolderComponent.oshi = deckComponent.oshi;
        DeckInfoHolderComponent.status = deckComponent.status;

        DontDestroyOnLoad(DeckInfoHolder);
        SceneManager.LoadScene("DeckEditor");
    }

    public void CreateNewDeck() {
        SceneManager.LoadScene("DeckEditor");
    }

    public class DeckInfo : MonoBehaviour
    {
        public string deckId { get; set; }
        public string deckName { get; set; }
        public string main { get; set; }
        public string energy { get; set; }
        public string oshi { get; set; }
        public string status { get; set; }
    }
}
