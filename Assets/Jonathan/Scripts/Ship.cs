using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public enum Rotation {
    One = 0,
    Two = 60,
    Three = -60,
}

[Serializable]
public enum IndexRotation {
    One = 0,
    Two = 1,
    Three = 2,
}

[Serializable]
public class AttackDirection {
    public Rotation rotation;
    public bool reverse;
    public bool noRange;
    [MyBox.ConditionalField(nameof(noRange), true)]
    public int length;
    public bool stopAtHit;
}

[Serializable]
public class AttackPattern {
    public string name;
    public AttackDirection[] directions;
    public bool doOffset;
    [MyBox.ConditionalField(nameof(doOffset), true)]
    public bool attackCenter;
    [MyBox.ConditionalField(nameof(doOffset), false)]
    public bool autoOptimize;
    public bool isInstant;
}

[Serializable]
public class AttackInfo {
    public string name;
    public bool unlimited;
    [MyBox.ConditionalField(nameof(unlimited), true)]
    public int maxUses;
    public bool isScan;
    public AttackPattern[] options;
    public bool isAllowed = true;
}

[Serializable]
public class ShipModel {
    public string name;
    public string attackName;
    public GameObject shipPrefab;
    public Sprite display;
    public int length;
    public AttackInfo[] attacks;
    
    public int CompareTo(ShipModel other) {
        return length.CompareTo(other.length);
    }
}

[Serializable]
public class Attack {
    public AttackInfo info;
    public int ammoLeft;
}

public class NetworkingShip {
    public string shipModelName;
    public int teamTypeIndex;
    public int rotation;
    public bool reverse;
    public int hexIndex;
}

[RequireComponent(typeof(CapsuleCollider))]
public class Ship : MonoBehaviour
{
    // Info
    public ShipModel shipModel;
    public Team team;
    public Rotation rotation;
    public bool reverse;
    public Attack[] attacks;
    public Attack[] remainingAttacks {
        get {
            var list = new List<Attack>();
            foreach (var attack in attacks) {
                if (attack.ammoLeft > 0 || attack.info.unlimited) {
                    list.Add(attack);
                }
            }

            return list.ToArray();
        }
    }
    public bool isPlayer {
        get {
            return team.isPlayer;
        }
    }
    public bool hasAmmoLeft {
        get {
            return remainingAttacks.Length > 0;
        }
    }

    // Segments
    public ShipSegment[] segments;
    public ShipSegment[] aliveSegments {
        get {
            var list = new List<ShipSegment>();
            foreach (var segment in segments) {
                if (segment.isAlive) {
                    list.Add(segment);
                }
            }

            return list.ToArray();
        }
    }
    public bool isAlive {
        get {
            return aliveSegments.Length > 0;
        }
    }

    // Data
    public GridUnit gridRef;
    public ShipInternals internals;

    public void EnableShip() {
        if (segments.Length != shipModel.length) {
            LoadSegments();
        }

        attacks = new Attack[shipModel.attacks.Length];

        for (int i = 0; i < shipModel.attacks.Length; i++) {
            attacks[i] = new Attack() {
                info = shipModel.attacks[i],
                ammoLeft = shipModel.attacks[i].unlimited ? -1 : shipModel.attacks[i].maxUses
            };
        }

        transform.name = (team.isPlayer ? "Player" : "Enemy") + " " + shipModel.name;

        transform.parent = team.teamBase.transform;

        internals = GetComponentInChildren<ShipInternals>();

        UpdateVisibility();
    }

    public void LoadSegments() {
        segments = GetComponentsInChildren<ShipSegment>();

        foreach (var segment in segments) {
            segment.parent = this;
            segment.isAlive = true;
        }
    }

    public bool SetOnGrid(GridUnit grid) {
        if (segments.Length != shipModel.length) {
            LoadSegments();
        }
  
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

            if (closestTo.shipSegment != null || dist > grid.hexRenderer.hexMap.size * 1.5f || closestTo.preventShips) {
                valid = false;
                break;
            }
        }

        if (valid) {
            foreach (var segment in segments) {
                var closestTo = hex.hexMap.ClosestTo(segment.transform.position, out float dist);

                segment.gridRef = closestTo;
                closestTo.shipSegment = segment;
            }
        }

        return valid;
    }

    public bool UpdateVisibility() {
        // in order to be hidden, the ship must not be a player ship, and must not have any alive segments
        var hidden = !isPlayer;
        var destroyed = !isAlive;

        if (internals == null) {
            return false;
        }
        
        internals.shipMesh.SetActive(false);
        internals.sunkMesh.SetActive(false);
        
        if (destroyed) {
            internals.sunkMesh.SetActive(true);
            
            foreach (var segment in segments) {
                segment.isAlive = false;

                segment.gridRef.hexRenderer.ClearFog();

                segment.gridRef.hexRenderer.DisableFlames();
            }

        } else if (!destroyed && !hidden) {
            internals.shipMesh.SetActive(true);
        }

        return destroyed;
    }
}
