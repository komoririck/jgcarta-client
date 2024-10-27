using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MatchRoom : MonoBehaviour
{
    [SerializeField] private PlayerInfo PlayerInfo = null;
    [SerializeField] private GameSettings GameSettings = null;
    [SerializeField] public HTTPSMaker _HTTPSMaker = null;
    [SerializeField] private PlayerMatchRoom playerMatchRoom = null;

    private float timer = 0f;
    private float interval = 5f;


    // Start is called before the first frame update
    void Start()
    {
        PlayerInfo = GameObject.Find("PlayerInfo").GetComponent<PlayerInfo>();
        GameSettings = GameObject.Find("GameSettings").GetComponent<GameSettings>();
        playerMatchRoom = GameObject.Find("PlayerInfo").GetComponent<PlayerMatchRoom>();
        _HTTPSMaker = GameSettings.GetComponent<HTTPSMaker>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= interval)
        {
            timer = 0f;
            StartCoroutine(HandleUpdateRoomData());
        }
    }

    public IEnumerator HandleUpdateRoomData()
    {
        yield return StartCoroutine(UpdateRoomData());

        // Check the return message after UpdateRoomData completes
        if (_HTTPSMaker.returnMessage.getRequestReturn().Equals("success"))
        {
            UpdateRoom();
        }
    }

    public IEnumerator UpdateRoomData()
    {
        // Assuming JoinRoom is a coroutine and _HTTPSMaker is properly initialized
        yield return StartCoroutine(_HTTPSMaker.JoinRoom(PlayerInfo.PlayerID, PlayerInfo.Password, "UpdateRoom", "000000"));
    }









    public void DismissMatchRoomButton()
    {
        StartCoroutine(HandleDismissMatchRoomButton());
    }
    public IEnumerator HandleDismissMatchRoomButton()
    {
        yield return StartCoroutine(DismissMatchRoom());

        if (_HTTPSMaker.returnMessage.getRequestReturn().Equals("success"))
        {
            _HTTPSMaker.returnMessage.resetMessage();
            SceneManager.LoadScene("MainMenu");
        }
    }
    public IEnumerator DismissMatchRoom()
    {
        int ownerid = playerMatchRoom.OwnerID;
        if (PlayerInfo.PlayerID == ownerid)
        {
            yield return StartCoroutine(_HTTPSMaker.JoinRoom(PlayerInfo.PlayerID, PlayerInfo.Password, "CancelRoom", "000000"));
        }
        yield return StartCoroutine(_HTTPSMaker.JoinRoom(PlayerInfo.PlayerID, PlayerInfo.Password, "LeaveRoom", "000000"));
        playerMatchRoom = new PlayerMatchRoom();
    }

    public void ReturnToMenuButton()
    {
        SceneManager.LoadScene("MainMenu");
    }




    public void JoinTableButton(Button boardNumberButton)
    {
        string boardNumber = "1"; //boardNumberButton.name;
        StartCoroutine(HandleJoinTableButton(boardNumber));
    }
    public IEnumerator HandleJoinTableButton(string boardNumber)
    {
        yield return StartCoroutine(JoinTable(boardNumber));

        if (_HTTPSMaker.returnMessage.getRequestReturn().Equals("success"))
        {
            _HTTPSMaker.returnMessage.resetMessage();
            //do the screen changes for the table joined
        }
    }
    public IEnumerator JoinTable(string boardNumber)
    {
        yield return StartCoroutine(_HTTPSMaker.JoinRoom(PlayerInfo.PlayerID, PlayerInfo.Password, "JoinTable", boardNumber));
    }






    public void LeaveTableButton()
    {
        StartCoroutine(HandleLeaveTableButton());
    }
    public IEnumerator HandleLeaveTableButton()
    {
        yield return StartCoroutine(LeaveTable());

        int i = 0;
        if (_HTTPSMaker.returnMessage.getRequestReturn().Equals("success"))
        {
            _HTTPSMaker.returnMessage.resetMessage();
            foreach (PlayerMatchRoomPool p in playerMatchRoom.PlayerMatchRoomPool)
            {

                if (p.PlayerID == PlayerInfo.PlayerID)
                {
                    playerMatchRoom.PlayerMatchRoomPool[i].Status = 'A';
                    playerMatchRoom.PlayerMatchRoomPool[i].Chair = 0;
                    playerMatchRoom.PlayerMatchRoomPool[i].Board = 0;
                }
                i++;
            }
        }
        UpdateRoom();
    }
    public IEnumerator LeaveTable()
    {
        yield return StartCoroutine(_HTTPSMaker.JoinRoom(PlayerInfo.PlayerID, PlayerInfo.Password, "LeaveTable", "000000"));

    }











    public void LockTableButton()
    {
        StartCoroutine(HandleLockTableButton());
    }
    public IEnumerator HandleLockTableButton()
    {
        yield return StartCoroutine(LockTable());

        int i = 0;
        if (_HTTPSMaker.returnMessage.getRequestReturn().Equals("success"))
        {
            _HTTPSMaker.returnMessage.resetMessage();
            foreach (PlayerMatchRoomPool p in playerMatchRoom.PlayerMatchRoomPool)
            {
                if (p.PlayerID == PlayerInfo.PlayerID)
                {
                    playerMatchRoom.PlayerMatchRoomPool[i].Status = 'D';
                }
                i++;
            }
        }
        UpdateRoom();
    }
    public IEnumerator LockTable()
    {
        yield return StartCoroutine(_HTTPSMaker.JoinRoom(PlayerInfo.PlayerID, PlayerInfo.Password, "LockTable", "000000"));
    }







    public void UnlockTableButton()
    {
        StartCoroutine(HandleUnlockTableButton());
    }
    public IEnumerator HandleUnlockTableButton()
    {
        yield return StartCoroutine(UnlockTable());

        int i = 0;
        if (_HTTPSMaker.returnMessage.getRequestReturn().Equals("success"))
        {
            _HTTPSMaker.returnMessage.resetMessage();
            foreach (PlayerMatchRoomPool p in playerMatchRoom.PlayerMatchRoomPool)
            {
                if (p.PlayerID == PlayerInfo.PlayerID)
                {
                    playerMatchRoom.PlayerMatchRoomPool[i].Status = 'R';
                }
                i++;
            }
        }
        UpdateRoom();
    }
    public IEnumerator UnlockTable()
    {
        yield return StartCoroutine(_HTTPSMaker.JoinRoom(PlayerInfo.PlayerID, PlayerInfo.Password, "UnlockTable", "000000"));
    }






    public void UpdateRoom() { 
    
    
    
    
    
    
    }




}
