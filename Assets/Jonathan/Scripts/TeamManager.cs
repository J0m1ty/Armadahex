using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using System;

[Serializable]
public enum TeamType {
    Red,
    Blue
}

[Serializable]
public class TerrainBlock {
    public string name;
    public GameObject terrainPrefab;
}

[Serializable]
public class Team {
    public TeamType teamType;
    public TeamBase teamBase;
    public bool isPlayer;
}

[RequireComponent(typeof(ShipManager))]
public class TeamManager : MonoBehaviour {
    public TerrainBlock[] terrainBlocks;

    public List<Team> teams;
    
    public ShipManager shipManager;

    void Awake() {
        shipManager = GetComponent<ShipManager>();
    }

    void Start() {
        Debug.Log("Generating random terrain");
        GenerateTerrain();
        Debug.Log("Generating random ships");
        GenerateShips();
        Debug.Log("Loading teams");
        TurnManager.instance.LoadTeams(teams);
        Debug.Log("Randomizing teams");
        TurnManager.instance.RandomizeTeams();
        Debug.Log("Enabiling ships");
        shipManager.EnableShips();
        Debug.Log("Setting turn");
        TurnManager.instance.SetTurn(TurnManager.instance.playerTeam);
    }

    public void GenerateTerrain() {
        var availableBlocks = new List<TerrainBlock>(terrainBlocks);

        foreach (var team in teams) {
            if (availableBlocks.Count == 0) {
                Debug.LogError("Not enough terrain blocks to generate all teams");
                break;
            }
            var index = Random.Range(0, availableBlocks.Count);
            var block = availableBlocks[index];
            availableBlocks.RemoveAt(index);
            
            team.teamBase.SetTerrain(block);
        }
    }

    public void GenerateShips() {
        foreach (var team in teams) {
            shipManager.GenerateShips(team);
        }
    }
}