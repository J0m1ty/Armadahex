using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using Photon.Pun;
using MyBox;
using System.Linq;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System.Text.RegularExpressions;

public class CustomUIManager : MonoBehaviourPunCallbacks {
    [SerializeField] private UIDocument document;

    // presets ui
    private DropdownField missionsDropdown;

    // match options
    private Toggle allowMatchChat;
    private Toggle isUnlimitedTime;
    private Label turnTimeLimitText;
    private SliderInt turnTimeLimitSlider;

    // mission options
    private Toggle isAdvancedAttacks;
    private Toggle isUnlimitedAdvancedAmmo;
    private Toggle isSalvo;
    private Toggle isBonus;
    
    // code sharing
    private TextField codeInputField;
    // the code, its label and the copy button show together
    private VisualElement codeContainer;
    private string code {
        get {
            return codeInputField.value;
        }
        set {
            codeInputField.value = value;
        }
    }
    private Button startButton;
    private string buttonText {
        get {
            return startButton.text;
        }
        set {
            startButton.text = value;
        }
    }

    // other
    private VisualElement connectingOverlay;
    private Label matchmakingPanel;
    private string matchmakingText {
        get {
            return matchmakingPanel.text;
        }
        set {
            matchmakingPanel.text = value;
        }
    }
    private Color panelColor {
        get {
            return matchmakingPanel.resolvedStyle.color;
        }
        set {
            matchmakingPanel.style.color = value;
        }
    }
    [SerializeField] private Color matchmakingColor;
    [SerializeField] private Color errorColor;

    [Serializable]
    public enum CustomsStage {
        Error,
        Customizing,
        Pre,
        Matchmaking,
        Found,
        Loading
    }

    [Serializable]
    public enum DependUpon {
        Stages,
        Toggle,
        Both
    }

    [Serializable]
    public class DefaultValues {
        public GameMode gameMode;

        [Header("Match Options")]
        public bool allowMatchChat;
        public bool isUnlimitedTime;
        [ConditionalField(nameof(isUnlimitedTime), true, true)]
        [MinValue(25)] [MaxValue(90)]
        public float turnTimeLimit;

        [Header("Mission Options")]
        public bool isAdvancedAttacks;
        [ConditionalField(nameof(isAdvancedAttacks), false, true)]
        public bool isUnlimitedAdvancedAmmo;
        public bool isSalvo;
        public bool isBonus;

        [Header("Overrides")]
        public bool overrideAllowMatchChat;
        public bool overrideValues;
    }

    [Serializable]
    public class VisabilityElement {
        public string name;
        // the element's name in the document
        public string element;
        public DependUpon dependUpon;
        [ConditionalField(nameof(dependUpon), false, DependUpon.Stages, DependUpon.Both)]
        public CollectionWrapper<CustomsStage> visableStages;
        [ConditionalField(nameof(dependUpon), false, DependUpon.Toggle, DependUpon.Both)]
        public string toggle;
        [ConditionalField(nameof(dependUpon), false, DependUpon.Toggle, DependUpon.Both)]
        public bool invert;
    }

    [SerializeField] private CustomsStage stage;

    [SerializeField] private GameMode gameMode;

    [SerializeField] private VisabilityElement[] visabilityElements;

    [SerializeField] private List<DefaultValues> defaultValues;

    public void DisableButtons(bool disable) {
        try {
            missionsDropdown.SetEnabled(!disable);
            allowMatchChat.SetEnabled(!disable);
            isUnlimitedTime.SetEnabled(!disable);
            turnTimeLimitSlider.SetEnabled(!disable);
            isAdvancedAttacks.SetEnabled(!disable);
            isUnlimitedAdvancedAmmo.SetEnabled(!disable);
            isSalvo.SetEnabled(!disable);
            isBonus.SetEnabled(!disable);
        }
        catch (Exception) {}
    }
    
    public void UpdateVisability() {
        foreach (VisabilityElement element in visabilityElements) {
            bool visable = false;
            if (element.dependUpon == DependUpon.Stages || element.dependUpon == DependUpon.Both) {
                visable = element.visableStages.Value.Contains(stage);
            }

            if (element.dependUpon == DependUpon.Toggle || element.dependUpon == DependUpon.Both) {
                visable = document.rootVisualElement.Q<Toggle>(element.toggle).value ^ element.invert;
            }

            UIKit.Show(document.rootVisualElement.Q(element.element), visable);
        }

        turnTimeLimitText.text = $"{turnTimeLimitSlider.value}S PER TURN";
    }

    public void SetDefaults(DefaultValues defaults) {
        if (defaults.overrideValues) {
            gameMode = GameMode.Customs; // set to customs so that it doesn't change back to customs after
            missionsDropdown.index = (int) defaults.gameMode;
            if (defaults.overrideAllowMatchChat) {
                allowMatchChat.value = defaults.allowMatchChat;
            }
            isUnlimitedTime.value = defaults.isUnlimitedTime;
            turnTimeLimitSlider.value = (int)defaults.turnTimeLimit;
            isAdvancedAttacks.value = defaults.isAdvancedAttacks;
            isUnlimitedAdvancedAmmo.value = defaults.isUnlimitedAdvancedAmmo;
            isSalvo.value = defaults.isSalvo;
            isBonus.value = defaults.isBonus;
            // now set game mode
            gameMode = defaults.gameMode;
        }

        UpdateVisability();
    }

    public void SetDefaults(GameMode gameMode) {
        SetDefaults(defaultValues.Find(d => d.gameMode == gameMode));
    }

    public void SetDropdown() {
        SetDefaults((GameMode) missionsDropdown.index);
    }

    public static string PrettyEnum(string enumString) {
        // add a space before each capital letter not including the first
        return Regex.Replace(enumString, "([A-Z])", " $1").TrimStart(' ');
    }

    public void LoadDropdown(GameMode defaultMode = GameMode.AdvancedCombat) {
        missionsDropdown.choices = Enum.GetNames(typeof(GameMode)).ToList().ConvertAll(PrettyEnum);

        missionsDropdown.index = (int) defaultMode;

        SetDefaults(defaultMode);
    }

    public void SetStage(CustomsStage stage) {
        this.stage = stage;

        DisableButtons(this.stage != CustomsStage.Customizing || this.stage == CustomsStage.Error);

        UpdateVisability();
    }

    public void UpdateValue() {
        UpdateVisability();

        // if not in customs and any value changes, set to customs
        if (gameMode != GameMode.Customs) {
            var defaultValues = this.defaultValues.Find(d => d.gameMode == gameMode);

            if ((defaultValues.overrideAllowMatchChat && allowMatchChat.value != defaultValues.allowMatchChat) ||
                isUnlimitedTime.value != defaultValues.isUnlimitedTime ||
                turnTimeLimitSlider.value != defaultValues.turnTimeLimit ||
                isAdvancedAttacks.value != defaultValues.isAdvancedAttacks ||
                isUnlimitedAdvancedAmmo.value != defaultValues.isUnlimitedAdvancedAmmo ||
                isSalvo.value != defaultValues.isSalvo ||
                isBonus.value != defaultValues.isBonus) {
                    
                LoadDropdown(GameMode.Customs);

                gameMode = GameMode.Customs;
            }
        }
    }

    void Awake() {
        var root = document.rootVisualElement;
        missionsDropdown = root.Q<DropdownField>("presets");
        allowMatchChat = root.Q<Toggle>("allowChat");
        isUnlimitedTime = root.Q<Toggle>("unlimitedTime");
        turnTimeLimitText = root.Q<Label>("turnTimeText");
        turnTimeLimitSlider = root.Q<SliderInt>("turnTime");
        isAdvancedAttacks = root.Q<Toggle>("advancedAttacks");
        isUnlimitedAdvancedAmmo = root.Q<Toggle>("unlimitedAmmo");
        isSalvo = root.Q<Toggle>("salvo");
        isBonus = root.Q<Toggle>("bonus");
        codeInputField = root.Q<TextField>("code");
        codeContainer = root.Q("codeContainer");
        startButton = root.Q<Button>("create");
        connectingOverlay = root.Q("connectingOverlay");
        matchmakingPanel = root.Q<Label>("matchmaking");

        // any rule change, and the preset
        foreach (var toggle in new[] { allowMatchChat, isUnlimitedTime, isAdvancedAttacks, isUnlimitedAdvancedAmmo, isSalvo, isBonus }) {
            toggle.RegisterValueChangedCallback(_ => UpdateValue());
        }
        turnTimeLimitSlider.RegisterValueChangedCallback(_ => UpdateValue());
        missionsDropdown.RegisterValueChangedCallback(_ => SetDropdown());

        UIKit.Show(connectingOverlay,PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient);
    }

    void Start() {
        LoadDropdown();

        code = "";
        UIKit.Show(codeContainer, false);
    }

    private bool cancelAction;

    public void StartOrCancelMatch() {
        if (stage == CustomsStage.Error) {
            Debug.Log("In error");
            return;
        }

        if (cancelAction) {
            Debug.Log("Canceling in progress");
            return;
        }

        panelColor = matchmakingColor;

        if (stage == CustomsStage.Customizing) {
            SetStage(CustomsStage.Pre);
            buttonText = "CANCEL";
            StartCoroutine(PreCountdown());
        } else if (stage != CustomsStage.Found) {
            if (PhotonNetwork.InRoom) {
                PhotonNetwork.LeaveRoom();
            } else {
                cancelAction = true;
                buttonText = "CANCELING";
                matchmakingText = "CANCELING...";
            }
        }
    }

    IEnumerator PreCountdown(int count = 3) {
        while (count > 0) {
            if (cancelAction) {
                Cancel();
                yield break;
            }

            matchmakingText = $"{count}";

            yield return new WaitForSeconds(1);
            count--;

            if (count == 0) {
                SetStage(CustomsStage.Matchmaking);
                
                buttonText = "LEAVE ROOM";
                matchmakingText = "WAITING FOR ANOTHER PLAYER...";
                
                CreateCustomRoom();
            }
        }
    }

    public void CreateCustomRoom() {
        if (!PhotonNetwork.IsConnected) {
            Error("Connection error", 2.5f);
            return;
        }

        if (cancelAction) {
            Cancel();
            return;
        }

        RoomOptions roomOptions = new RoomOptions();
        
        roomOptions.CustomRoomProperties = new Hashtable() {
            { Constants.GAME_MODE_PROP_KEY, gameMode.ToString() },
            { "AllowChat", allowMatchChat.value },
            { "TurnTimeLimit", isUnlimitedTime.value ? 0f : (float)turnTimeLimitSlider.value },
            { "IsAdvancedCombat", isAdvancedAttacks.value },
            { "IsUnlimitedAmmo", isUnlimitedAdvancedAmmo.value },
            { "IsSalvo", isSalvo.value },
            { "IsBonus", isBonus.value }
        };

        roomOptions.MaxPlayers = Constants.ROOM_NUM_EXPECTED_PLAYERS;
        
        roomOptions.IsVisible = false;

        TypedLobby typedLobby = new TypedLobby(GameMode.Customs.ToString(), LobbyType.Default);

        code = CreateCode();

        UIKit.Show(codeContainer, true);

        PhotonNetwork.CreateRoom(code, roomOptions, typedLobby);
    }

    public override void OnCreateRoomFailed(short returnCode, string message) {
        Debug.Log("Failed to create room: " + message);

        Error("Matchmaking error: 2", 2.5f);
    }

    public override void OnJoinedRoom() {
        Debug.Log("Joined room");

        if (PhotonNetwork.CurrentRoom.PlayerCount != Constants.ROOM_NUM_EXPECTED_PLAYERS) {
            Debug.Log("Waiting for other player");

            PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);
            return;
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("Player entered room");

        if (PhotonNetwork.CurrentRoom.PlayerCount == Constants.ROOM_NUM_EXPECTED_PLAYERS) {
            StartGame();
        }
    }

    public override void OnLeftRoom() {
        Debug.Log("Left room");

        Cancel();
    }


    public void Cancel() {
        if (stage == CustomsStage.Error) {
            startButton.SetEnabled(true);
        }

        cancelAction = false;
        SetStage(CustomsStage.Customizing);

        buttonText = "CREATE";

        panelColor = matchmakingColor;

        UIKit.Show(codeContainer, false);
    }

    public void Error(string message, float lifetime = 0) {
        Debug.Log("Error: " + message);

        if (lifetime > 0) {
            Invoke("Cancel", lifetime);
        }

        buttonText = "ERROR";

        startButton.SetEnabled(false);
        
        SetStage(CustomsStage.Error);
        matchmakingText = message;

        panelColor = errorColor;

        UIKit.Show(codeContainer, false);
    }

    public void StartGame() {
        Debug.Log("Starting game");

        SetStage(CustomsStage.Found);
            
        matchmakingText = "Match found!";
        
        PhotonNetwork.CurrentRoom.IsVisible = false;
        PlayerPrefs.SetInt(Constants.GAME_MODE_PREF_KEY, (int)GameMode.Customs); // will be reset when game networking takes over, but is used to associate with private multiplayer game

        Invoke("LoadLevel", 2f);
    }

    private void LoadLevel() {
        if (PhotonNetwork.IsMasterClient) {
            PhotonNetwork.LoadLevel("6 Game");
        }
    }

    public void InputToClipboard() {
        GUIUtility.systemCopyBuffer = codeInputField.value;
    }

    public static string CreateCode() {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        var random = new System.Random();

        return new string(Enumerable.Repeat(chars, 4)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
