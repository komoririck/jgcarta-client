using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RectTransformData
{
    public Vector2 anchoredPosition;
    public Vector2 sizeDelta;
    public Vector3 localScale;
    public Quaternion localRotation;
    public Vector2 pivot;
    public Vector2 anchoredPosition3D;

    public RectTransformData(RectTransform rectTransform)
    {
        anchoredPosition = rectTransform.anchoredPosition;
        sizeDelta = rectTransform.sizeDelta;
        localScale = rectTransform.localScale;
        localRotation = rectTransform.localRotation;
        pivot = rectTransform.pivot;
        anchoredPosition3D = rectTransform.anchoredPosition3D;
    }
    public RectTransform ApplyToRectTransform(RectTransform rectTransform = null)
    {
        if (rectTransform == null) {
            rectTransform = new RectTransform();
           rectTransform.anchoredPosition = Vector2.zero;
        }

        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.localScale = localScale;
        rectTransform.localRotation = localRotation;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition3D = anchoredPosition3D;
        return rectTransform;
    }
}