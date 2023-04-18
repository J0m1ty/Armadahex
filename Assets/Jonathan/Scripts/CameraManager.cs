using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    void Awake() {
        if (instance != null) {
            Destroy(gameObject);
            return;
        }

        instance = this;

        sideCameraController = sideCamera.GetComponent<CameraController>();
        topCameraController = topCamera.GetComponent<CameraController>();
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
            if (shareRotation && otherController.AlreadyAt(centerPos)) {
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
