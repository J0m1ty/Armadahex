using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Photon.Pun;

public class StatsUpdater : MonoBehaviour
{
    [SerializeField]
    private UIDocument document;

    [SerializeField]
    private string element;

    private Label text;

    private void Awake() {
        text = document.rootVisualElement.Q<Label>(element);
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
