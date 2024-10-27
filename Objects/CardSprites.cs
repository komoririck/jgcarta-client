using System.Collections.Generic;
using UnityEngine;

public class CardSprites : MonoBehaviour
{
    static public List<Sprite> imgs;

    void Start()
    {
        // Load all sprites from the "CardImages" folder inside the "Resources" folder
        Sprite[] loadedSprites = Resources.LoadAll<Sprite>("CardImages");

        // Add them to the List
        imgs = new List<Sprite>(loadedSprites);

        // You can now access and use the imgs list
        Debug.Log("Number of sprites loaded: " + imgs.Count);
    }

    static public Sprite GetSprite(string number) {
        foreach (Sprite sprite in imgs) { 
            sprite.name = number;
            return sprite;
        }
        return null;
    }
}