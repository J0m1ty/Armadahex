using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class SingleplayerStart : MonoBehaviour
{
    public MyBox.SceneReference gameScene;

    [SerializeField] private UIDocument document;

    void Start() {
        // the bot difficulty lives on the gameplay options page
        var difficulty = document.rootVisualElement.Q<Choices>("difficulty");
        if (difficulty == null) return;

        difficulty.value = (int)BotAI.SavedDifficulty;
        difficulty.changed += SetDifficulty;
    }

    public void SetDifficulty(int difficulty) {
        PlayerPrefs.SetInt(Constants.BOT_DIFFICULTY_PREF_KEY, difficulty);
    }

    public void StartGame() {
        Debug.Log("Starting game");
        PlayerPrefs.SetString(Constants.PLAYLIST_PREF_KEY, Playlists.Custom);

        GameModeInfo.SetCustomPrefs(true, true, false, false, false, 0f, false);

        //PlayerPrefs.SetInt(Constants.DO_TUTORIAL_PREF_KEY, );

        SceneManager.LoadScene(gameScene.SceneName);
    }
}
