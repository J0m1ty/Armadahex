using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TurnManager : MonoBehaviour
{
    public static TurnManager instance;

    private List<Team> teams;

    public AttackUIManager attackManager;
    public Selector selector;

    [SerializeField]
    private Countdown countdown;

    [SerializeField]
    private ShipManager shipManager;

    [SerializeField]
    private TeamManager teamManager;

    public Team currentTeam;
    public Team otherTeam {
        get {
            if (currentTeam == null) {
                return null;
            }

            return teams.Find(t => t != currentTeam);
        }
    }

    public Team playerTeam => teams.Find(t => t.isPlayer);
    public Team enemyTeam => teams.Find(t => !t.isPlayer);

    public CameraController cameraRig;

    public bool loading;
    public bool gameActive;

    public int consecutiveTurns { get; private set; }

    private void Awake() {
        instance = this;

        currentTeam = null;

        loading = true;
        gameActive = false;

        consecutiveTurns = 0;
    }

    public void LoadTeams(List<Team> teams) {
        this.teams = teams;
    }

    public void RandomizeTeams() {
        // set random teamType for each team, must be unique
        var teamTypes = new List<TeamType>(System.Enum.GetValues(typeof(TeamType)).Cast<TeamType>());

        foreach (var team in teams) {
            var index = Random.Range(0, teamTypes.Count);
            team.teamType = teamTypes[index];
            teamTypes.RemoveAt(index);
        }
    }
    
    public delegate void TurnEvent(Team newTeam);

    public event TurnEvent OnTurnOver;

    public void TurnOver() {
        if (!GameModeInfo.instance.IsSingleplayer || (GameModeInfo.instance.IsSingleplayer && !currentTeam.isPlayer)) {
            countdown.isPaused = true;
        }
        
        StartCoroutine(NextTurnAfterDelay(3f));
    }

    public void ContinueTurnDelay(bool isBotTurn = false) {
        countdown.isPaused = true;

        StartCoroutine(ContinueTurnAfterDelay(GameModeInfo.instance.IsSalvo ? 2.5f : 3f, isBotTurn));
    }

    private IEnumerator ContinueTurnAfterDelay(float delay, bool isBotTurn = false) {
        yield return new WaitForSeconds(delay);
        
        if (GameModeInfo.instance.IsSingleplayer) {
            ContinueTurn(isBotTurn);
        }
        else {
            GameNetworking.instance.ContinueTurn(countdown.currentTime);
        }
    }

    public void ContinueTurn(bool isBotTurn = false, int? setTime = null) {
        Debug.Log("Continuing Turn for " + currentTeam.teamType + " and setting countdown to " + setTime);
        
        countdown.isPaused = false;

        if (isBotTurn) {
            countdown.StartCountdownForBot();
        }
        else {
            countdown.AddTime(setTime);
        }

        if (currentTeam.isPlayer) {
            OnPlayerTurn();
        }
        else {
            OnEnemyTurn();
        }
    }

    public void StartGame() {
        loading = false;
        gameActive = true;

        if (GameModeInfo.instance.IsSingleplayer) {
            SetTurn(playerTeam);
        }
        else {
            SetTurn(teamManager.teams.Find(t => t.teamType == GameNetworking.instance.firstTeam));
        }
    }

    // <summary> Called to start the game (first turn) and to start subsequent turns. </summary>
    public void SetTurn(Team team) {
        consecutiveTurns = 0;

        currentTeam = team;
        
        countdown.isPaused = false;
        if (currentTeam.isPlayer) {
            OnPlayerTurn();
            countdown.StartCountdown();
        } else {
            OnEnemyTurn();
            if (GameModeInfo.instance.IsSingleplayer) {
                Debug.Log("Starting countdown for bot");
                countdown.StartCountdownForBot();
            }
            else {
                countdown.StartCountdown();
            }
        }
    }

    private void OnPlayerTurn() {
        consecutiveTurns++;

        MatchHud.instance.SetTurn(true);

        selector.SetTeam(TurnManager.instance.playerTeam, TurnManager.instance.otherTeam.teamBase);
        attackManager.SetState(AttackUIManager.AttackState.SelectShip);
        // your whole turn happens on the enemy board, you only go home to watch their shots
        CameraManager.instance.MoveTo(TurnManager.instance.enemyTeam.teamBase.transform.position);
    }

    private void OnEnemyTurn() {
        consecutiveTurns++;

        MatchHud.instance.SetTurn(false);

        selector.allowSelectingGrids = false;
        selector.allowSelectingShips = false;
        selector.SetTeam(TurnManager.instance.otherTeam, TurnManager.instance.playerTeam.teamBase);
        attackManager.SetState(AttackUIManager.AttackState.None);
        CameraManager.instance.MoveTo(TurnManager.instance.playerTeam.teamBase.transform.position);
    }

    public void NextTurn() {
        var index = teams.IndexOf(currentTeam);
        index++;

        if (index >= teams.Count) {
            index = 0;
        }

        SetTurn(teams[index]);

        OnTurnOver?.Invoke(currentTeam);
    }

    IEnumerator NextTurnAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        NextTurn();
    }

    // void OnGUI() {
    //     if (GUI.Button(new Rect(10, 30, 200, 100), "Next Turn")) {
    //         NextTurn();
    //     }
    // }
}
