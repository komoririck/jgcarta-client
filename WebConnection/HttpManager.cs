using Newtonsoft.Json.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using static Google.Apis.Requests.BatchRequest;

public class HttpManager : MonoBehaviour
{
    public IEnumerator MakeRequest(string url, string jsonData, System.Action<string> onSuccess, System.Action<string> onError, string requestType = "POST")
    {
        using (UnityWebRequest request = new(url, requestType))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("Password", null));

            Debug.Log($"Raw JSON Response:\n{request.downloadHandler.text}");

            GameController.INSTANCE.waitingForARequest = true;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                onError?.Invoke(request.error);
            }
            else if(request.responseCode >= 200 && request.responseCode < 300)
            {
                onSuccess?.Invoke(request.downloadHandler.text);
            }
            GameController.INSTANCE.waitingForARequest = false;
        }
    }
}
