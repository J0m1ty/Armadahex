using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class ShipManager : MonoBehaviour
{
    public List<ShipModel> shipBlueprints;

    public List<Team> teams;

    public List<Rotation> rotations;

    public void GenerateShip() {
        var thisTeam = teams.Find(x => x.isPlayer);

        var model = shipBlueprints[Random.Range(0, shipBlueprints.Count)];
        
        var obj = Instantiate(model.shipPrefab, transform) as GameObject;

        var ship = obj.GetComponent<Ship>();

        ship.shipModel = model;
        ship.team = thisTeam;
        ship.rotation = rotations[Random.Range(0, rotations.Count)];
        ship.reverse = Random.Range(0, 2) == 0;

        var grid = thisTeam.map;
        
        while (true) {
            var hex = grid.hexes[Random.Range(0, grid.hexes.Count)];
            
            var result = ship.SetOnGrid(hex);

            if (result) {
                break;
            }
        }
    }

    void Start() {
        GenerateShip();
        GenerateShip();
        GenerateShip();
    }
}
