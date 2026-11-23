using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Photon.Pun;

public class MatchChat : MonoBehaviourPunCallbacks
{
    [SerializeField] private UIDocument document;
    private Button disableMatchChatButton => document.rootVisualElement.Q<Button>("mute");

    public bool allowChat;

    void Start() {
        allowChat = GameModeInfo.instance.AllowChat;

        if (!allowChat || GameModeInfo.instance.IsSingleplayer) {
            DisableChat(false);
        }
    }

    public void DisableChat(bool share) {
        allowChat = false;
        disableMatchChatButton.text = "CHAT DISABLED";
        disableMatchChatButton.SetEnabled(false);

        if (share) {
            photonView.RPC("DisableChatRPC", RpcTarget.Others);
        }
    }

    [PunRPC]
    public void DisableChatRPC(PhotonMessageInfo info) {
        DisableChat(false);
    }
}
