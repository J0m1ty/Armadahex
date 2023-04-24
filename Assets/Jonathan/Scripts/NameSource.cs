using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

[RequireComponent(typeof(TMP_Text))]
public class NameSource : MonoBehaviour
{
    private TMP_Text nameText;

    [SerializeField]
    private bool useEnemyName = false;

    void Awake() {
        nameText = GetComponent<TMP_Text>();

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
