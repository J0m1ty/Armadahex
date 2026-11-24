using Photon.Pun;
using UnityEngine;
using UnityEngine.UIElements;

// the menu's home page: the glass on each control, the live numbers beside them, and the profile in the corner
public class MenuHome : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private string multiplayerScene = "3 Multiplayer";
    [SerializeField] private float refreshInterval = 1f;

    private const string dash = "-";

    private Label playing;

    void Start() {
        var root = document.rootVisualElement;
        var home = root.Q("home");

        home.Query(className: "nav-item").ForEach(Glass.Add);
        Glass.Add(home.Q("profile"));

        // no campaign yet, it shows where it will be
        root.Q("campaign").SetEnabled(false);
        var progress = root.Q<Label>("campaignProgress");
        progress.text = Mathf.RoundToInt(PlayerPrefs.GetFloat(Constants.STAT_CAMPAIGN_PROGRESS, 0f) * 100) + "% COMPLETE";

        playing = root.Q<Label>("playing");
        InvokeRepeating(nameof(Refresh), 0f, refreshInterval);

        ShowProfile();
    }

    // the photon lobby counts everyone online, in a match or not
    private void Refresh() {
        playing.text = PhotonNetwork.IsConnectedAndReady ? PhotonNetwork.CountOfPlayers + " PLAYING" : dash;
    }

    private void ShowProfile() {
        var root = document.rootVisualElement;
        var captain = Captains.active;

        var slot = root.Q("profileCaptain");
        slot.Clear();
        if (captain != null) {
            var portrait = ProgressionView.Portrait(captain);
            portrait.AddToClassList("profile__portrait");
            portrait.pickingMode = PickingMode.Ignore;
            slot.Add(portrait);
        }
        root.Q<Label>("profileCaptainName").text = captain != null ? captain.captainName.ToUpper() : dash;

        // no ranks or experience yet
        var rank = PlayerPrefs.GetString(Constants.STAT_MULTIPLAYER_RANK, "");
        root.Q<Label>("profileRank").text = rank == "" ? dash : rank;
        root.Q<Label>("profileXp").text = PlayerPrefs.HasKey(Constants.STAT_MULTIPLAYER_XP) ? PlayerPrefs.GetInt(Constants.STAT_MULTIPLAYER_XP).ToString("N0") : dash;
    }

    // until it's a tab on the custom game screen, join game opens multiplayer with customs picked
    public void JoinGame() {
        PlayerPrefs.SetString(Constants.OPEN_PLAYLIST_PREF_KEY, Playlists.Custom);
        PhotonNetwork.LoadLevel(multiplayerScene);
    }
}
