using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// the progression page: the highlighted captain's card on the left, the captain library on the right,
// and the player's stats along the bottom. it fills the named elements inside it, so it needs nothing in the scene
[UxmlElement]
public partial class ProgressionView : VisualElement
{
    private const string dash = "—";
    // tiles fade out over this many points as they near the bottom of the library
    private const float fadeDistance = 120f;

    private Captain highlighted;
    private ScrollView grid;
    private readonly Dictionary<Captain, VisualElement> tiles = new Dictionary<Captain, VisualElement>();

    public ProgressionView() {
        RegisterCallback<AttachToPanelEvent>(_ => Build());
    }

    private void Build() {
        if (!Application.isPlaying) return;

        grid = this.Q<ScrollView>("captainGrid");
        if (grid == null || tiles.Count > 0) return;

        foreach (var captain in Captains.All) {
            var tile = Tile(captain);
            grid.Add(tile);
            tiles[captain] = tile;
        }

        // one button, its job follows the captain: unlock a claimable one, or make an unlocked one active
        UIKit.OnClick(this.Q("captainAction"), () => {
            if (highlighted == null) return;
            if (Captains.State(highlighted) == CaptainState.Unlockable) Captains.Unlock(highlighted);
            else Captains.MakeActive(highlighted);
            Highlight(highlighted);
        });

        Highlight(Captains.active);
        FillStats();
        schedule.Execute(Fade).Every(16);
    }

    private VisualElement Tile(Captain captain) {
        var tile = new VisualElement();
        tile.AddToClassList("captain-tile");

        var portrait = Portrait(captain);
        portrait.AddToClassList("captain-tile__portrait");
        var tag = new Label { name = "tag" };
        tag.AddToClassList("key-chip");
        tag.AddToClassList("captain-tile__tag");
        portrait.Add(tag);
        var lockIcon = new LockIcon { name = "lock" };
        lockIcon.AddToClassList("captain-tile__lock");
        portrait.Add(lockIcon);
        tile.Add(portrait);

        var name = new Label(captain.captainName.ToUpper());
        name.AddToClassList("captain-tile__name");
        tile.Add(name);

        tile.AddManipulator(new Clickable(() => Highlight(captain)));
        return tile;
    }

    // a portrait, or the captain's initials until they have one
    private static VisualElement Portrait(Captain captain) {
        var portrait = new VisualElement();
        portrait.AddToClassList("captain-portrait");
        if (captain.portrait != null) {
            portrait.style.backgroundImage = captain.portrait;
        }
        else {
            var initials = new Label(captain.initials.ToUpper());
            initials.AddToClassList("captain-initials");
            portrait.Add(initials);
        }
        return portrait;
    }

    private void Highlight(Captain captain) {
        highlighted = captain;
        if (captain == null) return;

        var active = Captains.active;
        foreach (var pair in tiles) {
            var state = Captains.State(pair.Key);
            var tile = pair.Value;
            tile.EnableInClassList("lit", pair.Key == captain);
            tile.EnableInClassList("captain-tile--locked", state == CaptainState.Locked);
            tile.EnableInClassList("captain-tile--unlockable", state == CaptainState.Unlockable);
            tile.EnableInClassList("captain-tile--active", pair.Key == active);

            var tag = tile.Q<Label>("tag");
            tag.text = pair.Key == active ? "ACTIVE" : state == CaptainState.Unlockable ? "NEW" : "";
            UIKit.Show(tag, tag.text != "");
            UIKit.Show(tile.Q("lock"), state == CaptainState.Locked);
        }

        var portrait = this.Q("captainPortrait");
        portrait.Clear();
        var big = Portrait(captain);
        big.AddToClassList("captain-card__portrait");
        portrait.Add(big);

        var captainState = Captains.State(captain);
        this.Q<Label>("captainName").text = captain.captainName.ToUpper();
        this.Q<Label>("captainEpithet").text = captain.epithet.ToUpper();
        this.Q<Label>("passiveName").text = captain.passiveName;
        this.Q<Label>("passive").text = captain.passive;
        this.Q<Label>("specialName").text = captain.specialName;
        this.Q<Label>("special").text = captain.special;
        this.Q("captainCard").EnableInClassList("captain-card--locked", captainState == CaptainState.Locked);

        // where they come from, always shown for captains that had to be earned
        var source = this.Q("captainSource");
        UIKit.Show(source, captain.source != CaptainSource.Starter);
        UIKit.Show(this.Q("captainSourceLock"), captainState == CaptainState.Locked);
        this.Q<Label>("captainSourceKind").text = captain.source.ToString().ToUpper();
        this.Q<Label>("captainRequirement").text = captain.requirement;

        // the foot: ACTIVE for the active captain, otherwise the one thing to do, if anything
        var isActive = captain == active;
        UIKit.Show(this.Q("captainActive"), isActive);
        var action = this.Q<Button>("captainAction");
        UIKit.Show(action, !isActive && captainState != CaptainState.Locked);
        action.text = captainState == CaptainState.Unlockable ? "UNLOCK" : "MAKE ACTIVE";
    }

    // tiles near the bottom edge of the library fade out, so it reads as more below
    private void Fade() {
        if (grid == null) return;

        var view = grid.contentViewport.worldBound;
        foreach (var tile in tiles.Values) {
            var bounds = tile.worldBound;
            var below = view.yMax - bounds.center.y;
            tile.style.opacity = Mathf.Clamp01(below / fadeDistance);
        }
    }

    private void FillStats() {
        Set("statCampaign", Percent(Constants.STAT_CAMPAIGN_PROGRESS));
        Set("statSingleplayerGames", Number(Constants.STAT_SINGLEPLAYER_GAMES));
        Set("statMultiplayerXp", Number(Constants.STAT_MULTIPLAYER_XP));
        Set("statMultiplayerRank", Text(Constants.STAT_MULTIPLAYER_RANK));
        Set("statWins", Number(Constants.STAT_WINS));
        Set("statLosses", Number(Constants.STAT_LOSSES));

        var favorite = Captains.Find(PlayerPrefs.GetString(Constants.STAT_FAVORITE_CAPTAIN, ""));
        Set("statFavoriteCaptain", favorite != null ? favorite.captainName : dash);
        Set("statFavoriteSquare", Text(Constants.STAT_FAVORITE_SQUARE));
        Set("statFavoriteWeapon", Text(Constants.STAT_FAVORITE_WEAPON));
        Set("statTimePlayed", Duration(Constants.STAT_SECONDS_PLAYED));
        Set("statCaptains", Captains.unlockedCount + " / " + Captains.All.Count);
    }

    private void Set(string element, string value) {
        var label = this.Q<Label>(element);
        if (label != null) label.text = value;
    }

    private static string Number(string key) {
        return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key).ToString("N0") : dash;
    }

    private static string Text(string key) {
        var value = PlayerPrefs.GetString(key, "");
        return string.IsNullOrEmpty(value) ? dash : value;
    }

    private static string Percent(string key) {
        return PlayerPrefs.HasKey(key) ? Mathf.RoundToInt(PlayerPrefs.GetFloat(key) * 100) + "%" : dash;
    }

    private static string Duration(string key) {
        if (!PlayerPrefs.HasKey(key)) return dash;
        var seconds = PlayerPrefs.GetFloat(key);
        var hours = Mathf.FloorToInt(seconds / 3600);
        var minutes = Mathf.FloorToInt(seconds % 3600 / 60);
        return hours > 0 ? hours + "H " + minutes + "M" : minutes + "M";
    }
}
