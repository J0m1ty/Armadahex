using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Photon.Pun;

public class PingSource : MonoBehaviour
{
    [SerializeField]
    private UIDocument document;

    [SerializeField]
    private string element;

    private Label pingText;

    [SerializeField]
    private int interval = 1;

    void Start() {
        pingText = document.rootVisualElement.Q<Label>(element);
        
        StartCoroutine(UpdatePing());
    }

    IEnumerator UpdatePing() {
        while (true) {
            pingText.text = PhotonNetwork.GetPing().ToString() + " ms";
            yield return new WaitForSeconds(interval);
        }
    }
}
