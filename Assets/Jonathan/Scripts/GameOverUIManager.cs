using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using Photon.Pun;
using TMPro;
using System;

public class GameOverUIManager : MonoBehaviourPunCallbacks
{
    [Header("Scene References")]
    [MyBox.Scene]
    [SerializeField]
    private string menuScene;

    private WinInfo winInfo;

    [Header("UI References")]
    [SerializeField]
    private TMP_Text gameOverText;
    [SerializeField]
    private TMP_Text winTypeText;
    [SerializeField]
    private TMP_Text xpGainText;
    [SerializeField]
    private TMP_Text matchTimeText;
    [SerializeField]
    private TMP_Text accuracyText;
    [SerializeField]
    private TMP_Text shipsLostText;
    [SerializeField]
    private TMP_Text advancedAttacksUsedText;

    [SerializeField]
    private TMP_Text winnerXpGainText;
    [SerializeField]
    private TMP_Text loserXpGainText;
    [SerializeField]
    private TMP_Text winnerNameText;
    [SerializeField]
    private TMP_Text loserNameText;

    public void Leave() {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom() {
        PhotonNetwork.LoadLevel(menuScene);
    }

    public void Start() {
        winInfo = GameOver.instance?.winInfo;

        if (winInfo == null) {
            Debug.LogError("No win info found");
            return;
        }

        var isWinner = winInfo.winningTeam == winInfo.playerTeam;
        
        gameOverText.text = isWinner ? "TOTAL VICTORY" : "FULL DEFEAT";
        
        if (isWinner) {
            switch (winInfo.winType) {
                case WinType.Conquest:
                    winTypeText.text = "Win by conquest";
                    break;
                case WinType.Abandonment:
                    winTypeText.text = "Win by abandonment";
                    break;
                case WinType.Surrender:
                    winTypeText.text = "Win by surrender";
                    break;
            }
        }
        else {
            switch (winInfo.winType) {
                case WinType.Conquest:
                    winTypeText.text = "Defeat by conquest";
                    break;
                case WinType.Abandonment:
                    winTypeText.text = "Defeat by abandonment";
                    break;
                case WinType.Surrender:
                    winTypeText.text = "Defeat by surrender";
                    break;
            }
        }

        if (winInfo.winnerXpGain != null && winInfo.loserXpLoss != null) {
            xpGainText.text = (isWinner ? "+" : "-") + (isWinner ? winInfo.winnerXpGain : winInfo.loserXpLoss) + " XP";
        }

        if (winInfo.matchTime != null) {
            var timeSpan = TimeSpan.FromSeconds((double)winInfo.matchTime);
            matchTimeText.text = timeSpan.Minutes + ":" + timeSpan.Seconds + "m";
        }

        if (winInfo.playerTeamAccuracy != null) {
            accuracyText.text = winInfo.playerTeamAccuracy + "%";
        }

        if (winInfo.playerTeamShipsLost != null) {
            shipsLostText.text = "" + winInfo.playerTeamShipsLost;
        }

        if (winInfo.playerTeamAdvancedAttacksUsed != null) {
            advancedAttacksUsedText.text = "" + winInfo.playerTeamAdvancedAttacksUsed;
        }

        if (winInfo.winnerXpGain != null) {
            winnerXpGainText.text = "+" + winInfo.winnerXpGain + " XP";
        }

        if (winInfo.loserXpLoss != null) {
            loserXpGainText.text = "-" + winInfo.loserXpLoss + " XP";
        }
        
        winnerNameText.text = winInfo.playerName;
        loserNameText.text = winInfo.enemyName;
    }
}
