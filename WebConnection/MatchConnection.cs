using UnityEngine;
using NativeWebSocket;
using System;
using System.Threading.Tasks;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class MatchConnection : MonoBehaviour
{

    [SerializeField] public DuelFieldData _DuelFieldData = null;
    [SerializeField] public MultiMap<string, string> DuelActionList = null;
    [SerializeField] public List<string> DuelActionListIndex = null;
    [SerializeField] private PlayerInfo PlayerInfo = null;

    public WebSocket _webSocket;
    private readonly Uri _uri = new("wss://localhost:7047/ws");

    [SerializeField] private int _connectionState = 0;

    [SerializeField] private RequestData _requestData;

    [Flags]
    public enum ConnectionState : byte
    {
        None = 0,
        OpenWebSocket = 1,
        ClosedWebSocket = 2,
        ErrorWebSocket = 3,
        JoinPlayersList = 4,
        WaitingForList = 5
    }

    private async void Start()
    {
        PlayerInfo = GameObject.Find("PlayerInfo").GetComponent<PlayerInfo>();

        _DuelFieldData = new DuelFieldData();
        DuelActionList = new MultiMap<string, string>();
        DuelActionListIndex = new List<string>();
        _requestData = new RequestData();
        _requestData.type = "";

        _webSocket = new WebSocket(_uri.ToString());
        _webSocket.OnOpen += () =>
        {
            Debug.Log("Connected to server.");
        };

        _webSocket.OnMessage += (bytes) =>
        {
            string jsonString = System.Text.Encoding.UTF8.GetString(bytes);
            try
            {
                var responseData = JsonConvert.DeserializeObject<RequestData>(jsonString);

                if (responseData != null)
                {
                    switch (responseData.type)
                    {
                        case "Waitingforopponent":
                            _connectionState = 2;
                            break;
                        case "matchFound":
                            _connectionState = 3;
                            break;
                        case "goToRoom":
                            _connectionState = 4;
                            StartCoroutine(HandleLoadNewScene("DuelField"));
                            _DuelFieldData = JsonConvert.DeserializeObject<DuelFieldData>(responseData.requestObject);
                            DuelActionList.Add("StartDuel", "StartDuel");
                            DuelActionListIndex.Add("StartDuel");
                            break;
                        case "duelUpdate":
                            DuelActionList.Add(responseData.description, responseData.requestObject);
                            DuelActionListIndex.Add(responseData.description);
                            break;
                        case "GamePhase":
                            if (responseData.description.Equals("Endduel"))
                                FindAnyObjectByType<DuelField>().LockGameFlow = false;
                            DuelActionList.Add(responseData.description, responseData.requestObject);
                            DuelActionListIndex.Add(responseData.description);
                            break;
                        case "NextGamePhase":
                        case "BoardReadyToPlay":
                            DuelActionList.Add(responseData.description, responseData.requestObject);
                            DuelActionListIndex.Add(responseData.description);
                            break;
                        case "cancelMatch":
                            DontDestroyManager.DestroyAllDontDestroyOnLoadObjects();
                            StartCoroutine(HandleLoadNewScene("Match"));
                            break; 
                        case "Update":
                            DuelActionList.Add(responseData.description, responseData.requestObject);
                            DuelActionListIndex.Add(responseData.description);
                            break;
                    }
                    string s = responseData.requestObject;
                    Debug.Log("Msg recieved:\n" + responseData.type + "\n" + responseData.description + "\n" + s.Replace("\\u0022", "\""));
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error parsing JSON response: " + e.Message);
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

        if (_webSocket.State == WebSocketState.Open) {
            if (_connectionState == 0)
            {
                _connectionState = 1;
                StartCoroutine(TryToRequest("Waitingforopponent", 1, 5000f, SendCallToServer(PlayerInfo.PlayerID, PlayerInfo.Password, "JoinPlayerQueueList"), 2));


            }
        }
    }

    public IEnumerator TryToRequest(string expectedResponse, int maxTry, float timeInterval, Task asyncFunc, int conState, Action updateReturn = null) { 
        bool shouldAsk = true;
        int connectionRetry = 0;
        float timer = 0f;

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

                _connectionState = conState;
                yield break;
            }
        } while (connectionRetry > maxTry);

        if (connectionRetry < maxTry && _connectionState == 1) { }
        //            SceneManager.LoadScene("Login");
    }

    public async Task SendCallToServer(int playerID, string password, string type, string description = "", string aditionalinformation = "", int sync = 0, string extraRequestObject = "")
    {
        PlayerRequest _PlayerRequest = new();

        _PlayerRequest.playerID = playerID.ToString();
        _PlayerRequest.password = password;
        _PlayerRequest.requestData.type = type;
        _PlayerRequest.requestData.description = description;
        _PlayerRequest.requestData.sync = sync;
        _PlayerRequest.requestData.requestObject = aditionalinformation;
        _PlayerRequest.requestData.requestObject.Replace("\\u0022", "\"");
        _PlayerRequest.requestData.requestObject.Replace("\\", "");
        _PlayerRequest.requestData.extraRequestObject = extraRequestObject;
        _PlayerRequest.requestData.extraRequestObject.Replace("\\u0022", "\"");
        _PlayerRequest.requestData.extraRequestObject.Replace("\\", "");

        if (_webSocket.State == NativeWebSocket.WebSocketState.Open)
        {
            await _webSocket.SendText(JsonUtility.ToJson(_PlayerRequest));
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


    public class genericCommunicationData
    {
        public int playerID;
        public string password;

        public RequestData requestData;

    }

}

public class MultiMap<TKey, TValue>
{
    private Dictionary<TKey, List<TValue>> _dictionary = new();
    private List<TValue> _globalList = new();  // Store all values globally by insertion order

    // Add a value to the MultiMap
    public void Add(TKey key, TValue value)
    {
        if (!_dictionary.ContainsKey(key))
        {
            _dictionary[key] = new List<TValue>();
        }

        _dictionary[key].Add(value);
        _globalList.Add(value);  // Add to the global list for index-based access
    }

    // Get all values associated with a key
    public List<TValue> GetByKey(TKey key)
    {
        if (_dictionary.ContainsKey(key))
        {
            return _dictionary[key];
        }
        throw new KeyNotFoundException($"Key '{key}' not found.");
    }

    // Access value by global index
    public TValue GetByIndex(int index)
    {
        if (index >= 0 && index < _globalList.Count)
        {
            return _globalList[index];
        }
        throw new IndexOutOfRangeException($"Index {index} is out of range.");
    }

    // Get the count of globally added items
    public int Count()
    {
        return _globalList.Count;
    }
}