using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class VSAnim : MonoBehaviour {
    private Image image;

    private float elapsedTime = 0f;

    [SerializeField]
    private float duration = 2f;

    [SerializeField]
    private AnimationCurve curve;

    void Awake() {
        image = GetComponent<Image>();
        image.fillAmount = 0f;
        elapsedTime = 0f;
    }
    
    void Update() {
        elapsedTime += Time.deltaTime;
        image.fillAmount = curve.Evaluate(elapsedTime / duration);
    }
}
