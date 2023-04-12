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
public enum GameMode {
    AdvancedCombat,
    ClassicBattleship,
    TacticalSalvo,
    Customs
}

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

    private bool cancelAction = false;
    
    private MatchmakingStage _stage = MatchmakingStage.None;
    public MatchmakingStage stage {
        get {
            return _stage;
        }
        set {
            _stage = value;
            matchmakingPanel.SetActive(_stage != MatchmakingStage.None);
        }
    }

    public bool hideOnAwake = true;

    void Awake() {
        if (hideOnAwake) {
            stage = MatchmakingStage.None;
            cancelAction = false;
        }
    }

    public void PlayOrCancel() {
        if (cancelAction) {
            Debug.Log("Canceling in progress");
            return;
        }

        if (PhotonNetwork.InRoom) {
            Debug.Log("Already in a room");
            return;
        }

        if (stage == MatchmakingStage.None) {
            stage = MatchmakingStage.Pre;
            buttonText.text = "Cancel";
            StartCoroutine(Countdown());
        }
        else {
            cancelAction = true;
            buttonText.text = "Canceling";
            matchmakingText = "CANCELING...";
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

                gameMode = GetComponent<StateManager>().currentState.gameMode;

                JoinRandomRoom();
            }
        }
    }

    private void JoinRandomRoom() {
        if (!PhotonNetwork.IsConnected) {
            Error("Connection error");
            return;
        }

        if (cancelAction) {
            Cancel();
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
                
                Cancel();
            }
            else {
                Debug.Log("Waiting for other player");

                PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);
            }
        } else {
            stage = MatchmakingStage.Found;

            matchmakingText = "Match found!";

            Invoke("StartGame", 2f);
        }
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer) {
        Debug.Log("Player joined room");

        if (PhotonNetwork.CurrentRoom.PlayerCount == Constants.ROOM_NUM_EXPECTED_PLAYERS) {
            stage = MatchmakingStage.Found;

            matchmakingText = "Match found!";
            Invoke("StartGame", 2f);
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

    public override void OnCreateRoomFailed(short returnCode, string message) {
        Debug.Log("Failed to create room: " + message);

        Error("Matchmaking error: 2", 2.5f);
    }

    private void Cancel() {
        cancelAction = false;
        stage = MatchmakingStage.None;

        buttonText.text = "PLAY";

        if (stage == MatchmakingStage.Error) {
            buttonText.GetComponentInParent<Button>().interactable = true;
        }
    }

    private void Error(string message, float lifetime = 0) {
        buttonText.GetComponentInParent<Button>().interactable = false;

        stage = MatchmakingStage.Error;
        matchmakingText = message;

        if (lifetime > 0) {
            Invoke("Cancel", lifetime);
        }
    }

    private void StartGame() {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.QuickResends = 3;
        PhotonNetwork.MaxResendsBeforeDisconnect = 7;
        PhotonNetwork.CurrentRoom.IsVisible = false;
        Debug.Log("Starting game");
        if (PhotonNetwork.IsMasterClient) {
            PhotonNetwork.LoadLevel("6 Game");
        }
    }
}
