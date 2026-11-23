using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Photon.Pun;

public class NameSource : MonoBehaviour
{
    [SerializeField]
    private UIDocument document;

    [SerializeField]
    private string element;

    private Label nameText;

    [SerializeField]
    private bool useEnemyName = false;

    void Awake() {
        nameText = document.rootVisualElement.Q<Label>(element);

        var name = "Player" + (useEnemyName ? "2" : "1");

        if (PhotonNetwork.IsConnected) {
            name = PhotonNetwork.NickName;
        }

        if (useEnemyName && PhotonNetwork.InRoom && PhotonNetwork.PlayerListOthers.Length > 0) {
            name = PhotonNetwork.PlayerListOthers[0].NickName;
        }

        nameText.text = name;
    }
}
