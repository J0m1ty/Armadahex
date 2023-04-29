using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class HighligherSync : MonoBehaviourPun, IPunObservable, IPunInstantiateMagicCallback {
    private HexBorder hexBorder;

    [SerializeField]
    private Vector3 networkPosition;
    [SerializeField]
    private float networkHeight;
    [SerializeField]
    private int networkColorId;
    [SerializeField]
    private int teamTypeOfBaseToBeOverId;
    [SerializeField]
    private bool isVisible;

    [SerializeField]
    private string ownerName;
    
    int ColorToId(Color color) {
        if (color == Selector.instance.highlightColor)
            return 0;
        if (color == Selector.instance.lockedInColor)
            return 1;
        return -1;
    }

    Color IdToColor(int id) {
        if (id == 0)
            return Selector.instance.highlightColor;
        if (id == 1)
            return Selector.instance.lockedInColor;
        return Color.black;
    }

    TeamType? TeamBaseToOwnerTeamType(TeamBase teamBase) {
        if (teamBase == TurnManager.instance?.enemyTeam?.teamBase)
            return TurnManager.instance.enemyTeam.teamType;
        if (teamBase == TurnManager.instance?.playerTeam?.teamBase)
            return TurnManager.instance.playerTeam.teamType;
        return null;
    }

    TeamBase TeamTypeToTeamBase(TeamType teamType) {
        if (teamType == TurnManager.instance?.enemyTeam?.teamType)
            return TurnManager.instance.enemyTeam.teamBase;
        if (teamType == TurnManager.instance?.playerTeam?.teamType)
            return TurnManager.instance.playerTeam.teamBase;
        return null;
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info) {
        gameObject.name = info.Sender.NickName + "'s Highlight " + "(" + (photonView.IsMine ? "Player" : "Enemy") + ")";
    }

    public void Awake() {
        hexBorder = GetComponent<HexBorder>();
    }

    void Update() {
        if (photonView.IsMine) {
            TeamBase teamBase = null;
            
            try {
                teamBase = transform.GetParentComponent<TeamBase>();
            }
            catch {
                teamBase = null;
            }

            if (teamBase != null) {
                TeamType? teamType = TeamBaseToOwnerTeamType(teamBase);

                if (teamBase != null) {
                    networkPosition = transform.localPosition;
                    networkHeight = hexBorder.height;
                    networkColorId = ColorToId(hexBorder.GetColor());
                    teamTypeOfBaseToBeOverId = (int)teamType;
                    isVisible = hexBorder.isVisible;
                }
            }
        }
        else {
            var teamBase = TeamTypeToTeamBase((TeamType)teamTypeOfBaseToBeOverId);

            if (teamBase == null) {
                hexBorder.SetVisibility(false);
                return;
            }
            
            transform.localPosition = networkPosition;
            hexBorder.SetHeight(networkHeight);
            hexBorder.SetColor(IdToColor(networkColorId));
            transform.SetParent(teamBase.transform);
            hexBorder.SetVisibility(isVisible);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        // we need to sync the local position and parent of transform, height, color of the hexBorder
        if (stream.IsWriting && photonView.IsMine) {
            stream.SendNext(networkPosition);
            stream.SendNext(networkHeight);
            stream.SendNext(networkColorId);
            stream.SendNext(teamTypeOfBaseToBeOverId);
            stream.SendNext(isVisible);
        }
        else if (stream.IsReading) {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkHeight = (float)stream.ReceiveNext();
            networkColorId = (int)stream.ReceiveNext();
            teamTypeOfBaseToBeOverId = (int)stream.ReceiveNext();
            isVisible = (bool)stream.ReceiveNext();
        }
    }
}
