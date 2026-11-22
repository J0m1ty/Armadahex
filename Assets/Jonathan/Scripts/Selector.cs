using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MyBox;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
    private CloudFog hoveredClouds;

    public HexBorder border {
        get { return hexBorder; }
    }

    bool mouseMoved;
    int lastMovedFrames;

    private InputAction pointAction;
    private InputAction selectAction;
    private Vector2 pointer;

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
        // stay hidden until the mouse is over a hex
        hexBorder.SetVisibility(false);
    }

    void Start() {
        lastMovedFrames = 0;
        mouseMoved = false;

        pointAction = GameInput.Get("Gameplay/Point");
        selectAction = GameInput.Get("Gameplay/Select");
        pointer = pointAction.ReadValue<Vector2>();
    }

    public void ClearSelection() {
        if (selectedShip != null) {
            SetLayerAllChildren(selectedShip.transform, outlineLayer, shipLayer);
            selectedShip = null;
        }

        if (selectedHex != null) {
            selectedHex = null;
        }

        HoverClouds(null);

        hexBorder.SetColor(highlightColor);
        hexBorder.SetHeight(highlightHeight);
        
        hexBorder.SetVisibility(false);
    }

    public void SetTeam(Team team, TeamBase over) {
        ClearSelection();

        playerHexHighlight.transform.SetParent(over.transform);
        playerHexHighlight.transform.localPosition = Vector3.zero;
    }


    void Update() {
        Vector2 lastPointer = pointer;
        pointer = pointAction.ReadValue<Vector2>();
        mouseMoved = pointer != lastPointer;

        if (mouseMoved) {
            lastMovedFrames = 0;
        } else {
            lastMovedFrames++;
        }

        if (IsPointerOverUIObject() || TurnManager.instance == null || !TurnManager.instance.gameActive) {
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
        if (lastMovedFrames < (1/Time.deltaTime) * 10f) {
            RaycastHit hit;
            var ray = Camera.main.ScreenPointToRay(pointer);
            // the clouds are what you see on the enemy board, so pick the hex under them first
            hex = CloudHex(ray);
            if (hex == null && Physics.Raycast(ray, out hit, 1000, 1 << enemyHexLayer)) {
                hex = hit.transform.GetComponent<HexRenderer>();
            }

            // once a shot is locked the ring stays on it, but another hex can still be clicked
            if (selectedHex == null) {
                if (hex != null) {
                    // part the clouds over the hovered hex
                    HoverClouds(hex);

                    hexBorder.SetVisibility(true);
                    playerHexHighlight.transform.position = RingPosition(hex);
                } else {
                    hexBorder.SetVisibility(false);
                    HoverClouds(null);
                }

                // the pattern preview follows the mouse while aiming
                attackUIManager.AimAt(hex != null ? hex.gridRef : null);
            }
        }

        if (selectAction.WasPressedThisFrame() && hex != null) {
            // clicking the locked hex again unlocks it
            if (hex == selectedHex) {
                attackUIManager.Unlock();
                return;
            }

            // a lock parts the clouds over the hex and puts up the tall beam
            if (selectedHex != null) selectedHex.RemoveFogOverrideColor();
            selectedHex = hex;
            HoverClouds(hex);
            playerHexHighlight.transform.position = RingPosition(hex);
            hexBorder.SetVisibility(true);
            hexBorder.SetColor(lockedInColor);
            hexBorder.SetHeight(lockedInHeight);
            hex.SetFogColorInstant(FogColor.Selected, true);

            attackUIManager.SelectTarget(hex.gridRef);
        }
    }

    // roughly where the cloud tops read over a hex, as a height above the board
    private static float CloudSurface(CloudFog clouds, float cover) {
        return Mathf.Lerp(clouds.cloudBase, clouds.cloudTop, 0.75f * cover);
    }

    // over the clouds the ring sits on their tops, so it shows where the mouse is from any angle
    private Vector3 RingPosition(HexRenderer hex) {
        var position = hex.transform.position;
        var clouds = hex.hexMap.clouds;
        if (clouds != null && hex.fog != null && !hex.fog.cleared && hex.fog.cover > 0.5f) {
            position.y = hex.hexMap.transform.position.y + CloudSurface(clouds, hex.fog.cover);
        }
        return position;
    }

    // walks the ray down through the cloud layer and returns the first hex still covered by its cloud
    private HexRenderer CloudHex(Ray ray) {
        var team = TurnManager.instance.enemyTeam;
        if (team == null) return null;

        var grid = team.teamBase.hexMap;
        var clouds = grid.clouds;
        if (clouds == null || ray.direction.y >= 0f) return null;

        var baseY = grid.transform.position.y;
        var top = ray.GetPoint((baseY + clouds.cloudTop - ray.origin.y) / ray.direction.y);
        var bottom = ray.GetPoint((baseY + clouds.cloudBase - ray.origin.y) / ray.direction.y);

        var steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(top, bottom) / (grid.size * 0.15f)));
        for (var i = 0; i <= steps; i++) {
            var point = Vector3.Lerp(top, bottom, i / (float)steps);

            HexRenderer nearest = null;
            var best = grid.size * grid.size;
            foreach (var unit in grid.hexes) {
                var hex = unit.hexRenderer;
                if (hex == null || !hex.gameObject.activeInHierarchy) continue;

                var offset = hex.transform.position - point;
                offset.y = 0f;
                if (offset.sqrMagnitude < best) {
                    best = offset.sqrMagnitude;
                    nearest = hex;
                }
            }

            // a hex only counts once the ray is inside its cloud, whose visible top sits lower the thinner it is
            if (nearest != null && nearest.fog != null && !nearest.fog.cleared && nearest.fog.cover > 0.5f
                && point.y <= baseY + CloudSurface(clouds, nearest.fog.cover)) {
                return nearest;
            }
        }
        return null;
    }

    private void HoverClouds(HexRenderer hex) {
        var clouds = hex != null ? hex.hexMap.clouds : null;

        if (hoveredClouds != null && hoveredClouds != clouds) {
            hoveredClouds.SetHover(null);
        }

        hoveredClouds = clouds;
        if (clouds != null) {
            clouds.SetHover(hex);
        }
    }
    
    // check if the mouse is over a ship either on the ship layer or the outline layer
    // move the ship to the outline layer if it is
    // store the ship in selectedShip
    // if no ship is selected or the mouse is over a different ship, move the old ship back to the ship layer

    public void SelectShips() {
        if (lastMovedFrames < (1/Time.deltaTime) * 10f) {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(pointer), out hit, 1000, 1 << shipLayer | 1 << outlineLayer)) {
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
        
        if (selectAction.WasPressedThisFrame()) {
            if (selectedShip != null) {
                attackUIManager.SelectShip(selectedShip);
            }
        }
    }

    private bool IsPointerOverUIObject() {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = pointer;
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
