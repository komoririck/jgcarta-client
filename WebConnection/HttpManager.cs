using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class HttpManager : MonoBehaviour
{
    [SerializeField] public bool waitingForARequest = false;
    [SerializeField] private GameObject canvas;
    [SerializeField] private GameObject alertLoadingPopup;
    [SerializeField] private GameObject alertLoadingPopupPrefab;

    private void Start()
    {
        UpdateCanvas();
    }

    private void Update()
    {
        if (!waitingForARequest)
        {
            Destroy(alertLoadingPopupPrefab);
        }
        if (waitingForARequest && alertLoadingPopupPrefab == null)
        {
            if (canvas == null)
                UpdateCanvas();

            alertLoadingPopupPrefab =  Instantiate(alertLoadingPopup, Vector3.zero, Quaternion.identity);
            alertLoadingPopupPrefab.transform.SetParent(canvas.transform, false);
            alertLoadingPopupPrefab.SetActive(true);
        }

    }

    private void UpdateCanvas() {

        canvas = GameObject.Find("Canvas");
    }

    public IEnumerator PostRequest(string url, string jsonData, System.Action<string> onSuccess, System.Action<string> onError)
    {
        using (UnityWebRequest request = new(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");


            waitingForARequest = true;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                onError?.Invoke(request.error);
            }
            else
            {
                onSuccess?.Invoke(request.downloadHandler.text);
            }
            waitingForARequest = false;
        }
    }
}
