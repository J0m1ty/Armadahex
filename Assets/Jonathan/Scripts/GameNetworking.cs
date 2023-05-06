using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExitGames.Client.Photon;
using Photon.Realtime;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System.Linq;
using System;
using Random = UnityEngine.Random;

public class GameNetworking : MonoBehaviourPunCallbacks {
    public static GameNetworking instance;

    public GameMode gameMode { get; private set; }

    [SerializeField]
    private TeamManager teamManager;

    [SerializeField]
    private PregameManager pregameManager;

    [SerializeField]
    private ShipManager shipManager;

    [SerializeField]
    private AttackUIManager attackManager;
    
    public TeamType firstTeam { get; private set; }

    public bool localDoneLoading = false;
    public bool enemyDoneLoading = false;

    [SerializeField]
    private bool networkingLoaded = false;
    [SerializeField]
    private bool displayLoaded = false;

    void Awake() {
        if (instance == null) {
            instance = this;
        } else {
            Destroy(this);
        }

        localDoneLoading = false;
        enemyDoneLoading = false;
        networkingLoaded = false;
        displayLoaded = false;

        if (!PlayerPrefs.HasKey(Constants.GAME_MODE_PREF_KEY) || !PhotonNetwork.InRoom) {
            PlayerPrefs.SetInt(Constants.GAME_MODE_PREF_KEY, (int)GameMode.Customs);
        }

        gameMode = (GameMode)PlayerPrefs.GetInt(Constants.GAME_MODE_PREF_KEY);

        Debug.Log("Game mode is " + gameMode);

        if (gameMode == GameMode.Customs) {
            GameModeInfo.instance.SetCustomWithPrefs();

            if (GameModeInfo.instance.IsSingleplayer) {
                gameObject.AddComponent<AudioListener>();
            }
        }

        PlayerPrefs.DeleteKey(Constants.GAME_MODE_PREF_KEY);
    }

    void Start() {
        if (PhotonNetwork.IsMasterClient) {
            Debug.Log("Started init process as master");

            // set custom properties for the room, master is random TeamType and client is random TeamType
            var roomProps = new Hashtable();

            var availableTeams = new List<TeamType>(Enum.GetValues(typeof(TeamType)).Cast<TeamType>());
            var masterTeam = availableTeams[Random.Range(0, availableTeams.Count)];
            roomProps.Add("MasterTeam", masterTeam);
            availableTeams.Remove(masterTeam);
            var clientTeam = availableTeams[Random.Range(0, availableTeams.Count)];
            roomProps.Add("ClientTeam", clientTeam);

            var availableTerrain = new List<TerrainBlock>(teamManager.terrainBlocks);
            var masterTerrain = availableTerrain[Random.Range(0, availableTerrain.Count)];
            roomProps.Add("MasterTerrain", masterTerrain.name);
            availableTerrain.Remove(masterTerrain);
            var clientTerrain = availableTerrain[Random.Range(0, availableTerrain.Count)];
            roomProps.Add("ClientTerrain", clientTerrain.name);
            
            var firstTeamToGo = Random.Range(0, 2) == 0 ? masterTeam : clientTeam;
            roomProps.Add("FirstTeam", firstTeamToGo);

            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        }

        // get GAME_MODE_PROP_KEY prop from room
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(Constants.GAME_MODE_PROP_KEY)) {
            Enum.TryParse((string)PhotonNetwork.CurrentRoom.CustomProperties[Constants.GAME_MODE_PROP_KEY], out GameMode setGameMode);
            if (gameMode == setGameMode) {
                Debug.Log("Game mode confirmed to be " + gameMode);
            }
            else {
                Debug.LogError("Game mode mismatch, expected " + gameMode + " but got " + setGameMode);
            }
        }

        attackManager.OnAttack += OnAttack;

        TurnManager.instance.OnTurnOver += AdvanceTurn;

        GameOver.instance.OnGameOver += OnGameOver;

        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount > 1) {
            GameOver.instance.enemyName = PhotonNetwork.CurrentRoom.Players.Where(p => p.Value != PhotonNetwork.LocalPlayer).First().Value.NickName;
        }
        else if (GameModeInfo.instance.IsSingleplayer) {
            GameOver.instance.enemyName = "Enemy Bot";
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) {
        if (propertiesThatChanged.ContainsKey("MasterTeam") 
            && propertiesThatChanged.ContainsKey("ClientTeam")
            && propertiesThatChanged.ContainsKey("MasterTerrain")
            && propertiesThatChanged.ContainsKey("ClientTerrain")
            && propertiesThatChanged.ContainsKey("FirstTeam")) {
                
            Debug.Log("Initializing teams and terrain");

            var masterTeam = (TeamType)propertiesThatChanged["MasterTeam"];
            var clientTeam = (TeamType)propertiesThatChanged["ClientTeam"];

            var masterTerrain = teamManager.terrainBlocks.Find(t => t.name == (string)propertiesThatChanged["MasterTerrain"]);
            var clientTerrain = teamManager.terrainBlocks.Find(t => t.name == (string)propertiesThatChanged["ClientTerrain"]);

            var playerTeam = PhotonNetwork.IsMasterClient ? masterTeam : clientTeam;
            foreach (var team in teamManager.teams) {
                if (team.isPlayer == PhotonNetwork.IsMasterClient) {
                    team.teamType = masterTeam;
                    team.teamBase.SetTerrain(masterTerrain);
                } else if (team.isPlayer != PhotonNetwork.IsMasterClient) {
                    team.teamType = clientTeam;
                    team.teamBase.SetTerrain(clientTerrain);
                }
            }

            TurnManager.instance.LoadTeams(teamManager.teams);
            firstTeam = (TeamType)propertiesThatChanged["FirstTeam"];
            
            teamManager.Colorize();
            
            var ships = shipManager.GenerateShips(teamManager.teams.Find(t => t.teamType == playerTeam));
            photonView.RPC("GenerateShipsRPC", RpcTarget.Others, 
                (int)playerTeam,
                ships.Select(ship => ship.shipModelName).Cast<object>().ToArray() as object,
                ships.Select(ship => ship.teamTypeIndex).Cast<object>().ToArray() as object,
                ships.Select(ship => ship.rotation).Cast<object>().ToArray() as object,
                ships.Select(ship => ship.reverse).Cast<object>().ToArray() as object,
                ships.Select(ship => ship.hexIndex).Cast<object>().ToArray() as object
            );
        }
    }

    [PunRPC]
    public void GenerateShipsRPC(int teamType, object[] shipModelNames, object[] teamTypeIndices, object[] rotations, object[] reverses, object[] hexIndices) {
        Debug.Log("Loading enemy ships");

        var ships = new List<NetworkingShip>();
        for (int i = 0; i < shipModelNames.Length; i++) {
            ships.Add(new NetworkingShip {
                shipModelName = (string)shipModelNames[i],
                teamTypeIndex = (int)teamTypeIndices[i],
                rotation = (int)rotations[i],
                reverse = (bool)reverses[i],
                hexIndex = (int)hexIndices[i]
            });
        }

        shipManager.GenerateShipsFromData(ships);
        shipManager.EnableShips();

        networkingLoaded = true;

        if (displayLoaded) {
            OnDoneLoading();
        }
    }

    public void DisplayLoaded() {
        displayLoaded = true;

        if (networkingLoaded) {
            OnDoneLoading();
        }
    }

    public void OnDoneLoading() {
        if (!PhotonNetwork.IsConnectedAndReady) return;

        photonView.RPC("OnDoneLoadingRPC", RpcTarget.All);
    }

    [PunRPC]
    public void OnDoneLoadingRPC(PhotonMessageInfo info) {
        if (info.Sender == PhotonNetwork.LocalPlayer) {
            localDoneLoading = true;
        } else {
            enemyDoneLoading = true;
        }

        if (localDoneLoading && enemyDoneLoading) {
            Debug.Log("Done loading");

            TurnManager.instance.loading = false;
            
            pregameManager.TryStartCountdown();
        }
    }

    public void OnAttack(Team against, bool hit, int hexIndex, bool finalAttack) {
        if (!PhotonNetwork.IsConnectedAndReady) return;

        photonView.RPC("OnAttackRPC", RpcTarget.Others, (int)against.teamType, hit, hexIndex, finalAttack);
    }

    [PunRPC]
    public void OnAttackRPC(int against, bool hit, int hexIndex, bool finalAttack) {
        attackManager.GetAttackFromEnemy((TeamType)against, hexIndex, finalAttack);
    }

    public void AdvanceTurn(Team newTeam) {
        if (!PhotonNetwork.IsConnectedAndReady) return;

        photonView.RPC("AdvanceTurnRPC", RpcTarget.Others, (int)newTeam.teamType);
    }

    [PunRPC]
    public void AdvanceTurnRPC(int newTeam) {
        TurnManager.instance.SetTurn(teamManager.teams.Find(t => t.teamType == (TeamType)newTeam));
    }

    public void ContinueTurn(int currentCountdown) {
        if (!PhotonNetwork.IsConnectedAndReady) return;

        Debug.Log("Sending continue turn RPC with countdown " + currentCountdown);

        photonView.RPC("ContinueTurnRPC", RpcTarget.All, currentCountdown);
    }

    [PunRPC]
    public void ContinueTurnRPC(int currentCountdown) {
        TurnManager.instance.ContinueTurn(false, currentCountdown);
    }

    public void OnGameOver(TeamType winner, WinType winType) {
        if (!PhotonNetwork.IsConnectedAndReady) return;
        
        photonView.RPC("OnWinRPC", RpcTarget.Others, (int)winner, (int)winType);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer) {
        GameOver.instance.EnemyAbandonment();
        PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);
    }

    [PunRPC]
    public void OnWinRPC(int winner, int winType) {
        GameOver.instance.OnWin((TeamType)winner, (WinType)winType);
    }
}
