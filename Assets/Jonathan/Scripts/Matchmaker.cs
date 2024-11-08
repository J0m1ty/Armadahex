using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[Serializable]
public enum MatchmakingStage {
    None,
    Error,
    Pre,
    Searching,
    Found,
    Loading
}

[RequireComponent(typeof(StateManager))]
public class Matchmaker : MonoBehaviourPunCallbacks
{
    private StateManager stateManager;

    private GameMode gameMode;

    [SerializeField]
    private TMP_Text buttonText;

    [SerializeField]
    private GameObject matchmakingPanel;
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

    [SerializeField]
    private GameObject backButton; // hide this when searching    

    [SerializeField]
    private Color matchmakingColor;
    [SerializeField]
    private Color errorColor;
    
    private MatchmakingStage _stage = MatchmakingStage.None;
    public MatchmakingStage stage {
        get {
            return _stage;
        }
        set {
            _stage = value;
            matchmakingPanel.SetActive(_stage != MatchmakingStage.None);
            backButton.SetActive(_stage == MatchmakingStage.None);
            stateManager.lockedIn = _stage != MatchmakingStage.None;
        }
    }

    private bool cancelAction = false;

    [SerializeField]
    private bool hideOnAwake = true;

    void Awake() {
        stateManager = GetComponent<StateManager>();

        matchmakingMaterial = matchmakingPanel.GetComponent<Image>().material;

        if (hideOnAwake) {
            stage = MatchmakingStage.None;
            cancelAction = false;
        }
    }

    public void PlayOrCancel() {
        if (stage == MatchmakingStage.Error) {
            Debug.Log("In error");
            return;
        }

        if (cancelAction) {
            Debug.Log("Canceling in progress");
            return;
        }
        
        panelColor = matchmakingColor;

        if (stage == MatchmakingStage.None) {
            gameMode = stateManager.currentState.gameMode;

            if (gameMode == GameMode.Customs) {
                string joinCode = stateManager.stateInput.text.ToUpper();

                if (!ValidateCode(joinCode)) {
                    Error("Invalid code", 2.5f);
                    return;
                }
            }

            stage = MatchmakingStage.Pre;
            buttonText.text = "CANCEL";
            StartCoroutine(Countdown());
        }
        else if (stage != MatchmakingStage.Found) {
            if (PhotonNetwork.InRoom) {
                PhotonNetwork.LeaveRoom();
            }
            else {
                cancelAction = true;
                buttonText.text = "Canceling";
                matchmakingText = "CANCELING...";
            }
        }
    }

    IEnumerator Countdown() {
        var count = 5;
        while (count > 0) {
            if (cancelAction) {
                Cancel();
                yield break;
            }

            matchmakingText = count.ToString();

            yield return new WaitForSeconds(1);
            count--;

            if (count == 0) {
                stage = MatchmakingStage.Searching;
                
                matchmakingText = "SEARCHING...";
                buttonText.text = "CANCEL SEARCH";

                gameMode = stateManager.currentState.gameMode;

                JoinRandomRoom();
            }
        }
    }

    private void JoinRandomRoom() {
        if (!PhotonNetwork.IsConnected) {
            Error("Connection error", 2.5f);
            return;
        }

        if (cancelAction) {
            Cancel();
            return;
        }
        
        if (gameMode == GameMode.Customs) {
            string joinCode = stateManager.stateInput.text.ToUpper();

            if (!ValidateCode(joinCode)) {
                Error("Invalid code", 2.5f);
                return;
            }
            
            Debug.Log("Looking to join custom room " + joinCode);

            PhotonNetwork.JoinRoom(joinCode);
            return;
        }

        Debug.Log("Looking to join " + gameMode);

        TypedLobby typedLobby = new TypedLobby(gameMode.ToString(), LobbyType.Default);
        
        Hashtable expectedCustomRoomProperties = new Hashtable() {
            { Constants.GAME_MODE_PROP_KEY, gameMode.ToString() }
        };
        PhotonNetwork.JoinRandomRoom(expectedCustomRoomProperties, Constants.ROOM_NUM_EXPECTED_PLAYERS, MatchmakingMode.FillRoom, typedLobby, null, null);
    }

    public override void OnJoinedRoom() {
        Debug.Log("Joined room");
        
        if (PhotonNetwork.CurrentRoom.PlayerCount != Constants.ROOM_NUM_EXPECTED_PLAYERS) {
            if (cancelAction) {
                PhotonNetwork.LeaveRoom();
            }
            else if (gameMode == GameMode.Customs) {
                Error("Invalid code", 2.5f);

                PhotonNetwork.LeaveRoom();
            }
            else {
                Debug.Log("Waiting for other player");

                PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);
            }
        } else {
            StartGame();
        }
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer) {
        Debug.Log("Player joined room");

        if (PhotonNetwork.CurrentRoom.PlayerCount == Constants.ROOM_NUM_EXPECTED_PLAYERS) {
            StartGame();
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message) {
        if (message != "No match found") {
            Error("Matchmaking error: 1", 2.5f);
            return;
        }

        RoomOptions roomOptions = new RoomOptions();
        
        roomOptions.CustomRoomPropertiesForLobby = new string[] { Constants.GAME_MODE_PROP_KEY };
        roomOptions.CustomRoomProperties = new Hashtable() {
            { Constants.GAME_MODE_PROP_KEY, gameMode.ToString() }
        };
        roomOptions.MaxPlayers = Constants.ROOM_NUM_EXPECTED_PLAYERS;

        TypedLobby typedLobby = new TypedLobby(gameMode.ToString(), LobbyType.Default);

        PhotonNetwork.CreateRoom(null, roomOptions, typedLobby);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("Failed to join room: " + message);

        if (gameMode == GameMode.Customs) {
            Error("Invalid code", 2.5f);
            return;
        }
    }

    public override void OnLeftRoom() {
        Debug.Log("Left room");

        Cancel();
    }

    public override void OnCreateRoomFailed(short returnCode, string message) {
        Debug.Log("Failed to create room: " + message);

        Error("Matchmaking error: 2", 2.5f);
    }

    private void Cancel() {
        if (stage == MatchmakingStage.Error) {
            buttonText.GetComponentInParent<Button>().interactable = true;
        }

        cancelAction = false;
        stage = MatchmakingStage.None;
        
        stateManager.stateInput.interactable = true;
        buttonText.GetComponentInParent<Button>().interactable = true;
        buttonText.text = stateManager.currentState.buttonText;

        panelColor = matchmakingColor;

        stateManager.lockedIn = false;
    }

    private void Error(string message, float lifetime = 0) {
        panelColor = errorColor;

        stateManager.stateInput.interactable = false;
        buttonText.GetComponentInParent<Button>().interactable = false;

        stage = MatchmakingStage.Error;
        matchmakingText = message;

        if (lifetime > 0) {
            Invoke("Cancel", lifetime);
        }
    }

    private void StartGame() {
        Debug.Log("Starting game");

        stage = MatchmakingStage.Found;

        PhotonNetwork.CurrentRoom.IsVisible = false;
        PlayerPrefs.SetInt(Constants.GAME_MODE_PREF_KEY, (int)gameMode);

        matchmakingText = "Match found!";

        Invoke("LoadLevel", 2f);
    }

    private void LoadLevel() {
        if (PhotonNetwork.IsMasterClient) {
            PhotonNetwork.LoadLevel("6 Game");
        }
    }

    private bool ValidateCode(string code) {
        if (code.Length != 4) {
            return false;
        }

        foreach (char c in code) {
            if (!char.IsLetterOrDigit(c)) {
                return false;
            }
        }

        return true;
    }
}
