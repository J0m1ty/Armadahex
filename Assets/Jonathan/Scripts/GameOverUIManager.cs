using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using Photon.Pun;
using TMPro;
using System;
using UnityEngine.UI;

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
    private Image topImage;
    [SerializeField]
    private Image bottomImage;
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
        if (PhotonNetwork.InRoom) {
            PhotonNetwork.LeaveRoom();
        }
        else {
            OnLeftRoom();
        }
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

        Debug.Log("Game over: " + winInfo.winningTeam + " won");
        Debug.Log("Win type: " + winInfo.winType);
        Debug.Log("Player team: " + winInfo.playerTeam + " (" + winInfo.playerName + ")" + " vs " + winInfo.enemyName + " (" + winInfo.enemyName + ")");

        var isWinner = winInfo.winningTeam == winInfo.playerTeam;

        AudioManager.instance?.PlayResultSound(isWinner);
        
        gameOverText.text = isWinner ? "MISSION SUCCESS" : "MISSION FAILED";
        
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
            matchTimeText.text = timeSpan.Minutes + ":" + timeSpan.Seconds.ToString("D2") + "m";
        }

        if (winInfo.playerTeamAccuracy != null) {
            accuracyText.text = ((int)winInfo.playerTeamAccuracy) + "%";
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
        
        winnerNameText.text = isWinner ? winInfo.playerName : winInfo.enemyName;
        loserNameText.text = isWinner ? winInfo.enemyName : winInfo.playerName;
        
        if (winInfo.playerImage != null && winInfo.enemyImage != null) {
            var topTex = isWinner ? winInfo.playerImage : winInfo.enemyImage;
            var bottomTex = isWinner ? winInfo.enemyImage : winInfo.playerImage;
            topImage.sprite = Sprite.Create(topTex, new Rect(0, 0, topTex.width, topTex.height), new Vector2(0.5f, 0.5f));
            bottomImage.sprite = Sprite.Create(bottomTex, new Rect(0, 0, bottomTex.width, bottomTex.height), new Vector2(0.5f, 0.5f));

            // MAINTAIN aspect
            topImage.preserveAspect = true;
            bottomImage.preserveAspect = true;
        }
    }
}
