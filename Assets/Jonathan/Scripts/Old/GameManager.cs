using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviourPunCallbacks
{
    #region Singleton Fields
    public static GameManager Instance;
    #endregion

    #region Public Fields
    public Player[] players { get; private set; }
    #endregion

    #region MonoBehaviour Callbacks
    void Awake() {
        if (GameManager.Instance == null) {
            GameManager.Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }
    }

    void Start() {
        UpdatePlayers();
    }
    #endregion

    #region Public Methods
    public void LeaveRoom() {
        PhotonNetwork.LeaveRoom();
    }
    #endregion

    #region MonoBehaviourPunCallbacks Callbacks
    public override void OnLeftRoom() {
        SceneManager.LoadScene(0);
    }

    public override void OnPlayerEnteredRoom(Player other) {
        Debug.LogFormat("OnPlayerEnteredRoom() {0}", other.NickName);

        UpdatePlayers();

        LoadArena();
    }

    public override void OnPlayerLeftRoom(Player other)
    {
        Debug.LogFormat("OnPlayerLeftRoom() {0}", other.NickName);

        UpdatePlayers();
    }
    #endregion

    #region Private Methods
    private void LoadArena() {
        if (!PhotonNetwork.IsMasterClient) {
            Debug.LogError("PhotonNetwork : Trying to Load a level but we are not the master Client");
            return;
        }
        
        int maxUsers = PhotonNetwork.CurrentRoom.MaxPlayers;
        int currentUsers = PhotonNetwork.CurrentRoom.PlayerCount;
        
        if (currentUsers == maxUsers) {
            Debug.LogFormat("PhotonNetwork : Max Players Reached, Loading Arena");
            PhotonNetwork.LoadLevel("Arena");
        }
        else {
            Debug.LogFormat("PhotonNetwork : Waiting for more players to join");
        }
    }

    private void UpdatePlayers() {
        players = PhotonNetwork.PlayerList;
    }
    #endregion
}
