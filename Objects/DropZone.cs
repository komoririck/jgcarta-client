using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DropZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool isHovered = false; // To track if the drop zone is being hovered

    public string zoneType; // Assign a unique type (or identifier) for each drop zone

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true; // When pointer (dragged item) enters this zone
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false; // When pointer (dragged item) exits this zone
    }

    static public GameObject GetZoneByName(string name, DropZone[] zones) {
        foreach (DropZone zone in zones) {
            if (zone.gameObject.name.Equals(name)) {
                return zone.gameObject;
            }
        }
        return null;
    }
}
