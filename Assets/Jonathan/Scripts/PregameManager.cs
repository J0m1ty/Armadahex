using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UIElements;

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
    private UIDocument document;
    private VisualElement pregameDisplay;
    private VisualElement uiDisplay;
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

    private Label friendlyPlayerName;
    private Label friendlyGamesPlayed;
    private Label enemyPlayerName;
    private Label enemyGamesPlayed;

    private Label connectionType;
    private Label gameType;
    private Label advancedAttacks;
    private Label timePerTurn;
    private Label firstPlayer;

    private Label waitingForOtherPlayerText;
    private Label countdownText;

    [Header("Countdown Info")]
    [SerializeField]
    private float countdownDuration = 5f;
    [SerializeField]
    private bool countdownStarted;
    // each new number lands big and settles
    [SerializeField]
    private float countdownPunch = 1.4f;
    [SerializeField]
    private float countdownSettle = 0.35f;

    // the scoreboard
    private Label uiConnectionType;
    private Label uiGameType;
    private Label uiPlayers;

    // other scripts can set the info before this one starts
    void Awake() {
        var root = document.rootVisualElement;
        pregameDisplay = root.Q("pregame");
        uiDisplay = root.Q("hud");
        friendlyPlayerName = root.Q<Label>("friendlyName");
        friendlyGamesPlayed = root.Q<Label>("friendlyGames");
        enemyPlayerName = root.Q<Label>("enemyName");
        enemyGamesPlayed = root.Q<Label>("enemyGames");
        connectionType = root.Q<Label>("connectionType");
        gameType = root.Q<Label>("gameType");
        advancedAttacks = root.Q<Label>("advancedAttacks");
        timePerTurn = root.Q<Label>("timePerTurn");
        firstPlayer = root.Q<Label>("firstPlayer");
        waitingForOtherPlayerText = root.Q<Label>("waiting");
        countdownText = root.Q<Label>("countdownText");
        uiConnectionType = root.Q<Label>("boardType");
        uiGameType = root.Q<Label>("boardMode");
        uiPlayers = root.Q<Label>("boardPlayers");
    }

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

        UIKit.Show(pregameDisplay, true);
        UIKit.Show(uiDisplay, false);
        Fade();

        AudioManager.instance?.PlayGameModeSound(GameNetworking.instance.gameMode);

        UIKit.Show(waitingForOtherPlayerText, !GameModeInfo.instance.IsSingleplayer);
    }

    public void Fade() {
        StartCoroutine(FadeCoroutine());
    }

    private IEnumerator FadeCoroutine() {
        float time = 0;
        while (time < fadeInDuration) {
            pregameDisplay.style.backgroundColor = Color.Lerp(fadeInFrom, fadeInTo, time / fadeInDuration);
            time += Time.deltaTime;
            yield return null;
        }
        pregameDisplay.style.backgroundColor = fadeInTo;

        fadedIn = true;

        GameNetworking.instance.DisplayLoaded();

        if (GameModeInfo.instance.IsSingleplayer) {
            TryStartCountdown();
        }
    }

    public void TryStartCountdown() {
        if (TurnManager.instance.loading || countdownStarted || !fadedIn) return;

        Debug.Log("Starting countdown...");
        UIKit.Show(waitingForOtherPlayerText, false);

        StartCountdown(() => {
            UIKit.Show(pregameDisplay, false);
            UIKit.Show(uiDisplay, true);
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
        var scale = 1f;
        while (time > 0) {
            var number = Mathf.CeilToInt(time);
            if (number != shown) {
                shown = number;
                countdownText.text = number + "";
                scale = countdownPunch;
            }

            var settle = 1 - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, countdownSettle) * 4);
            scale = Mathf.Lerp(scale, 1, settle);
            countdownText.style.scale = new Scale(Vector3.one * scale);

            time -= Time.deltaTime;
            yield return null;
        }
        callback();
    }
}
