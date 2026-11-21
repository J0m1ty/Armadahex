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

[Serializable]
public class InstructionAnimation {
    [Header("Animation Info")]
    public float duration;
    public float delay;
    public AnimationCurve curve;

    public float elapsedTime;
    public bool completed;

    [Header("UI References")]
    public GameObject stepContainer;
    public TMP_Text stepText;
    public TMP_Text stepDescription;
    public TMP_Text stepDescriptionTargetRef;
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

    [Header("Player Info")]
    [SerializeField]
    private TMP_Text friendlyPlayerName;
    [SerializeField]
    private TMP_Text friendlyGamesPlayed;
    [SerializeField]
    private Image friendlyRankImage;
    [SerializeField]
    private TMP_Text friendlyRank;
    [SerializeField]
    private TMP_Text friendlyXP;

    [SerializeField]
    private TMP_Text enemyPlayerName;
    [SerializeField]
    private TMP_Text enemyGamesPlayed;  
    [SerializeField]
    private Image enemyRankImage;
    [SerializeField]
    private TMP_Text enemyRank;
    [SerializeField]
    private TMP_Text enemyXP;

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

    [Header("UI Connection")]
    [SerializeField] private TMP_Text uiConnectionType;
    [SerializeField] private TMP_Text uiGameType;
    [SerializeField] private TMP_Text uiPlayers;

    [Header("Instruction Animations")]
    [SerializeField] private InstructionAnimation[] instructionAnimations;

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
        this.connectionType.text = ConnectionTypeToString(ct);
        this.gameType.text = gameMode;
        this.advancedAttacks.text = "Advanced Attacks <color=#FB980E>" + (advancedAttacks ? "Enabled" : "Disabled") + "</color>";
        this.timePerTurn.text = (timePerTurn > 0 ? "<color=#FB980E>" + timePerTurn + "</color> seconds" : "<color=#FB980E>Unlimited</color> time") + " per turn";
        this.firstPlayer.text = "Your turn <color=#FB980E>" + (firstPlayer ? "First" : "Second") + "</color>";

        SetLeaderboardInfo(ct, gameMode);
    }

    public void SetLeaderboardInfo(ConnectionType ct, string gameMode) {
        this.uiConnectionType.text = ConnectionTypeToString(ct);
        this.uiGameType.text = gameMode;
    }
    
    public void SetPlayerInfo(string friendlyName, RankInfo rank, int friendlyGamesPlayed, int friendlyXP, string enemyName, RankInfo enemyRank, int enemyGamesPlayed, int enemyXP) {
        this.friendlyPlayerName.text = friendlyName.Length > 0 ? friendlyName : "Player";
        this.friendlyGamesPlayed.text = friendlyGamesPlayed + " Games Played";
        if (rank != null) {
            this.friendlyRankImage.sprite = rank.sprite;
            this.friendlyRank.text = rank.rank.ToString();
        }
        this.friendlyXP.text = friendlyXP + " XP";

        this.enemyPlayerName.text = enemyName;
        this.enemyGamesPlayed.text = enemyGamesPlayed + " Games Played";
        if (rank != null) {
            this.enemyRankImage.sprite = enemyRank.sprite;
            this.enemyRank.text = enemyRank.rank.ToString();
        }
        this.enemyXP.text = enemyXP + " XP";

        SetLeaderboardPlayerInfo(friendlyName, enemyName);
    }

    public void SetLeaderboardPlayerInfo(string friendlyName, string enemyName) {
        this.uiPlayers.text = friendlyName.ToUpper() + " VS. " + enemyName.ToUpper();
    }

    void Start() {
        countdownStarted = false;

        countdownText.text = countdownDuration + "";

        pregameDisplay.SetActive(true);
        uiDisplay.SetActive(false);
        Fade();

        AudioManager.instance?.PlayGameModeSound(GameNetworking.instance.gameMode);

        waitingForOtherPlayerText.gameObject.SetActive(!GameModeInfo.instance.IsSingleplayer);

        foreach (InstructionAnimation ia in instructionAnimations) {
            ia.stepText.gameObject.SetActive(false);
            ia.elapsedTime = 0f;
            ia.completed = false;
        }
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
            TurnManager.instance.StartGame();
        });
    }

    private void StartCountdown(Action callback) {
        StartCoroutine(Countdown(callback));
    }

    private IEnumerator Countdown(Action callback) {
        countdownStarted = true;
        float time = countdownDuration;
        while (time > 0) {
            countdownText.text = Mathf.CeilToInt(time) + "";
            time -= Time.deltaTime;
            yield return null;
        }
        callback();
    }

    void Update() {
        foreach (InstructionAnimation ia in instructionAnimations) {
            if (ia.completed) continue;

            if (ia.delay > 0) {
                ia.delay -= Time.deltaTime;
                continue;
            }
            
            ia.elapsedTime += Time.deltaTime;
            
            // move ia.stepDescription to ia.ia.stepDescriptionTargetRef and lerp text size
            // use ia.curve to lerp
            float t = ia.elapsedTime / ia.duration;
            if (t > 1f) {
                t = 1;
                ia.completed = true;
            }

            if (t > 0.3f) {
                ia.stepText.gameObject.SetActive(true);

                //lerp alpha from 0 to 1 twice as fast
                float alpha = Mathf.Lerp(0, 1, ia.curve.Evaluate((t - 0.3f) * (1/0.3f)));
                ia.stepText.color = new Color(ia.stepText.color.r, ia.stepText.color.g, ia.stepText.color.b, alpha);
            }
            
            ia.stepDescription.transform.position = Vector3.Lerp(ia.stepDescription.transform.position, ia.stepDescriptionTargetRef.transform.position, ia.curve.Evaluate(t));

            ia.stepDescription.fontSize = Mathf.Lerp(ia.stepDescription.fontSize, ia.stepDescriptionTargetRef.fontSize, ia.curve.Evaluate(t));
        }
    }
}
