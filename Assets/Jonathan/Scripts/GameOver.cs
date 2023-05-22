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
    public Texture2D playerImage;
    public Texture2D enemyImage;
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

    public ScreenshotSaver screenshotSaver;

    [Header("Ships Remaining")]
    [SerializeField]
    public TMPro.TMP_Text friendlyShipsRemainingText;
    
    [SerializeField]
    private MyBox.SceneReference gameScene;
    
    [SerializeField]
    private MyBox.SceneReference gameOverScene;

    public string enemyName;
    
    public WinInfo winInfo { get; private set; }

    [SerializeField]
    private ShipManager shipManager;

    [SerializeField]
    private AttackUIManager attackManager;

    public Texture2D playerImage;
    public Texture2D enemyImage;

    private void Awake() {
        if (instance != null) {
            Destroy(gameObject);
            return;
        }
        else {
            instance = this;

            DontDestroyOnLoad(gameObject);

            OnGameOver += OnWin;

            SceneManager.sceneLoaded += OnSceneLoaded;

            playerImage = null;
            enemyImage = null;
        }
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (scene.name != gameScene.SceneName && scene.name != gameOverScene.SceneName) {
            if (gameObject != null) {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Destroy(gameObject);
            }
            return;
        }
    }

    public delegate void GameOverEvent(TeamType win, WinType winType);

    public event GameOverEvent OnGameOver;

    void Start() {
        attackManager.OnAttack += CheckIfGameOver;
    }

    public bool CheckIfGameOver() {
        var shipCounts = new Dictionary<TeamType, int>();

        foreach (var ship in shipManager.ships) {
            if (!ship.isAlive) continue;

            if (!shipCounts.ContainsKey(ship.team.teamType)) {
                shipCounts.Add(ship.team.teamType, 0);
            }

            shipCounts[ship.team.teamType]++;
        }

        UpdateShipCounts(shipCounts);
        
        if (shipCounts.Keys.Count == 1) {
            var win = shipCounts.First().Key;
            
            return true;
        }

        return false;
    }

    public void CheckIfGameOver(Team against, bool hit, int hexIndex, bool finalAttack)
    {
        if (!hit) return;

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
        OnGameOver?.Invoke(TurnManager.instance.enemyTeam.teamType, WinType.Surrender);
    }

    public void UpdateShipCounts(Dictionary<TeamType, int> shipCounts) {
        var s = "/5\n<size=28>Ships</size>";
        if (friendlyShipsRemainingText != null) {
            foreach (var kvp in shipCounts) {
                if (kvp.Key == TurnManager.instance.playerTeam.teamType) {
                    friendlyShipsRemainingText.text = kvp.Value.ToString() + s;
                }
            }
        }
    }

    public void EnemyAbandonment() {
        OnGameOver?.Invoke(TurnManager.instance.playerTeam.teamType, WinType.Abandonment);
    }

    public void OnWin(TeamType win, WinType winType) {
        TurnManager.instance.gameActive = false;

        Debug.Log("Game over");

        float accuracy;
        if (attackManager.shotsFired == 0) {
            accuracy = 0;
        }
        else {
            accuracy = (float)attackManager.shotsHit / (float)attackManager.shotsFired;
        }

        winInfo = new WinInfo {
            playerTeam = TurnManager.instance.playerTeam.teamType ,
            winningTeam = win,
            playerName = PhotonNetwork.NickName.Length > 0 ? PhotonNetwork.NickName : "Player",
            enemyName = enemyName,
            winType = winType,
            winnerXpGain = 100,
            loserXpLoss = 50,
            matchTime = Time.timeSinceLevelLoadAsDouble,
            playerTeamAccuracy = accuracy * 100f,
            playerTeamShipsLost = shipManager.playerShips.FindAll(s => !s.isAlive).Count,
            playerTeamAdvancedAttacksUsed = attackManager.advancedShotsFired,
        };
        
        screenshotSaver.TakeScreenshots();
    }

    public void ScreenshotsTaken() {
        Debug.Log("Screenshots taken, loading game over scene");

        winInfo.playerImage = playerImage;
        winInfo.enemyImage = enemyImage;
        

        if (!PhotonNetwork.IsConnected || GameModeInfo.instance.IsSingleplayer) {
            SceneManager.LoadScene(gameOverScene.SceneName);
        }
        else if (PhotonNetwork.IsMasterClient) {
            PhotonNetwork.DestroyAll();
            PhotonNetwork.LoadLevel(gameOverScene.SceneName);
        }
    }
}
