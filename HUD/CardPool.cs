using System.Collections;
using System.Collections.Generic;
using UnityEngine;






public class CardPool : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab; // Assign your card prefab in the inspector
    [SerializeField] private int poolSize = 200; // Adjust pool size as needed
    private List<GameObject> pool;

    private void Awake()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        pool = new List<GameObject>(poolSize); // Initialize list with capacity for efficiency
        for (int i = 0; i < poolSize; i++)
        {
            AddNewCardToPool();
        }
    }

    private void AddNewCardToPool()
    {
        GameObject card = Instantiate(cardPrefab);
        card.SetActive(false); // Set inactive for pool
        pool.Add(card);
    }

    public GameObject GetCard()
    {
        foreach (GameObject card in pool)
        {
            if (!card.activeInHierarchy)
            {
                card.SetActive(true); // Activate the card for use
                return card;
            }
        }

        // Optionally handle overflow by expanding the pool if needed
        Debug.LogWarning("Expanding pool: all cards are in use.");
        AddNewCardToPool();
        GameObject newCard = pool[pool.Count - 1];
        newCard.SetActive(true);
        return newCard;
    }

    public void ReturnCard(GameObject card)
    {
        card.SetActive(false); // Deactivate to return to pool
        // Optionally, reset card state here if it has any unique properties
    }

    public List<GameObject> GetAllCards()
    {
        return pool; // Expose entire pool list if needed for external operations
    }

    public int PoolSize => pool.Count; // Property to access current pool size if needed
}