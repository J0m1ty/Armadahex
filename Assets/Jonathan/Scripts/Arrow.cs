using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SliderPos {
    Front,
    Middle,
    Back
}

public class Arrow : MonoBehaviour
{
    public float offset;

    [SerializeField]
    private float totalLength;

    [SerializeField]
    private GameObject front;
    [SerializeField]
    private GameObject back;

    [SerializeField]
    private GameObject slider;

    [SerializeField]
    private Transform frontTransform;
    [SerializeField]
    private Transform backTransform;

    [SerializeField]
    private Material arrowMaterial;

    [SerializeField]
    private Renderer frontRenderer;
    [SerializeField]
    private Renderer backRenderer;

    private float initialFrontLength;
    private float initialBackLength;

    #if UNITY_EDITOR
    [MyBox.ButtonMethod]
    public void Setup() {
        Awake();
    }
    [MyBox.ButtonMethod]
    public void RandomLengthAtFront() {
        SetLength(Random.Range(20f, 80f), SliderPos.Front);
    }
    [MyBox.ButtonMethod]
    public void RandomLengthAtMiddle() {
        SetLength(Random.Range(20f, 80f), SliderPos.Middle);
    }
    [MyBox.ButtonMethod]
    public void RandomLengthAtBack() {
        SetLength(Random.Range(20f, 80f), SliderPos.Back);
    }
    #endif

    void Awake() {
        frontTransform = front.transform;
        backTransform = back.transform;

        slider = transform.GetChild(0).gameObject;

        frontRenderer = front.GetComponentInChildren<MeshRenderer>();
        backRenderer = back.GetComponentInChildren<MeshRenderer>();

        var material = arrowMaterial != null ? new Material(arrowMaterial) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = Color.white;
        frontRenderer.material = material;
        backRenderer.material = material;

        initialFrontLength = frontRenderer.bounds.size.x;
        initialBackLength = backRenderer.bounds.size.x;
    }

    // the flow shader needs to know where the head is to run its bands toward it
    void LateUpdate() {
        var head = frontRenderer.bounds.center;
        var dir = head - backRenderer.bounds.center;
        if (dir.sqrMagnitude < 0.001f) return;

        frontRenderer.sharedMaterial.SetVector("_FlowOrigin", head);
        frontRenderer.sharedMaterial.SetVector("_FlowDir", dir.normalized);
    }

    public void SetLength(float length, SliderPos pos) {
        if (length < initialFrontLength + initialBackLength) {
            length = initialFrontLength + initialBackLength;
        }

        var scale = Mathf.Abs(length - initialFrontLength) / initialBackLength;

        backTransform.localScale = new Vector3(scale, 1, 1);

        totalLength = initialFrontLength + initialBackLength * scale;

        switch (pos) {
            case SliderPos.Front:
                slider.transform.localPosition = new Vector3(initialFrontLength + offset, 0, 0);
                break;
            case SliderPos.Middle:
                slider.transform.localPosition = new Vector3((initialFrontLength - initialBackLength * scale) / 2f, 0, 0);
                break;
            case SliderPos.Back:
                slider.transform.localPosition = new Vector3(-(initialBackLength * scale) - offset, 0, 0);
                break;
        }
    }

    public void SetColor(Color color) {
        frontRenderer.sharedMaterial.color = color;
        backRenderer.sharedMaterial.color = color;
    }

    [SerializeField]
    private float growTime = 0.3f;
    private float growLength;
    private SliderPos growPos;
    private float growStart = -1;

    // grow out from the target instead of popping in
    void Update() {
        if (growStart < 0) return;
        var t = Mathf.Clamp01((Time.time - growStart) / growTime);
        SetLength(growLength * (1 - Mathf.Pow(1 - t, 3)), growPos);
        if (t >= 1) growStart = -1;
    }

    public void SetArrow(Rotation rotation, bool reverse, float length, SliderPos spos) {
        transform.localPosition = Vector3.zero;

        var r = 0;
        switch (rotation) {
            case Rotation.One:
                r = 0;
                break;
            case Rotation.Two:
                r = 60;
                break;
            case Rotation.Three:
                r = 120;
                break;
        }

        if (!reverse) {
            r += 180;
        }

        r = CoordinateSystem.Mod(r, 360);

        transform.localRotation = Quaternion.Euler(0, r - 30, 0);
        SetLength(0, spos);
        growLength = length;
        growPos = spos;
        growStart = Time.time;
    }
}
