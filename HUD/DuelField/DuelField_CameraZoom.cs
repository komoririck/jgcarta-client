using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuelField_CameraZoom : MonoBehaviour
{
    public Camera cam;
    public float zoomSpeed = 10f;
    public float minFov = 20f;
    public float maxFov = 60f;

    void Update()
    {
        // Mouse Scroll Zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            cam.fieldOfView -= scroll * zoomSpeed;
            cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, minFov, maxFov);
        }

        // Pinch Zoom for Mobile
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            float prevDist = (touch0.position - touch0.deltaPosition - (touch1.position - touch1.deltaPosition)).magnitude;
            float currentDist = (touch0.position - touch1.position).magnitude;

            float deltaDistance = prevDist - currentDist;
            cam.fieldOfView += deltaDistance * Time.deltaTime * zoomSpeed;
            cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, minFov, maxFov);
        }
    }
}
