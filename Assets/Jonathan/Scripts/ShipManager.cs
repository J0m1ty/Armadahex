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

    private TeamManager teamManager;

    void Awake() {
        shipBlueprints.Sort((a, b) => a.name.CompareTo(b.name));
        teamManager = GetComponent<TeamManager>();
    }

    private NetworkingShip GenerateShip(ShipModel model, Team team) {
        var obj = Instantiate(model.shipPrefab, transform) as GameObject;

        var ship = obj.GetComponent<Ship>();
        
        ships.Add(ship);

        ship.shipModel = model;
        ship.team = team;
        ship.rotation = rotations[Random.Range(0, rotations.Count)];
        ship.reverse = Random.Range(0, 2) == 0;

        var grid = team.teamBase.hexMap;
        
        var hex = grid.hexes[Random.Range(0, grid.hexes.Count)];
        while (true) {
            hex = grid.hexes[Random.Range(0, grid.hexes.Count)];

            ship.rotation = rotations[Random.Range(0, rotations.Count)];
            ship.reverse = Random.Range(0, 2) == 0;
            
            if (ship.SetOnGrid(hex)) break;
        }

        return new NetworkingShip {
            shipModelName = model.name,
            teamTypeIndex = (int) team.teamType,
            rotation = rotations.IndexOf(ship.rotation),
            reverse = ship.reverse,
            hexIndex = hex.coords.index
        };
    }

    private void GenerateShipFromData(NetworkingShip data, Team team) {
        var shipModel = shipBlueprints.Find(s => s.name == data.shipModelName);
        var obj = Instantiate(shipModel.shipPrefab, transform) as GameObject;

        var ship = obj.GetComponent<Ship>();
        
        ships.Add(ship);

        ship.shipModel = shipModel;
        ship.team = team;
        ship.rotation = rotations[data.rotation];
        ship.reverse = data.reverse;

        var grid = team.teamBase.hexMap;
        
        var hex = grid.hexes[data.hexIndex];

        if (!ship.SetOnGrid(hex)) {
            Debug.Log("Failed to place ship from data");
            
            Destroy(obj);
        }
    }

    public List<NetworkingShip> GenerateShips(Team team) {
        var shipInfo = new List<NetworkingShip>();
        foreach (var ship in shipBlueprints) {
            if (ship.length != 3) continue;
            shipInfo.Add(GenerateShip(ship, team));
        }
        return shipInfo;
    }

    public void GenerateShipsFromData(List<NetworkingShip> data) {
        foreach (var ship in data) {
            var team = teamManager.teams.Find(t => (int) t.teamType == ship.teamTypeIndex);
            GenerateShipFromData(ship, team);
        }
    }
    
    public void EnableShips() {
        foreach (var ship in ships) {
            ship.EnableShip();
        }
    }
}
