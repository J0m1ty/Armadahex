using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

[RequireComponent(typeof(TMP_Text))]
public class StatsUpdater : MonoBehaviour
{
    private TMP_Text text;

    private void Awake() {
        text = GetComponent<TMP_Text>();
    }

    void Update() {
        if (text != null && PhotonNetwork.IsConnectedAndReady) {
            var numberOfPlayersOnMaster = PhotonNetwork.CountOfPlayersOnMaster;
            var numberOfPlayersInRooms = PhotonNetwork.CountOfPlayersInRooms;
            var numberOfPlayersTotal = PhotonNetwork.CountOfPlayers;
            var numberOfRooms = PhotonNetwork.CountOfRooms;
            
            text.text = numberOfPlayersTotal + " Players Online" + "\n" + 
                numberOfRooms + " Games Active";
        }
    }
}
