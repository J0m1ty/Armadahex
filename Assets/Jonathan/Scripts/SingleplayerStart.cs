using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SingleplayerStart : MonoBehaviour
{
    public MyBox.SceneReference gameScene;

    public void StartGame() {
        Debug.Log("Starting game");
        PlayerPrefs.SetInt(Constants.GAME_MODE_PREF_KEY, (int)GameMode.Customs);

        GameModeInfo.SetCustomPrefs(true, true, false, 0f);

        //PlayerPrefs.SetInt(Constants.DO_TUTORIAL_PREF_KEY, );

        SceneManager.LoadScene(gameScene.SceneName);
    }
}
