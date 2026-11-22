using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SingleplayerStart : MonoBehaviour
{
    public MyBox.SceneReference gameScene;

    [SerializeField] private Slider difficultySlider;
    [SerializeField] private TMP_Text difficultyText;

    void Start() {
        difficultySlider.value = (int)BotAI.SavedDifficulty;
        SetDifficultyText();

        difficultySlider.onValueChanged.AddListener(SetDifficulty);
    }

    public void SetDifficulty(float difficulty) {
        PlayerPrefs.SetInt(Constants.BOT_DIFFICULTY_PREF_KEY, (int)difficulty);
        SetDifficultyText();
    }

    private void SetDifficultyText() {
        difficultyText.text = BotAI.SavedDifficulty.ToString().ToUpper();
    }

    public void StartGame() {
        Debug.Log("Starting game");
        PlayerPrefs.SetInt(Constants.GAME_MODE_PREF_KEY, (int)GameMode.Customs);

        GameModeInfo.SetCustomPrefs(true, true, false, false, false, 0f, false);

        //PlayerPrefs.SetInt(Constants.DO_TUTORIAL_PREF_KEY, );

        SceneManager.LoadScene(gameScene.SceneName);
    }
}
