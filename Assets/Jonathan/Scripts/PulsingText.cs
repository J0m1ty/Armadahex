using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class PulsingText : MonoBehaviour
{
    [SerializeField]
    private Color colorPulseFrom = Color.white;
    private Color colorPulseFromOriginal;

    [SerializeField]
    private Color colorPulseTo = Color.white;
    private Color colorPulseToOriginal;

    [SerializeField]
    private float colorPulseDuration = 1f;

    [SerializeField]
    private float holdAtFromColorDuration = 0f;

    [SerializeField]
    private float holdAtToColorDuration = 0f;

    private TMP_Text text;

    void Awake() {
        text = GetComponent<TMP_Text>();
    }
    
    void Start() {
        StartCoroutine(SmoothstepPingPongColor());
    }
    
    private IEnumerator SmoothstepPingPongColor() {

        colorPulseFromOriginal = colorPulseFrom;
        colorPulseToOriginal = colorPulseTo;

        float t = 0f;
        while (true) {
            t += Time.deltaTime / colorPulseDuration;
            text.color = Color.Lerp(colorPulseFrom, colorPulseTo, Mathf.SmoothStep(0f, 1f, t));

            if (t >= 1f) {
                Color temp = colorPulseFrom;
                colorPulseFrom = colorPulseTo;
                colorPulseTo = temp;

                t = 0f;

                yield return new WaitForSeconds(colorPulseFrom == colorPulseFromOriginal ? holdAtFromColorDuration : holdAtToColorDuration);
            }

            yield return null;
        }
    }
}
