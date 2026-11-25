using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

// the custom game screen: create a room with your own rules and share its code, or join one with a code.
// the room lives here until the match loads, the button under the rules shows where it's at
public class CustomUIManager : MonoBehaviourPunCallbacks {
    [SerializeField] private UIDocument document;

    public enum CustomsStage {
        Error,
        Customizing,
        Working, // making or joining the room
        Waiting, // in our own room for someone to join
        Found
    }

    private CustomsStage stage = CustomsStage.Customizing;

    // the tabs
    private Button createTab;
    private Button joinTab;
    private VisualElement createPage;
    private VisualElement joinPage;
    private bool joining;

    // the rules
    private ListDropdown presetsDropdown;
    private Switch isAdvancedAttacks;
    private Switch isUnlimitedAmmo;
    private Switch isSalvo;
    private Switch isBonus;
    private Switch allowChat;
    private Switch captains;
    private SliderInt turnTimeSlider;
    private Label turnTimeText;
    private VisualElement unlimitedAmmoRow;

    // every listed playlist played without a code is a preset, the last choice is custom
    private List<Playlist> presets;
    // null when the rules are the player's own
    private Playlist preset;

    // the slider's last step is no time limit
    private const int unlimitedTime = 95;

    // the made room's code and the code to join
    private VisualElement codeContainer;
    private Label codeText;
    private CopyIcon copyIcon;
    private TextField joinCode;
    private VisualElement codeSlots;
    private bool typing;
    private string code;

    // the button: create or join, then what the room is doing; hovering it while it's cancelable reads CANCEL
    private Button button;
    private string statusText;
    private bool hovering;
    private float waitStart;
    private bool cancelAction;

    private VisualElement backButton;

    void Awake() {
        var root = document.rootVisualElement;
        createTab = root.Q<Button>("createTab");
        joinTab = root.Q<Button>("joinTab");
        createPage = root.Q("createPage");
        joinPage = root.Q("joinPage");
        presetsDropdown = root.Q<ListDropdown>("presets");
        isAdvancedAttacks = root.Q<Switch>("advancedAttacks");
        isUnlimitedAmmo = root.Q<Switch>("unlimitedAmmo");
        isSalvo = root.Q<Switch>("salvo");
        isBonus = root.Q<Switch>("bonus");
        allowChat = root.Q<Switch>("allowChat");
        captains = root.Q<Switch>("captains");
        turnTimeSlider = root.Q<SliderInt>("turnTime");
        turnTimeText = root.Q<Label>("turnTimeText");
        unlimitedAmmoRow = root.Q("unlimitedAmmoRow");
        codeContainer = root.Q("codeContainer");
        codeText = root.Q<Label>("code");
        copyIcon = root.Q<CopyIcon>("copy");
        joinCode = root.Q<TextField>("joinCode");
        codeSlots = root.Q("codeSlots");
        button = root.Q<Button>("create");
        backButton = root.Q("backButton");

        UIKit.OnClick(createTab, () => SetTab(false), ClickSound.Light);
        UIKit.OnClick(joinTab, () => SetTab(true), ClickSound.Light);

        foreach (var rule in new[] { isAdvancedAttacks, isUnlimitedAmmo, isSalvo, isBonus, captains, allowChat }) {
            rule.changed += _ => RulesChanged();
        }
        turnTimeSlider.RegisterValueChangedCallback(e => {
            // whole steps of 5 seconds
            var snapped = Mathf.RoundToInt(e.newValue / 5f) * 5;
            if (snapped != e.newValue) {
                turnTimeSlider.SetValueWithoutNotify(snapped);
            }
            RulesChanged();
        });
        presetsDropdown.changed += PickPreset;

        // codes are letters and numbers, shown in capitals
        joinCode.RegisterValueChangedCallback(e => {
            var clean = new string(e.newValue.ToUpper().Where(char.IsLetterOrDigit).ToArray());
            if (clean != e.newValue) {
                joinCode.SetValueWithoutNotify(clean);
            }
            ShowCode();
            UpdateButton();
        });
        joinCode.RegisterCallback<FocusInEvent>(_ => { typing = true; ShowCode(); });
        joinCode.RegisterCallback<FocusOutEvent>(_ => { typing = false; ShowCode(); });
        joinCode.RegisterCallback<KeyDownEvent>(e => {
            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) {
                StartOrCancelMatch();
            }
        }, TrickleDown.TrickleDown);

        button.RegisterCallback<PointerEnterEvent>(_ => hovering = true);
        button.RegisterCallback<PointerLeaveEvent>(_ => hovering = false);
    }

    void Start() {
        presets = Playlists.All.Where(p => p.listed && !p.usesCode).ToList();
        var names = presets.Select(p => p.title.ToUpper()).ToList();
        names.Add("CUSTOM");
        presetsDropdown.SetChoices(names);

        PickPreset(0);
        SetTab(false);
    }

    void Update() {
        var busy = stage == CustomsStage.Working || stage == CustomsStage.Waiting;
        button.EnableInClassList("search-busy", busy);
        button.EnableInClassList("search-error", stage == CustomsStage.Error);
        if (!busy && stage != CustomsStage.Found) return;

        if (busy && !cancelAction && hovering) {
            button.text = "CANCEL";
        }
        else if (stage == CustomsStage.Waiting && !cancelAction) {
            var time = Mathf.FloorToInt(Time.time - waitStart);
            button.text = "WAITING  " + time / 60 + ":" + (time % 60).ToString("00");
        }
        else {
            button.text = statusText;
        }
    }

    public void SetTab(bool join) {
        if (stage != CustomsStage.Customizing) return;

        joining = join;
        UIKit.SetLit(createTab, !join);
        UIKit.SetLit(joinTab, join);
        UIKit.Show(createPage, !join);
        UIKit.Show(joinPage, join);

        if (join) {
            joinCode.schedule.Execute(() => joinCode.Focus());
        }
        else {
            joinCode.Blur();
        }

        UpdateButton();
    }

    // the typed code, a character to a slot; the next empty slot is lit while typing
    private void ShowCode() {
        var value = joinCode.value;
        codeSlots.EnableInClassList("focused", typing);
        var i = 0;
        foreach (var slot in codeSlots.Children().OfType<Label>()) {
            slot.text = i < value.Length ? value[i].ToString() : "";
            slot.EnableInClassList("code-slot--filled", i < value.Length);
            slot.EnableInClassList("code-slot--current", i == value.Length);
            i++;
        }
    }

    // join only works once the code is whole; the button stays put either way
    private void UpdateButton() {
        if (stage != CustomsStage.Customizing) return;

        button.text = joining ? "JOIN" : "CREATE";
        button.SetEnabled(!joining || joinCode.value.Length == 4);
    }

    private void PickPreset(int index) {
        // the custom choice keeps the rules as they are
        if (index >= presets.Count) {
            preset = null;
            presetsDropdown.index = presets.Count;
            return;
        }

        preset = presets[index];
        presetsDropdown.index = index;

        isAdvancedAttacks.value = preset.advancedCombat;
        isUnlimitedAmmo.value = preset.unlimitedAmmo;
        isSalvo.value = preset.salvo;
        isBonus.value = preset.bonus;
        allowChat.value = preset.allowChat;
        captains.value = preset.captains;
        turnTimeSlider.SetValueWithoutNotify(preset.turnTimeLimit <= 0 ? unlimitedTime : (int)preset.turnTimeLimit);

        RefreshRules();
    }

    // any change away from the preset makes it a custom game
    private void RulesChanged() {
        if (preset != null && !Matches(preset)) {
            preset = null;
            presetsDropdown.index = presets.Count;
        }

        RefreshRules();
    }

    private bool Matches(Playlist playlist) {
        return isAdvancedAttacks.value == playlist.advancedCombat &&
            isUnlimitedAmmo.value == playlist.unlimitedAmmo &&
            isSalvo.value == playlist.salvo &&
            isBonus.value == playlist.bonus &&
            allowChat.value == playlist.allowChat &&
            captains.value == playlist.captains &&
            turnTimeLimit == playlist.turnTimeLimit;
    }

    private float turnTimeLimit => turnTimeSlider.value >= unlimitedTime ? 0f : turnTimeSlider.value;

    private void RefreshRules() {
        // unlimited ammo is only for advanced attacks
        unlimitedAmmoRow.SetEnabled(isAdvancedAttacks.value);
        unlimitedAmmoRow.EnableInClassList("unavailable", !isAdvancedAttacks.value);
        turnTimeText.text = turnTimeLimit > 0 ? turnTimeLimit + " SECONDS" : "UNLIMITED";
    }

    private void SetStage(CustomsStage stage) {
        this.stage = stage;

        var customizing = stage == CustomsStage.Customizing;
        createTab.SetEnabled(customizing);
        joinTab.SetEnabled(customizing);
        createPage.SetEnabled(customizing);
        joinCode.SetEnabled(customizing);
        UIKit.Show(backButton, customizing);
        if (stage == CustomsStage.Waiting) waitStart = Time.time;

        RefreshRules();
    }

    public void StartOrCancelMatch() {
        if (stage == CustomsStage.Error || stage == CustomsStage.Found || cancelAction) return;

        if (stage == CustomsStage.Customizing) {
            if (joining && joinCode.value.Length != 4) return;

            if (!PhotonNetwork.IsConnectedAndReady) {
                Error("OFFLINE");
                return;
            }

            statusText = joining ? "JOINING" : "CREATING";
            SetStage(CustomsStage.Working);

            if (joining) {
                PhotonNetwork.JoinRoom(joinCode.value);
            }
            else {
                CreateCustomRoom();
            }
        }
        else if (PhotonNetwork.InRoom) {
            PhotonNetwork.LeaveRoom();
        }
        else {
            cancelAction = true;
            statusText = "CANCELING";
        }
    }

    private void CreateCustomRoom() {
        RoomOptions roomOptions = new RoomOptions();

        // a preset plays by its own playlist's rules, anything else sends them along
        roomOptions.CustomRoomProperties = new Hashtable() {
            { Constants.GAME_MODE_PROP_KEY, preset != null ? preset.id : Playlists.Custom },
            { "AllowChat", allowChat.value },
            { "TurnTimeLimit", turnTimeLimit },
            { "IsAdvancedCombat", isAdvancedAttacks.value },
            { "IsUnlimitedAmmo", isAdvancedAttacks.value && isUnlimitedAmmo.value },
            { "IsSalvo", isSalvo.value },
            { "IsBonus", isBonus.value },
            { "Captains", captains.value }
        };

        roomOptions.MaxPlayers = Constants.ROOM_NUM_EXPECTED_PLAYERS;
        roomOptions.IsVisible = false;

        TypedLobby typedLobby = new TypedLobby(Playlists.Custom, LobbyType.Default);

        code = CreateCode();
        PhotonNetwork.CreateRoom(code, roomOptions, typedLobby);
    }

    public override void OnCreateRoomFailed(short returnCode, string message) {
        Debug.Log("Failed to create room: " + message);

        // someone has this code already, try another
        if (returnCode == ErrorCode.GameIdAlreadyExists && !cancelAction) {
            CreateCustomRoom();
            return;
        }

        Error("CAN'T CREATE");
    }

    public override void OnJoinRoomFailed(short returnCode, string message) {
        Debug.Log("Failed to join room: " + message);

        if (returnCode == ErrorCode.GameDoesNotExist) {
            Error("NO SUCH GAME");
        }
        else if (returnCode == ErrorCode.GameFull || returnCode == ErrorCode.GameClosed) {
            Error("GAME FULL");
        }
        else {
            Error("CAN'T JOIN");
        }
    }

    public override void OnJoinedRoom() {
        Debug.Log("Joined room " + PhotonNetwork.CurrentRoom.Name);

        if (cancelAction) {
            PhotonNetwork.LeaveRoom();
            return;
        }

        if (joining) {
            if (PhotonNetwork.CurrentRoom.PlayerCount == Constants.ROOM_NUM_EXPECTED_PLAYERS) {
                StartGame();
            }
            else {
                // the host left as we came in
                Error("NO SUCH GAME");
                PhotonNetwork.LeaveRoom();
            }
            return;
        }

        codeText.text = code;
        codeContainer.RemoveFromClassList("invisible");
        SetStage(CustomsStage.Waiting);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) {
        Debug.Log("Player entered room");

        if (PhotonNetwork.CurrentRoom.PlayerCount == Constants.ROOM_NUM_EXPECTED_PLAYERS) {
            StartGame();
        }
    }

    public override void OnLeftRoom() {
        Debug.Log("Left room");

        // an error clears itself after a moment
        if (stage != CustomsStage.Error) {
            Cancel();
        }
    }

    public override void OnDisconnected(DisconnectCause cause) {
        if (stage != CustomsStage.Customizing) {
            Error("DISCONNECTED");
        }
    }

    private void Cancel() {
        cancelAction = false;
        SetStage(CustomsStage.Customizing);

        codeContainer.AddToClassList("invisible");
        UpdateButton();
    }

    private void Error(string message) {
        Debug.Log("Error: " + message);

        SetStage(CustomsStage.Error);
        button.SetEnabled(false);
        button.text = message;
        UIKit.Shake(button);

        CancelInvoke(nameof(Cancel));
        Invoke(nameof(Cancel), 2.5f);
    }

    private void StartGame() {
        Debug.Log("Starting game");

        SetStage(CustomsStage.Found);
        statusText = "MATCH FOUND";

        PhotonNetwork.CurrentRoom.IsVisible = false;
        // the game reads the rules from the room
        PlayerPrefs.SetString(Constants.PLAYLIST_PREF_KEY, Playlists.Custom);

        Invoke(nameof(LoadLevel), 2f);
    }

    private void LoadLevel() {
        if (PhotonNetwork.IsMasterClient) {
            PhotonNetwork.LoadLevel("6 Game");
        }
    }

    public void InputToClipboard() {
        GUIUtility.systemCopyBuffer = code;
        copyIcon.ShowCopied();
    }

    public static string CreateCode() {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        var random = new System.Random();

        return new string(Enumerable.Repeat(chars, 4)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
