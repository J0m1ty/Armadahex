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
    private TMP_Text gameType;
    [SerializeField]
    private TMP_Text advancedAttacks;
    [SerializeField]
    private TMP_Text timePerTurn;
    [SerializeField]
    private TMP_Text firstPlayer;

    [Header("Countdown Info")]
    [SerializeField]
    private TMP_Text countdownText; 
    [SerializeField]
    private float countdownDuration = 5f;
    [SerializeField]
    private bool countdownStarted;
    
    public void SetInfo(string friendlyName, string enemyName, string gameType, bool advancedAttacks, int timePerTurn, bool firstPlayer) {
        this.friendlyPlayerName.text = friendlyName;
        this.enemyPlayerName.text = enemyName;
        this.gameType.text = gameType;
        this.advancedAttacks.text = "Advanced Attacks <color=#FB980E>" + (advancedAttacks ? "Enabled" : "Disabled") + "</color>";
        this.timePerTurn.text = "<color=#FB980E>" + timePerTurn + "</color> seconds per turn";
        this.firstPlayer.text = "Your turn <color=#FB980E>" + (firstPlayer ? "First" : "Second") + "</color>";
    }
    
    public void SetPlayerInfo(string friendlyName, RankInfo rank, int friendlyGamesPlayed, int friendlyXP, string enemyName, RankInfo enemyRank, int enemyGamesPlayed, int enemyXP) {
        this.friendlyPlayerName.text = friendlyName;
        this.friendlyGamesPlayed.text = friendlyGamesPlayed + " Games Played";
        this.friendlyRankImage.sprite = rank.sprite;
        this.friendlyRank.text = rank.rank.ToString();
        this.friendlyXP.text = friendlyXP + " XP";

        this.enemyPlayerName.text = enemyName;
        this.enemyGamesPlayed.text = enemyGamesPlayed + " Games Played";
        this.enemyRankImage.sprite = enemyRank.sprite;
        this.enemyRank.text = enemyRank.rank.ToString();
        this.enemyXP.text = enemyXP + " XP";
    }

    void Start() {
        countdownStarted = false;

        countdownText.text = countdownDuration + "";

        pregameDisplay.SetActive(true);
        uiDisplay.SetActive(false);
        Fade();
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
}
