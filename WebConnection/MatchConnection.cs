using Assets.Scripts.HUD.DuelField;
using NativeWebSocket;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchConnection : MonoBehaviour
{
    public static MatchConnection INSTANCE;
    public readonly Uri WebSocketConnectionUrl = new("wss://localhost:7047/ws");
    public List<Request> PendingActions = new();

    private WebSocket _webSocket;
    internal async void CloseConnection()
    {
        PendingActions.Clear();
        if (_webSocket.State.Equals(WebSocketState.Open))
            await _webSocket.Close();
    }
    internal async void StartConnection()
    {
        INSTANCE = this;
        _webSocket = new WebSocket(WebSocketConnectionUrl.ToString());

        _webSocket.OnOpen += () =>
        {
            StartCoroutine(TryToRequest("Waitingforopponent", 1, 5000f, SendRequest(null, "JoinPlayerQueueList")));
        };

        _webSocket.OnMessage += (bytes) =>
        {
            string json = Encoding.UTF8.GetString(bytes);
            string jsonString = System.Text.Encoding.UTF8.GetString(bytes);
            try
            {
                var response = JsonConvert.DeserializeObject<Request>(json);
                if (response == null)
                    return;

                PendingActions.Add(response);

                if (response.description == "StartDuel")
                {
                    GameLifecycle.Set(GameState.LoadingScene);
                    StartCoroutine(HandleLoadNewScene("DuelField"));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{e}\n{json}");
            }
        };

        _webSocket.OnError += (e) =>
        {
            Debug.Log("Connection error.");
        };

        _webSocket.OnClose += (e) =>
        {
            Debug.Log("Connection closed.");
        };

        await _webSocket.Connect();

    }
    private void Update()
    {

        #if !UNITY_WEBGL || UNITY_EDITOR
        if(_webSocket != null && _webSocket.State == WebSocketState.Open)
            _webSocket.DispatchMessageQueue();
        #endif
    }

    public IEnumerator TryToRequest(string expectedResponse, int maxTry, float timeInterval, Task asyncFunc, Action updateReturn = null) { 
        bool shouldAsk = true;
        int connectionRetry = 0;
        float timer = 0f;

        Request _requestData = new() { type = null };

        do {
            if (shouldAsk)
            {
                yield return new WaitUntil(() => asyncFunc.IsCompleted);
                timer = 0f;
                connectionRetry++;
                shouldAsk = false;
            }

            timer += Time.deltaTime;
            if (timer >= timeInterval)
            {
                timer = 0f;
                shouldAsk = true;
            }

            if (_requestData.type.Equals(expectedResponse)) 
            {
                updateReturn();
                _requestData.type = null;

                yield break;
            }
        } while (connectionRetry > maxTry);

        if (connectionRetry < maxTry) { }
        //            SceneManager.LoadScene("Login");
    }

    public async Task SendRequest(DuelAction da = null, string? type = null)
    {
        Request _PlayerRequest = new()
        {
            playerID = PlayerInfo.INSTANCE.PlayerID,
            password = PlayerInfo.INSTANCE.Password,
            type = type,
            duelAction = da,
        };

        JsonSerializerSettings jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };

        if (_webSocket.State == NativeWebSocket.WebSocketState.Open)
        {
            var json = JsonConvert.SerializeObject(_PlayerRequest, jsonSettings);
            Debug.Log($"SEND:\n{json}");
            await _webSocket.SendText(json);
        }
        else
        {
            Debug.LogWarning("WebSocket connection is not open. Unable to send message.");
        }
    }

    public IEnumerator HandleLoadNewScene(string scene)
    {
        yield return LoadNewScene(scene);
        yield return 0;
    }

    public IEnumerator LoadNewScene(string scene)
    {
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(scene);
        while (!asyncOperation.isDone)
        {
            yield return null;
        }
    }
}