using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerProfile : MonoBehaviour
{

    [SerializeField] private PlayerInfo PlayerInfo = null;
    [SerializeField] private GameSettings GameSettings = null;
    [SerializeField] public HTTPSMaker _HTTPSMaker = null;

    public GameObject ProfilePictureList = null;
    [SerializeField] private TMP_InputField PlayerNameInput = null;
    [SerializeField] private TMP_Text PlayerNameText = null;

    [SerializeField] private Button PlayerNameButton = null;
    [SerializeField] private Button PlayerNameConfirmButton = null;


    [SerializeField] private List<Image> ProfileBadges = null;
    [SerializeField] private List<Button> ProfileIconButton = null;


    [SerializeField] private Image imageToChange;
    public string spritePath;

    [SerializeField] int selectedProfilePicture = 0;

    void Start()
    {
        PlayerInfo = GameObject.Find("PlayerInfo").GetComponent<PlayerInfo>();
        GameSettings = GameObject.Find("GameSettings").GetComponent<GameSettings>();
        _HTTPSMaker = GameSettings.GetComponent<HTTPSMaker>();

        PlayerNameText.text = PlayerInfo.PlayerName;


        spritePath = "AvatarIcons/AvatarIcon" + PlayerInfo.PlayerIcon;
        Sprite newSprite = Resources.Load<Sprite>(spritePath);
        if (imageToChange != null && newSprite != null)
        {
            imageToChange.sprite = newSprite;
        }

        int i = 0;
        foreach (PlayerBadge a in PlayerInfo.Badges) {
            if (a.rank != 0)
            {
                ProfileBadges[i].gameObject.SetActive(true);
            }
            i++;
        }
        selectedProfilePicture = PlayerInfo.PlayerIcon;
        foreach (Button b in ProfileIconButton)
        {
            // need to make the current icon select when loading the menu
            // also, need to make the button be selected when clicked, not just change the variable value
            //if (b.name.Equals("Image (" + selectedProfilePicture + ")" ))
                //b.Select
        }
    }

    void Update()
    {
        
    }

    public void ReturnButton()
    {
        SceneManager.LoadScene("MainMenu");
    }
    public void ChangeNameButton()
    {
        PlayerNameInput.gameObject.SetActive(true);
        PlayerNameText.gameObject.SetActive(false);
        PlayerNameButton.gameObject.SetActive(false);
        PlayerNameConfirmButton.gameObject.SetActive(true);
    }
    public void ConfirmNameButton()
    {
        StartCoroutine(HandleConfirmNameButton());
    }
    public IEnumerator HandleConfirmNameButton()
    {

        PlayerNameInput.gameObject.SetActive(false);
        PlayerNameText.gameObject.SetActive(true);
        PlayerNameButton.gameObject.SetActive(true);
        PlayerNameConfirmButton.gameObject.SetActive(false);

        yield return StartCoroutine(UpdatePlayerName());
        if (_HTTPSMaker.returnMessage.Equals("success"))
        {
            _HTTPSMaker.returnMessage = "";
            PlayerNameText.text = PlayerNameInput.text;
            PlayerInfo.PlayerName = PlayerNameText.text;
            PlayerNameInput.text = "";
        }
    }
    public void PPListButton()
    {
        ProfilePictureList.SetActive(true);
    }
    public void PPListOffButton()
    {
        ProfilePictureList.SetActive(false);
    }
    public void SelectPPButton(Button b)
    {
        int i = int.Parse(b.gameObject.name[7].ToString());

        if (i < 0 || i > 3)
            i = 1;

        selectedProfilePicture = i;
    }
    public void ConfirmPPButton()
    {
        StartCoroutine(HandleConfirmPPButton());
    }
    public IEnumerator HandleConfirmPPButton()
    {
        yield return StartCoroutine(UpdatePlayerIcon(selectedProfilePicture));
        if (_HTTPSMaker.returnMessage.Equals("success"))
        {
            _HTTPSMaker.returnMessage = "";
            PlayerInfo.PlayerIcon = selectedProfilePicture;
                Sprite newSprite = Resources.Load<Sprite>("AvatarIcons/AvatarIcon" + PlayerInfo.PlayerIcon);
                if (imageToChange != null && newSprite != null)
                {
                    imageToChange.sprite = newSprite;
                }
            }
            PPListOffButton();
    }
    public IEnumerator UpdatePlayerName()
    {
        HTTPSMaker.PlayerInfoData playerinfoupdate = new()
        {
            playerName = PlayerNameInput.text,
            password = PlayerInfo.Password,
            playerID = PlayerInfo.PlayerID,
            email = "",
            playerIcon = 0,
            holoCoins = 0,
            holoGold = 0,
            nnMaterial = 0,
            rrMaterial = 0,
            srMaterial = 0,
            urMaterial = 0,
            matchVictory = 0,
            matchLoses = 0,
            matchesTotal = 0,
            badges =  new List<HTTPSMaker.PlayerBadgeData>(),
            playerTitles = new List<HTTPSMaker.PlayerTitleData>(),
            playerItemBox = new List<HTTPSMaker.PlayerItemBoxData>(),
            playerMessageBox = new List<HTTPSMaker.PlayerMessageBoxData>(),
            playerMissionList = new List<HTTPSMaker.PlayerMissionData>(),
            requestData = new Request { type = "UpdateName", description = "", duelAction = null }
        };

        yield return StartCoroutine(_HTTPSMaker.UpdatePlayerInfo(playerinfoupdate));
    }
    public IEnumerator UpdatePlayerIcon(int i)
    {
        HTTPSMaker.PlayerInfoData playerinfoupdate = new()
        {
            playerName = "",
            password = PlayerInfo.Password,
            playerID = PlayerInfo.PlayerID,
            email = "",
            playerIcon = i,
            holoCoins = 0,
            holoGold = 0,
            nnMaterial = 0,
            rrMaterial = 0,
            srMaterial = 0,
            urMaterial = 0,
            matchVictory = 0,
            matchLoses = 0,
            matchesTotal = 0,
            badges = new List<HTTPSMaker.PlayerBadgeData>(),
            playerTitles = new List<HTTPSMaker.PlayerTitleData>(),
            playerItemBox = new List<HTTPSMaker.PlayerItemBoxData>(),
            playerMessageBox = new List<HTTPSMaker.PlayerMessageBoxData>(),
            playerMissionList = new List<HTTPSMaker.PlayerMissionData>(),
            requestData = new Request { type = "UpdateProfilePicture", description = "", duelAction = null }
        };

        yield return StartCoroutine(_HTTPSMaker.UpdatePlayerInfo(playerinfoupdate));
    }
}
