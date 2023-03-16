using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Text;

public class Launcher : MonoBehaviourPunCallbacks
{
    #region Private Serializable Fields
    [SerializeField]
    private byte maxPlayersPerRoom = 4;

    [SerializeField]
    private GameObject controlPanel;

    [SerializeField]
    private GameObject progressLabel;

    [SerializeField]
    private TMP_InputField usernameInput;

    [SerializeField]
    private TMP_InputField passwordInput;

    [SerializeField]
    private TMP_Text errorText;
    #endregion

    #region Private Fields
    bool isConnecting;
    bool isLoggingIn;
    string errorMessage {
        get {
            return errorText.text;
        }
        set {
            errorText.text = value;
        }
    }
    #endregion

    #region Private Constants
    const string playerNamePrefKey = "PlayerName";
    const string tokenPrefKey = "Token";
    #endregion

    #region MonoBehaviour CallBacks
    void Awake() {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    void Start() {
        if (PlayerPrefs.HasKey(tokenPrefKey) && PlayerPrefs.HasKey(playerNamePrefKey)) {
            Debug.Log("Token and username found, attempting login");
            progressLabel.SetActive(true);
            controlPanel.SetActive(false);

            Login(PlayerPrefs.GetString(playerNamePrefKey), PlayerPrefs.GetString(tokenPrefKey));
        }
        else {
            progressLabel.SetActive(false);
            controlPanel.SetActive(true);
        }
    }
    #endregion

    #region Public Methods
    public void Connect() {
        Connect(usernameInput.text, passwordInput.text);
    }
    #endregion

    #region MonoBehaviourPunCallbacks Callbacks
    public override void OnConnectedToMaster() {
        Debug.Log("PUN Basics Tutorial/Launcher: OnConnectedToMaster() was called by PUN");

        errorMessage = string.Empty;

        if (isConnecting || isLoggingIn) {
            isConnecting = false;
            isLoggingIn = false;
            PhotonNetwork.JoinRandomRoom();
        }
    }
    
    public override void OnCustomAuthenticationFailed(string debugMessage)
    {
        Debug.Log("Custom Auth Error:" + debugMessage);

        progressLabel.SetActive(false);
        controlPanel.SetActive(true);

        isConnecting = false;
        isLoggingIn = false;
        
        PlayerPrefs.DeleteKey(tokenPrefKey);

        if (debugMessage.Contains("Version mismatch")) {
            errorMessage = "Outdated client version, please update.";
        }
        else if (debugMessage.Contains("username", System.StringComparison.OrdinalIgnoreCase) ||
                 debugMessage.Contains("password", System.StringComparison.OrdinalIgnoreCase)) {
            errorMessage = "Invalid username or password.";
        }
        else if (debugMessage.Contains("Token invalid or expired")) {
            errorMessage = "Please log in again.";
        }
        else {
            errorMessage = "Unknown error, try again.";
        }
    }

    public override void OnDisconnected(DisconnectCause cause) {
        progressLabel.SetActive(false);
        controlPanel.SetActive(true);

        Debug.LogWarningFormat("PUN Basics Tutorial/Launcher: OnDisconnected() was called by PUN with reason {0}", cause);

        isConnecting = false;
        isLoggingIn = false;
    }

    public override void OnJoinRandomFailed(short returnCode, string message) {
        Debug.Log("PUN Basics Tutorial/Launcher:OnJoinRandomFailed() was called by PUN. No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom");

        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayersPerRoom });
    }

    public override void OnJoinedRoom() {
        Debug.Log("PUN Basics Tutorial/Launcher: OnJoinedRoom() called by PUN. Now this client is in a room.");

        if (PhotonNetwork.CurrentRoom.PlayerCount == 1) {
            Debug.Log("We load the Waiting Room");
            
            PhotonNetwork.LoadLevel("Waiting Room");
        }
    }

    public override void OnCustomAuthenticationResponse(Dictionary<string, object> data)
    {
        Debug.Log("PUN : Response from custom authentication server received.");

        if (data.ContainsKey("Token")) {
            string token = (string)data["Token"];
            Debug.Log("Token loaded: " + token);
            PlayerPrefs.SetString(tokenPrefKey, token);
        }
        else {
            Debug.Log("No token");
        }
    }
    #endregion

    #region Private Methods
    private void Connect(string userId, string pass) {
        progressLabel.SetActive(true);
        controlPanel.SetActive(false);

        if (PhotonNetwork.IsConnected) {
            PhotonNetwork.JoinRandomRoom();
        }
        else {
            // make sure the username is between 4 and 16 characters
            if (userId.Length < 4 || userId.Length > 16) {
                errorMessage = "Username must be between 4 and 16 characters.";
                progressLabel.SetActive(false);
                controlPanel.SetActive(true);
                return;
            }

            // make sure password is longer than 6 characters
            if (pass.Length < 6) {
                errorMessage = "Password should be at least 6 characters.";
                progressLabel.SetActive(false);
                controlPanel.SetActive(true);
                return;
            }

            // make sure username and passord are alphanumeric and underscore only
            if (!System.Text.RegularExpressions.Regex.IsMatch(userId, "^[a-zA-Z0-9_]*$") ||
                !System.Text.RegularExpressions.Regex.IsMatch(pass, "^[a-zA-Z0-9_]*$")) {
                errorMessage = "Invalid characters.";
                progressLabel.SetActive(false);
                controlPanel.SetActive(true);
                return;
            }

            string version = Application.version;

            AuthenticationValues authValues = new AuthenticationValues();
            authValues.AuthType = CustomAuthenticationType.Custom;
            authValues.AddAuthParameter("user", userId);
            authValues.AddAuthParameter("pass", Encode(pass));
            authValues.AddAuthParameter("version", version);
            
            PhotonNetwork.AuthValues = authValues;
            PhotonNetwork.GameVersion = version;
            
            isConnecting = PhotonNetwork.ConnectUsingSettings();
        }
    }

    private void Login(string userId, string token) {
        progressLabel.SetActive(true);
        controlPanel.SetActive(false);

        if (PhotonNetwork.IsConnected) {
            PhotonNetwork.JoinRandomRoom();
        }
        else {
            string version = Application.version;

            AuthenticationValues authValues = new AuthenticationValues();
            authValues.AuthType = CustomAuthenticationType.Custom;
            authValues.AddAuthParameter("user", userId);
            authValues.AddAuthParameter("token", token);
            authValues.AddAuthParameter("version", version);
            
            PhotonNetwork.AuthValues = authValues;
            PhotonNetwork.GameVersion = version;
            
            isLoggingIn = PhotonNetwork.ConnectUsingSettings();
        }
    }

    private string Encode(string str) {
        var crypt = new System.Security.Cryptography.SHA256Managed();
        var hash = new System.Text.StringBuilder();
        byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(str));
        foreach (byte theByte in crypto)
        {
            hash.Append(theByte.ToString("x2"));
        }
        return hash.ToString();
    }

    private bool CheckHash(string str, string hash) {
        return Encode(str) == hash;
    }
    #endregion
}
