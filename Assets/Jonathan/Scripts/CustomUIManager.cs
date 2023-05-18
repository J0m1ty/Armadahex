using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using Photon.Pun;
using MyBox;
using System.Linq;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class CustomUIManager : MonoBehaviourPunCallbacks {
    [Header("Presets UI")]
    [SerializeField] private TMP_Dropdown missionsDropdown;

    [Header("Match options")]
    [SerializeField] private Toggle allowMatchChat;
    [SerializeField] private Toggle isUnlimitedTime;
    [SerializeField] private TMP_Text turnTimeLimitText;
    [SerializeField] private Slider turnTimeLimitSlider;

    [Header("Mission options")]
    [SerializeField] private Toggle isAdvancedAttacks;
    [SerializeField] private Toggle isUnlimitedAdvancedAmmo;
    [SerializeField] private Toggle isSalvo;
    [SerializeField] private Toggle isBonus;
    
    [Header("Code sharing")]
    [SerializeField] private TMP_InputField codeInputField;
    private string code {
        get {
            return codeInputField.text;
        }
        set {
            codeInputField.text = value;
        }
    }
    [SerializeField] private Button startButton;
    private string buttonText {
        get {
            return startButton.GetComponentInChildren<TMP_Text>().text;
        }
        set {
            startButton.GetComponentInChildren<TMP_Text>().text = value;
        }
    }

    [Header("Other")]
    [SerializeField] private GameObject connectingOverlay;
    [SerializeField] private GameObject matchmakingPanel;
    private string matchmakingText {
        get {
            return matchmakingPanel.GetComponentInChildren<TMP_Text>().text;
        }
        set {
            matchmakingPanel.GetComponentInChildren<TMP_Text>().text = value;
        }
    }
    private Material matchmakingMaterial {
        get {
            return matchmakingPanel.GetComponent<Image>().material;
        }
        set {
            matchmakingPanel.GetComponent<Image>().material = value;
        }
    }
    private Color panelColor {
        get {
            return matchmakingMaterial.GetColor("_AccentColor");
        }
        set {
            matchmakingMaterial.SetColor("_AccentColor", value);
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
        public GameObject gameObject;
        public DependUpon dependUpon;
        [ConditionalField(nameof(dependUpon), false, DependUpon.Stages, DependUpon.Both)]
        public CollectionWrapper<CustomsStage> visableStages;
        [ConditionalField(nameof(dependUpon), false, DependUpon.Toggle, DependUpon.Both)]
        public Toggle toggle;
        [ConditionalField(nameof(dependUpon), false, DependUpon.Toggle, DependUpon.Both)]
        public bool invert;
    }

    [SerializeField] private CustomsStage stage;

    [SerializeField] private GameMode gameMode;

    [SerializeField] private VisabilityElement[] visabilityElements;

    [SerializeField] private List<DefaultValues> defaultValues;

    public void DisableButtons(bool disable) {
        try {
            missionsDropdown.interactable = !disable;
            allowMatchChat.interactable = !disable;
            isUnlimitedTime.interactable = !disable;
            turnTimeLimitSlider.interactable = !disable;
            isAdvancedAttacks.interactable = !disable;
            isUnlimitedAdvancedAmmo.interactable = !disable;
            isSalvo.interactable = !disable;
            isBonus.interactable = !disable;
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
                visable = element.toggle.isOn ^ element.invert;
            }

            element.gameObject.SetActive(visable);
        }

        turnTimeLimitText.text = $"{turnTimeLimitSlider.value}s Limit";
    }

    public void SetDefaults(DefaultValues defaults) {
        if (defaults.overrideValues) {
            gameMode = GameMode.Customs; // set to customs so that it doesn't change back to customs after
            missionsDropdown.value = (int) defaults.gameMode;
            if (defaults.overrideAllowMatchChat) {
                allowMatchChat.isOn = defaults.allowMatchChat;
            }
            isUnlimitedTime.isOn = defaults.isUnlimitedTime;
            turnTimeLimitSlider.value = defaults.turnTimeLimit;
            isAdvancedAttacks.isOn = defaults.isAdvancedAttacks;
            isUnlimitedAdvancedAmmo.isOn = defaults.isUnlimitedAdvancedAmmo;
            isSalvo.isOn = defaults.isSalvo;
            isBonus.isOn = defaults.isBonus;
            // now set game mode
            gameMode = defaults.gameMode;
        }

        UpdateVisability();
    }

    public void SetDefaults(GameMode gameMode) {
        SetDefaults(defaultValues.Find(d => d.gameMode == gameMode));
    }

    public void SetDropdown() {
        SetDefaults((GameMode) missionsDropdown.value);
    }

    public void LoadDropdown(GameMode defaultMode = GameMode.AdvancedCombat) {
        missionsDropdown.ClearOptions();
        missionsDropdown.AddOptions(Enum.GetNames(typeof(GameMode)).ToList());

        missionsDropdown.value = (int) defaultMode;

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

            if ((defaultValues.overrideAllowMatchChat && allowMatchChat.isOn != defaultValues.allowMatchChat) ||
                isUnlimitedTime.isOn != defaultValues.isUnlimitedTime ||
                turnTimeLimitSlider.value != defaultValues.turnTimeLimit ||
                isAdvancedAttacks.isOn != defaultValues.isAdvancedAttacks ||
                isUnlimitedAdvancedAmmo.isOn != defaultValues.isUnlimitedAdvancedAmmo ||
                isSalvo.isOn != defaultValues.isSalvo ||
                isBonus.isOn != defaultValues.isBonus) {
                    
                LoadDropdown(GameMode.Customs);

                gameMode = GameMode.Customs;
            }
        }
    }

    void Awake() {
        connectingOverlay.SetActive(PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient);
    }

    void Start() {
        LoadDropdown();

        code = "";
        codeInputField.transform.parent.gameObject.SetActive(false);
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
            { "AllowChat", allowMatchChat.isOn },
            { "TurnTimeLimit", isUnlimitedTime.isOn ? 0 : turnTimeLimitSlider.value },
            { "IsAdvancedCombat", isAdvancedAttacks.isOn },
            { "IsUnlimitedAmmo", isUnlimitedAdvancedAmmo.isOn },
            { "IsSalvo", isSalvo.isOn },
            { "IsBonus", isBonus.isOn }
        };

        roomOptions.MaxPlayers = Constants.ROOM_NUM_EXPECTED_PLAYERS;
        
        roomOptions.IsVisible = false;

        TypedLobby typedLobby = new TypedLobby(GameMode.Customs.ToString(), LobbyType.Default);

        code = CreateCode();

        codeInputField.transform.parent.gameObject.SetActive(true);

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
            startButton.interactable = true;
        }

        cancelAction = false;
        SetStage(CustomsStage.Customizing);

        buttonText = "CREATE";

        panelColor = matchmakingColor;

        codeInputField.transform.parent.gameObject.SetActive(false);
    }

    public void Error(string message, float lifetime = 0) {
        Debug.Log("Error: " + message);

        if (lifetime > 0) {
            Invoke("Cancel", lifetime);
        }

        buttonText = "ERROR";

        startButton.interactable = false;
        
        SetStage(CustomsStage.Error);
        matchmakingText = message;

        panelColor = errorColor;

        codeInputField.transform.parent.gameObject.SetActive(false);
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
        GUIUtility.systemCopyBuffer = codeInputField.text;
    }

    public static string CreateCode() {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        var random = new System.Random();

        return new string(Enumerable.Repeat(chars, 4)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
