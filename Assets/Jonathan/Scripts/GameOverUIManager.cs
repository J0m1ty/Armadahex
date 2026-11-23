using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using Photon.Pun;
using System;
using UnityEngine.UIElements;

public class GameOverUIManager : MonoBehaviourPunCallbacks
{
    [Header("Scene References")]
    [MyBox.Scene]
    [SerializeField]
    private string menuScene;

    private WinInfo winInfo;

    [Header("UI References")]
    [SerializeField]
    private UIDocument document;

    private Label gameOverText;
    private Label winTypeText;
    private Label xpGainText;
    private Image topImage;
    private Image bottomImage;
    private Label matchTimeText;
    private Label accuracyText;
    private Label shipsLostText;
    private Label advancedAttacksUsedText;

    private Label winnerXpGainText;
    private Label loserXpGainText;
    private Label winnerNameText;
    private Label loserNameText;

    [SerializeField]
    private Color victoryColor = new Color(0.1f, 0.55f, 0.1f);
    [SerializeField]
    private Color defeatColor = new Color(0.75f, 0.1f, 0.1f);

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
        var root = document.rootVisualElement;
        gameOverText = root.Q<Label>("result");
        winTypeText = root.Q<Label>("comments");
        xpGainText = root.Q<Label>("xpGain");
        topImage = root.Q<Image>("topImage");
        bottomImage = root.Q<Image>("bottomImage");
        matchTimeText = root.Q<Label>("matchTime");
        accuracyText = root.Q<Label>("accuracy");
        shipsLostText = root.Q<Label>("shipsLost");
        advancedAttacksUsedText = root.Q<Label>("specials");
        winnerXpGainText = root.Q<Label>("winnerXp");
        loserXpGainText = root.Q<Label>("loserXp");
        winnerNameText = root.Q<Label>("winnerName");
        loserNameText = root.Q<Label>("loserName");

        winInfo = GameOver.instance?.winInfo;

        // opened without a match (straight from the editor), so show an empty report
        if (winInfo == null) {
            Debug.Log("No win info found");
            gameOverText.text = "NO MATCH";
            winTypeText.text = "Play a match to see its report here.";
            xpGainText.text = "";
            UIKit.Show(topImage, false);
            UIKit.Show(bottomImage, false);
            return;
        }

        Debug.Log("Game over: " + winInfo.winningTeam + " won");
        Debug.Log("Win type: " + winInfo.winType);
        Debug.Log("Player team: " + winInfo.playerTeam + " (" + winInfo.playerName + ")" + " vs " + winInfo.enemyName + " (" + winInfo.enemyName + ")");

        var isWinner = winInfo.winningTeam == winInfo.playerTeam;

        AudioManager.instance?.PlayResultSound(isWinner);
        
        gameOverText.text = isWinner ? "VICTORY" : "DEFEAT";
        gameOverText.style.color = isWinner ? victoryColor : defeatColor;
        
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
            matchTimeText.text = timeSpan.Minutes + ":" + timeSpan.Seconds.ToString("D2");
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
            topImage.image = topTex;
            bottomImage.image = bottomTex;
            topImage.scaleMode = ScaleMode.ScaleToFit;
            bottomImage.scaleMode = ScaleMode.ScaleToFit;
        }
        else {
            // need to hide images
            UIKit.Show(topImage, false);
            UIKit.Show(bottomImage, false);
        }
    }
}
