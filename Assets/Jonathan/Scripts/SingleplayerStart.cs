using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class SingleplayerStart : MonoBehaviour
{
    public MyBox.SceneReference gameScene;

    public void StartGame() {
        Debug.Log("Starting game");
        PlayerPrefs.SetString(Constants.PLAYLIST_PREF_KEY, Playlists.Custom);

        GameModeInfo.SetCustomPrefs(true, true, false, false, false, 0f, false);

        //PlayerPrefs.SetInt(Constants.DO_TUTORIAL_PREF_KEY, );

        SceneManager.LoadScene(gameScene.SceneName);
    }
}
