using Photon.Pun;
using UnityEngine;
using UnityEngine.UIElements;

// the menu's home page: the glass on each control, the live numbers beside them, and the profile in the corner
public class MenuHome : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private string multiplayerScene = "3 Multiplayer";
    [SerializeField] private string campaignScene = "8 Campaign";
    [SerializeField] private float refreshInterval = 1f;

    private const string dash = "-";
    private const string firstRank = "Ensign";

    private Label playing;

    void Start() {
        var root = document.rootVisualElement;
        var home = root.Q("home");

        home.Query(className: "nav-item").ForEach(Glass.Add);
        Glass.Add(home.Q("profile"));

        var progress = root.Q<Label>("campaignProgress");
        progress.text = Mathf.RoundToInt(PlayerPrefs.GetFloat(Constants.STAT_CAMPAIGN_PROGRESS, 0f) * 100) + "% COMPLETE";

        playing = root.Q<Label>("playing");
        InvokeRepeating(nameof(Refresh), 0f, refreshInterval);

        ShowProfile();
    }

    // the photon lobby counts everyone online, in a match or not
    private void Refresh() {
        playing.text = PhotonNetwork.IsConnectedAndReady ? PhotonNetwork.CountOfPlayers + " PLAYING" : "OFFLINE";
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

        // no ranks yet, everyone starts as the first one
        root.Q<Label>("profileRank").text = PlayerPrefs.GetString(Constants.STAT_MULTIPLAYER_RANK, firstRank).ToUpper();
    }

    // the campaign has its own scene, empty for now
    public void Campaign() {
        PhotonNetwork.LoadLevel(campaignScene);
    }

    // until it's a tab on the custom game screen, join game opens multiplayer with customs picked
    public void JoinGame() {
        PlayerPrefs.SetString(Constants.OPEN_PLAYLIST_PREF_KEY, Playlists.Custom);
        PhotonNetwork.LoadLevel(multiplayerScene);
    }
}
