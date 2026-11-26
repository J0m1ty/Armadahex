using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

// the match's two sides: you on the left, the enemy on the right, each with a clock down its edge;
// the enemy's side keeps every shot, and held space shows both fleets
public class MatchHud : MonoBehaviour
{
    public static MatchHud instance { get; private set; }

    [SerializeField] private UIDocument document;
    [SerializeField] private ShipManager shipManager;
    [SerializeField] private AttackUIManager attackManager;

    [Header("Feed")]
    [SerializeField] private int feedLength = 8;
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
    private Label youName;
    private Label youTurn;
    private Label enemyName;
    private Label enemyTurn;
    private VisualElement youClockFill;
    private VisualElement enemyClockFill;
    private VisualElement feed;
    private VisualElement scoreboard;

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
        youName = root.Q<Label>("youName");
        youTurn = root.Q<Label>("youTurn");
        enemyName = root.Q<Label>("enemyName");
        enemyTurn = root.Q<Label>("enemyTurn");
        youClockFill = root.Q("youClockFill");
        enemyClockFill = root.Q("enemyClockFill");
        feed = root.Q("feed");
        scoreboard = root.Q("scoreboard");

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
        you.AddToClassList("away");
        youFade.AddToClassList("away");
        youTurn.EnableInClassList("off", true);
        enemyTurn.EnableInClassList("off", true);
        SetClockColors(false, false);
    }

    // your side pulls back to your name on their turn
    public void SetTurn(bool yours) {
        yourTurn = yours;
        low = false;

        you.EnableInClassList("away", !yours);
        youFade.EnableInClassList("away", !yours);
        youTurn.EnableInClassList("off", !yours);
        enemyTurn.EnableInClassList("off", yours);
        SetClockColors(yours, !yours);

        // a fresh turn starts full, without draining up from empty
        youClockFill.style.height = Length.Percent(100);
        enemyClockFill.style.height = Length.Percent(100);
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

    public enum ShotKind { Hit, Miss, Sunk, Contact, Clear }

    // newest on top; the old ones fade
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
        feed.Insert(0, row);

        while (feed.childCount > feedLength) feed.RemoveAt(feed.childCount - 1);
        for (var i = 0; i < feed.childCount; i++) {
            feed[i].EnableInClassList("feed-row--old", i >= 2 && i < 5);
            feed[i].EnableInClassList("feed-row--older", i >= 5);
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

    private void FillScoreboard() {
        var root = document.rootVisualElement;
        root.Q<Label>("boardMode").text = GameModeInfo.instance.GetName.ToUpper();

        var mine = shipManager.playerShips;
        var theirs = shipManager.enemyShips;
        var specials = mine.Where(s => s.isAlive).SelectMany(s => s.attacks).Where(a => !a.info.unlimited && a.info.isAllowed).Sum(a => Mathf.Max(0, a.ammoLeft));

        FillSide(root.Q("boardYou"), playerName, playerRank, SideColors.Player, mine, true, new[] {
            ("ACCURACY", Percent(attackManager.shotsHit, attackManager.shotsFired)),
            ("HITS", attackManager.shotsHit.ToString()),
            ("SHIPS AFLOAT", mine.Count(s => s.isAlive) + " / " + mine.Count),
            ("SPECIALS LEFT", GameModeInfo.instance.IsAdvancedCombat ? specials.ToString() : "-"),
        });
        FillSide(root.Q("boardEnemy"), otherName, otherRank, SideColors.Enemy, theirs, false, new[] {
            ("ACCURACY", Percent(enemyHits, enemyShots)),
            ("HITS", enemyHits.ToString()),
            ("SHIPS AFLOAT", theirs.Count(s => s.isAlive) + " / " + theirs.Count),
            ("SPECIALS LEFT", "-"),
        });
    }

    private static string Percent(int hits, int shots) {
        return shots == 0 ? "-" : Mathf.RoundToInt(100f * hits / shots) + "%";
    }

    // your own ships show their damage; theirs only show whether they're afloat
    private void FillSide(VisualElement column, string name, string rank, Color color, List<Ship> ships, bool showHull, (string, string)[] stats) {
        column.Clear();
        var right = column.ClassListContains("board__col--right");

        var title = new Label(name);
        title.AddToClassList("board__name");
        title.style.color = color;
        column.Add(title);
        var rankLabel = new Label(rank);
        rankLabel.AddToClassList("board__rank");
        column.Add(rankLabel);

        foreach (var ship in AttackUIManager.InFleetOrder(ships)) {
            var row = new VisualElement();
            row.AddToClassList("board__ship");
            var image = new VisualElement();
            image.AddToClassList("board__ship-image");
            image.EnableInClassList("sunk", !ship.isAlive);
            if (ship.shipModel.silhouette != null) image.style.backgroundImage = new StyleBackground(ship.shipModel.silhouette);
            row.Add(image);

            if (showHull && ship.segments != null) {
                var pips = new VisualElement();
                pips.AddToClassList("board__pips");
                foreach (var segment in ship.segments) {
                    var pip = new VisualElement();
                    pip.AddToClassList("hull-pip");
                    if (!segment.isAlive) pip.style.backgroundColor = ship.isAlive ? SideColors.Enemy : new Color(0.78f, 0.86f, 1f, 0.12f);
                    pips.Add(pip);
                }
                row.Add(pips);
            }
            column.Add(row);
        }

        var block = new VisualElement();
        block.AddToClassList("board__stats");
        foreach (var (key, value) in stats) {
            var row = new VisualElement();
            row.AddToClassList("stat-row");
            if (right) row.style.flexDirection = FlexDirection.RowReverse;
            var k = new Label(key);
            k.AddToClassList("stat-name");
            var v = new Label(value);
            v.AddToClassList("stat-value");
            row.Add(k);
            row.Add(v);
            block.Add(row);
        }
        column.Add(block);
    }
}
