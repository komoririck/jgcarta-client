using System.Collections;
using TMPro;
using UnityEngine;

public class PhaseMessage : MonoBehaviour
{
    public RectTransform messageTransform;
    public TMP_Text messageText;
    public float movementSpeed = 1.0f;
    public float delayAtCenter = 1.0f;

    private Vector3 startPosition;
    private Vector3 centerPosition;
    private Vector3 endPosition;
    private Coroutine currentAnimationCoroutine;

    void Start()
    {
        InitPositions();
    }

    public void StartMessage(string phaseName)
    {
        // If there's an ongoing animation, stop it
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            InitPositions();
        }

        // Start the new message coroutine
        currentAnimationCoroutine = StartCoroutine(DisplayPhaseMessage(phaseName));
    }

    private void InitPositions()
    {
        startPosition = new Vector3(-Screen.width, 0, 0); // Off-screen to the left
        centerPosition = new Vector3(0, 0, 0);           // Center of the screen
        endPosition = new Vector3(Screen.width, 0, 0);   // Off-screen to the right
    }

    private IEnumerator DisplayPhaseMessage(string phaseName)
    {
        // Reset the message position
        messageTransform.anchoredPosition = startPosition;

        // Set the phase name
        messageText.text = phaseName;

        // Move from left to center
        yield return StartCoroutine(MoveToPosition(messageTransform, centerPosition, movementSpeed));

        // Wait at center for a short time
        yield return new WaitForSeconds(delayAtCenter);

        // Move from center to right
        yield return StartCoroutine(MoveToPosition(messageTransform, endPosition, movementSpeed));

        // Reset the animation coroutine reference
        currentAnimationCoroutine = null;

        // Reset position for the next message
        messageTransform.anchoredPosition = startPosition;
    }

    private IEnumerator MoveToPosition(RectTransform rectTransform, Vector3 targetPosition, float speed)
    {
        while (Vector3.Distance(rectTransform.anchoredPosition, targetPosition) > 0.1f)
        {
            rectTransform.anchoredPosition = Vector3.MoveTowards(rectTransform.anchoredPosition, targetPosition, speed * Time.deltaTime);
            yield return null;
        }
        rectTransform.anchoredPosition = targetPosition;
    }
}
