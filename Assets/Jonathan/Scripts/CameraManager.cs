using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using EZCameraShake;

public enum CameraState {
    Pivot = 0,
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
        if (switchViewAction.WasPressedThisFrame() && TurnManager.instance != null && TurnManager.instance.gameActive && !GameInput.typing) {
            SetCameraState(state == CameraState.Pivot ? CameraState.Top : CameraState.Pivot);
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

        var controller = state == CameraState.Pivot ? sideCameraController : topCameraController;
        controller.FocusOn(area);
    }

    public void Shake(bool hit) {
        CameraShaker cameraShaker;

        switch (state) {
            case CameraState.Pivot:
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
            case CameraState.Pivot:
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

            cameraController.Settle();

        }
    }

    // check if already there, if not, move to regular
    public void MoveToOnce(Vector3 pos) {
        centerPos = pos;
        switch (state) {
            case CameraState.Pivot:
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
            case CameraState.Pivot:
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
            case CameraState.Pivot:
                sideCameraController.MoveTo(pos);
                break;
            case CameraState.Top:
                topCameraController.MoveTo(pos);
                break;
        }
    }

    public void ZoomIn() {
        switch (state) {
            case CameraState.Pivot:
                sideCameraController.ZoomIn();
                break;
            case CameraState.Top:
                topCameraController.ZoomIn();
                break;
        }
    }

    public void ZoomOut() {
        switch (state) {
            case CameraState.Pivot:
                sideCameraController.ZoomOut();
                break;
            case CameraState.Top:
                topCameraController.ZoomOut();
                break;
        }
    }
}
