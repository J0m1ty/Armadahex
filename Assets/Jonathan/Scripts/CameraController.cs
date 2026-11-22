using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MyBox;

[System.Serializable]
public struct Vector3Range
{
    public Vector3 min;
    public Vector3 max;

    public Vector3Range(Vector3 min, Vector3 max)
    {
        this.min = min;
        this.max = max;
    }
}

public class CameraController : MonoBehaviour
{
    public GameObject followChild;
    public Camera newCameraChild;

    [Header("General Settings")]
    //public bool rotationAffectsRigOrCamera;
    public bool allowZoomKeys;
    public bool allowMousePanning;
    public float movementTime;
    public bool isOrthographic;
    [ConditionalField("isOrthographic", false, true)]
    public float OrthoFOV;
    [ConditionalField("isOrthographic", false, true)]
    public float distanceOffset;
    [ConditionalField("isOrthographic", false, false)]
    public float FOV;

    [Header("Movement Settings")]
    public float moveSpeed = 1.2f;
    public float shiftMultiplier = 3f;
    public float maxDist;

    [Header("Rotation Settings")]
    public float rotationAmount;

    [Header("Zoom Settings")]
    public Vector3 zoomAmount;
    public MinMaxFloat zoomRange;
    public Vector2 angleOffset;

    private float movementSpeed;

    [Header("Input Settings")]
    public Vector3 newPosition;
    public Quaternion newRotation;
    public Vector3 newZoom;

    private Vector3 dragStartPosition;
    private Vector3 dragCurrentPosition;
    private Vector3 rotateStartPosition;
    private Vector3 rotateCurrentPosition;
    
    [Header("Center")]
    [SerializeField]
    private Vector3 center;
    private Vector3 newCenter;
    // the center added last frame, so a hop or jump moves the rig instead of dragging behind it
    private Vector3 appliedCenter;
    [SerializeField]
    private float transportTime;

    [Header("Hop")]
    [SerializeField]
    private float hopDistance = 500f;
    [SerializeField]
    private float hopHeight = 230f;
    [SerializeField]
    private float hopRiseTime = 0.6f;
    [SerializeField]
    private float hopSlideTime = 1f;
    // off: slide straight across at the current height instead of hopping over
    [SerializeField]
    private bool hopUp = true;
    private Coroutine hop;

    private InputAction moveAction;
    private InputAction fastAction;
    private InputAction rotateAction;
    private InputAction zoomAction;
    private InputAction zoomKeysAction;
    private InputAction dragAction;
    private InputAction orbitAction;
    private InputAction pointAction;

    void Start() {
        followChild = transform.GetChild(0).gameObject;

        moveAction = GameInput.Get("Camera/Move");
        fastAction = GameInput.Get("Camera/Fast");
        rotateAction = GameInput.Get("Camera/Rotate");
        zoomAction = GameInput.Get("Camera/Zoom");
        zoomKeysAction = GameInput.Get("Camera/Zoom Keys");
        dragAction = GameInput.Get("Camera/Drag");
        orbitAction = GameInput.Get("Camera/Orbit");
        pointAction = GameInput.Get("Camera/Point");

        newCameraChild.GetComponentInChildren<Camera>().orthographic = isOrthographic;
        if (isOrthographic) {
            newCameraChild.GetComponentInChildren<Camera>().orthographicSize = OrthoFOV;
        } else {
            newCameraChild.GetComponentInChildren<Camera>().fieldOfView = FOV;
            distanceOffset = 0;
        }

        newPosition = transform.position;
        newRotation = transform.rotation;
        newZoom = followChild.transform.localPosition + (zoomAmount * -distanceOffset); 
    }

    void LateUpdate() {
        if (TurnManager.instance != null && TurnManager.instance.gameActive) {
            HandleMouseInput();
            HandleKeyboardInput();
        }

        transform.position -= appliedCenter;

        newZoom = newZoom.normalized * Mathf.Clamp(newZoom.magnitude, zoomRange.Min + distanceOffset, zoomRange.Max + distanceOffset);

        newZoom.y = Mathf.Abs(newZoom.y);
        newZoom.z = -Mathf.Abs(newZoom.z);

        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
        // constrain transform.position and newPosition to 100 units from the origin
        transform.position = new Vector3(Mathf.Clamp(transform.position.x, -maxDist, maxDist), Mathf.Clamp(transform.position.y, -maxDist, maxDist), Mathf.Clamp(transform.position.z, -maxDist, maxDist));
        newPosition = new Vector3(Mathf.Clamp(newPosition.x, -maxDist, maxDist), Mathf.Clamp(newPosition.y, -maxDist, maxDist), Mathf.Clamp(newPosition.z, -maxDist, maxDist));

        // a rig that hasn't started yet can hand over an empty rotation when switching views
        if (newRotation.x == 0f && newRotation.y == 0f && newRotation.z == 0f && newRotation.w == 0f) {
            newRotation = transform.rotation;
        }

        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * movementTime);
        followChild.transform.localPosition = Vector3.Lerp(followChild.transform.localPosition, newZoom, Time.deltaTime * movementTime);

        followChild.transform.LookAt(transform.position);

        float offset = Mathf.Lerp(angleOffset[0], angleOffset[1], Mathf.InverseLerp(zoomRange.Min, zoomRange.Max, followChild.transform.localPosition.magnitude - distanceOffset));
        followChild.transform.rotation *= Quaternion.AngleAxis(offset, Vector3.right);

        if (hop == null) {
            center = Vector3.Lerp(center, newCenter, Time.deltaTime * transportTime);
        }

        transform.position += center;
        appliedCenter = center;
    }

    void OnDisable() {
        // switching views stops the hop coroutine, so land it rather than freeze mid-air
        if (hop != null) {
            hop = null;
            center = newCenter;
        }
    }

    void HandleMouseInput() {
        float scroll = zoomAction.ReadValue<float>();
        if (scroll != 0) {
            newZoom += scroll * zoomAmount;
        }

        Vector2 pointer = pointAction.ReadValue<Vector2>();

        if (allowMousePanning) {
            if (dragAction.WasPressedThisFrame()) {
                Plane plane = new Plane(Vector3.up, Vector3.zero);
                Ray ray = Camera.main.ScreenPointToRay(pointer);

                float entry;

                if (plane.Raycast(ray, out entry)) {
                    dragStartPosition = ray.GetPoint(entry);
                }
            }

            if (dragAction.IsPressed()) {
                Plane plane = new Plane(Vector3.up, Vector3.zero);
                Ray ray = Camera.main.ScreenPointToRay(pointer);

                float entry;

                if (plane.Raycast(ray, out entry)) {
                    dragCurrentPosition = ray.GetPoint(entry);
                    newPosition = transform.position + dragStartPosition - dragCurrentPosition;
                }
            }
        }

        if (orbitAction.WasPressedThisFrame()) {
            rotateStartPosition = pointer;
        }
        if (orbitAction.IsPressed()) {
            rotateCurrentPosition = pointer;

            Vector3 difference = rotateStartPosition - rotateCurrentPosition;
            rotateStartPosition = rotateCurrentPosition;

            newRotation *= Quaternion.Euler(Vector3.up * (-difference.x / 10f));
        }
    }

    void HandleKeyboardInput() {
        // units per frame at 60 fps, so the speed is the same at any frame rate
        movementSpeed = moveSpeed * Time.deltaTime * 60f;
        if (fastAction.IsPressed()) {
            movementSpeed *= shiftMultiplier;
        }

        Vector2 move = moveAction.ReadValue<Vector2>();
        Vector3 positionMod = transform.forward * move.y + transform.right * move.x;
        
        if (positionMod != Vector3.zero) {
            positionMod = positionMod.normalized * movementSpeed;
        }

        newPosition += positionMod;

        // q is positive, e negative
        newRotation *= Quaternion.Euler(Vector3.up * rotationAmount * rotateAction.ReadValue<float>());

        if (allowZoomKeys) {
            // r is positive, f negative
            newZoom += zoomAmount * zoomKeysAction.ReadValue<float>();
        }
        
    }

    public void MoveTo(Vector3 position) {
        // already there or on the way, keep the player's panning and don't restart the hop
        if (AlreadyAt(position)) return;

        newPosition = new Vector3(0, 0, 0);
        newCenter = position;

        // far moves hop up over the terrain, slide across, then drop back down
        if (Vector3.Distance(center, position) > hopDistance && gameObject.activeInHierarchy) {
            if (hop != null) StopCoroutine(hop);
            hop = StartCoroutine(Hop(center, position));
        }
    }

    public void InstantMoveTo(Vector3 position) {
        if (hop != null) {
            StopCoroutine(hop);
            hop = null;
        }

        if (!AlreadyAt(position)) {
            newPosition = new Vector3(0, 0, 0);
        }
        newCenter = position;
        center = newCenter;
    }

    private IEnumerator Hop(Vector3 from, Vector3 to) {
        if (!hopUp) {
            yield return HopLeg(from, to, hopSlideTime);
            hop = null;
            yield break;
        }

        var up = Vector3.up * hopHeight;
        yield return HopLeg(from, from + up, hopRiseTime);
        yield return HopLeg(from + up, to + up, hopSlideTime);
        yield return HopLeg(to + up, to, hopRiseTime);
        hop = null;
    }

    private IEnumerator HopLeg(Vector3 from, Vector3 to, float time) {
        for (var t = 0f; t < time; t += Time.deltaTime) {
            center = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / time));
            yield return null;
        }
        center = to;
    }

    public bool AlreadyAt(Vector3 centerPos) {
        return newCenter == centerPos;
    }

    [SerializeField]
    private float zoomAmountButton;

    public void ZoomIn() {
        newZoom += zoomAmountButton * zoomAmount;
    }

    public void ZoomOut() {
        newZoom -= zoomAmountButton * zoomAmount;
    }
}
