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
    private PanelSlider panelSlider;

    [SerializeField]
    private Countdown countdown;
    
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

    private void Awake() {
        instance = this;

        currentTeam = null;
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
        StartCoroutine(NextTurnAfterDelay(3f));
    }

    public void SetTurn(Team team) {
        currentTeam = team;
        
        panelSlider.QuickActivate();

        countdown.StartCountdown();

        if (currentTeam.isPlayer) {
            OnPlayerTurn();
            panelSlider.connectedText.text = "Your turn! Select a ship to attack with.";
        } else {
            OnEnemyTurn();
            panelSlider.connectedText.text = "Enemy turn! Waiting for enemy's move.";
        }
    }

    private void OnPlayerTurn() {
        selector.SetTeam(TurnManager.instance.playerTeam, TurnManager.instance.otherTeam.teamBase);
        attackManager.SetState(AttackUIManager.AttackState.SelectShip);
        CameraManager.instance.MoveTo(TurnManager.instance.playerTeam.teamBase.transform.position);
    }

    private void OnEnemyTurn() {
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
