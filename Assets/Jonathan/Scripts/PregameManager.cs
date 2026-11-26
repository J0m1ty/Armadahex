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

// the intro: the map with the playlist over it, then the player going second and the one going first,
// each a spiral out over their board, a flash between each, and a count into the first turn from the top view
public class PregameManager : MonoBehaviour
{
    [SerializeField]
    private UIDocument document;
    [SerializeField]
    private IntroCamera introCamera;

    [Header("Timing")]
    // the screen comes up from black
    [SerializeField]
    private float fadeInDuration = 1f;
    // the map shows at least this long, or until the other player is ready
    [SerializeField]
    private float overviewTime = 2.5f;
    [SerializeField]
    private float spiralTime = 3.4f;
    [SerializeField]
    private float flashIn = 0.2f;
    [SerializeField]
    private float flashHold = 0.12f;
    [SerializeField]
    private float flashOut = 0.4f;

    [Header("Count")]
    [SerializeField]
    private int countFrom = 3;
    [SerializeField]
    private float countStep = 0.8f;
    // each new number lands big and settles
    [SerializeField]
    private float countPunch = 1.4f;
    [SerializeField]
    private float countSettle = 0.35f;

    private const string separator = "   ·   ";
    private const string defaultRank = "Ensign";

    private VisualElement intro;
    private VisualElement vignette;
    private VisualElement card;
    private Label kicker;
    private Label title;
    private Label line;
    private Label count;
    private VisualElement flash;

    private bool fadedIn;
    private bool started;
    private float shownAt;

    private ConnectionType connection;
    private string playlist = "";
    private string rules = "";
    private bool youFirst = true;
    private string youName = "Player";
    private string enemyName = "Enemy";

    // other scripts can set the info before this one starts
    void Awake() {
        var root = document.rootVisualElement;
        intro = root.Q("intro");
        vignette = root.Q("introVignette");
        card = root.Q("introCard");
        kicker = root.Q<Label>("introKicker");
        title = root.Q<Label>("introTitle");
        line = root.Q<Label>("introLine");
        count = root.Q<Label>("introCount");
        flash = root.Q("flash");
    }

    public void SetInfo(ConnectionType ct, string gameMode, bool advancedAttacks, int timePerTurn, bool firstPlayer) {
        connection = ct;
        playlist = gameMode.ToUpper();
        youFirst = firstPlayer;

        // one or two rules that change how it plays
        var parts = new List<string> { advancedAttacks ? "ADVANCED COMBAT" : "CLASSIC RULES" };
        var info = GameModeInfo.instance;
        if (info.IsSalvo) parts.Add("SALVO");
        else if (info.IsBonus) parts.Add("BONUS SHOTS");
        else if (timePerTurn > 0 && ct != ConnectionType.Offline) parts.Add(timePerTurn + "S TURNS");
        rules = string.Join(separator, parts);

        if (!started) ShowOverview();
    }

    // ranks and xp aren't tracked yet: a bot is a bot, everyone else starts as the first rank, unranked
    public void SetPlayerInfo(string friendlyName, RankInfo rank, int friendlyGamesPlayed, int friendlyXP, string enemyName, RankInfo enemyRank, int enemyGamesPlayed, int enemyXP) {
        youName = friendlyName.Length > 0 ? friendlyName : "Player";
        this.enemyName = enemyName;
        MatchHud.instance.SetNames(youName, YourRank(), this.enemyName, EnemyRank());
    }

    private bool offline => connection == ConnectionType.Offline;

    private string YourRank() {
        return PlayerPrefs.GetString(Constants.STAT_MULTIPLAYER_RANK, defaultRank);
    }

    private string EnemyRank() {
        return offline ? "Bot" : defaultRank;
    }

    void Start() {
        MatchHud.instance.Show(false);
        UIKit.Show(intro, true);
        UIKit.Show(count, false);
        ShowOverview();
        StartCoroutine(FadeIn());
    }

    private void ShowOverview() {
        kicker.text = "";
        title.text = playlist;
        line.text = rules;
    }

    private void ShowPlayer(bool you) {
        kicker.text = you ? "YOU" : "ENEMY";
        kicker.style.color = you ? SideColors.Player : SideColors.Enemy;
        title.text = (you ? youName : enemyName).ToUpper();

        var rank = (you ? YourRank() : EnemyRank()).ToUpper();
        line.text = offline ? (you ? rank : "TRAINING") : rank + separator + "UNRANKED";
    }

    private IEnumerator FadeIn() {
        flash.style.opacity = 1;
        for (var time = 0f; time < fadeInDuration; time += Time.deltaTime) {
            flash.style.opacity = 1 - time / fadeInDuration;
            yield return null;
        }
        flash.style.opacity = 0;

        fadedIn = true;
        shownAt = Time.time;

        GameNetworking.instance.DisplayLoaded();

        if (GameModeInfo.instance.IsSingleplayer) {
            TryStartCountdown();
        }
    }

    // both players start this together, and every step after it takes a fixed time, so the first turn lines up
    public void TryStartCountdown() {
        if (TurnManager.instance.loading || started || !fadedIn) return;

        Debug.Log("Starting intro...");
        started = true;
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence() {
        while (Time.time - shownAt < overviewTime) yield return null;

        var tm = TurnManager.instance;
        var you = tm.playerTeam;
        var enemy = tm.enemyTeam;
        var second = youFirst ? enemy : you;
        var first = youFirst ? you : enemy;

        yield return Flash(() => ShowPlayer(!youFirst));
        yield return Spiral(second);

        yield return Flash(() => ShowPlayer(youFirst));
        yield return Spiral(first);

        // back from the last flash in the top view, over the board the first turn plays on
        yield return Flash(() => {
            introCamera.End();
            var board = youFirst ? enemy : you;
            CameraManager.instance.SetCameraState(CameraState.Top, false);
            CameraManager.instance.MoveToInstant(board.teamBase.transform.position);
            UIKit.Show(card, false);
            UIKit.Show(vignette, false);
            MatchHud.instance.Show(true);
        });

        yield return Count();

        UIKit.Show(intro, false);
        tm.StartGame();
    }

    private IEnumerator Spiral(Team team) {
        for (var time = 0f; time < spiralTime; time += Time.deltaTime) {
            introCamera.Spiral(team, time / spiralTime);
            yield return null;
        }
        introCamera.Spiral(team, 1);
    }

    // the screen dips to black, swaps while it's dark and comes back
    private IEnumerator Flash(Action swap) {
        for (var time = 0f; time < flashIn; time += Time.deltaTime) {
            flash.style.opacity = time / flashIn;
            yield return null;
        }
        flash.style.opacity = 1;

        swap();
        yield return new WaitForSeconds(flashHold);

        for (var time = 0f; time < flashOut; time += Time.deltaTime) {
            flash.style.opacity = 1 - time / flashOut;
            yield return null;
        }
        flash.style.opacity = 0;
    }

    private IEnumerator Count() {
        UIKit.Show(count, true);
        for (var number = countFrom; number > 0; number--) {
            count.text = number.ToString();
            var scale = countPunch;
            for (var time = 0f; time < countStep; time += Time.deltaTime) {
                var settle = 1 - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, countSettle) * 4);
                scale = Mathf.Lerp(scale, 1, settle);
                count.style.scale = new Scale(Vector3.one * scale);
                count.style.opacity = 1 - Mathf.Clamp01((time - countStep * 0.6f) / (countStep * 0.4f));
                yield return null;
            }
        }
        UIKit.Show(count, false);
    }
}
