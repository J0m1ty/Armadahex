using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

[RequireComponent(typeof(TMP_Text))]
public class PingSource : MonoBehaviour
{
    private TMP_Text pingText;

    [SerializeField]
    private int interval = 1;

    void Start() {
        pingText = GetComponent<TMP_Text>();
        
        StartCoroutine(UpdatePing());
    }

    IEnumerator UpdatePing() {
        while (true) {
            pingText.text = PhotonNetwork.GetPing().ToString() + " ms";
            yield return new WaitForSeconds(interval);
        }
    }
}
