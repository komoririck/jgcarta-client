using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]  // Ensures the UI object has an Image component
public class GlowEffect : MonoBehaviour
{
    public bool isGlowing = true;   // The condition to control the glow
    private Material originalMaterial;
    private Material glowingMaterial;
    private Image image;

    // Variables to customize the glow
    public Color glowColor = Color.yellow;
    public float glowIntensity = 1.5f;

    void Start()
    {
        // Get the Image component
        image = GetComponent<Image>();

        // Create a copy of the material (so we can modify it without affecting others)
        originalMaterial = image.material;

        // Create a new material based on the original but with emission
        glowingMaterial = new Material(originalMaterial);
        glowingMaterial.EnableKeyword("_EMISSION");
    }

    void Update()
    {
        // Check if the glow condition is true
        if (isGlowing)
        {
            ApplyGlow();
        }
        else
        {
            RemoveGlow();
        }
    }

    void ApplyGlow()
    {
        // Set the emission color of the material (this creates the glow effect)
        glowingMaterial.SetColor("_EmissionColor", glowColor * glowIntensity);

        // Apply the glowing material to the Image
        image.material = glowingMaterial;
    }

    void RemoveGlow()
    {
        // Revert back to the original material when not glowing
        image.material = originalMaterial;
    }
}
