using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using System;
using Photon.Pun;

[Serializable]
public static class TeamColor {
    public static Color Tevex = new Color(0.0f, 0.8f, 0.8f); // light blue
    public static Color Soven = new Color(0.0f, 0.6f, 0.0f); // dark green
    public static Color Vekor = new Color(0.8f, 0.8f, 0.0f); // yellow
    public static Color Azura = new Color(0.0f, 0.0f, 0.8f); // dark blue
    public static Color Niron = new Color(0.8f, 0.0f, 0.8f); // purple
    public static Color Rilix = new Color(0.8f, 0.4f, 0.0f); // orange
    public static Color Lumin = new Color(0.8f, 0.8f, 0.8f); // light grey
    public static Color Folor = new Color(0.6f, 0.6f, 0.6f); // medium grey
    public static Color Xalor = new Color(0.4f, 0.4f, 0.4f); // dark grey
    public static Color Spirax = new Color(1.0f, 0.0f, 0.0f); // red

    public static Color GetColor(TeamType teamType) {
        switch (teamType) {
            case TeamType.Tevex:
                return Tevex;
            case TeamType.Soven:
                return Soven;
            case TeamType.Vekor:
                return Vekor;
            case TeamType.Azura:
                return Azura;
            case TeamType.Niron:
                return Niron;
            case TeamType.Rilix:
                return Rilix;
            case TeamType.Lumin:
                return Lumin;
            case TeamType.Folor:
                return Folor;
            case TeamType.Xalor:
                return Xalor;
            case TeamType.Spirax:
                return Spirax;
            default:
                return Color.white;
        }
    }

    // each player sees their own side and the enemy's in the colors they picked (blue and red unless changed)
    public static Color Friendly => SideColors.Player;
    public static Color Enemy => SideColors.Enemy;

    public static Color GetSideColor(Team team) {
        return team.isPlayer ? Friendly : Enemy;
    }
}

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
    public GameObject screenshotCameraPrefab;
}

[Serializable]
public class Team {
    public TeamType teamType;
    public TeamBase teamBase;
    public bool isPlayer;
}

[RequireComponent(typeof(ShipManager))]
public class TeamManager : MonoBehaviour {
    [SerializeField]
    private PregameManager pregameManager;

    public List<TerrainBlock> terrainBlocks;

    public List<Team> teams;

    // the tile edges glow in each side's color, your own side brighter
    [SerializeField]
    private float edgeGlow = 2.4f;
    [SerializeField]
    private float enemyEdgeGlow = 1f;
    
    public ShipManager shipManager { get; private set; }

    private Color EdgeColor(Team team) {
        var color = TeamColor.GetSideColor(team) * (team.isPlayer ? edgeGlow : enemyEdgeGlow);
        color.a = 1;
        return color;
    }

    void Awake() {
        shipManager = GetComponent<ShipManager>();
    }

    void Start() {
        if (!PhotonNetwork.InRoom) {
            TurnManager.instance.LoadTeams(teams);
            TurnManager.instance.RandomizeTeams();
            TurnManager.instance.currentTeam = TurnManager.instance.playerTeam;
            Colorize();
            GenerateTerrain();
            GenerateShips();
            Debug.Log("Done loading");
            TurnManager.instance.loading = false;

            pregameManager.TryStartCountdown();
        }
    }

    public void GenerateTerrain() {
        var availableBlocks = new List<TerrainBlock>(terrainBlocks);

        foreach (var team in teams) {
            var index = Random.Range(0, availableBlocks.Count);
            var block = availableBlocks[index];
            availableBlocks.RemoveAt(index);
            
            team.teamBase.SetTerrain(block, Random.Range(0, int.MaxValue));
        }
    }

    public void GenerateShips() {
        foreach (var team in teams) {
            shipManager.GenerateShips(team);
        }

        shipManager.EnableShips();
    }

    public void Colorize() {
        foreach (var team in teams) {
            foreach (var hex in team.teamBase.hexMap.hexes) {
                var hr = hex.hexRenderer;
                hr.SetColor(Color.Lerp(TeamColor.GetSideColor(team), AttackColor.none, 0.5f));
                hr.SetFogColorInstant(FogColor.Normal);
            }

            team.teamBase.hexMap.UseGlowTiles(Resources.Load<Material>("Hex Tile Glow"), EdgeColor(team));
        }
    }
}