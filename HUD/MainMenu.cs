using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private PlayerInfo PlayerInfo = null;
    [SerializeField] private GameSettings GameSettings = null;

    [SerializeField] private TMP_Text UserProfile = null;
    [SerializeField] private TMP_Text HoloGold = null;
    [SerializeField] private TMP_Text HoloCoin = null;
    [SerializeField] private TMP_Text NNMaterial = null;
    [SerializeField] private TMP_Text RRMaterial = null;
    [SerializeField] private TMP_Text SRMaterial = null;
    [SerializeField] private TMP_Text URMaterial = null;

    [SerializeField] private Image imageToChange;

    public string spritePath;   

    void Start()
    {
        PlayerInfo = GameObject.Find("PlayerInfo").GetComponent<PlayerInfo>();
        GameSettings = GameObject.Find("GameSettings").GetComponent<GameSettings>();

        UserProfile.text = PlayerInfo.PlayerIcon.ToString();
        HoloGold.text = PlayerInfo.HoloGold.ToString();
        HoloCoin.text = PlayerInfo.HoloCoins.ToString();
        NNMaterial.text = PlayerInfo.NNMaterial.ToString();
        RRMaterial.text = PlayerInfo.RRMaterial.ToString();
        SRMaterial.text = PlayerInfo.SRMaterial.ToString();
        URMaterial.text = PlayerInfo.URMaterial.ToString();

        spritePath = "AvatarIcons/AvatarIcon" + PlayerInfo.PlayerIcon;
        Sprite newSprite = Resources.Load<Sprite>(spritePath);
        if (imageToChange != null && newSprite != null)
        {
            imageToChange.sprite = newSprite;
        }
    }
}
