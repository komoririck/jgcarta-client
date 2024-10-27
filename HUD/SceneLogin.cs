using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoginLoginButton : MonoBehaviour // refact this name later
{

    [SerializeField] public PlayerInfo PlayerInfo = null;
    [SerializeField] public GameSettings GameSettings = null;
    [SerializeField] private GameObject DataTransferPanel = null;
    [SerializeField] private TMP_InputField DTEmail = null;
    [SerializeField] private TMP_InputField DTPassword = null;
    

    public HTTPSMaker _HTTPSMaker;

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        DontDestroyOnLoad(PlayerInfo.gameObject);
        DontDestroyOnLoad(GameSettings.gameObject);
    }

    void Start()
    {

        PlayerInfo.PlayerID = PlayerPrefs.GetInt("PlayerID");
        PlayerInfo.Password = PlayerPrefs.GetString("Password", null);

    }

    void Update()
    {
        
    }

    public IEnumerator CreateAccount()
    {
        yield return StartCoroutine(_HTTPSMaker.CreateAccount());
    }

    public IEnumerator LoginAccount(int id, string password) 
    {
        yield return StartCoroutine(_HTTPSMaker.GetPlayerInfo(id, password));
    }

    public void StartButton()
    {
        StartCoroutine(HandleStartButton());
    }
    private IEnumerator HandleStartButton()
    {
        PlayerInfo.PlayerID = PlayerPrefs.GetInt("PlayerID");
        PlayerInfo.Password = PlayerPrefs.GetString("Password", null);

        if (string.IsNullOrEmpty(PlayerInfo.Password))
        {
            yield return StartCoroutine(CreateAccount());
            PlayerInfo.PlayerID = PlayerPrefs.GetInt("PlayerID");
            PlayerInfo.Password = PlayerPrefs.GetString("Password", null);
        }

        yield return StartCoroutine(LoginAccount(PlayerInfo.PlayerID, PlayerInfo.Password));


        if (_HTTPSMaker.returnMessage.getRequestReturn().Equals("success"))
        {
            _HTTPSMaker.returnMessage.resetMessage();
            SceneManager.LoadScene("MainMenu");
        }
    }
    public void ResetButton()
    {
        PlayerPrefs.DeleteAll();

        PlayerInfo.PlayerID = 0;
        PlayerInfo.Password = null;
    }
    public void OpenDataTransferPanel() {
        DataTransferPanel.SetActive(true);
    }
    public void CloseDataTransferPanel()
    {
        DataTransferPanel.SetActive(false);
    }
    public void LoginAccountButton()
    {
        StartCoroutine(LoginAccount());
    }
    public IEnumerator LoginAccount()
    {
        yield return StartCoroutine(HandleLoginAccount());
    }
    public IEnumerator HandleLoginAccount()
    {
        yield return StartCoroutine(_HTTPSMaker.LoginAccount(DTEmail.text, DTPassword.text));
        PlayerInfo.PlayerID = PlayerPrefs.GetInt("PlayerID");
        PlayerInfo.Password = PlayerPrefs.GetString("Password", null);
        CloseDataTransferPanel();
        StartCoroutine(LoginAccount(PlayerInfo.PlayerID, PlayerInfo.Password));


        if (_HTTPSMaker.returnMessage.getRequestReturn().Equals("success"))
        {
            _HTTPSMaker.returnMessage.resetMessage();
            SceneManager.LoadScene("MainMenu");
        }
    }
}