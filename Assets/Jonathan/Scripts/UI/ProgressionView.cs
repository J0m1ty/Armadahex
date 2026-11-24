using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// the progression page: the highlighted captain's card on the left, the captain library on the right,
// and the player's stats along the bottom. it fills the named elements inside it, so it needs nothing in the scene
[UxmlElement]
public partial class ProgressionView : VisualElement
{
    private const string dash = "—";

    private Captain highlighted;
    private readonly Dictionary<Captain, VisualElement> tiles = new Dictionary<Captain, VisualElement>();

    public ProgressionView() {
        RegisterCallback<AttachToPanelEvent>(_ => Build());
    }

    private void Build() {
        if (!Application.isPlaying) return;

        var grid = this.Q<ScrollView>("captainGrid");
        if (grid == null || tiles.Count > 0) return;

        foreach (var captain in Captains.All) {
            var tile = Tile(captain);
            grid.Add(tile);
            tiles[captain] = tile;
        }

        UIKit.OnClick(this.Q("chooseCaptain"), () => {
            Captains.Choose(highlighted);
            Highlight(highlighted);
        });

        Highlight(Captains.chosen);
        FillStats();
    }

    private VisualElement Tile(Captain captain) {
        var tile = new VisualElement();
        tile.AddToClassList("captain-tile");
        tile.EnableInClassList("captain-tile--locked", !Captains.IsUnlocked(captain));

        var portrait = Portrait(captain);
        portrait.AddToClassList("captain-tile__portrait");
        var chosen = new Label("CHOSEN");
        chosen.AddToClassList("key-chip");
        chosen.AddToClassList("captain-tile__chosen");
        portrait.Add(chosen);
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

        var chosen = Captains.chosen;
        foreach (var pair in tiles) {
            pair.Value.EnableInClassList("lit", pair.Key == captain);
            pair.Value.EnableInClassList("captain-tile--chosen", pair.Key == chosen);
        }

        var portrait = this.Q("captainPortrait");
        portrait.Clear();
        var big = Portrait(captain);
        big.AddToClassList("captain-card__portrait");
        portrait.Add(big);

        var unlocked = Captains.IsUnlocked(captain);
        this.Q<Label>("captainName").text = captain.captainName.ToUpper();
        this.Q<Label>("captainEpithet").text = captain.epithet.ToUpper();
        this.Q<Label>("passiveName").text = captain.passiveName;
        this.Q<Label>("passive").text = captain.passive;
        this.Q<Label>("specialName").text = captain.specialName;
        this.Q<Label>("special").text = captain.special;
        this.Q("captainCard").EnableInClassList("captain-card--locked", !unlocked);

        var lockLabel = this.Q<Label>("captainLock");
        lockLabel.text = unlocked ? "" : "FOUND IN THE " + captain.unlockedBy.ToUpper();
        UIKit.Show(lockLabel, !unlocked);

        var choose = this.Q<Button>("chooseCaptain");
        UIKit.Show(choose, unlocked);
        choose.text = captain == chosen ? "CHOSEN" : "CHOOSE";
        choose.SetEnabled(captain != chosen);
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
