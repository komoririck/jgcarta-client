using UnityEngine;
using System.Collections;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;

public class HTTPSMaker : MonoBehaviour
{
    public GameObject alertLoadingPopupPrefab;
    [SerializeField] private GameObject alertLoadingPopup;
    public HttpManager httpManager;

    public string returnMessage;
    public List<object> returnedObjects = new();


    string ConnectionUrl = "https://localhost:7047";
    JsonSerializerSettings jsonSettings;

    void Start()
    {
        Application.runInBackground = true;
        jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };
    }
    public IEnumerator CreateAccount()
    {
        PlayerRequest _PlayerRequest = new() { 
            playerID = "",
            password =  "",
        };

        yield return httpManager.MakeRequest(
            (ConnectionUrl + "/Account/CreateAccount"),
            JsonConvert.SerializeObject(_PlayerRequest, jsonSettings),
            onSuccess: (response) =>
            {
                PlayerRequest responseData = JsonUtility.FromJson<PlayerRequest>(response);
                PlayerPrefs.SetString("Password", responseData.password);
                PlayerPrefs.SetString("PlayerID", responseData.playerID);
                PlayerPrefs.Save();
            },
            onError: (error) =>
            {
                Debug.LogError("Request Error: " + error);
            },
            "Post"
        );
    }
    public IEnumerator GetPlayerInfo(string playerID, string password)
    {

        PlayerRequest _PlayerRequest = new() {
            playerID = playerID.ToString(),
            password = password,
        };

        yield return httpManager.MakeRequest(
            ConnectionUrl + "/PlayerInfo/GetFullProfile",
            JsonConvert.SerializeObject(_PlayerRequest, jsonSettings),
            response =>
            {
                PlayerInfoData responseData = JsonUtility.FromJson<PlayerInfoData>(response);
                GameObject hudObject = GameObject.Find("PlayerInfo");
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
                returnMessage = ("success");
            },
            error =>
            {
                returnMessage = "connecition fail";
                Debug.LogError("Request Error: " + error);
            },
            "Post"
        );
    }
    public IEnumerator LoginAccount(string email, string password)
    {

        PlayerRequest playerRequest = new PlayerRequest()
        {
            password = password,
            email = email
        };

        yield return httpManager.MakeRequest(
            (ConnectionUrl + "/Account/Login"),
            JsonConvert.SerializeObject(playerRequest, jsonSettings),
            onSuccess: (response) =>
            {
                PlayerRequest responseData = JsonUtility.FromJson<PlayerRequest>(response);

                if (responseData != null)
                {
                    PlayerPrefs.SetString("Password", responseData.password);
                    PlayerPrefs.SetString("PlayerID", responseData.playerID);
                    PlayerPrefs.Save();

                    returnMessage = ("success");
                }
            },
            onError: (error) =>
            {
                Debug.LogError("Request Error: " + error);
                returnMessage = "connecition fail";
            },
            "Post"
        );
    }
    public IEnumerator UpdatePlayerInfo(PlayerInfoData playerinfo)
    {
        yield return httpManager.MakeRequest(
            (ConnectionUrl + ""),
            JsonConvert.SerializeObject(playerinfo, jsonSettings),
            onSuccess: (response) =>
            {
                returnMessage = ("success");
            },
            onError: (error) =>
            {
                Debug.LogError("Request Error: " + error);
                returnMessage = "connecition fail";
            },
            "Put"
        );
    }
    public IEnumerator JoinMatchQueue(string playerID, string password, string type)
    {

        PlayerRequest _PlayerRequest = new() {
            playerID = playerID.ToString(),
            password = password,
            type = "JoinQueue",
            description = type
        };

        yield return httpManager.MakeRequest(
            (ConnectionUrl + "/ControlMatchQueue/JoinQueue"),
            JsonConvert.SerializeObject(_PlayerRequest, jsonSettings),
            onSuccess: (response) =>
            {
                returnMessage = "success";
            },
            onError: (error) =>
            {
                returnMessage = "connecition fail";
                Debug.LogError("Request Error: " + error);
            },
            "Post"
        );
    }
    public IEnumerator CancelMatchQueue(string playerID, string password)
    {
        PlayerRequest _PlayerRequest = new() {
            playerID = playerID.ToString(),
            password = password,
            description = "Cancel"
        };

        yield return httpManager.MakeRequest(
            (ConnectionUrl + "/ControlMatchQueue/JoinLeave"),
            JsonConvert.SerializeObject(_PlayerRequest, jsonSettings),
            onSuccess: (response) =>
            {
                returnMessage = ("success");
            },
            onError: (error) =>
            {
                returnMessage = "connecition fail";
                Debug.LogError("Request Error: " + error);
            },
            "Put"
        );
    }
    public IEnumerator JoinRoom(string playerID, string password, string type, string code)
    {
        PlayerRequest _PlayerRequest = new()
        {
            playerID = playerID.ToString(),
            password = password,
            description = code.ToString()
        };

        yield return httpManager.MakeRequest(
            (ConnectionUrl + $"/ControlMatchRoom/{type}"),
            JsonConvert.SerializeObject(_PlayerRequest, jsonSettings),
            onSuccess: (response) =>
            {
               
                PlayerMatchRoomData responseData = JsonUtility.FromJson<PlayerMatchRoomData>(response);

                if (responseData.roomCode == 0)
                {
                    returnMessage = ("noroomfound");
                    return;
                }

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
                }
                
            },
            onError: (error) =>
            {
                returnMessage = "connecition fail";
                Debug.LogError("Request Error: " + error);
            },
            "Post"
        );
    }
    public IEnumerator GetDeckRequest()
    {
        PlayerInfo playerInfo = FindAnyObjectByType<PlayerInfo>();
        PlayerRequest _PlayerRequest = new()
        {
            playerID = playerInfo.PlayerID,
            password = playerInfo.Password,
        };

        yield return httpManager.MakeRequest(
            (ConnectionUrl + "/DeckInfo/GetDeck"),
            JsonConvert.SerializeObject(_PlayerRequest, jsonSettings),
            onSuccess: (response) =>
            {
                try
                {
                    List<DeckData> _DeckData = JsonConvert.DeserializeObject<List<DeckData>>(response);
                    returnedObjects.AddRange(_DeckData);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error parsing JSON response: " + e.Message);
                }
            },
            onError: (error) =>
            {
                Debug.LogError("Request Error: " + error);
                returnMessage = "connecition fail";
            },
            "Post"
        );
    }
    public IEnumerator UpdateDeckRequest(DeckData deckData)
    {
        PlayerInfo playerInfo = FindAnyObjectByType<PlayerInfo>();
        PlayerRequest _PlayerRequest = new()
        {
            playerID = playerInfo.PlayerID,
            password = playerInfo.Password,
            jsonObject = deckData
        };

        yield return httpManager.MakeRequest(
            (ConnectionUrl + "/DeckInfo/UpdateDeck"),
            JsonConvert.SerializeObject(_PlayerRequest, jsonSettings),
            onSuccess: (response) =>
            {
                returnMessage = ("success");
            },
            onError: (error) =>
            {
                Debug.LogError("Request Error: " + error);
                returnMessage = "connecition fail";
            },
            "Put"
        );
        
    }
    public IEnumerator SetDeckAsActive(DeckData deckData)
    {
        DeckData deckDataId = new() { deckId = deckData.deckId };
        PlayerInfo playerInfo = FindAnyObjectByType<PlayerInfo>();
        PlayerRequest _PlayerRequest = new()
        {
            playerID = playerInfo.PlayerID,
            password = playerInfo.Password,
            jsonObject = deckDataId
        };

        yield return httpManager.MakeRequest(
            (ConnectionUrl + "/DeckInfo/SetDeckAsActive"),
            JsonConvert.SerializeObject(_PlayerRequest, jsonSettings),
            onSuccess: (response) =>
            {
                returnMessage = ("success");
            },
            onError: (error) =>
            {
                Debug.LogError("Request Error: " + error);
                returnMessage = "connecition fail";
            },
            "Put"
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
    public class PlayerInfoData
    {
        public string playerID;
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

        public PlayerRequest requestData;
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
}
