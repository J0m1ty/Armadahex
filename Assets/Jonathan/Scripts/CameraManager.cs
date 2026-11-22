using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using EZCameraShake;

public enum CameraState {
    Side = 0,
    Top = 1
}

public class CameraManager : MonoBehaviour
{   
    public static CameraManager instance { get; private set; }

    [Header("State Info")]
    [SerializeField]
    private CameraState state;
    [SerializeField]
    private CameraState editorDefaultState;
    [SerializeField]
    private CameraState buildDefaultState;

    [Header("Camera Refereces")]
    [SerializeField]
    private GameObject sideCamera;
    private CameraController sideCameraController;
    [SerializeField]
    private GameObject topCamera;
    private CameraController topCameraController;

    [Header("Hit Shake Settings")]
    [SerializeField]
    private float shakeMagnitude;
    [SerializeField]
    private float shakeRoughness;
    [SerializeField]
    private float shakeFadeInTime;
    [SerializeField]
    private float shakeFadeOutTime;

    [Header("Miss Shake Settings")]
    [SerializeField]
    private float missShakeMagnitude;
    [SerializeField]
    private float missShakeRoughness;
    [SerializeField]
    private float missShakeFadeInTime;
    [SerializeField]
    private float missShakeFadeOutTime;

    private Vector3 centerPos;

    private InputAction switchViewAction;

    // the side view frames what's happening, this is the last thing it looked at
    private Vector3? framePoint;
    private float frameRadius;
    private ShipManager shipManager;


    // shots landing within a moment of each other are one volley
    private Bounds? volley;
    private Coroutine volleyFocus;

    
    #if UNITY_EDITOR
    [Header("Debug Options")]
    public bool hit;
    [MyBox.ButtonMethod]
    public void DebugShake() {
        Shake(hit);
    }
    #endif


    void Awake() {
        if (instance != null) {
            Destroy(gameObject);
            return;
        }

        instance = this;

        sideCameraController = sideCamera.GetComponent<CameraController>();
        topCameraController = topCamera.GetComponent<CameraController>();
        switchViewAction = GameInput.Get("Gameplay/Switch View");

        if (Application.isEditor) {
            SetCameraState(editorDefaultState, false);
        }
        else {
            SetCameraState(buildDefaultState, false);
        }
    }

    void Update() {
        // not during the intro, it still has the rig cameras switched off
        if (switchViewAction.WasPressedThisFrame() && TurnManager.instance != null && TurnManager.instance.gameActive) {
            SetCameraState(state == CameraState.Side ? CameraState.Top : CameraState.Side);
        }
    }

    public void NoteShot(Vector3 position) {
        if (volley == null) {
            volley = new Bounds(position, Vector3.zero);
        } else {
            var area = volley.Value;
            area.Encapsulate(position);
            volley = area;
        }

        if (volleyFocus == null) {
            volleyFocus = StartCoroutine(FocusVolley());
        }
    }

    private IEnumerator FocusVolley() {
        yield return new WaitForSeconds(0.1f);

        var area = volley.Value;
        volley = null;
        volleyFocus = null;

        // the side view keeps looking at the last volley until something else happens
        framePoint = area.center;
        frameRadius = Mathf.Max(area.extents.x, area.extents.z);

        var controller = state == CameraState.Side ? sideCameraController : topCameraController;
        controller.FocusOn(area);
    }

    public void Shake(bool hit) {
        CameraShaker cameraShaker;

        switch (state) {
            case CameraState.Side:
                cameraShaker = sideCamera.GetComponentInChildren<CameraShaker>();
                break;
            case CameraState.Top:
                cameraShaker = topCamera.GetComponentInChildren<CameraShaker>();
                break;
            default:
                return;
        }

        if (cameraShaker == null)
            return;

        if (hit)
            cameraShaker.ShakeOnce(shakeMagnitude, shakeRoughness, shakeFadeInTime, shakeFadeOutTime);
        else
            cameraShaker.ShakeOnce(missShakeMagnitude, missShakeRoughness, missShakeFadeInTime, missShakeFadeOutTime);
    }

    public void SetCameraState(int state) {
        SetCameraState((CameraState)state);
    }

    public void SetCameraState(CameraState state, bool shareRotation = true) {
        this.state = state;
        CameraController cameraController = null;
        CameraController otherController = null;
        switch (state) {
            case CameraState.Side:
                sideCamera.SetActive(true);
                topCamera.SetActive(false);
                cameraController = sideCameraController;
                otherController = topCameraController;
                break;
            case CameraState.Top:
                sideCamera.SetActive(false);
                topCamera.SetActive(true);
                cameraController = topCameraController;
                otherController = sideCameraController;
                break;
        }

        if (cameraController != null && otherController != null) {
            // a fixed view always keeps its own angle
            if (shareRotation && otherController.AlreadyAt(centerPos) && !cameraController.isFixed && !otherController.isFixed) {
                cameraController.newRotation = otherController.newRotation;
            }

            if (!cameraController.AlreadyAt(centerPos)) {
                if (otherController.AlreadyAt(centerPos)) {
                    cameraController.InstantMoveTo(centerPos);
                }
                else {
                    cameraController.MoveTo(centerPos);
                }
            }

            if (state == CameraState.Side && framePoint != null) {
                cameraController.Frame(framePoint.Value, frameRadius);
            }

        }
    }

    // the side view looks at a spot, sized to fit radius
    public void Frame(Vector3 point, float radius) {
        framePoint = point;
        frameRadius = radius;

        if (state == CameraState.Side) {
            sideCameraController.Frame(point, radius);
        }
    }

    public void FrameFleet(bool player) {
        if (shipManager == null) shipManager = FindFirstObjectByType<ShipManager>();
        if (shipManager == null) return;

        Bounds? fleet = null;
        foreach (var ship in shipManager.ships) {
            if (ship.team.isPlayer != player || !ship.isAlive) continue;

            var at = ship.transform.position;
            if (fleet == null) {
                fleet = new Bounds(at, Vector3.zero);
            } else {
                var area = fleet.Value;
                area.Encapsulate(at);
                fleet = area;
            }
        }

        if (fleet != null) {
            Frame(fleet.Value.center, Mathf.Max(fleet.Value.extents.x, fleet.Value.extents.z) + 60f);
        }
    }

    // check if already there, if not, move to regular
    public void MoveToOnce(Vector3 pos) {
        centerPos = pos;
        switch (state) {
            case CameraState.Side:
                if (sideCameraController.AlreadyAt(pos))
                    return;
                sideCameraController.MoveTo(pos);
                break;
            case CameraState.Top:
                if (topCameraController.AlreadyAt(pos))
                    return;
                topCameraController.MoveTo(pos);
                break;
        }
    }

    // check if already there, if not, move to instant
    public void MoveToInstant(Vector3 pos) {
        centerPos = pos;
        switch (state) {
            case CameraState.Side:
                if (sideCameraController.AlreadyAt(pos))
                    return;
                sideCameraController.InstantMoveTo(pos);
                break;
            case CameraState.Top:
                if (topCameraController.AlreadyAt(pos))
                    return;
                topCameraController.InstantMoveTo(pos);
                break;
        }
    }


    public void MoveTo(Vector3 pos) {
        centerPos = pos;
        switch (state) {
            case CameraState.Side:
                sideCameraController.MoveTo(pos);
                break;
            case CameraState.Top:
                topCameraController.MoveTo(pos);
                break;
        }
    }

    public void ZoomIn() {
        switch (state) {
            case CameraState.Side:
                sideCameraController.ZoomIn();
                break;
            case CameraState.Top:
                topCameraController.ZoomIn();
                break;
        }
    }

    public void ZoomOut() {
        switch (state) {
            case CameraState.Side:
                sideCameraController.ZoomOut();
                break;
            case CameraState.Top:
                topCameraController.ZoomOut();
                break;
        }
    }
}
