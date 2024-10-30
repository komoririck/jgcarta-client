using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardPool : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab; // Assign your card prefab in the inspector
    [SerializeField] private int poolSize = 200; // Adjust this size based on your needs
    private List<GameObject> pool;

    private void Awake()
    {
        pool = new List<GameObject>();
        for (int i = 0; i < poolSize; i++)
        {
            GameObject card = Instantiate(cardPrefab);
            card.SetActive(false); // Initially inactive
            pool.Add(card);
        }
    }

    public GameObject GetCard()
    {
        foreach (GameObject card in pool)
        {
            if (!card.activeInHierarchy)
            {
                card.SetActive(true); // Activate the card when taken from the pool
                return card;
            }
        }
        return null; // Handle overflow as necessary
    }

    public void ReturnCard(GameObject card)
    {
        card.SetActive(false); // Deactivate the card when returning to the pool
    }
}