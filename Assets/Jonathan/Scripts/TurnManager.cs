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
    private TurnUIManager turnUI;
    
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
        
        turnUI.ChangeTurn(currentTeam);

        if (currentTeam.isPlayer) {
            OnPlayerTurn();
        } else {
            OnEnemyTurn();
        }
    }

    private void OnPlayerTurn() {
        attackManager.SetState(AttackUIManager.AttackState.SelectShip);
        cameraRig.MoveTo(TurnManager.instance.playerTeam.teamBase.transform.position);
    }

    private void OnEnemyTurn() {
        attackManager.SetState(AttackUIManager.AttackState.None);
        selector.allowSelectingGrids = false;
        selector.allowSelectingShips = false;
        selector.SetTeam(TurnManager.instance.playerTeam);
        cameraRig.MoveTo(TurnManager.instance.playerTeam.teamBase.transform.position);
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
