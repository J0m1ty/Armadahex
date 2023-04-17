using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using Photon.Pun;

public enum WinType {
    None = 0,
    Conquest = 1,
    Abandonment = 2,
    Surrender = 3,
}

public class WinInfo {
    public TeamType playerTeam;
    public TeamType winningTeam;
    public string playerName;
    public string enemyName;
    public WinType winType;
    public int? winnerXpGain;
    public int? loserXpLoss;
    public double? matchTime;
    public float? playerTeamAccuracy;
    public int? playerTeamShipsLost;
    public int? playerTeamAdvancedAttacksUsed;
}

public class GameOver : MonoBehaviour
{
    public static GameOver instance { get; private set; }

    [Header("Ships Remaining")]
    [SerializeField]
    public TMPro.TMP_Text friendlyShipsRemainingText;
    [SerializeField]
    public TMPro.TMP_Text enemyShipsRemainingText;

    [MyBox.Scene]
    [SerializeField]
    private string gameScene;
    
    [MyBox.Scene]
    [SerializeField]
    private string gameOverScene;

    public string enemyName;
    
    public WinInfo winInfo { get; private set; }

    [SerializeField]
    private ShipManager shipManager;

    [SerializeField]
    private AttackUIManager attackManager;

    private void Awake() {
        if (SceneManager.GetActiveScene().name != gameScene && SceneManager.GetActiveScene().name != gameOverScene) {
            Destroy(gameObject);
            return;
        }

        if (instance != null) {
            Destroy(gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject);

        OnGameOver += OnWin;
    }

    public delegate void GameOverEvent(TeamType win, WinType winType);

    public event GameOverEvent OnGameOver;

    void Start() {
        attackManager.OnAttack += CheckIfGameOver;
    }

    public void CheckIfGameOver(Team against, bool hit, int hexIndex, bool finalAttack)
    {
        if (!hit || !finalAttack) return;

        var shipCounts = new Dictionary<TeamType, int>();

        foreach (var ship in shipManager.ships) {
            if (!ship.isAlive) continue;

            if (!shipCounts.ContainsKey(ship.team.teamType)) {
                shipCounts.Add(ship.team.teamType, 0);
            }

            shipCounts[ship.team.teamType]++;
        }

        // Debug.Log("Ship counts: " + string.Join(", ", shipCounts.Select(kvp => kvp.Key + ": " + kvp.Value).ToArray()));
        // Debug.Log(shipCounts.Keys.Count);

        UpdateShipCounts(shipCounts);
        
        if (shipCounts.Keys.Count == 1) {
            var win = shipCounts.First().Key;
            
            OnGameOver?.Invoke(win, WinType.Conquest);
        }
    }

    public void Surrender() {
        OnGameOver?.Invoke(TurnManager.instance.otherTeam.teamType, WinType.Surrender);
    }

    public void UpdateShipCounts(Dictionary<TeamType, int> shipCounts) {
        var s = "Ships remaining: ";
        if (friendlyShipsRemainingText != null && enemyShipsRemainingText != null) {
            foreach (var kvp in shipCounts) {
                if (kvp.Key == TurnManager.instance.playerTeam.teamType) {
                    friendlyShipsRemainingText.text = s + kvp.Value.ToString();
                }
                else {
                    enemyShipsRemainingText.text = s + kvp.Value.ToString();
                }
            }
        }
    }

    public void EnemyAbandonment() {
        OnGameOver?.Invoke(TurnManager.instance.playerTeam.teamType, WinType.Abandonment);
    }

    public void OnWin(TeamType win, WinType winType) {
        Debug.Log("Game over");

        float accuracy;
        if (attackManager.shotsFired == 0) {
            accuracy = 0;
        }
        else {
            accuracy = (float)attackManager.shotsHit / (float)attackManager.shotsFired;
        }

        winInfo = new WinInfo {
            playerTeam = TurnManager.instance.playerTeam.teamType,
            winningTeam = win,
            playerName = PhotonNetwork.NickName,
            enemyName = enemyName,
            winType = winType,
            winnerXpGain = 100,
            loserXpLoss = 50,
            matchTime = Time.timeSinceLevelLoadAsDouble,
            playerTeamAccuracy = accuracy * 100f,
            playerTeamShipsLost = shipManager.playerShips.FindAll(s => !s.isAlive).Count,
            playerTeamAdvancedAttacksUsed = attackManager.advancedShotsFired,
        };
        
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.LoadLevel(gameOverScene);
    }
}
