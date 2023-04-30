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

        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = Color.white;
        frontRenderer.material = material;
        backRenderer.material = material;

        initialFrontLength = frontRenderer.bounds.size.x;
        initialBackLength = backRenderer.bounds.size.x;
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
        SetLength(length, spos);
    }
}
