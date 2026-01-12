using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchMenu : MonoBehaviour
{
    [SerializeField] private PlayerInfo PlayerInfo = null;
    [SerializeField] private GameSettings GameSettings = null;
    [SerializeField] public HTTPSMaker _HTTPSMaker = null;
    [SerializeField] private TMP_Text t = null;


    // Start is called before the first frame update
    void Start()
    {
        PlayerInfo = GameObject.Find("PlayerInfo").GetComponent<PlayerInfo>();
        GameSettings = GameObject.Find("GameSettings").GetComponent<GameSettings>();
        _HTTPSMaker = GameSettings.GetComponent<HTTPSMaker>();

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void MenuToogleButton(GameObject g)
    {
        if (g.activeInHierarchy)
        {
            g.SetActive(false);
        }
        else
        {
            g.SetActive(true);
        }
    }
    public void GoToRoom()
    {
        int ownerid = GameObject.Find("PlayerInfo").GetComponent<PlayerMatchRoom>().OwnerID;
        if (ownerid > 0)
            SceneManager.LoadScene("MatchRoom");
    }

    public void JoinMatchQueueButton(GameObject b)
    {
         StartCoroutine(HandleJoinMatchPoolButton(b.name));
    }
    public void JoinRoomButton(GameObject b)
    {

        StartCoroutine(HandleJoinRoomButton(b.name, t.text));
    }

    public IEnumerator HandleJoinMatchPoolButton(string buttonName)
    {
        string queueType = "";

        if (buttonName.Equals("Casual"))
            queueType = "Casual";
        if (buttonName.Equals("Ranked"))
            queueType = "Ranked";

        yield return StartCoroutine(JoinMatchQueue(queueType));
        if (_HTTPSMaker.returnMessage.Equals("success"))
        {
            _HTTPSMaker.returnMessage = "";
            SceneManager.LoadScene("Queuing");
        }
    }
    public IEnumerator JoinMatchQueue(string queueType)
    {
        yield return StartCoroutine(_HTTPSMaker.JoinMatchQueue(PlayerInfo.PlayerID, PlayerInfo.Password, queueType));
    }


    public IEnumerator HandleJoinRoomButton(string buttonName, string t)
    {
        string queueType = "";
        string roomID = t;

        if (buttonName.Equals("JoinRoomInfo"))
            queueType = "JoinRoom";

        if (buttonName.Substring(0, 10).Equals("CreateRoom"))
            queueType = "CreateRoom";

        yield return StartCoroutine(JoinRoom(queueType, roomID));
            switch (_HTTPSMaker.returnMessage) {
                case "success":
                    SceneManager.LoadScene("MatchRoom");
                    break;
                case "noroomfound":
                    NoRoomFound();
                    break;
            }
            _HTTPSMaker.returnMessage = "";
        }
    public IEnumerator JoinRoom(string queueType, string roomId)
    {
        yield return StartCoroutine(_HTTPSMaker.JoinRoom(PlayerInfo.PlayerID, PlayerInfo.Password, queueType, roomId));
    }
    public void NoRoomFound() { 
    
    
    }



    public void ReturnButton()
    {
        SceneManager.LoadScene("MainMenu");
    }
}

