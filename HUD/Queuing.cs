using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Queuing : MonoBehaviour
{
    [SerializeField] private PlayerInfo PlayerInfo = null;
    [SerializeField] private GameSettings GameSettings = null;
    [SerializeField] public HTTPSMaker _HTTPSMaker = null;
    MatchConnection _MatchConnection;
    void Start()
    {
        PlayerInfo = GameObject.Find("PlayerInfo").GetComponent<PlayerInfo>();
        GameSettings = GameObject.Find("GameSettings").GetComponent<GameSettings>();
        _HTTPSMaker = GameSettings.GetComponent<HTTPSMaker>();
        _MatchConnection = GameSettings.AddComponent<MatchConnection>();
    }

    void Update()
    {
        
    }

    public void CancelMatchPoolButton()
    {
        StartCoroutine(HandleCancelMatchPoolButton());
    }
    public IEnumerator HandleCancelMatchPoolButton()
    {
        yield return StartCoroutine(CancelMatchQueue());

        if (_HTTPSMaker.returnMessage.getRequestReturn().Equals("success"))
        {
            _HTTPSMaker.returnMessage.resetMessage();
            _ = _MatchConnection._webSocket.Close();
            Destroy(_MatchConnection);
            SceneManager.LoadScene("MainMenu");
            

        }
    }
    public IEnumerator CancelMatchQueue()
    {
        yield return StartCoroutine(_HTTPSMaker.CancelMatchQueue(PlayerInfo.PlayerID, PlayerInfo.Password));
    }

}
