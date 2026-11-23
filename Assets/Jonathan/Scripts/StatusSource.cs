using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Photon.Pun;

public class StatusSource : MonoBehaviour
{
    [SerializeField]
    private UIDocument document;

    [SerializeField]
    private string element;

    private Label statusText;

    [SerializeField]
    private int interval = 1;

    void Start() {
        statusText = document.rootVisualElement.Q<Label>(element);
        
        StartCoroutine(UpdateStatus());
    }

    IEnumerator UpdateStatus() {
        while (true) {
            statusText.text = PhotonNetwork.IsConnectedAndReady ? "v_" + Application.version : "OFFLINE";
            yield return new WaitForSeconds(interval);
        }
    }
}
