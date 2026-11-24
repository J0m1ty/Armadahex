using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UIElements;
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

    // the playlist id being searched for
    private string gameMode;

    [SerializeField]
    private UIDocument document;

    // the play button, its text follows the search
    private Button button;
    private TextElement buttonText => button;

    // the search lives in the play button: the countdown, the running time, the result or an error.
    // this is the text it shows while it's busy; hovering it during a search reads CANCEL
    private string matchmakingText;
    private bool hovering;
    private float searchStart;

    private VisualElement backButton; // hide this when searching

    private MatchmakingStage _stage = MatchmakingStage.None;
    public MatchmakingStage stage {
        get {
            return _stage;
        }
        set {
            _stage = value;
            UIKit.Show(backButton, _stage == MatchmakingStage.None);
            if (_stage == MatchmakingStage.Searching) searchStart = Time.time;
            stateManager.lockedIn = _stage != MatchmakingStage.None;
        }
    }

    private bool cancelAction = false;

    [SerializeField]
    private bool hideOnAwake = true;

    void Awake() {
        stateManager = GetComponent<StateManager>();

        var root = document.rootVisualElement;
        button = root.Q<Button>("join");
        backButton = root.Q("back");
        button.RegisterCallback<PointerEnterEvent>(_ => hovering = true);
        button.RegisterCallback<PointerLeaveEvent>(_ => hovering = false);

        if (hideOnAwake) {
            stage = MatchmakingStage.None;
            cancelAction = false;
        }
    }

    void Update() {
        var busy = stage != MatchmakingStage.None || cancelAction;
        button.EnableInClassList("search-busy", busy && stage != MatchmakingStage.Found && stage != MatchmakingStage.Error);
        button.EnableInClassList("search-error", stage == MatchmakingStage.Error);
        if (!busy) return;

        var cancelable = !cancelAction && (stage == MatchmakingStage.Pre || stage == MatchmakingStage.Searching);
        if (cancelable && hovering) {
            buttonText.text = "CANCEL";
        }
        else if (stage == MatchmakingStage.Searching && !cancelAction) {
            var time = Mathf.FloorToInt(Time.time - searchStart);
            buttonText.text = "SEARCHING  " + time / 60 + ":" + (time % 60).ToString("00");
        }
        else {
            buttonText.text = matchmakingText;
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
        

        if (stage == MatchmakingStage.None) {
            if (stateManager.current == null) return;
            gameMode = stateManager.current.id;

            if (gameMode == Playlists.Custom) {
                string joinCode = stateManager.stateInput.value.ToUpper();

                if (!ValidateCode(joinCode)) {
                    Error("Invalid code", 2.5f);
                    return;
                }
            }

            stage = MatchmakingStage.Pre;
            StartCoroutine(Countdown());
        }
        else if (stage != MatchmakingStage.Found) {
            if (PhotonNetwork.InRoom) {
                PhotonNetwork.LeaveRoom();
            }
            else {
                cancelAction = true;
                matchmakingText = "CANCELING";
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

            matchmakingText = "STARTING IN " + count;

            yield return new WaitForSeconds(1);
            count--;

            if (count == 0) {
                stage = MatchmakingStage.Searching;

                gameMode = stateManager.current.id;

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
        
        if (gameMode == Playlists.Custom) {
            string joinCode = stateManager.stateInput.value.ToUpper();

            if (!ValidateCode(joinCode)) {
                Error("Invalid code", 2.5f);
                return;
            }
            
            Debug.Log("Looking to join custom room " + joinCode);

            PhotonNetwork.JoinRoom(joinCode);
            return;
        }

        Debug.Log("Looking to join " + gameMode);

        TypedLobby typedLobby = new TypedLobby(gameMode, LobbyType.Default);
        
        Hashtable expectedCustomRoomProperties = new Hashtable() {
            { Constants.GAME_MODE_PROP_KEY, gameMode }
        };
        PhotonNetwork.JoinRandomRoom(expectedCustomRoomProperties, Constants.ROOM_NUM_EXPECTED_PLAYERS, MatchmakingMode.FillRoom, typedLobby, null, null);
    }

    public override void OnJoinedRoom() {
        Debug.Log("Joined room");
        
        if (PhotonNetwork.CurrentRoom.PlayerCount != Constants.ROOM_NUM_EXPECTED_PLAYERS) {
            if (cancelAction) {
                PhotonNetwork.LeaveRoom();
            }
            else if (gameMode == Playlists.Custom) {
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
            { Constants.GAME_MODE_PROP_KEY, gameMode }
        };
        roomOptions.MaxPlayers = Constants.ROOM_NUM_EXPECTED_PLAYERS;

        TypedLobby typedLobby = new TypedLobby(gameMode, LobbyType.Default);

        PhotonNetwork.CreateRoom(null, roomOptions, typedLobby);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("Failed to join room: " + message);

        if (gameMode == Playlists.Custom) {
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
            button.SetEnabled(true);
        }

        cancelAction = false;
        stage = MatchmakingStage.None;
        
        stateManager.stateInput.SetEnabled(true);
        button.SetEnabled(true);
        buttonText.text = stateManager.buttonText;


        stateManager.lockedIn = false;
    }

    private void Error(string message, float lifetime = 0) {

        stateManager.stateInput.SetEnabled(false);
        button.SetEnabled(false);

        stage = MatchmakingStage.Error;
        matchmakingText = message.ToUpper();
        UIKit.Shake(button);

        if (lifetime > 0) {
            Invoke("Cancel", lifetime);
        }
    }

    private void StartGame() {
        Debug.Log("Starting game");

        stage = MatchmakingStage.Found;

        PhotonNetwork.CurrentRoom.IsVisible = false;
        PlayerPrefs.SetString(Constants.PLAYLIST_PREF_KEY, gameMode);

        matchmakingText = "MATCH FOUND";

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
