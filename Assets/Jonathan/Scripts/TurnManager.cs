using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager instance;

    private List<Team> teams;

    private Team _currentTeam;
    public Team currentTeam { 
        get => _currentTeam;
        private set {
            _currentTeam = value;
            OnTurnChange?.Invoke(value);
        }
    }
    public Team otherTeam {
        get {
            if (currentTeam == null) {
                return null;
            }

            return teams.Find(t => t != currentTeam);
        }
    }
    public Team playerTeam => teams.Find(t => t.isPlayer);

    public CameraController cameraRig;

    private void Awake() {
        instance = this;

        currentTeam = null;
    }

    public void LoadTeams(List<Team> teams) {
        this.teams = teams;
    }

    public void SetTurn(Team team) {
        currentTeam = team;
    }

    // event
    public delegate void TurnChange(Team team);

    public event TurnChange OnTurnChange;

    void OnGUI() {
        if (GUI.Button(new Rect(10, 30, 200, 100), "Next Turn")) {
            NextTurn();
        }
    }

    public void NextTurn() {
        var index = teams.IndexOf(currentTeam);
        index++;

        if (index >= teams.Count) {
            index = 0;
        }

        currentTeam = teams[index];
    }
}
