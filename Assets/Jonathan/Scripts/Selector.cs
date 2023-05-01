using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MyBox;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;

[RequireComponent(typeof(AttackUIManager))]
public class Selector : MonoBehaviour
{
    public static Selector instance;

    private AttackUIManager attackUIManager;
    
    [Header("Config")]
    public bool allowSelectingGrids;
    public bool allowSelectingShips;

    [Header("Networked Highlight")]
    [SerializeField]
    private GameObject hexHighlightPrefab;

    private GameObject playerHexHighlight;
    private HexBorder hexBorder;

    [Header("Display Options")]
    public Color highlightColor;
    public float highlightHeight;
    public Color lockedInColor;
    public float lockedInHeight;

    [Header("Hex Layers")]
    [Layer]
    public int friendlyHexLayer;
    [Layer]
    public int enemyHexLayer;
    
    [Header("Ship Layers")]
    [Layer]
    public int shipLayer;
    [Layer]
    public int outlineLayer;
    [Layer]
    public int deadLayer;

    public Ship selectedShip { get; private set; }
    
    public HexRenderer selectedHex { get; private set; }
    private GameObject hiddenFog;

    bool mouseMoved;
    int lastMovedFrames;

    TeamBase playerTeamBase;

    void Awake() {
        if (instance == null) {
            instance = this;
        } else {
            Destroy(gameObject);
        }
        
        attackUIManager = GetComponent<AttackUIManager>();

        if (PhotonNetwork.InRoom) {
            playerHexHighlight = PhotonNetwork.Instantiate(hexHighlightPrefab.name, Vector3.zero, Quaternion.identity);
        }
        else {
            playerHexHighlight = Instantiate(hexHighlightPrefab);
            playerHexHighlight.name = "Player Hex Highlight";
        }
        
        hexBorder = playerHexHighlight.GetComponent<HexBorder>();
    }

    void Start() {
        lastMovedFrames = 0;
        mouseMoved = false;
    }

    public void SetTeam(Team team, TeamBase over) {
        if (selectedShip != null) {
            SetLayerAllChildren(selectedShip.transform, outlineLayer, shipLayer);
            selectedShip = null;
        }

        if (selectedHex != null) {
            selectedHex = null;
        }

        if (hiddenFog != null) {
            hiddenFog.SetActive(true);
            hiddenFog = null;
        }

        hexBorder.SetColor(highlightColor);
        hexBorder.SetHeight(highlightHeight);
        
        hexBorder.SetVisibility(false);

        playerHexHighlight.transform.SetParent(over.transform);
        playerHexHighlight.transform.localPosition = Vector3.zero;
    }

    void Update() {
        mouseMoved = Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0;

        if (mouseMoved) {
            lastMovedFrames = 0;
        } else {
            lastMovedFrames++;
        }

        if (IsPointerOverUIObject() || !TurnManager.instance.gameActive) {
            return;
        }
        
        if (allowSelectingGrids) {
            SelectGrids();
        }

        if (allowSelectingShips) {
            SelectShips();
        }
    }

    public void SelectGrids() {
        HexRenderer hex = null;
        if (lastMovedFrames < (1/Time.deltaTime) * 10f && selectedHex == null) {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 1000, 1 << enemyHexLayer)) {
                hex = hit.transform.GetComponent<HexRenderer>();

                // Show previously hidden fog
                if (hiddenFog != null) {
                    hiddenFog.SetActive(true);
                }
                
                // Hide new fog
                hiddenFog = hex.fog.ps.gameObject;
                hiddenFog.SetActive(false);

                hexBorder.SetVisibility(true);
                playerHexHighlight.transform.position = hex.transform.position;
            } else {
                hexBorder.SetVisibility(false);

                // Show previously hidden fog
                if (hiddenFog != null) {
                    hiddenFog.SetActive(true);
                    hiddenFog = null;
                }
            }
        }
        
        if (Input.GetMouseButtonDown(0) && hexBorder.isVisible) {
            if (selectedHex != null) {
                if (hex == selectedHex) {
                    selectedHex = null;
                    hexBorder.SetColor(highlightColor);
                    hexBorder.SetHeight(highlightHeight);
                    hex.RemoveFogOverrideColor();
                }
            }
            
            if (selectedHex == null && hex != null) {
                selectedHex = hex;
                hexBorder.SetColor(lockedInColor);
                hexBorder.SetHeight(lockedInHeight);

                // remove fog hide
                if (hiddenFog != null) {
                    hiddenFog.SetActive(true);
                    hiddenFog = null;
                }

                hex.SetFogColorInstant(FogColor.Selected, true);
            }

            if (hex != null) {
                attackUIManager.SelectTarget(hex.gridRef);
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

    private bool IsPointerOverUIObject() {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        
        for (int i = results.Count - 1; i >= 0; i--) {
            if (results[i].gameObject.GetComponent<Button>() == null) {
                results.RemoveAt(i);
            }
        }

        return results.Count > 0;
    }

    // from https://forum.unity.com/threads/help-with-layer-change-in-all-children.779147/
    public static void SetLayerAllChildren(Transform root, int? from, int layer) {
        var children = root.GetComponentsInChildren<Transform>(includeInactive: true);
        foreach (var child in children) {
            if (from == null) {
                if (child.gameObject.layer == instance.outlineLayer || child.gameObject.layer == instance.shipLayer) {
                    child.gameObject.layer = layer;
                }
            }
            else {
                if (child.gameObject.layer == from) {
                    child.gameObject.layer = layer;
                }
            }
        }
    }
}
