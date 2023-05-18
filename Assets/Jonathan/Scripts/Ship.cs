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
public enum ShipType {
    Carrier,
    Battleship,
    Cruiser,
    Submarine,
    Destroyer,
    Aircraft
}


[Serializable]
public class ShipModel {
    public string name;
    public string attackName;
    public ShipType type;
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
                if ((attack.ammoLeft > 0 || attack.info.unlimited) && attack.info.isAllowed) {
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
    public bool sink;
    public float beforeSink;
    public float sinkTo;
    public bool sinkReverse;

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

    // Buoyancy for sinking
    private Buoyancy _buoyancy;
    public Buoyancy buoyancy {
        get {
            if (_buoyancy == null) {
                _buoyancy = GetComponent<Buoyancy>();
            }

            return _buoyancy;
        }
    }

    private Rigidbody _rb;
    public Rigidbody rb {
        get {
            if (_rb == null) {
                _rb = GetComponent<Rigidbody>();
            }

            return _rb;
        }
    }

    // Data
    private bool setToSunk;
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
                ammoLeft = shipModel.attacks[i].unlimited ? -1 : shipModel.attacks[i].maxUses,
            };

            attacks[i].info.isAllowed = true;

            if (!GameModeInfo.instance.IsAdvancedCombat) {
                if (shipModel.attacks[i].name != "Single Shot") {
                    attacks[i].info.isAllowed = false;
                }
            }

            if (GameModeInfo.instance.IsUnlimitedAmmo) {
                attacks[i].ammoLeft = -1;
                attacks[i].info.unlimited = true;
            }
        }

        transform.name = (team.isPlayer ? "Player" : "Enemy") + " " + shipModel.name;

        transform.parent = team.teamBase.transform;

        internals = GetComponentInChildren<ShipInternals>();

        setToSunk = false;

        UpdateVisibility();
    }

    public void LoadSegments() {
        segments = GetComponentsInChildren<ShipSegment>();

        foreach (var segment in segments) {
            segment.parent = this;
            segment.isAlive = true;
            if (segment.flames != null)
            {
                segment.flames.SetActive(false);
            }
        }
    }

    public bool SetOnGrid(GridUnit grid) {
        if (segments.Length != shipModel.length) {
            LoadSegments();
        }
  
        gridRef = grid;

        var hex = gridRef.hexRenderer;

        transform.position = hex.transform.position + (Vector3.up * 3f);
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
        
        internals.shipMesh.SetActive(true);
        internals.sunkVFX.SetActive(false);

        // rb.isKinematic = true;
        // rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationZ;
        sink = false;

        if (destroyed) {
            if (!setToSunk && !GameOver.instance.CheckIfGameOver()) {
                AudioManager.instance?.PlayShipSound(shipModel.type, isPlayer, 6f);

                setToSunk = true;
            }
            
            internals.sunkVFX.SetActive(true);

            // rb.isKinematic = false;
            // rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
            sink = true;
            
            foreach (var segment in segments) {
                segment.isAlive = false;

                segment.gridRef.hexRenderer.ClearFog();

                segment.gridRef.hexRenderer.DisableFlames();
            }

            Selector.SetLayerAllChildren(transform, null, Selector.instance.deadLayer);
        }
        
        if (hidden && !destroyed) {
            internals.shipMesh.SetActive(false);
        }

        return destroyed;
    }

    public float sinkSpeed = 2f;
    private float sinkDuration;
    void Update() {
        if (sink) {
            sinkDuration += Time.deltaTime;

            // rotate on the z-axis towards 45 degrees down, keep the x and y rotation the same
            //transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, transform.localRotation.eulerAngles.y, Mathf.LerpAngle(transform.localRotation.eulerAngles.z, (90f + 15f) * (sinkReverse ? -1 : 1), Time.deltaTime * 0.5f));

            // sink the ship
            //transform.localPosition = new Vector3(transform.localPosition.x, Mathf.Lerp(transform.localPosition.y, beforeSink - sinkTo, Time.deltaTime * 0.5f), transform.localPosition.z);

            // smoothly rotate the ship to 45 degrees down, using smoothstep
            transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, transform.localRotation.eulerAngles.y, Mathf.LerpAngle(transform.localRotation.eulerAngles.z, (90f + 15f) * (sinkReverse ? -1 : 1), Mathf.SmoothStep(0f, 1f, sinkDuration / sinkSpeed)));

            // sink the ship
            transform.localPosition = new Vector3(transform.localPosition.x, Mathf.Lerp(transform.localPosition.y, beforeSink - sinkTo, Mathf.SmoothStep(0f, 1f, sinkDuration / sinkSpeed)), transform.localPosition.z);

        }
        else {
            sinkDuration = 0f;
            beforeSink = transform.localPosition.y;
        }
    }
}
