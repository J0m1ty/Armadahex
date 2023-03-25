using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

[RequireComponent(typeof(TMP_Text))]
public class NameSource : MonoBehaviour
{
    private TMP_Text nameText;

    void Awake() {
        nameText = GetComponent<TMP_Text>();

        nameText.text = PhotonNetwork.NickName;
    }
}
