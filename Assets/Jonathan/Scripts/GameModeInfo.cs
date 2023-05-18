using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public enum GameMode {
    AdvancedCombat,
    ClassicBattleship,
    TacticalSalvo,
    BonusBarrage,
    Customs
}

[Serializable]
public class GameModeData {
    public string name;
    public GameMode gameMode;
    public bool isSingleplayer;
    public bool isAdvancedCombat;
    public bool isSalvo;
    public bool isBonus;
    public bool isUnlimitedAmmo;
    public float turnTimeLimit;
    public bool allowChat;
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
    public bool IsBonus => From(GameNetworking.instance.gameMode).isBonus;
    public bool IsUnlimitedAmmo => From(GameNetworking.instance.gameMode).isUnlimitedAmmo;
    public float TurnTimeLimit => From(GameNetworking.instance.gameMode).turnTimeLimit;
    public bool AllowChat => From(GameNetworking.instance.gameMode).allowChat;

    public string GetName => From(GameNetworking.instance.gameMode).name;

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

        if (PlayerPrefs.HasKey(Constants.CUSTOMS_IS_BONUS_PREF_KEY)) {
            custom.isBonus = PlayerPrefs.GetInt(Constants.CUSTOMS_IS_BONUS_PREF_KEY) == 1;
        }
        
        if (PlayerPrefs.HasKey(Constants.CUSTOMS_IS_UNLIMITED_PREF_KEY)) {
            custom.isUnlimitedAmmo = PlayerPrefs.GetInt(Constants.CUSTOMS_IS_UNLIMITED_PREF_KEY) == 1;
        }

        if (PlayerPrefs.HasKey(Constants.CUSTOMS_TURN_TIME_LIMIT_KEY)) {
            custom.turnTimeLimit = PlayerPrefs.GetFloat(Constants.CUSTOMS_TURN_TIME_LIMIT_KEY);
        }
        
        if (PlayerPrefs.HasKey(Constants.CUSTOMS_ALLOW_CHAT_KEY)) {
            custom.allowChat = PlayerPrefs.GetInt(Constants.CUSTOMS_ALLOW_CHAT_KEY) == 1;
        }

        PlayerPrefs.DeleteKey(Constants.CUSTOMS_IS_SINGLEPLAYER_PREF_KEY);
        PlayerPrefs.DeleteKey(Constants.CUSTOMS_IS_ADVANCED_COMBAT_PREF_KEY);
        PlayerPrefs.DeleteKey(Constants.CUSTOMS_IS_SALVO_PREF_KEY);
        PlayerPrefs.DeleteKey(Constants.CUSTOMS_IS_BONUS_PREF_KEY);
        PlayerPrefs.DeleteKey(Constants.CUSTOMS_IS_UNLIMITED_PREF_KEY);
        PlayerPrefs.DeleteKey(Constants.CUSTOMS_TURN_TIME_LIMIT_KEY);
        PlayerPrefs.DeleteKey(Constants.CUSTOMS_ALLOW_CHAT_KEY);
    }

    public void SetCustom(bool isSingleplayer, bool isAdvancedCombat, bool isSalvo, bool isBonus, bool isUnlimitedAmmo, float turnTimeLimit, bool allowChat) {
        var custom = gameModes.Find(g => g.gameMode == GameMode.Customs);

        custom.isSingleplayer = isSingleplayer;
        custom.isAdvancedCombat = isAdvancedCombat;
        custom.isSalvo = isSalvo;
        custom.isBonus = isBonus;
        custom.isUnlimitedAmmo = isUnlimitedAmmo;
        custom.turnTimeLimit = turnTimeLimit;
        custom.allowChat = allowChat;
    }

    public static void SetCustomPrefs(bool isSingleplayer, bool isAdvancedCombat, bool isSalvo, bool isBonus, bool isUnlimited, float turnTimeLimit, bool allowChat) {
        PlayerPrefs.SetInt(Constants.CUSTOMS_IS_SINGLEPLAYER_PREF_KEY, isSingleplayer ? 1 : 0);
        PlayerPrefs.SetInt(Constants.CUSTOMS_IS_ADVANCED_COMBAT_PREF_KEY, isAdvancedCombat ? 1 : 0);
        PlayerPrefs.SetInt(Constants.CUSTOMS_IS_SALVO_PREF_KEY, isSalvo ? 1 : 0);
        PlayerPrefs.SetInt(Constants.CUSTOMS_IS_BONUS_PREF_KEY, isBonus ? 1 : 0);
        PlayerPrefs.SetInt(Constants.CUSTOMS_IS_UNLIMITED_PREF_KEY, isUnlimited ? 1 : 0);
        PlayerPrefs.SetFloat(Constants.CUSTOMS_TURN_TIME_LIMIT_KEY, turnTimeLimit);
        PlayerPrefs.SetInt(Constants.CUSTOMS_ALLOW_CHAT_KEY, allowChat ? 1 : 0);
    }
}
