using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Queuing : MonoBehaviour
{
    [SerializeField] public HTTPSMaker _HTTPSMaker = null;
    void Start()
    {
        _HTTPSMaker = FindAnyObjectByType<HTTPSMaker>();
        FindAnyObjectByType<MatchConnection>().StartConnection();
    }
    public void CancelMatchPoolButton()
    {
        StartCoroutine(HandleCancelMatchPoolButton());
    }
    public IEnumerator HandleCancelMatchPoolButton()
    {
        yield return _HTTPSMaker.CancelMatchQueue(PlayerInfo.INSTANCE.PlayerID, PlayerInfo.INSTANCE.Password);
        if (_HTTPSMaker.returnMessage.Equals("success"))
        {
            _HTTPSMaker.returnMessage = null;
            FindAnyObjectByType<MatchConnection>().CloseConnection();
            SceneManager.LoadScene("MainMenu");
        }
    }
}
