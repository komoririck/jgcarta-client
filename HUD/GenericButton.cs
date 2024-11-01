using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GenericButton : MonoBehaviour
{
    public void closeViewButton(GameObject obj) { 
        obj.SetActive(false);
    }

    public void LoadSceneButton(string scene)
    {
        SceneManager.LoadScene(scene);
    }
    public static void DisplayPopUp(GameObject Panel, TMP_Text txtHolder, string text) {
        Panel.SetActive(true);
        txtHolder.text = text;
    }
}
