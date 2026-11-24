using System;
using UnityEngine;
using UnityEngine.UIElements;
using Photon.Pun;

// the game over screen: the result, both boards as they ended with each side's numbers, and continue
public class GameOverUIManager : MonoBehaviourPunCallbacks
{
    [Header("Scene References")]
    [MyBox.Scene]
    [SerializeField]
    private string menuScene;

    [Header("UI References")]
    [SerializeField]
    private UIDocument document;

    private WinInfo winInfo;

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
        var result = root.Q<Label>("result");
        var comments = root.Q<Label>("comments");

        winInfo = GameOver.instance?.winInfo;

        // opened without a match (straight from the editor), so there's nothing to show but the way out
        if (winInfo == null) {
            Debug.Log("No win info found");
            result.text = "NO MATCH";
            comments.text = "";
            UIKit.Show(root.Q("boards"), false);
            return;
        }

        var isWinner = winInfo.winningTeam == winInfo.playerTeam;
        result.text = isWinner ? "VICTORY" : "DEFEAT";
        result.EnableInClassList("danger", !isWinner);

        var how = winInfo.winType switch {
            WinType.Conquest => "BY CONQUEST",
            WinType.Abandonment => "BY ABANDONMENT",
            WinType.Surrender => "BY SURRENDER",
            _ => ""
        };
        if (winInfo.matchTime != null) {
            var time = TimeSpan.FromSeconds((double)winInfo.matchTime);
            how += "  ·  " + (int)time.TotalMinutes + ":" + time.Seconds.ToString("D2");
        }
        comments.text = how;

        // each side's shots landed on the other side's board
        Side(root, "player", winInfo.playerName, SideColors.Player, winInfo.playerBoard, winInfo.enemyBoard, isWinner);
        Side(root, "enemy", winInfo.enemyName, SideColors.Enemy, winInfo.enemyBoard, winInfo.playerBoard, !isWinner);
    }

    private static void Side(VisualElement root, string side, string name, Color color, BoardSnapshot own, BoardSnapshot target, bool winner) {
        root.Q<Label>(side + "Name").text = string.IsNullOrEmpty(name) ? (side == "player" ? "YOU" : "ENEMY") : name.ToUpper();
        root.Q(side + "Swatch").style.backgroundColor = color;
        UIKit.Show(root.Q(side + "Winner"), winner);
        root.Q<BoardView>(side + "Board").Show(own, color);

        if (target == null || own == null) return;
        root.Q<Label>(side + "Shots").text = target.shots.ToString();
        root.Q<Label>(side + "Hits").text = target.hits.ToString();
        root.Q<Label>(side + "Accuracy").text = target.shots > 0 ? Mathf.RoundToInt(100f * target.hits / target.shots) + "%" : "—";
        root.Q<Label>(side + "Sunk").text = target.shipsSunk.ToString();
        root.Q<Label>(side + "Left").text = own.shipsLeft + " / " + own.shipCount;
    }
}
