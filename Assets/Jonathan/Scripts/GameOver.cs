using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOver : MonoBehaviour
{
    private ShipManager shipManager;

    public AttackUIManager attackManager;

    void Start() {
        shipManager = GetComponent<ShipManager>();

        attackManager.OnAttack += CheckIfGameOver;
    }

    public void CheckIfGameOver(bool hit)
    {
        if (!hit) return;

        var redAlive = false;
        var blueAlive = false;
        foreach (var ship in shipManager.ships) {
            if (ship.team.teamType == TeamType.Red && ship.isAlive)
                redAlive = true;
            if (ship.team.teamType == TeamType.Blue && ship.isAlive)
                blueAlive = true;
        }

        if (!redAlive) {
            OnGameOver?.Invoke(TeamType.Blue);
        }

        if (!blueAlive) {
            OnGameOver?.Invoke(TeamType.Red);
        }
    }

    public delegate void GameOverEvent(TeamType win);

    public event GameOverEvent OnGameOver;
}
