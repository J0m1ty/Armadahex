using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using System;
using Photon.Pun;

[Serializable]
public enum TeamType {
    Tevex = 0,
    Soven = 1,
    Vekor = 2,
    Azura = 3,
    Niron = 4,
    Rilix = 5,
    Lumin = 6,
    Folor = 7,
    Xalor = 8,
    Spirax = 9,
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
    public List<TerrainBlock> terrainBlocks;

    public List<Team> teams;
    
    public ShipManager shipManager { get; private set; }

    void Awake() {
        shipManager = GetComponent<ShipManager>();
    }

    void Start() {
        if (PhotonNetwork.OfflineMode) {
            TurnManager.instance.LoadTeams(teams);
            TurnManager.instance.RandomizeTeams();
            TurnManager.instance.currentTeam = TurnManager.instance.playerTeam;
            TurnManager.instance.SetTurn(TurnManager.instance.playerTeam);
            GenerateTerrain();
            GenerateShips();
        }
    }

    public void GenerateTerrain() {
        var availableBlocks = new List<TerrainBlock>(terrainBlocks);

        foreach (var team in teams) {
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

        shipManager.EnableShips();
    }
}