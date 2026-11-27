using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

// the match's two sides: you on the left, the enemy on the right, each with a clock down its edge;
// only the side whose turn it is stays lit. every shot lands in the feed in the bottom corner,
// and held space shows both players as cards
public class MatchHud : MonoBehaviour
{
    public static MatchHud instance { get; private set; }

    [SerializeField] private UIDocument document;
    [SerializeField] private ShipManager shipManager;
    [SerializeField] private AttackUIManager attackManager;

    [Header("Feed")]
    [SerializeField] private int feedLength = 6;
    [SerializeField] private Color missColor = new Color(0.47f, 0.55f, 0.65f);
    [SerializeField] private Color sunkColor = new Color(1f, 0.77f, 0.31f);
    [SerializeField] private Color contactColor = new Color(0.31f, 0.78f, 1f);

    [Header("Clock")]
    // the last seconds of your turn pulse your turn line
    [SerializeField] private float lowTime = 3f;
    [SerializeField] private float pulseSpeed = 5f;

    private VisualElement hud;
    private VisualElement you;
    private VisualElement youFade;
    private VisualElement enemy;
    private VisualElement enemyFade;
    private Label youName;
    private Label youTurn;
    private Label enemyName;
    private Label enemyTurn;
    private VisualElement youClockFill;
    private VisualElement enemyClockFill;
    private VisualElement feed;
    private VisualElement scoreboard;
    private VisualElement flash;

    private bool yourTurn;
    private bool low;
    private string playerName = "PLAYER";
    private string otherName = "ENEMY";
    private string playerRank = "";
    private string otherRank = "";

    // what the enemy has done to you, for the scoreboard
    public int enemyShots { get; private set; }
    public int enemyHits { get; private set; }

    void Awake() {
        instance = this;

        var root = document.rootVisualElement;
        hud = root.Q("hud");
        you = root.Q("you");
        youFade = root.Q("youFade");
        enemy = root.Q("enemy");
        enemyFade = root.Q("enemyFade");
        youName = root.Q<Label>("youName");
        youTurn = root.Q<Label>("youTurn");
        enemyName = root.Q<Label>("enemyName");
        enemyTurn = root.Q<Label>("enemyTurn");
        youClockFill = root.Q("youClockFill");
        enemyClockFill = root.Q("enemyClockFill");
        feed = root.Q("feed");
        scoreboard = root.Q("scoreboard");
        flash = root.Q("flash");

        youTurn.style.color = SideColors.Player;
        enemyTurn.style.color = SideColors.Enemy;

        // nobody's turn until the game starts
        SetWaiting();
        UIKit.Show(scoreboard, false);
    }

    void Update() {
        // the turn line breathes in the last seconds instead of blinking
        if (low && yourTurn) {
            var pulse = 0.5f + 0.5f * Mathf.Cos(Time.time * pulseSpeed);
            youTurn.style.opacity = Mathf.Lerp(0.2f, 1f, pulse);
        }
        else {
            youTurn.style.opacity = StyleKeyword.Null;
        }
    }

    public void Show(bool show) {
        UIKit.Show(hud, show);
    }

    public void SetNames(string player, string playerRank, string enemy, string enemyRank) {
        playerName = player.ToUpper();
        otherName = enemy.ToUpper();
        this.playerRank = playerRank.ToUpper();
        this.otherRank = enemyRank.ToUpper();
        youName.text = playerName;
        enemyName.text = otherName;
    }

    // before the first turn both sides are quiet
    public void SetWaiting() {
        SetSide(you, youFade, youTurn, false);
        SetSide(enemy, enemyFade, enemyTurn, false);
        SetClockColors(false, false);
    }

    // only the side whose turn it is stays lit, the other pulls back to its edge
    public void SetTurn(bool yours) {
        yourTurn = yours;
        low = false;

        SetSide(you, youFade, youTurn, yours);
        SetSide(enemy, enemyFade, enemyTurn, !yours);
        SetClockColors(yours, !yours);

        // a fresh turn starts full, without draining up from empty
        youClockFill.style.height = Length.Percent(100);
        enemyClockFill.style.height = Length.Percent(100);
    }

    private static void SetSide(VisualElement side, VisualElement fade, Label turn, bool lit) {
        side.EnableInClassList("away", !lit);
        fade.EnableInClassList("away", !lit);
        turn.EnableInClassList("off", !lit);
    }

    private void SetClockColors(bool youActive, bool enemyActive) {
        youClockFill.style.backgroundColor = Fade(SideColors.Player, youActive ? 0.9f : 0.22f);
        enemyClockFill.style.backgroundColor = Fade(SideColors.Enemy, enemyActive ? 0.9f : 0.22f);
    }

    // how much of the current side's turn is left, 0..1; unlimited turns stay full
    public void SetClock(float fraction, float secondsLeft, bool unlimited) {
        var fill = yourTurn ? youClockFill : enemyClockFill;
        fill.style.height = Length.Percent((unlimited ? 1 : Mathf.Clamp01(fraction)) * 100);
        low = !unlimited && secondsLeft <= lowTime;
    }

    private static Color Fade(Color color, float alpha) {
        color.a = alpha;
        return color;
    }

    // the screen goes to black, for leaving the match
    public void FadeOut(float time) {
        StartCoroutine(FadeOutRoutine(time));
    }

    private IEnumerator FadeOutRoutine(float time) {
        for (var t = 0f; t < time; t += Time.unscaledDeltaTime) {
            flash.style.opacity = t / time;
            yield return null;
        }
        flash.style.opacity = 1;
    }

    public enum ShotKind { Hit, Miss, Sunk, Contact, Clear }

    // newest at the bottom; the old ones fade as they climb
    public void AddShot(bool byYou, ShotKind kind, string what) {
        var row = new VisualElement { pickingMode = PickingMode.Ignore };
        row.AddToClassList("feed-row");

        var label = new Label(what.ToUpper()) { pickingMode = PickingMode.Ignore };
        label.AddToClassList("feed-row__what");
        var word = new Label(kind.ToString().ToUpper()) { pickingMode = PickingMode.Ignore };
        word.AddToClassList("feed-row__word");
        word.style.color = kind switch {
            ShotKind.Hit => byYou ? SideColors.Player : SideColors.Enemy,
            ShotKind.Sunk => sunkColor,
            ShotKind.Contact => contactColor,
            _ => missColor
        };
        var dot = new VisualElement { pickingMode = PickingMode.Ignore };
        dot.AddToClassList("feed-row__dot");
        dot.style.backgroundColor = byYou ? SideColors.Player : SideColors.Enemy;

        row.Add(label);
        row.Add(word);
        row.Add(dot);
        feed.Add(row);

        while (feed.childCount > feedLength) feed.RemoveAt(0);
        for (var i = 0; i < feed.childCount; i++) {
            var age = feed.childCount - 1 - i;
            feed[i].EnableInClassList("feed-row--old", age >= 2 && age < 4);
            feed[i].EnableInClassList("feed-row--older", age >= 4);
        }

        if (!byYou) {
            enemyShots++;
            if (kind == ShotKind.Hit || kind == ShotKind.Sunk) enemyHits++;
        }
    }

    // held space
    public void ShowScoreboard(bool show) {
        if (show) FillScoreboard();
        UIKit.Show(scoreboard, show);
    }

    public bool scoreboardShown => UIKit.IsShown(scoreboard);

    private static readonly string[] columns = { "RANK", "RATING", "HITS", "ACCURACY", "SPECIALS" };

    // a card per player, stacked: a block in their color with the ships they have left, their name and their numbers
    private void FillScoreboard() {
        var root = document.rootVisualElement;
        root.Q<Label>("boardMode").text = GameModeInfo.instance.GetName.ToUpper();

        var mine = shipManager.playerShips;
        var theirs = shipManager.enemyShips;
        var advanced = GameModeInfo.instance.IsAdvancedCombat;
        var specials = mine.Where(s => s.isAlive).SelectMany(s => s.attacks).Where(a => !a.info.unlimited && a.info.isAllowed).Sum(a => Mathf.Max(0, a.ammoLeft));

        var header = root.Q("boardHeader");
        header.Clear();
        var spacer = new VisualElement();
        spacer.AddToClassList("board__header-spacer");
        header.Add(spacer);
        foreach (var column in columns) {
            header.Add(Cell(column, "board__head"));
        }

        Card(root.Q("boardYou"), playerName, SideColors.Player, mine.Count(s => s.isAlive), new[] {
            playerRank, "-", attackManager.shotsHit.ToString(), Percent(attackManager.shotsHit, attackManager.shotsFired), advanced ? specials.ToString() : "-"
        });
        Card(root.Q("boardEnemy"), otherName, SideColors.Enemy, theirs.Count(s => s.isAlive), new[] {
            otherRank, "-", enemyHits.ToString(), Percent(enemyHits, enemyShots), "-"
        });
    }

    private static string Percent(int hits, int shots) {
        return shots == 0 ? "-" : Mathf.RoundToInt(100f * hits / shots) + "%";
    }

    private static Label Cell(string text, string className) {
        var cell = new Label(text);
        cell.AddToClassList("board__cell");
        cell.AddToClassList(className);
        return cell;
    }

    private static void Card(VisualElement card, string name, Color color, int afloat, string[] values) {
        card.Clear();

        var block = new VisualElement();
        block.AddToClassList("board__block");
        block.style.backgroundColor = color;
        var count = new Label(afloat.ToString());
        count.AddToClassList("board__afloat");
        // dark on a light color, white on a dark one
        var light = 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b > 0.55f;
        count.style.color = light ? new Color(0.04f, 0.05f, 0.09f) : Color.white;
        block.Add(count);
        card.Add(block);

        var title = new Label(name);
        title.AddToClassList("board__name");
        card.Add(title);

        foreach (var value in values) {
            card.Add(Cell(value, "board__value"));
        }
    }
}
