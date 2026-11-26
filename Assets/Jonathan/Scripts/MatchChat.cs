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
        // a bot match has no mute button
        var button = disableMatchChatButton;
        if (button != null) {
            button.text = "CHAT MUTED";
            button.SetEnabled(false);
        }

        if (share) {
            photonView.RPC("DisableChatRPC", RpcTarget.Others);
        }
    }

    [PunRPC]
    public void DisableChatRPC(PhotonMessageInfo info) {
        DisableChat(false);
    }
}
