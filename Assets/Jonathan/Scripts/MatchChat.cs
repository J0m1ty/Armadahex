using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class MatchChat : MonoBehaviourPunCallbacks
{
    [SerializeField] private Button disableMatchChatButton;
    private TMP_Text buttonText => disableMatchChatButton.GetComponentInChildren<TMP_Text>();

    public bool allowChat;

    void Start() {
        allowChat = GameModeInfo.instance.AllowChat;

        if (!allowChat || GameModeInfo.instance.IsSingleplayer) {
            DisableChat(false);
        }
    }

    public void DisableChat(bool share) {
        allowChat = false;
        buttonText.text = "CHAT DISABLED";
        disableMatchChatButton.interactable = false;

        if (share) {
            photonView.RPC("DisableChatRPC", RpcTarget.Others);
        }
    }

    [PunRPC]
    public void DisableChatRPC(PhotonMessageInfo info) {
        DisableChat(false);
    }
}
