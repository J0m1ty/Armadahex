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
    [SerializeField]
    private GameObject front;
    [SerializeField]
    private GameObject back;

    private GameObject slider;

    private Transform frontTransform;
    private Transform backTransform;

    private Renderer frontRenderer;
    private Renderer backRenderer;

    private float totalLength;

    public float offset;

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

        totalLength = frontTransform.localScale.x + backTransform.localScale.x;
    }

    public void SetLength(float length, SliderPos pos) {
        backTransform.localScale = new Vector3(length, 1, 1);

        totalLength = frontTransform.localScale.x + backTransform.localScale.x;

        switch (pos) {
            case SliderPos.Front:
                slider.transform.localPosition = new Vector3(0, 0, 0);
                break;
            case SliderPos.Middle:
                slider.transform.localPosition = new Vector3(-(totalLength + offset) / 2f, 0, 0);
                break;
            case SliderPos.Back:
                slider.transform.localPosition = new Vector3(-(totalLength + offset), 0, 0);
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
