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
    public GameObject shipPrefab;
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

    void Start() {
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

        transform.name = shipModel.name;
    }

    public void LoadSegments() {
        segments = GetComponentsInChildren<ShipSegment>();

        foreach (var segment in segments) {
            segment.parent = this;
        }
    }

    public void UpdateStatus() {
        // check if ship is dead
        var alive = false;
        for (int i = 0; i < segments.Length; i++) {
            if (segments[i].isAlive) {
                alive = true;
            }
        }
        
        if (!alive) {
            // destroy ship
            Destroy(gameObject);

            // TODO: add death animation
            // clear fog of war
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

            segment.gridRef = closestTo;
            closestTo.shipSegment = segment;
        }

        return valid;
    }
}
