using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class HttpManager : MonoBehaviour
{
    public IEnumerator MakeRequest(string url, Request jsonData, System.Action<string> onSuccess, System.Action<string> onError, string requestType = "POST")
    {

        var settings = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Ignore,
            Formatting = Formatting.Indented,
        };

        using (UnityWebRequest request = new(url, requestType))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(jsonData, settings));
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
