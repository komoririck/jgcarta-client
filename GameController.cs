using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    static public GameController INSTANCE;

    public string Language = "JP";
    [SerializeField] public bool waitingForARequest = false;
    [SerializeField] public GameObject canvas;
    [SerializeField] public GameObject alertLoadingPopupPrefab;
    GameObject alertLoadingPopup;

    public float DuelSpeed = 0.2f;
    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        INSTANCE = FindAnyObjectByType<GameController>();
    }
    private void Update()
    {
        if (GameController.INSTANCE.canvas == null)
            GameController.INSTANCE.canvas = FindAnyObjectByType<Canvas>().gameObject;

        if (!GameController.INSTANCE.waitingForARequest)
        {
            Destroy(alertLoadingPopup);
        }
        if (GameController.INSTANCE.waitingForARequest && GameController.INSTANCE.alertLoadingPopup == null)
        {
            alertLoadingPopup = Instantiate(GameController.INSTANCE.alertLoadingPopupPrefab, Vector3.zero, Quaternion.identity);
            alertLoadingPopup.transform.SetParent(GameController.INSTANCE.canvas.transform, false);
            alertLoadingPopup.SetActive(true);
        }
    }
}
