using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Navigator : MonoBehaviour
{
    public void MenuNav(string sceneName) {
        PhotonNetwork.LoadLevel(sceneName);
    }
}
