using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public enum GameMode {
    AdvancedCombat,
    ClassicBattleship,
    TacticalSalvo,
    Customs
}

[Serializable]
public class GameModeData {
    public string name;
    public GameMode gameMode;
    public bool isSingleplayer;
    public bool isAdvancedCombat;
    public bool isSalvo;
    public float turnTimeLimit;
}

public class GameModeInfo : MonoBehaviour {
    public List<GameModeData> gameModes;

    public static GameModeInfo instance;

    private void Awake() {
        if (instance == null) {
            instance = this;
        } else {
            Destroy(this);
        }
    }

    public GameModeData From(GameMode gameMode) {
        return gameModes.Find(g => g.gameMode == gameMode);
    }

    public bool IsSingleplayer => From(GameNetworking.instance.gameMode).isSingleplayer;
    public bool IsAdvancedCombat => From(GameNetworking.instance.gameMode).isAdvancedCombat;
    public bool IsSalvo => From(GameNetworking.instance.gameMode).isSalvo;
    public float TurnTimeLimit => From(GameNetworking.instance.gameMode).turnTimeLimit;

    public void SetCustomWithPrefs() {
        Debug.Log("Getting custom prefs");

        var custom = gameModes.Find(g => g.gameMode == GameMode.Customs);
        
        if (PlayerPrefs.HasKey(Constants.CUSTOMS_IS_SINGLEPLAYER_PREF_KEY)) {
            custom.isSingleplayer = PlayerPrefs.GetInt(Constants.CUSTOMS_IS_SINGLEPLAYER_PREF_KEY) == 1;
        }

        if (PlayerPrefs.HasKey(Constants.CUSTOMS_IS_ADVANCED_COMBAT_PREF_KEY)) {
            custom.isAdvancedCombat = PlayerPrefs.GetInt(Constants.CUSTOMS_IS_ADVANCED_COMBAT_PREF_KEY) == 1;
        }
        if (PlayerPrefs.HasKey(Constants.CUSTOMS_IS_SALVO_PREF_KEY)) {
            custom.isSalvo = PlayerPrefs.GetInt(Constants.CUSTOMS_IS_SALVO_PREF_KEY) == 1;
        }

        if (PlayerPrefs.HasKey(Constants.CUSTOMS_TURN_TIME_LIMIT_KEY)) {
            custom.turnTimeLimit = PlayerPrefs.GetFloat(Constants.CUSTOMS_TURN_TIME_LIMIT_KEY);
        }

        PlayerPrefs.DeleteKey(Constants.CUSTOMS_IS_SINGLEPLAYER_PREF_KEY);
        PlayerPrefs.DeleteKey(Constants.CUSTOMS_IS_ADVANCED_COMBAT_PREF_KEY);
        PlayerPrefs.DeleteKey(Constants.CUSTOMS_IS_SALVO_PREF_KEY);
        PlayerPrefs.DeleteKey(Constants.CUSTOMS_TURN_TIME_LIMIT_KEY);
    }

    public void SetCustom(bool isSingleplayer, bool isAdvancedCombat, bool isSalvo) {
        var custom = gameModes.Find(g => g.gameMode == GameMode.Customs);

        custom.isSingleplayer = isSingleplayer;
        custom.isAdvancedCombat = isAdvancedCombat;
        custom.isSalvo = isSalvo;
    }

    public static void SetCustomPrefs(bool isSingleplayer, bool isAdvancedCombat, bool isSalvo, float turnTimeLimit) {
        PlayerPrefs.SetInt(Constants.CUSTOMS_IS_SINGLEPLAYER_PREF_KEY, isSingleplayer ? 1 : 0);
        PlayerPrefs.SetInt(Constants.CUSTOMS_IS_ADVANCED_COMBAT_PREF_KEY, isAdvancedCombat ? 1 : 0);
        PlayerPrefs.SetInt(Constants.CUSTOMS_IS_SALVO_PREF_KEY, isSalvo ? 1 : 0);
        PlayerPrefs.SetFloat(Constants.CUSTOMS_TURN_TIME_LIMIT_KEY, turnTimeLimit);
    }
}
