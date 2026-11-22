using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;

public enum Rank {
    Ensign,
    Lieutenant,
    Commander,
    Captain,
    Admiral
}

public class RankInfo {
    public Rank rank;
    public int minXP;
    public Sprite sprite;
}

public enum ConnectionType {
    PublicMultiplayer,
    PrivateMultiplayer,
    Offline
}

public class PregameManager : MonoBehaviour
{
    [Header("Panel Info")]
    [SerializeField]
    private GameObject pregameDisplay;
    [SerializeField]
    private GameObject uiDisplay;
    [SerializeField]
    private Color fadeInFrom;
    [SerializeField]
    private Color fadeInTo;
    [SerializeField]
    private float fadeInDuration = 1f;
    [SerializeField]
    private bool fadedIn;
    [SerializeField]
    private IntroCamera introCamera;

    [Header("Player Info")]
    [SerializeField]
    private TMP_Text friendlyPlayerName;
    [SerializeField]
    private TMP_Text friendlyGamesPlayed;

    [SerializeField]
    private TMP_Text enemyPlayerName;
    [SerializeField]
    private TMP_Text enemyGamesPlayed;

    [Header("Game Info")]
    [SerializeField]
    private TMP_Text connectionType;
    [SerializeField]
    private TMP_Text gameType;
    [SerializeField]
    private TMP_Text advancedAttacks;
    [SerializeField]
    private TMP_Text timePerTurn;
    [SerializeField]
    private TMP_Text firstPlayer;

    [Header("Countdown Info")]
    [SerializeField]
    private TMP_Text waitingForOtherPlayerText; 
    [SerializeField]
    private TMP_Text countdownText; 
    [SerializeField]
    private float countdownDuration = 5f;
    [SerializeField]
    private bool countdownStarted;
    // each new number lands big and settles
    [SerializeField]
    private float countdownPunch = 1.4f;
    [SerializeField]
    private float countdownSettle = 0.35f;

    [Header("UI Connection")]
    [SerializeField] private TMP_Text uiConnectionType;
    [SerializeField] private TMP_Text uiGameType;
    [SerializeField] private TMP_Text uiPlayers;

    public static string ConnectionTypeToString(ConnectionType ct) {
        switch (ct) {
            case ConnectionType.PublicMultiplayer:
                return "Public Multiplayer";
            case ConnectionType.PrivateMultiplayer:
                return "Private Multiplayer";
            case ConnectionType.Offline:
                return "Singleplayer";
        }
        return "";
    }

    public void SetInfo(ConnectionType ct, string gameMode, bool advancedAttacks, int timePerTurn, bool firstPlayer) {
        this.connectionType.text = ConnectionTypeToString(ct).ToUpper();
        this.gameType.text = gameMode.ToUpper();
        this.advancedAttacks.text = advancedAttacks ? "ADVANCED ATTACKS" : "CLASSIC ATTACKS";
        this.timePerTurn.text = timePerTurn > 0 ? timePerTurn + "S PER TURN" : "NO TURN LIMIT";
        this.firstPlayer.text = firstPlayer ? "YOU GO FIRST" : "THEY GO FIRST";

        SetLeaderboardInfo(ct, gameMode);
    }

    public void SetLeaderboardInfo(ConnectionType ct, string gameMode) {
        this.uiConnectionType.text = ConnectionTypeToString(ct).ToUpper();
        this.uiGameType.text = gameMode.ToUpper();
    }
    
    // ranks and xp aren't tracked yet, so the cards only show names and games played
    public void SetPlayerInfo(string friendlyName, RankInfo rank, int friendlyGamesPlayed, int friendlyXP, string enemyName, RankInfo enemyRank, int enemyGamesPlayed, int enemyXP) {
        this.friendlyPlayerName.text = friendlyName.Length > 0 ? friendlyName : "Player";
        this.friendlyGamesPlayed.text = friendlyGamesPlayed + " GAMES PLAYED";

        this.enemyPlayerName.text = enemyName;
        this.enemyGamesPlayed.text = enemyGamesPlayed + " GAMES PLAYED";

        SetLeaderboardPlayerInfo(this.friendlyPlayerName.text, enemyName);
    }

    public void SetLeaderboardPlayerInfo(string friendlyName, string enemyName) {
        this.uiPlayers.text = friendlyName.ToUpper() + "  VS  " + enemyName.ToUpper();
    }

    void Start() {
        countdownStarted = false;

        countdownText.text = countdownDuration + "";

        pregameDisplay.SetActive(true);
        uiDisplay.SetActive(false);
        Fade();

        AudioManager.instance?.PlayGameModeSound(GameNetworking.instance.gameMode);

        waitingForOtherPlayerText.gameObject.SetActive(!GameModeInfo.instance.IsSingleplayer);
    }

    public void Fade() {
        StartCoroutine(FadeCoroutine());
    }

    private IEnumerator FadeCoroutine() {
        float time = 0;
        while (time < fadeInDuration) {
            pregameDisplay.GetComponent<Image>().color = Color.Lerp(fadeInFrom, fadeInTo, time / fadeInDuration);
            time += Time.deltaTime;
            yield return null;
        }
        pregameDisplay.GetComponent<Image>().color = fadeInTo;

        fadedIn = true;

        GameNetworking.instance.DisplayLoaded();

        if (GameModeInfo.instance.IsSingleplayer) {
            TryStartCountdown();
        }
    }

    public void TryStartCountdown() {
        if (TurnManager.instance.loading || countdownStarted || !fadedIn) return;

        Debug.Log("Starting countdown...");
        waitingForOtherPlayerText.gameObject.SetActive(false);

        StartCountdown(() => {
            pregameDisplay.SetActive(false);
            uiDisplay.SetActive(true);
            if (introCamera != null) introCamera.End();
            TurnManager.instance.StartGame();
        });
    }

    private void StartCountdown(Action callback) {
        StartCoroutine(Countdown(callback));
    }

    private IEnumerator Countdown(Action callback) {
        countdownStarted = true;
        float time = countdownDuration;
        var shown = -1;
        while (time > 0) {
            var number = Mathf.CeilToInt(time);
            if (number != shown) {
                shown = number;
                countdownText.text = number + "";
                countdownText.transform.localScale = Vector3.one * countdownPunch;
            }

            var settle = 1 - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, countdownSettle) * 4);
            countdownText.transform.localScale = Vector3.Lerp(countdownText.transform.localScale, Vector3.one, settle);

            time -= Time.deltaTime;
            yield return null;
        }
        callback();
    }
}
