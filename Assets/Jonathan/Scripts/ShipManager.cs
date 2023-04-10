using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

[RequireComponent(typeof(TeamManager))]
public class ShipManager : MonoBehaviour
{
    public List<ShipModel> shipBlueprints;

    public List<Rotation> rotations;

    public List<Ship> ships;

    public List<Ship> playerShips => ships.FindAll(s => s.team == TurnManager.instance.playerTeam);
    
    public List<Ship> enemyShips => ships.FindAll(s => s.team != TurnManager.instance.playerTeam);

    private void GenerateShip(ShipModel model, Team team) {
        var obj = Instantiate(model.shipPrefab, transform) as GameObject;

        var ship = obj.GetComponent<Ship>();
        
        ships.Add(ship);

        ship.shipModel = model;
        ship.team = team;
        ship.rotation = rotations[Random.Range(0, rotations.Count)];
        ship.reverse = Random.Range(0, 2) == 0;

        var grid = team.teamBase.hexMap;
        
        var attempts = 100;
        while (attempts > 0) {
            var hex = grid.hexes[Random.Range(0, grid.hexes.Count)];

            ship.rotation = rotations[Random.Range(0, rotations.Count)];
            ship.reverse = Random.Range(0, 2) == 0;
            
            var result = ship.SetOnGrid(hex);

            if (result) {
                break;
            }

            attempts--;

            if (attempts == 0) {
                Debug.Log("Failed to place ship");

                Destroy(obj);
            }
        }
    }

    void Awake() {
        shipBlueprints.Sort((a, b) => a.name.CompareTo(b.name));
    }

    public void GenerateShips(Team team) {
        foreach (var ship in shipBlueprints) {
            GenerateShip(ship, team);
        }
    }

    public void EnableShips() {
        foreach (var ship in ships) {
            ship.EnableShip();
        }
    }
}
