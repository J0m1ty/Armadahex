using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MyBox;

[Serializable]
public class HighlightInfo {
    public GameObject hexHighlight;
    [HideInInspector]
    public HexBorder hexBorder;
    [Layer]
    public int layer;
    public Color color;
    public float height;
    public Color lockedInColor;
    public float lockedInHeight;
    public bool allowedToLockIn = false;
}

[RequireComponent(typeof(AttackUIManager))]
public class Selector : MonoBehaviour
{
    public bool allowSelectingGrids = true;
    public bool allowSelectingShips = true;
    
    [Layer]
    public int shipLayer;
    [Layer]
    public int outlineLayer;

    public Ship selectedShip;

    [MyBox.MustBeAssigned]
    public HighlightInfo friendly;
    [MyBox.MustBeAssigned]
    public HighlightInfo enemy;
    private HighlightInfo current;

    bool mouseMoved;
    int lastMovedFrames;

    bool lockedIn;
    HexRenderer lockedInGrid;

    private AttackUIManager attackUIManager;

    void Awake() {
        friendly.hexBorder = friendly.hexHighlight.GetComponent<HexBorder>();
        enemy.hexBorder = enemy.hexHighlight.GetComponent<HexBorder>();
        attackUIManager = GetComponent<AttackUIManager>();
    }

    void Start()
    {
        friendly.hexHighlight.SetActive(false);
        enemy.hexHighlight.SetActive(false);
        
        lockedIn = false;
        lastMovedFrames = 0;
        mouseMoved = false;
    }

    public void SetTeam(Team team) {
        if (selectedShip != null) {
            SetLayerAllChildren(selectedShip.transform, outlineLayer, shipLayer);
        }

        if (lockedIn) {
            lockedIn = false;
            current.hexBorder.SetColor(current.color);
            current.hexBorder.SetHeight(current.height);
        }

        friendly.hexHighlight.SetActive(false);
        enemy.hexHighlight.SetActive(false);

        if (team.isPlayer) {
            current = friendly;
        } else {
            current = enemy;
        }

        current.hexBorder.SetColor(current.color);
        current.hexBorder.SetHeight(current.height);
        current.hexBorder.hexGrid = team.teamBase.hexMap;
    }

    void Update()
    {
        mouseMoved = Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0;

        if (mouseMoved) {
            lastMovedFrames = 0;
        } else {
            lastMovedFrames++;
        }
        
        if (current == null) {
            return;
        }

        Debug.Log("Current team: " + TurnManager.instance.currentTeam.teamType);

        Debug.Log("Allowing selecting grids: " + allowSelectingGrids);
        
        if (allowSelectingGrids) {
            SelectGrids();
        }

        if (allowSelectingShips) {
            SelectShips();
        }
    }

    public void SelectGrids() {
        if (lastMovedFrames < (1/Time.deltaTime) * 10f && !lockedIn) {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 1000, 1 << current.layer)) {
                var hex = hit.transform.GetComponent<HexRenderer>();

                current.hexHighlight.SetActive(true);
                current.hexHighlight.transform.position = hex.transform.position;
            } else {
                current.hexHighlight.SetActive(false);
            }
        }
        
        if (Input.GetMouseButtonDown(0)) {
            if (current.hexHighlight.activeSelf) {
                RaycastHit hit;
                HexRenderer hex = null;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 1000, 1 << current.layer)) {
                    hex = hit.transform.GetComponent<HexRenderer>();
                    
                    attackUIManager.SelectTarget(hex.gridRef);
                }

                if (lockedIn) {
                    if (hex == lockedInGrid) {
                        lockedIn = false;
                        current.hexBorder.SetColor(current.color);
                        current.hexBorder.SetHeight(current.height);
                    }
                } else {
                    if (current.allowedToLockIn) {
                        lockedIn = true;
                        current.hexBorder.SetColor(current.lockedInColor);
                        lockedInGrid = hex;
                    }
                }
            }
        }
    }
    
    // check if the mouse is over a ship either on the ship layer or the outline layer
    // move the ship to the outline layer if it is
    // store the ship in selectedShip
    // if no ship is selected or the mouse is over a different ship, move the old ship back to the ship layer

    public void SelectShips() {
        if (lastMovedFrames < (1/Time.deltaTime) * 10f) {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 1000, 1 << shipLayer | 1 << outlineLayer)) {
                var ship = hit.transform.GetComponent<Ship>();

                if (ship != selectedShip) {
                    if (selectedShip != null) {
                        SetLayerAllChildren(selectedShip.transform, outlineLayer, shipLayer);
                    }

                    selectedShip = ship;
                    SetLayerAllChildren(selectedShip.transform, shipLayer, outlineLayer);
                }
            } else {
                if (selectedShip != null) {
                    SetLayerAllChildren(selectedShip.transform, outlineLayer, shipLayer);
                    selectedShip = null;
                }
            }
        }
        
        if (Input.GetMouseButtonDown(0)) {
            if (selectedShip != null) {
                attackUIManager.SelectShip(selectedShip);
            }
        }
    }

    // from https://forum.unity.com/threads/help-with-layer-change-in-all-children.779147/
    public void SetLayerAllChildren(Transform root, int from, int layer) {
        var children = root.GetComponentsInChildren<Transform>(includeInactive: true);
        foreach (var child in children) {
            if (child.gameObject.layer == from) {
                child.gameObject.layer = layer;
            }
        }
    }
}
