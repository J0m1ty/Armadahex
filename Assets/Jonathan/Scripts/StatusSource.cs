using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

[RequireComponent(typeof(TMP_Text))]
public class StatusSource : MonoBehaviour
{
    private TMP_Text statusText;

    [SerializeField]
    private int interval = 1;

    void Start() {
        statusText = GetComponent<TMP_Text>();
        
        StartCoroutine(UpdateStatus());
    }

    IEnumerator UpdateStatus() {
        while (true) {
            statusText.text = PhotonNetwork.IsConnectedAndReady ? "V" + PhotonNetwork.AppVersion : "OFFLINE";
            yield return new WaitForSeconds(interval);
        }
    }
}
