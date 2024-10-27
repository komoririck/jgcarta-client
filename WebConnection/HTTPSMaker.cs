using UnityEngine;
using System.Collections;
using System;
using Newtonsoft.Json;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

public class HTTPSMaker : MonoBehaviour
{
    public GameObject alertLoadingPopupPrefab;
    [SerializeField] private GameObject alertLoadingPopup;
    public HttpManager httpManager;

    public string authenticationToken = null;
    public string clientToken = null;

    public ReturnMessage returnMessage;

    string ConnectionUrl = "https://localhost:7047";
    string CreateAccountEndpoint = "/CreateAccount";
    string GetPlayerInfoEndpoint = "/GetPlayerInfo";
    string LoginAccountEndpoint = "/Login";
    string UpdatePlayerInfoEndpoint = "/UpdatePlayerInfo";
    string JoinMatchQueueEndpoint = "/ControlMatchQueue";
    string JoinRoomEndpoint = "/ControlMatchRoom";

    [Flags]
    public enum ConnectionState : byte
    {
        None = 0,
        OpenWebSocket = 1,
        CallForToken = 2,
        WaitForToken = 3,
        CallForAuth = 4,
        WaitForAuth = 5,
        ReadyToWork = 6
    }

    public ConnectionState connectionState;

    void Start()
    {
        Application.runInBackground = true;
    }



    public IEnumerator CreateAccount()
    {


        string jsonData = @"{
                              ""playerID"": 0,
                              ""playerHash"": """"
                            }";

        yield return httpManager.PostRequest(
            (ConnectionUrl + CreateAccountEndpoint),
            jsonData,
            onSuccess: (response) =>
            {
                try
                {
                    Debug.Log("Raw JSON Response: " + response);

                    GenericAuthData responseData = JsonUtility.FromJson<GenericAuthData>(response);

                    if (responseData != null)
                    {
                        PlayerPrefs.SetString("Password", responseData.password);
                        PlayerPrefs.SetInt("PlayerID", responseData.playerID);
                        PlayerPrefs.Save();
                        Debug.Log("Player Account Created!");
                    }
                    else
                    {
                        Debug.LogError("Deserialization returned null.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error parsing JSON response: " + e.Message);
                }
            },
            onError: (error) =>
            {
                Debug.LogError("Request Error: " + error);
            }

        );
    }

    public IEnumerator GetPlayerInfo(int playerID, string password)
    {

        PlayerRequest _PlayerRequest = new();
        _PlayerRequest.playerID = playerID.ToString();
        _PlayerRequest.password = password;
        _PlayerRequest.requestData.type = "PlayerFullProfile";
        Debug.Log(JsonUtility.ToJson(_PlayerRequest));

        yield return httpManager.PostRequest(
            ConnectionUrl + GetPlayerInfoEndpoint,
            JsonUtility.ToJson(_PlayerRequest),
            response =>
            {
                try
                {
                    Debug.Log("Raw JSON Response: " + response);
                    PlayerInfoData responseData = JsonUtility.FromJson<PlayerInfoData>(response);

                    if (responseData != null)
                    {
                        GameObject hudObject = GameObject.Find("PlayerInfo");
                        if (hudObject != null)
                        {
                            PlayerInfo p = hudObject.GetComponent<PlayerInfo>();

                            p.PlayerID = responseData.playerID;
                            p.Password = responseData.password;
                            p.PlayerName = responseData.playerName;
                            p.PlayerIcon = responseData.playerIcon;
                            p.HoloCoins = responseData.holoCoins;
                            p.HoloGold = responseData.holoGold;
                            p.NNMaterial = responseData.nnMaterial;
                            p.RRMaterial = responseData.rrMaterial;
                            p.SRMaterial = responseData.srMaterial;
                            p.URMaterial = responseData.urMaterial;
                            p.MatchVictory = responseData.matchVictory;
                            p.MatchLoses = responseData.matchLoses;
                            p.MatchesTotal = responseData.matchesTotal;
                            foreach (PlayerBadgeData b in responseData.badges)
                            {
                                p.Badges.Add(new PlayerBadge
                                {
                                    playerID = b.playerID,
                                    badgeID = b.badgeID,
                                    rank = b.rank,
                                    obtainedDate = b.obtainedDate
                                });
                            }
                            foreach (PlayerItemBoxData b in responseData.playerItemBox)
                            {
                                p.PlayerItemBox.Add(new PlayerItemBox
                                {
                                    playerItemBoxID = b.playerID,
                                    playerID = b.playerID,
                                    itemID = b.itemID,
                                    amount = b.amount,
                                    obtainedDate = b.obtainedDate,
                                    expirationDate = b.expirationDate
                                });
                            }
                            foreach (PlayerMessageBoxData b in responseData.playerMessageBox)
                            {
                                p.PlayerMessageBox.Add(new PlayerMessageBox
                                {
                                    messageID = b.messageID,
                                    playerID = b.playerID,
                                    title = b.title,
                                    description = b.description,
                                    obtainedDate = b.obtainedDate
                                });
                            }
                            foreach (PlayerMissionData b in responseData.playerMissionList)
                            {
                                p.PlayerMissionList.Add(new PlayerMission
                                {
                                    playerMissionListID = b.playerMissionListID,
                                    playerID = b.playerID,
                                    missionID = b.missionID,
                                    obtainedDate = b.obtainedDate,
                                    clearData = b.clearData
                                });
                            }
                            foreach (PlayerTitleData b in responseData.playerTitles)
                            {
                                p.PlayerTitles.Add(new PlayerTitle
                                {
                                    titleID = b.titleID,
                                    playerID = b.playerID,
                                    titleName = b.titleName,
                                    titleDescription = b.titleDescription,
                                    obtainedDate = b.obtainedDate
                                });
                            }
                            returnMessage.setRequestReturn("success");
                        }
                        else
                        {
                            Debug.LogError("HUD GameObject not found.");
                        }
                    }
                    else
                    {
                        Debug.LogError("Deserialization returned null.");
                    }
                }
                catch (Exception e)
                {
                    returnMessage.setRequestReturn("fail");
                    Debug.LogError("Error parsing JSON response: " + e.Message);
                }
            },
            error =>
            {
                returnMessage.setRequestReturn("connecition fail");
                Debug.LogError("Request Error: " + error);
            }
        );
    }

    public IEnumerator LoginAccount(string email, string password)
    {
        string jsonData = @"{
            ""email"": """ + email + @""",
            ""password"": """ + password + @"""
                            }";

        yield return httpManager.PostRequest(
            (ConnectionUrl + LoginAccountEndpoint),
            jsonData,
            onSuccess: (response) =>
            {
                try
                {
                    Debug.Log("Raw JSON Response: " + response);

                    GenericAuthData responseData = JsonUtility.FromJson<GenericAuthData>(response);

                    if (responseData != null)
                    {
                        PlayerPrefs.SetString("Password", responseData.password);
                        PlayerPrefs.SetInt("PlayerID", responseData.playerID);
                        PlayerPrefs.Save();
                        Debug.Log("Player Account Logged!");
                        returnMessage.setRequestReturn("success");
                    }
                    else
                    {
                        Debug.LogError("Deserialization returned null.");
                        returnMessage.setRequestReturn("fail");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error parsing JSON response: " + e.Message);
                }
            },
            onError: (error) =>
            {
                Debug.LogError("Request Error: " + error);
                returnMessage.setRequestReturn("connecition fail");
            }
        );
    }

    public IEnumerator UpdatePlayerInfo(PlayerInfoData playerinfo)
    {

        string jsonData = JsonConvert.SerializeObject(playerinfo);
        try { Debug.Log(jsonData); } catch (Exception e) { Debug.Log(e); }

        yield return httpManager.PostRequest(
            (ConnectionUrl + UpdatePlayerInfoEndpoint),
            jsonData,
            onSuccess: (response) =>
            {
                try
                {
                    Debug.Log("Raw JSON Response: " + response);

                    ReturnMessageData responseData = JsonUtility.FromJson<ReturnMessageData>(response);

                    if (responseData != null)
                    {
                        if (responseData.requestReturn.Equals("success"))
                        {
                            returnMessage.setRequestReturn(responseData.requestReturn);
                        }
                    }
                    else
                    {
                        Debug.LogError("Deserialization returned null.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error parsing JSON response: " + e.Message);
                }
            },
            onError: (error) =>
            {
                Debug.LogError("Request Error: " + error);
                returnMessage.setRequestReturn("connecition fail");
            }

        );
    }
    public IEnumerator JoinMatchQueue(int playerID, string password, string type)
    {

        PlayerRequest _PlayerRequest = new();
        _PlayerRequest.playerID = playerID.ToString();
        _PlayerRequest.password = password;
        _PlayerRequest.requestData.type = "JoinQueue";
        _PlayerRequest.requestData.description = type;

        yield return httpManager.PostRequest(
            (ConnectionUrl + JoinMatchQueueEndpoint),
            JsonUtility.ToJson(_PlayerRequest),
            onSuccess: (response) =>
            {
                try
                {
                    Debug.Log("Raw JSON Response: " + response);

                    ReturnMessageData responseData = JsonUtility.FromJson<ReturnMessageData>(response);

                    if (responseData != null)
                    {
                        if (responseData.requestReturn.Equals("success"))
                        {
                            returnMessage.setRequestReturn(responseData.requestReturn);
                        }
                    }
                    else
                    {
                        Debug.LogError("Deserialization returned null.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error parsing JSON response: " + e.Message);
                }
                finally
                {
                }
            },
            onError: (error) =>
            {
                returnMessage.setRequestReturn("connecition fail");
                Debug.LogError("Request Error: " + error);
            }

        );
    }
    public IEnumerator CancelMatchQueue(int playerID, string password)
    {

        PlayerRequest _PlayerRequest = new();
        _PlayerRequest.playerID = playerID.ToString();
        _PlayerRequest.password = password;
        _PlayerRequest.requestData.description = "Cancel";

        yield return httpManager.PostRequest(
            (ConnectionUrl + JoinMatchQueueEndpoint),
            JsonUtility.ToJson(_PlayerRequest),
            onSuccess: (response) =>
            {
                try
                {
                    Debug.Log("Raw JSON Response: " + response);

                    ReturnMessageData responseData = JsonUtility.FromJson<ReturnMessageData>(response);

                    if (responseData != null)
                    {
                        if (responseData.requestReturn.Equals("success"))
                        {
                            returnMessage.setRequestReturn(responseData.requestReturn);
                        }
                    }
                    else
                    {
                        Debug.LogError("Deserialization returned null.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error parsing JSON response: " + e.Message);
                }
                finally
                {
                }
            },
            onError: (error) =>
            {
                returnMessage.setRequestReturn("connecition fail");
                Debug.LogError("Request Error: " + error);
            }

        );
    }
    public IEnumerator JoinRoom(int playerID, string password, string type, string code)
    {

        PlayerRequest _PlayerRequest = new();
        _PlayerRequest.playerID = playerID.ToString();
        _PlayerRequest.password = password;
        _PlayerRequest.requestData.type = type;
        _PlayerRequest.requestData.description = code.ToString();


        yield return httpManager.PostRequest(
            (ConnectionUrl + JoinRoomEndpoint),
            JsonUtility.ToJson(_PlayerRequest),
            onSuccess: (response) =>
            {
                try
                {
                    Debug.Log("Raw JSON Response: " + response);


                    PlayerMatchRoomData responseData = JsonUtility.FromJson<PlayerMatchRoomData>(response);

                    if (responseData.roomCode == 0) {
                        returnMessage.setRequestReturn("noroomfound");
                        return;
                    }

                    if (responseData != null)
                    {
                        PlayerMatchRoom RoomInfo = GameObject.Find("PlayerInfo").GetComponent<PlayerMatchRoom>();
                        if (RoomInfo != null)
                        {
                            RoomInfo.RoomID = responseData.roomID;
                            RoomInfo.RegDate = responseData.regDate;
                            RoomInfo.RoomCode = responseData.roomCode;
                            RoomInfo.MaxPlayer = responseData.maxPlayer;
                            RoomInfo.OwnerID = responseData.ownerID;

                            List<PlayerMatchRoomPool> p = new();
                            foreach (PlayerMatchRoomPoolData b in responseData.playerMatchRoomPool)
                            {
                                p.Add(new PlayerMatchRoomPool
                                {
                                    MRPID = b.mrpid,
                                    PlayerID = b.playerID,
                                    Board = b.board,
                                    Status = b.status,
                                    Chair = b.chair,
                                    MatchRoomID = b.matchPoolID, //MUDAR
                                    RegDate = b.regDate,
                                    LasActionDate = b.lasActionDate
                                });
                            }
                            returnMessage.setRequestReturn("success");
                        }
                        else
                        {
                            Debug.LogError("Failed to fetch de room data.");
                        }
                    }
                    else
                    {
                        Debug.LogError("Deserialization returned null.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error parsing JSON response: " + e.Message);
                }
                finally
                {
                }
            },
            onError: (error) =>
            {
                returnMessage.setRequestReturn("connecition fail");
                Debug.LogError("Request Error: " + error);
            }

        );
    }
    [Serializable]
    public class PlayerMatchRoomPoolData
    {
        public string mrpid;
        public int playerID;
        public int board;
        public int chair;
        public int status;
        public string matchPoolID;
        public DateTime regDate;
        public DateTime lasActionDate;
    }
    [Serializable]
    public class PlayerMatchRoomData
    {
        public string roomID;
        public DateTime regDate;
        public int roomCode;
        public int maxPlayer;
        public int ownerID;

        public List<PlayerMatchRoomPoolData> playerMatchRoomPool;
    }
    [Serializable]
    public class GenericAuthData
    {
        public int playerID;
        public string password;
    }
    [Serializable]
    public class PlayerInfoData
    {
        public int playerID;
        public string playerName;
        public int playerIcon;

        public int holoCoins;
        public int holoGold;
        public int nnMaterial;
        public int rrMaterial;
        public int srMaterial;
        public int urMaterial;
        public int matchVictory;
        public int matchLoses;
        public int matchesTotal;
        public string email;
        public string password;

        public List<PlayerTitleData> playerTitles;
        public List<PlayerMessageBoxData> playerMessageBox;
        public List<PlayerItemBoxData> playerItemBox;
        public List<PlayerMissionData> playerMissionList;
        public List<PlayerBadgeData> badges;

        public RequestData requestData;
    }
    [Serializable]
    public class PlayerBadgeData
    {
        public int badgeID;
        public int rank;
        public int playerID;
        public DateTime? obtainedDate;
    }
    [Serializable]
    public class PlayerItemBoxData
    {
        public int playerItemBoxID;
        public int playerID;
        public int itemID;
        public int amount;
        public DateTime obtainedDate;
        public DateTime? expirationDate;
    }
    [Serializable]
    public class PlayerMessageBoxData
    {
        public int messageID;
        public int playerID;
        public string title;
        public string description;
        public DateTime? obtainedDate;
    }
    [Serializable]
    public class PlayerMissionData
    {
        public int playerMissionListID;
        public int playerID;
        public int missionID;
        public DateTime? obtainedDate;
        public DateTime? clearData;
    }
    [Serializable]
    public class PlayerTitleData
    {
        public int titleID;
        public int playerID;
        public string titleName;
        public string titleDescription;
        public DateTime obtainedDate;
    }
    [Serializable]
    public class ReturnMessageData
    {
        public string requestReturn;
    }


    public static void PrintObjectDetails(object obj)
    {
        if (obj == null)
        {
            Debug.Log("Object is null.");
            return;
        }

        Type type = obj.GetType();

        // Print fields
        Debug.Log("Fields:");
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
            object value = field.GetValue(obj);
            Debug.Log($"{field.Name}: {value}");
        }

        // Print properties
        Debug.Log("Properties:");
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (PropertyInfo property in properties)
        {
            object value = property.GetValue(obj);
            Debug.Log($"{property.Name}: {value}");
        }
    }
}
