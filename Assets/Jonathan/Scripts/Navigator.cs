using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using MyBox;

public class Navigator : MonoBehaviour {
    
    [SerializeField]
    private SceneReference mainMenu;

    public void Back() {
        PhotonNetwork.LoadLevel(mainMenu.SceneName);
    }

    public void MenuNav(string sceneName) {
        PhotonNetwork.LoadLevel(sceneName);
    }

    public void Signout(bool quit) {
        Debug.Log("Signing out");
        
        Debug.Log("Deleting token and nickname");
        PlayerPrefs.DeleteKey(Constants.TOKEN_PREF_KEY);
        PlayerPrefs.DeleteKey(Constants.NICK_NAME_PREF_KEY);

        Debug.Log("Disconnecting from Photon");
        PhotonNetwork.Disconnect();

        if (quit) {
            Debug.Log("Quitting");
            Application.Quit();
        }
        else {
            Debug.Log("Loading login scene");
            PhotonNetwork.LoadLevel("1 Account");
        }
    }

    public void Quit(bool signout) {
        if (signout) {
            Signout(true);
        }
        else {
            Debug.Log("Quitting");
            Application.Quit();
        }
    }
}
