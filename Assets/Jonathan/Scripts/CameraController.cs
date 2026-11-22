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

public enum CameraControl {
    // wasd pans, q and e rotate, scroll zooms
    Free,
    // the player can't move it, it frames the board and follows the shots
    Fixed,
    // a and d orbit the pivot, w and s pull in and out
    Orbit
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

    [Header("Control")]
    [SerializeField]
    private CameraControl control = CameraControl.Free;
    // degrees per second
    [SerializeField]
    private float orbitSpeed = 70f;
    // whole pulls per second
    [SerializeField]
    private float pullSpeed = 0.7f;
    // the view angle down at the board, closest and farthest
    [SerializeField]
    private float closePitch = 12f;
    [SerializeField]
    private float farPitch = 38f;
    // framing puts the subject this far (of its radius) below the middle of the screen
    [SerializeField]
    private float composeAhead = 0.35f;
    // 0 is pulled all the way in, 1 all the way out
    private float pull = 1f;
    private float pullVelocity;
    private float targetPull = 1f;

    public bool isFixed {
        get { return control == CameraControl.Fixed; }
    }

    [Header("Board Fit")]
    // the farthest zoom frames the whole board, so every shot is in view without panning
    [SerializeField]
    private bool fitBoard;
    [SerializeField]
    private float fitPadding = 1.1f;
    // closest zoom, as a fraction of the whole board distance
    [SerializeField]
    private float closestZoom = 0.3f;
    // fraction of the distance one scroll notch moves
    [SerializeField]
    private float zoomStep = 0.12f;
    // how high an orthographic view sits, it only matters for fog and clipping
    [SerializeField]
    private float orthoHeight = 700f;
    private float boardRadius;
    private float fitTan;
    private Vector3? fittedFor;

    [Header("Shot Focus")]
    // briefly zoom to where a volley landed, then go back
    [SerializeField]
    private bool focusOnShots;
    [SerializeField]
    private float focusHold = 1.6f;
    // never closer than this fraction of the whole board distance
    [SerializeField]
    private float focusClosest = 0.5f;
    private Coroutine focus;
    private Vector3 focusReturnPosition;
    private Vector3 focusReturnZoom;
    private float focusReturnPull;

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
            switch (control) {
                case CameraControl.Free:
                    HandleMouseInput();
                    HandleKeyboardInput();
                    break;
                case CameraControl.Orbit:
                    HandleOrbitInput();
                    break;
            }
        }

        transform.position -= appliedCenter;

        if (fitBoard) {
            FitBoard();
        }

        if (control == CameraControl.Orbit && boardRadius > 0) {
            pull = Mathf.SmoothDamp(pull, targetPull, ref pullVelocity, 0.15f);
            newZoom = PullOffset(pull);
        }

        newZoom = newZoom.normalized * Mathf.Clamp(newZoom.magnitude, zoomRange.Min + distanceOffset, zoomRange.Max + distanceOffset);

        newZoom.y = Mathf.Abs(newZoom.y);
        newZoom.z = -Mathf.Abs(newZoom.z);

        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
        // constrain transform.position and newPosition to 100 units from the origin
        // a fitted board can only be panned as far as zooming in hides it
        var limit = maxDist;
        if (fitBoard && boardRadius > 0) {
            // a shot focus or a framed spot may need to reach the board's edge
            limit = focus != null || control == CameraControl.Orbit ? boardRadius : boardRadius * (1f - 0.8f * Mathf.InverseLerp(zoomRange.Min, zoomRange.Max, newZoom.magnitude));
        }
        transform.position = new Vector3(Mathf.Clamp(transform.position.x, -limit, limit), Mathf.Clamp(transform.position.y, -limit, limit), Mathf.Clamp(transform.position.z, -limit, limit));
        newPosition = new Vector3(Mathf.Clamp(newPosition.x, -limit, limit), Mathf.Clamp(newPosition.y, -limit, limit), Mathf.Clamp(newPosition.z, -limit, limit));

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

        StopFocus(true);
    }

    // sets the zoom range from the board at the current center, once per center
    private void FitBoard() {
        if (fittedFor == newCenter) return;

        var radius = BoardRadius(newCenter);
        if (radius <= 0) return;

        var first = fittedFor == null;
        fittedFor = newCenter;
        boardRadius = radius;

        var cam = newCameraChild.GetComponentInChildren<Camera>();

        // straight down and flat, the board fills the screen at any height
        if (cam.orthographic) {
            cam.orthographicSize = boardRadius * fitPadding / Mathf.Min(1f, cam.aspect);
            zoomRange = new MinMaxFloat(orthoHeight, orthoHeight);
            newZoom = ZoomDirection() * orthoHeight;
            return;
        }

        var halfV = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        var halfH = Mathf.Atan(Mathf.Tan(halfV) * cam.aspect);
        fitTan = Mathf.Tan(Mathf.Min(halfV, halfH));

        var fit = boardRadius * fitPadding / fitTan;
        zoomRange = new MinMaxFloat(fit * closestZoom, fit);

        if (first) {
            newZoom = ZoomDirection() * fit;
        }
    }

    private float BoardRadius(Vector3 at) {
        HexGrid nearest = null;
        var best = float.MaxValue;
        foreach (var grid in FindObjectsByType<HexGrid>(FindObjectsSortMode.None)) {
            var distance = Vector3.Distance(grid.transform.position, at);
            if (distance < best) {
                best = distance;
                nearest = grid;
            }
        }

        if (nearest == null || nearest.hexes == null) return 0;

        var radius = 0f;
        foreach (var unit in nearest.hexes) {
            if (unit.hexRenderer == null || !unit.hexRenderer.gameObject.activeInHierarchy) continue;

            var offset = unit.hexRenderer.transform.position - at;
            offset.y = 0;
            radius = Mathf.Max(radius, offset.magnitude + nearest.size);
        }
        return radius;
    }

    // the camera's offset from the pivot at a pull, lower and closer when pulled in
    private Vector3 PullOffset(float at) {
        var distance = Mathf.Lerp(zoomRange.Min, zoomRange.Max, at);
        var pitch = Mathf.Lerp(closePitch, farPitch, at) * Mathf.Deg2Rad;
        return new Vector3(0, Mathf.Sin(pitch), -Mathf.Cos(pitch)) * distance;
    }

    void HandleOrbitInput() {
        Vector2 move = moveAction.ReadValue<Vector2>();
        float rotate = rotateAction.ReadValue<float>();
        float scroll = zoomAction.ReadValue<float>();

        if (move != Vector2.zero || rotate != 0 || scroll != 0) {
            // the player took over, stay where they put it
            StopFocus(false);
        }

        float speed = fastAction.IsPressed() ? shiftMultiplier : 1f;

        // a and d, or q and e, go around the pivot
        newRotation *= Quaternion.Euler(Vector3.up * (rotate - move.x) * orbitSpeed * speed * Time.deltaTime);

        // w and s pull in and out, the same as the scroll wheel
        targetPull = Mathf.Clamp01(targetPull - move.y * pullSpeed * speed * Time.deltaTime - scroll * zoomStep);

        if (orbitAction.WasPressedThisFrame()) {
            rotateStartPosition = pointAction.ReadValue<Vector2>();
        }
        if (orbitAction.IsPressed()) {
            rotateCurrentPosition = pointAction.ReadValue<Vector2>();

            Vector3 difference = rotateStartPosition - rotateCurrentPosition;
            rotateStartPosition = rotateCurrentPosition;

            newRotation *= Quaternion.Euler(Vector3.up * (-difference.x / 10f));
        }
    }

    private Vector3 ZoomDirection() {
        return new Vector3(0, Mathf.Abs(zoomAmount.y), -Mathf.Abs(zoomAmount.z)).normalized;
    }

    public Camera view {
        get { return newCameraChild.GetComponentInChildren<Camera>(true); }
    }

    // an orbiting view looks at a spot and pulls in or out to fit it, and stays there
    public void Frame(Vector3 point, float radius) {
        if (control != CameraControl.Orbit || boardRadius <= 0) return;

        StopFocus(false);

        radius = Mathf.Min(radius, boardRadius);

        // look a little past the subject, so it sits in the lower middle with the board behind it
        var ahead = Vector3.ProjectOnPlane(newRotation * Vector3.forward, Vector3.up).normalized;
        // none when framing the whole board, so it orbits around the middle
        var offset = point + ahead * radius * (1f - radius / boardRadius) * composeAhead - newCenter;
        newPosition = new Vector3(offset.x, 0, offset.z);

        var distance = radius * fitPadding / fitTan;
        targetPull = Mathf.InverseLerp(zoomRange.Min, zoomRange.Max, distance);
    }

    public void FocusOn(Bounds area) {
        if (!focusOnShots || !isActiveAndEnabled || boardRadius <= 0) return;

        if (control == CameraControl.Orbit) {
            // keep most of the board around the shots
            Frame(area.center, Mathf.Max(Mathf.Max(area.extents.x, area.extents.z) + boardRadius * 0.25f, boardRadius * 0.6f));
            return;
        }

        // a new volley during a focus keeps the place to go back to
        if (focus != null) {
            StopCoroutine(focus);
        } else {
            focusReturnPosition = newPosition;
            focusReturnZoom = newZoom;
            focusReturnPull = targetPull;
        }

        focus = StartCoroutine(Focus(area));
    }

    private IEnumerator Focus(Bounds area) {
        var offset = area.center - newCenter;
        newPosition = new Vector3(offset.x, 0, offset.z);

        var radius = Mathf.Max(area.extents.x, area.extents.z) + boardRadius * 0.15f;
        var distance = Mathf.Clamp(radius * fitPadding / fitTan, zoomRange.Max * focusClosest, zoomRange.Max);
        newZoom = ZoomDirection() * distance;
        targetPull = Mathf.InverseLerp(zoomRange.Min, zoomRange.Max, distance);

        yield return new WaitForSeconds(focusHold);

        newPosition = focusReturnPosition;
        newZoom = focusReturnZoom;
        targetPull = focusReturnPull;
        focus = null;
    }

    private void StopFocus(bool goBack) {
        if (focus == null) return;

        StopCoroutine(focus);
        focus = null;

        if (goBack) {
            newPosition = focusReturnPosition;
            newZoom = focusReturnZoom;
            targetPull = focusReturnPull;
        }
    }

    void HandleMouseInput() {
        float scroll = zoomAction.ReadValue<float>();
        if (scroll != 0) {
            // the player took over, stay where they put it
            StopFocus(false);

            if (fitBoard) {
                newZoom *= 1f - scroll * zoomStep;
            } else {
                newZoom += scroll * zoomAmount;
            }
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

        if (move != Vector2.zero || rotateAction.ReadValue<float>() != 0) {
            StopFocus(false);
        }
        
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

        StopFocus(true);
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
            StopFocus(true);
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
        if (control == CameraControl.Orbit) {
            targetPull = Mathf.Clamp01(targetPull - zoomStep * 2f);
            return;
        }
        newZoom += zoomAmountButton * zoomAmount;
    }

    public void ZoomOut() {
        if (control == CameraControl.Orbit) {
            targetPull = Mathf.Clamp01(targetPull + zoomStep * 2f);
            return;
        }
        newZoom -= zoomAmountButton * zoomAmount;
    }
}
