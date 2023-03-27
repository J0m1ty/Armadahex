using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public enum TeamType {
    Blue,
    Red
}

[Serializable]
public class Team {
    public TeamType type;
    public HexGrid map;
    public bool isPlayer;
}

[Serializable]
public enum Rotation {
    One = 0,
    Two = 60,
    Three = -60,
}

[Serializable]
public class ShipModel {
    public string name;
    public GameObject shipPrefab;
}

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class Ship : MonoBehaviour
{
    // Info
    public ShipModel shipModel;
    public Team team;
    public Rotation rotation;
    public bool reverse;

    // Segments
    public ShipSegment[] segments;

    // Data
    public GridUnit gridRef;

    void Awake() {
        segments = GetComponentsInChildren<ShipSegment>();

        foreach (var segment in segments) {
            segment.parent = this;
        }
    }

    public bool SetOnGrid(GridUnit grid) {
        gridRef = grid;

        var hex = gridRef.hexRenderer;

        transform.position = hex.transform.position;
        transform.rotation = Quaternion.Euler(0, (int)rotation, 0);

        if (reverse) {
            transform.Rotate(0, 180, 0);
        }

        bool valid = true;
        foreach (var segment in segments) {
            var closestTo = hex.hexMap.ClosestTo(segment.transform.position, out float dist);

            if (closestTo.shipSegment != null || dist > grid.hexRenderer.hexMap.size * 1.5f) {
                valid = false;
                break;
            }

            segment.gridRef = closestTo;
            closestTo.shipSegment = segment;
        }

        return valid;
    }
}
