using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Text;

[RequireComponent(typeof(NoticeManager))]
[RequireComponent(typeof(FormManager))]
public class ConnectionManager : MonoBehaviourPunCallbacks
{
    #region Private Fields
    bool isQuickConnecting;
    bool isConnecting;
    NoticeManager noticeManager;
    FormManager formManager;
    #endregion

    #region MonoBehaviour CallBacks
    void Awake() {
        noticeManager = GetComponent<NoticeManager>();
        noticeManager.SetState(NoticeState.Default);

        formManager = GetComponent<FormManager>();
        formManager.isEnabled = true;

        PhotonNetwork.AutomaticallySyncScene = true;
    }
    #endregion

    #region MonoBehaviourPunCallbacks Callbacks
    public override void OnConnectedToMaster() {
        Debug.Log("Connected to master");

        if (isQuickConnecting || isConnecting) {
            isQuickConnecting = false;
            isConnecting = false;

            Debug.Log("Loading main menu");

            PhotonNetwork.LoadLevel("2 Menu");
        }
    }

    public override void OnDisconnected(DisconnectCause cause) {
        Debug.Log("Disconnected from server: " + cause);

        // only a login in progress cares; logging out also lands here, a moment after this scene loads
        bool wasConnecting = isQuickConnecting || isConnecting;
        isQuickConnecting = false;
        isConnecting = false;
        if (!wasConnecting) return;

        // a refused login disconnects too, and its own message is the useful one
        if (cause != DisconnectCause.CustomAuthenticationFailed) {
            noticeManager.SetError("Couldn't reach the server. Try again.");
        }
        else if (noticeManager.state == NoticeState.Connecting) {
            noticeManager.SetError("Login failed. Try again.");
        }
    }

    public override void OnCustomAuthenticationResponse(Dictionary<string, object> data)
    {
        Debug.Log("Response from custom authentication server received");

        if (data.ContainsKey("Token")) {
            string token = (string)data["Token"];
            Debug.Log("Token loaded: " + token);
            PlayerPrefs.SetString(Constants.TOKEN_PREF_KEY, token);
        }
        else {
            Debug.Log("No token");
        }

        if (data.ContainsKey("UserID")) {
            string username = (string)data["UserID"];
            Debug.Log("Username loaded: " + username);
            
            if (PlayerPrefs.HasKey(Constants.PLAYER_NAME_PREF_KEY) && PlayerPrefs.GetString(Constants.PLAYER_NAME_PREF_KEY) != username) {
                PlayerPrefs.DeleteKey(Constants.NICK_NAME_PREF_KEY);
            }

            PlayerPrefs.SetString(Constants.PLAYER_NAME_PREF_KEY, username);
            
            if (!PlayerPrefs.HasKey(Constants.NICK_NAME_PREF_KEY)) {
                PhotonNetwork.NickName = username;

                PlayerPrefs.SetString(Constants.NICK_NAME_PREF_KEY, username);
            }
            else {
                PhotonNetwork.NickName = PlayerPrefs.GetString(Constants.NICK_NAME_PREF_KEY);
            }
        }
        else {
            Debug.Log("No username");
        }
    }

    public override void OnCustomAuthenticationFailed(string debugMessage)
    {
        Debug.Log("Custom auth error: " + debugMessage);

        if (isQuickConnecting) {
            isQuickConnecting = false;

            PlayerPrefs.DeleteKey(Constants.TOKEN_PREF_KEY);

            if (debugMessage.Contains("Version mismatch")) {
                Debug.Log("Outdated version error");
                noticeManager.SetError("This version is out of date. Update to play.", true);
            }
            else {
                Debug.Log("Other error, redirecting to login");
                PhotonNetwork.LoadLevel("1 Account");
            }
        }
        else if (isConnecting) {
            isConnecting = false;

            if (debugMessage.Contains("Version mismatch")) {
                noticeManager.SetError("This version is out of date. Update to play.", true);
            }
            else if (debugMessage.Contains("username", System.StringComparison.OrdinalIgnoreCase) ||
                    debugMessage.Contains("password", System.StringComparison.OrdinalIgnoreCase)) {
                // a new name is registered on its first login, so a refusal means the name is someone else's or the password is wrong
                noticeManager.SetError(formManager.signingUp ? "That username is taken." : "Wrong username or password.");
                formManager.Incorrect();
            }
            else if (debugMessage.Contains("Token invalid or expired")) {
                noticeManager.SetError("Your session expired. Log in again.");

                PlayerPrefs.DeleteKey(Constants.TOKEN_PREF_KEY);
            }
            else {
                noticeManager.SetError("Something went wrong. Try again.");
            }
        }
    }
    #endregion

    #region Public Methods
    public void Play() {
        if (isQuickConnecting || noticeManager.fatal) {
            return;
        }

        noticeManager.SetState(NoticeState.Connecting);

        if (PlayerPrefs.HasKey(Constants.TOKEN_PREF_KEY) && PlayerPrefs.HasKey(Constants.PLAYER_NAME_PREF_KEY)) {
            Debug.Log("Token and username found, attempting to quick connect");

            QuickConnect(PlayerPrefs.GetString(Constants.PLAYER_NAME_PREF_KEY), PlayerPrefs.GetString(Constants.TOKEN_PREF_KEY));
        }
        else {
            Debug.Log("Token and username not found, redirecting to quick connect");

            PhotonNetwork.LoadLevel("1 Account");
        }
    }

    public void Login() {
        if (isConnecting || noticeManager.fatal) {
            return;
        }

        if (!formManager.Check(out var error)) {
            noticeManager.SetError(error);
            return;
        }

        noticeManager.SetState(NoticeState.Connecting);
        
        string username = formManager.username;
        string password = formManager.password;

        Login(username, password);
    }
    #endregion

    #region Private Methods
    private void QuickConnect(string userId, string token) {
        if (PhotonNetwork.IsConnected) {
            Debug.Log("Already connected, loading main menu");

            PhotonNetwork.LoadLevel("2 Menu");
        }
        else if (isQuickConnecting || isConnecting) {
            Debug.Log("Already connecting");
        }
        else {
            string version = Application.version;

            AuthenticationValues authValues = new AuthenticationValues();
            authValues.AuthType = CustomAuthenticationType.Custom;
            Debug.Log("user: " + userId + ", token: " + token + ", version: " + version);
            authValues.AddAuthParameter("user", userId);
            authValues.AddAuthParameter("token", token);
            authValues.AddAuthParameter("version", version);
            
            PhotonNetwork.AuthValues = authValues;
            PhotonNetwork.GameVersion = version;
            
            isQuickConnecting = PhotonNetwork.ConnectUsingSettings();
        }
    }

    private void Login(string username, string password) {
        if (PhotonNetwork.IsConnected) {
            Debug.Log("Already connected, loading main menu");

            PhotonNetwork.LoadLevel("2 Menu");
        }
        else if (isQuickConnecting || isConnecting) {
            Debug.Log("Already connecting");
        }
        else {
            if (username.Length < 4 || username.Length > 16) {
                noticeManager.SetError("Usernames are 4 to 16 characters.");
                return;
            }

            if (password.Length < 6) {
                noticeManager.SetError("Passwords need at least 6 characters.");
                formManager.Incorrect();
                return;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(username, "^[a-zA-Z0-9_]*$") ||
                !System.Text.RegularExpressions.Regex.IsMatch(password, "^[a-zA-Z0-9_]*$")) {
                noticeManager.SetError("Use only letters, numbers and underscores.");
                formManager.Clear();
                return;
            }

            string version = Application.version;

            AuthenticationValues authValues = new AuthenticationValues();
            authValues.AuthType = CustomAuthenticationType.Custom;
            authValues.AddAuthParameter("user", username);
            authValues.AddAuthParameter("pass", Encode(password));
            authValues.AddAuthParameter("version", version);
            
            PhotonNetwork.AuthValues = authValues;
            PhotonNetwork.GameVersion = version;
            
            isConnecting = PhotonNetwork.ConnectUsingSettings();
        }
    }

    private string Encode(string str) {
        var crypt = new System.Security.Cryptography.SHA256Managed();
        var hash = new System.Text.StringBuilder();
        byte[] crypto = crypt.ComputeHash(System.Text.Encoding.UTF8.GetBytes(str));
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
