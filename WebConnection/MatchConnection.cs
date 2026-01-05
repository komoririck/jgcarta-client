using NativeWebSocket;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DuelField;

public class MatchConnection : MonoBehaviour
{
    public static MatchConnection INSTANCE;
    public WebSocket _webSocket;
    public readonly Uri WebSocketConnectionUrl = new("wss://localhost:7047/ws");

    private Queue<Tuple<string, DuelAction>> PendingActions = new();
    public Tuple<string, DuelAction> GetPendingActions() { return PendingActions.Dequeue(); }
    public int GetPendingActionsCount() { return PendingActions.Count; }


    private async void Start()
    {
        INSTANCE = this;
        _webSocket = new WebSocket(WebSocketConnectionUrl.ToString());

        _webSocket.OnOpen += () =>
        {
            StartCoroutine(TryToRequest("Waitingforopponent", 1, 5000f, SendCallToServer(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password, "JoinPlayerQueueList", null)));
        };

        _webSocket.OnMessage += (bytes) =>
        {
            string jsonString = System.Text.Encoding.UTF8.GetString(bytes);
            try
            {
                var responseData = JsonConvert.DeserializeObject<Request>(jsonString);

                if (responseData != null)
                {
                    switch (responseData.type)
                    {
                        case "goToRoom":
                            StartCoroutine( HandleLoadNewScene("DuelField"));
                            break;
                        case "DuelUpdate":
                           // if (responseData.description.Equals("Endduel") || responseData.description.Equals("unlockGame"))
                            break;
                        case "cancelMatch":
                            DontDestroyManager.DestroyAllDontDestroyOnLoadObjects();
                            StartCoroutine(HandleLoadNewScene("Match"));
                            break;
                    }
                    PendingActions.Enqueue(new Tuple<string, DuelAction>(responseData.description, responseData.duelAction));
                
                
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{e}\nError parsing JSON response\n{jsonString}");
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
            _webSocket.DispatchMessageQueue();
        #endif
    }

    public IEnumerator TryToRequest(string expectedResponse, int maxTry, float timeInterval, Task asyncFunc, Action updateReturn = null) { 
        bool shouldAsk = true;
        int connectionRetry = 0;
        float timer = 0f;

        Request _requestData = new() { type = "" };

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

            if (_requestData.type.Equals(expectedResponse)) {
                updateReturn();
                _requestData.type = "";

                yield break;
            }
        } while (connectionRetry > maxTry);

        if (connectionRetry < maxTry) { }
        //            SceneManager.LoadScene("Login");
    }

    public async Task SendCallToServer(string playerID, string password, string? type = null, string? description = null, DuelAction da = null)
    {
        Request _PlayerRequest = new()
        {
            playerID = playerID,
            password = password,
            type = type,
            description = description,
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
        yield return StartCoroutine(LoadNewScene(scene));
    }

    public IEnumerator LoadNewScene(string scene)
    {
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(scene);
        while (!asyncOperation.isDone)
        {
            yield return null;
        }
    }

    private async void OnDestroy()
    {
        await _webSocket.Close();
    }
}