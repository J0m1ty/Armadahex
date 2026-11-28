using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// the menu's home page: one control always selected (the last one pointed at, or moved to with the arrows or w/s),
// the live numbers beside them, the profile in the corner and what's new under it
public class MenuHome : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private string campaignScene = "8 Campaign";
    [SerializeField] private float refreshInterval = 1f;
    // a placeholder picture for the newest update until the changelog comes from the server
    [SerializeField] private Texture2D changelogImage;
    // where read more goes, the site for now
    [SerializeField] private string changelogUrl = "https://jomity.net";

    private const string firstRank = "Ensign";
    private const string selectedClass = "selected";

    private VisualElement home;
    private Label playing;
    private readonly List<VisualElement> items = new List<VisualElement>();
    private int selected;

    void Start() {
        var root = document.rootVisualElement;
        home = root.Q("home");

        home.Query(className: "nav-item").ForEach(item => {
            Glass.Add(item);
            var index = items.Count;
            items.Add(item);
            item.RegisterCallback<PointerEnterEvent>(_ => Select(index));
        });
        Glass.Add(home.Q("profile"));

        // the arrows move the selection here, not unity's own focus order
        home.RegisterCallback<NavigationMoveEvent>(e => {
            e.StopPropagation();
            home.focusController?.IgnoreEvent(e);
        }, TrickleDown.TrickleDown);

        var progress = root.Q<Label>("campaignProgress");
        progress.text = Mathf.RoundToInt(Missions.progress * 100) + "% COMPLETE";

        playing = root.Q<Label>("playing");
        InvokeRepeating(nameof(Refresh), 0f, refreshInterval);

        ShowProfile();
        Changelog.Fill(root.Q<ScrollView>("changelogEntries"), changelogImage);
        UIKit.OnClick(root.Q("changelogMore"), () => Application.OpenURL(changelogUrl));
        Select(0);
    }

    void Update() {
        if (items.Count == 0 || !UIKit.IsShown(home) || GameInput.typing) return;

        var keyboard = Keyboard.current;
        if (keyboard != null) {
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) Select((selected - 1 + items.Count) % items.Count);
            if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) Select((selected + 1) % items.Count);
        }

        // a click on nothing drops focus, so give it back to the selected control and enter still works
        if (UIKit.Focused(document) == null) items[selected].Focus();
    }

    public void Select(int index) {
        selected = index;
        for (var i = 0; i < items.Count; i++) {
            items[i].EnableInClassList(selectedClass, i == index);
        }
        items[index].Focus();
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

        // no ranks or experience yet, everyone starts as the first rank with none
        root.Q<Label>("profileRank").text = PlayerPrefs.GetString(Constants.STAT_MULTIPLAYER_RANK, firstRank).ToUpper();
        var xp = PlayerPrefs.GetInt(Constants.STAT_MULTIPLAYER_XP, 0);
        root.Q<Label>("profileXp").text = xp.ToString("N0") + " XP";
    }

    // the campaign map has its own scene
    public void Campaign() {
        PhotonNetwork.LoadLevel(campaignScene);
    }
}
