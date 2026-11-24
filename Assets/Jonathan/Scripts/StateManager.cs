using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// the multiplayer screen's playlists: a card per listed playlist in the strip, and the side panel for the picked one
public class StateManager : MonoBehaviour
{
    // the picked playlist, null when nothing is
    public Playlist current { get; private set; }

    public bool allowLocking;
    public bool lockedIn;

    [SerializeField]
    private UIDocument document;

    private VisualElement modes;
    private Label stateName;
    private Label stateDescription;
    private Button stateButton;
    public TextField stateInput { get; private set; }

    private Dictionary<Playlist, VisualElement> cards = new Dictionary<Playlist, VisualElement>();

    // the play button's word for the picked playlist
    public string buttonText => current != null && current.usesCode ? "JOIN" : "PLAY";

    private void Awake() {
        var root = document.rootVisualElement;
        modes = root.Q("modes");
        stateName = root.Q<Label>("stateName");
        stateDescription = root.Q<Label>("stateDescription");
        stateButton = root.Q<Button>("join");
        stateInput = root.Q<TextField>("code");
    }

    private void Start() {
        foreach (var playlist in Playlists.All) {
            if (!playlist.listed) continue;

            var picked = playlist;
            var card = Card(playlist);
            UIKit.OnClick(card, () => Select(picked), ClickSound.Light);
            modes.Add(card);
            cards[playlist] = card;
        }

        // the menu can ask for a playlist to be picked already, only this once
        var open = PlayerPrefs.GetString(Constants.OPEN_PLAYLIST_PREF_KEY, "");
        PlayerPrefs.DeleteKey(Constants.OPEN_PLAYLIST_PREF_KEY);
        Select((Playlist)null);
        if (open != "") Select(open);

        stateInput.RegisterValueChangedCallback(e => {
            DisplayButton(e.newValue.Length == 4);
        });
    }

    // a code playlist's button stays put but only works once the code is whole, so nothing jumps
    public void DisplayButton(bool display) {
        stateButton.SetEnabled(current == null || !current.usesCode || display);
    }

    // picking the picked one again puts it back down
    public void Select(Playlist playlist) {
        if (allowLocking && lockedIn) return;
        if (playlist != null && playlist == current) playlist = null;

        current = playlist;
        foreach (var pair in cards) {
            UIKit.SetLit(pair.Value, pair.Key == current);
        }

        var picked = current != null;
        UIKit.Show(stateName, picked);
        UIKit.Show(stateDescription, picked);
        UIKit.Show(stateButton, picked);

        stateName.text = picked ? current.title.ToUpper() : "";
        stateDescription.text = picked ? current.description : "";
        stateButton.text = buttonText;

        stateInput.value = "";
        UIKit.Show(stateInput, picked && current.usesCode);
        DisplayButton(false);
    }

    public void Select(string id) {
        Select(Playlists.Find(id));
    }

    // a playlist's card: its art if it has some, in a solid frame, with the name along the bottom
    private static VisualElement Card(Playlist playlist) {
        var card = new VisualElement { name = playlist.id };
        card.AddToClassList("mode-card");
        if (playlist.art != null) {
            card.style.backgroundImage = playlist.art;
        }

        var band = new VisualElement();
        band.AddToClassList("mode-card__band");
        var label = new Label(playlist.title.ToUpper());
        label.AddToClassList("mode-card__name");
        band.Add(label);
        card.Add(band);
        return card;
    }
}
