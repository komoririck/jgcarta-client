using System.Collections;
using TMPro;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class PhaseMessage : MonoBehaviour
{
    public RectTransform messageTransform;
    public TMP_Text messageText;

    private Vector3 startPosition;
    private Vector3 centerPosition;
    private Vector3 endPosition;

    private float stateTimer = 0f;

    private bool isRunning = false;
    private float timer = 0f;

    private enum MoveState { Idle, MoveX, Wait, MoveY, Done }
    private MoveState state = MoveState.Idle;

    void Start()
    {
        startPosition = new Vector3(-950, 0, 0);
        centerPosition = new Vector3(0, 0, 0);
        endPosition = new Vector3(950, 0, 0);
    }

    void Update()
    {
        if (!isRunning) return;

        timer += Time.deltaTime;

        if (timer > 4f)
        {
            StopStop();
            return;
        }

        DuelField.INSTANCE.NeedsOrganize = true;

        switch (state)
        {
            case MoveState.MoveX:
                UpdateMove(startPosition, centerPosition, 1f, MoveState.Wait);
                break;

            case MoveState.Wait:
                UpdateWait(1f, MoveState.MoveY);
                break;

            case MoveState.MoveY:
                UpdateMove(centerPosition, endPosition, 1f, MoveState.Done);
                break;

            case MoveState.Done:
                StopStop();
                break;
        }
    }
    public void StartMessage(string phaseName)
    {
        if (isRunning)
            StopStop();
        
        startPosition = messageTransform.anchoredPosition;

        state = MoveState.MoveX;
        timer = 0f;
        isRunning = true;

        messageText.text = phaseName;
    }
    private void UpdateMove(Vector2 from, Vector2 to, float duration, MoveState next)
    {
        stateTimer += Time.deltaTime;
        float t = Mathf.Clamp01(stateTimer / duration);
        messageTransform.anchoredPosition = Vector2.Lerp(from, to, t);

        if (t >= 1f)
        {
            state = next;
            stateTimer = 0f;
        }
    }
    private void UpdateWait(float duration, MoveState next)
    {
        stateTimer += Time.deltaTime;
        if (stateTimer >= duration)
        {
            state = next;
            stateTimer = 0f;
        }
    }
    private void StopStop()
    {
        isRunning = false;
        state = MoveState.Idle;
        stateTimer = 0f;
        timer = 0f;
        messageTransform.anchoredPosition = new Vector3(-950, 0, 0);
        DuelField.INSTANCE.NeedsOrganize = false;
    }
}
