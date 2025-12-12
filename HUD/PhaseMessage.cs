using System.Collections;
using TMPro;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class PhaseMessage : MonoBehaviour
{
    public static RectTransform messageTransform;
    public static TMP_Text messageText;

    private static Vector3 startPosition = new Vector3(-950, 0, 0);
    private static Vector3 centerPosition = new Vector3(0, 0, 0);
    private static Vector3 endPosition = new Vector3(950, 0, 0);

    private void Awake()
    {
        messageTransform = GetComponent<RectTransform>();
        messageText = GetComponent<TMP_Text>();
    }
    static public IEnumerator ShowMessage(string phaseName, float moveDuration = 1f, float waitDuration = 1f)
    {
        messageTransform.anchoredPosition = startPosition;

        messageText.text = phaseName;
        messageText.gameObject.SetActive(true);

        yield return MoveRect(messageTransform, startPosition, centerPosition, moveDuration);

        yield return new WaitForSeconds(waitDuration);

        yield return MoveRect(messageTransform, centerPosition, endPosition, moveDuration);

        messageText.gameObject.SetActive(false);
        messageTransform.anchoredPosition = startPosition;
    }
    private static IEnumerator MoveRect(RectTransform rect, Vector3 from, Vector3 to, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            rect.anchoredPosition = Vector3.Lerp(from, to, t);
            yield return null;
        }
        rect.anchoredPosition = to;
    }
}