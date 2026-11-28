using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UIElements;
using System.Linq;

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
    private VisualElement rule;
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
        rule = card.Q(className: "intro__rule");
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

        // one or two rules that change how it plays, leaving out what the playlist's name already says
        var parts = new List<string> { advancedAttacks ? "ADVANCED COMBAT" : "CLASSIC RULES" };
        var info = GameModeInfo.instance;
        if (info.IsSalvo) parts.Add("SALVO");
        else if (info.IsBonus) parts.Add("BONUS SHOTS");
        if (timePerTurn > 0 && ct != ConnectionType.Offline) parts.Add(timePerTurn + "S TURNS");
        parts.RemoveAll(part => playlist.Contains(part));
        rules = string.Join(separator, parts.Take(2));

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

    // every card: the kicker says what it is, the title names it, and the line under the rule adds to it
    private void ShowOverview() {
        kicker.text = MatchType();
        SetKickerColor(StyleKeyword.Null);
        title.text = playlist;
        SetLine(rules);
    }

    // a person's line is their rank and rating; a bot has neither
    private void ShowPlayer(bool you) {
        kicker.text = you ? "YOU" : "ENEMY";
        SetKickerColor(you ? SideColors.Player : SideColors.Enemy);
        title.text = (you ? youName : enemyName).ToUpper();

        var bot = offline && !you;
        var rank = (you ? YourRank() : EnemyRank()).ToUpper();
        SetLine(bot ? "" : offline ? rank : rank + separator + "UNRANKED");
    }

    // the chip's text and border, null for the default white
    private void SetKickerColor(StyleColor color) {
        kicker.style.color = color;
        kicker.style.borderTopColor = color;
        kicker.style.borderRightColor = color;
        kicker.style.borderBottomColor = color;
        kicker.style.borderLeftColor = color;
    }

    private void SetLine(string text) {
        line.text = text;
        rule.style.visibility = string.IsNullOrEmpty(text) ? Visibility.Hidden : Visibility.Visible;
    }

    private string MatchType() {
        if (Missions.playing != null) return "CAMPAIGN";
        switch (connection) {
            case ConnectionType.Offline: return "TRAINING";
            case ConnectionType.PrivateMultiplayer: return "CUSTOM GAME";
            default: return "MULTIPLAYER";
        }
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
        // the board the first turn plays on
        var board = youFirst ? enemy : you;

        if (GameSettings.reducedMotion) {
            yield return Still(board);
        }
        else {
            yield return Flash(() => {
                ShowPlayer(!youFirst);
                Spiral(second);
            });
            yield return SpiralEnd();

            yield return Flash(() => {
                ShowPlayer(youFirst);
                Spiral(first);
            });
            yield return SpiralEnd();

            // back from the last flash in the top view, over the board the first turn plays on
            yield return Flash(() => {
                ToTop(board);
                ShowHud();
            });
        }

        yield return Count();

        UIKit.Show(intro, false);
        tm.StartGame();
    }

    // reduced motion: one cut to the top view, then the cards change over it without the spirals.
    // each step takes as long as its spiral would, so a player with it off still starts the same turn together
    private IEnumerator Still(Team board) {
        var start = Time.time;
        yield return Flash(() => {
            ShowPlayer(!youFirst);
            ToTop(board);
        });
        while (Time.time - start < spiralTime) yield return null;

        start = Time.time;
        yield return Fade(() => ShowPlayer(youFirst), card);
        while (Time.time - start < spiralTime) yield return null;

        yield return Fade(ShowHud, card, vignette);
    }

    private void ToTop(Team board) {
        StopSpiral();
        introCamera.End();
        CameraManager.instance.SetCameraState(CameraState.Top, false);
        CameraManager.instance.MoveToInstant(board.teamBase.transform.position);
    }

    private void ShowHud() {
        UIKit.Show(card, false);
        UIKit.Show(vignette, false);
        MatchHud.instance.Show(true);
    }

    private Coroutine spiral;
    private float spiralStart;

    private void Spiral(Team team) {
        StopSpiral();
        spiralStart = Time.time;
        spiral = StartCoroutine(SpiralRoutine(team));
    }

    private void StopSpiral() {
        if (spiral != null) StopCoroutine(spiral);
        spiral = null;
    }

    // keeps moving until it's stopped, so the next flash starts while it's still turning
    private IEnumerator SpiralRoutine(Team team) {
        while (true) {
            introCamera.Spiral(team, (Time.time - spiralStart) / spiralTime);
            yield return null;
        }
    }

    // the next flash starts just before the spiral is done
    private IEnumerator SpiralEnd() {
        while (Time.time - spiralStart < spiralTime - flashIn) yield return null;
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

    // like a flash, but only the given elements dip out and back, for when nothing behind them changes
    private IEnumerator Fade(Action swap, params VisualElement[] elements) {
        for (var time = 0f; time < flashIn; time += Time.deltaTime) {
            foreach (var element in elements) element.style.opacity = 1 - time / flashIn;
            yield return null;
        }
        foreach (var element in elements) element.style.opacity = 0;

        swap();
        yield return new WaitForSeconds(flashHold);

        for (var time = 0f; time < flashOut; time += Time.deltaTime) {
            foreach (var element in elements) element.style.opacity = time / flashOut;
            yield return null;
        }
        foreach (var element in elements) element.style.opacity = StyleKeyword.Null;
    }

    private IEnumerator Count() {
        UIKit.Show(count, true);
        for (var number = countFrom; number > 0; number--) {
            count.text = number.ToString();
            // reduced motion keeps the fade but drops the punch
            var scale = GameSettings.reducedMotion ? 1 : countPunch;
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
