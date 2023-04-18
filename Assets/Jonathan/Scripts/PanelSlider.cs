using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using MyBox;

public enum PanelState {
    In,
    Out,
    MovingIn,
    MovingOut,
}

[RequireComponent(typeof(RectTransform))]
public class PanelSlider : MonoBehaviour
{
    private RectTransform rectTransform;

    private float width => rectTransform.rect.width;
    private float height => rectTransform.rect.height;

    [SerializeField]
    [MyBox.DefinedValues("left", "right")]
    private string direction = "left";

    [SerializeField]
    private float exp = 10;
    
    public TMPro.TMP_Text connectedText;

    [SerializeField]
    private bool hasAttackIntent;
    [ConditionalField(nameof(hasAttackIntent))]
    [SerializeField]
    private TMPro.TMP_Text shipStatus;
    [ConditionalField(nameof(hasAttackIntent))]
    [SerializeField]
    private TMPro.TMP_Text fleetStatus;
    [ConditionalField(nameof(hasAttackIntent))]
    [SerializeField]
    private TMPro.TMP_Text attkIdent;
    [ConditionalField(nameof(hasAttackIntent))]

    [SerializeField]
    private TMPro.TMP_Text ammoRemain;

    private float inX => (direction == "left" ? -width : width);
    private float outX => 0;

    [SerializeField]
    private PanelState state;

    public float speedOut;
    public float speedIn;
    public bool infinite;
    public float duration;

    private float targetX;
    private TimeSpan elapsedTime;
    private TimeSpan delay;

    private bool persistingHit;
    private bool? persistingShipDestroyed;

    private void Awake() {
        rectTransform = GetComponent<RectTransform>();

        state = PanelState.In;
        rectTransform.anchoredPosition = new Vector2(inX, rectTransform.anchoredPosition.y);
    }

    public void SetState(PanelState goTo) {
        infinite = true;
        if (goTo == PanelState.In) {
            state = PanelState.MovingIn;
            targetX = inX;
        }
        else if (goTo == PanelState.Out) {
            state = PanelState.MovingOut;
            targetX = outX;
        }
    }

    public void QuickActivate(bool resetText = true) {
        rectTransform.anchoredPosition = new Vector2(inX, rectTransform.anchoredPosition.y);
        
        this.targetX = outX;
        this.elapsedTime = TimeSpan.FromSeconds(0.6f);
        this.delay = TimeSpan.FromSeconds(duration);

        state = PanelState.MovingOut;

        if (resetText) RandomText.ResetText(transform);
    }

    public void Update() {
        var thresh = 0.5f;
        // move the panel to the correct anchoredPosition
        if (state == PanelState.In) {
            rectTransform.anchoredPosition = new Vector2(inX, rectTransform.anchoredPosition.y);
        }
        else if (state == PanelState.MovingOut) {
            float elasped = elapsedTime.Seconds;
            float stepAmount = Mathf.Pow(elasped * speedOut, exp);
            var x = Mathf.SmoothStep(rectTransform.anchoredPosition.x, targetX, stepAmount);
            rectTransform.anchoredPosition = new Vector2(x, rectTransform.anchoredPosition.y);
            elapsedTime += TimeSpan.FromSeconds(Time.deltaTime);
            if (Mathf.Abs(x - targetX) < thresh) {
                state = PanelState.Out;
                elapsedTime = TimeSpan.FromSeconds(0.6f);
                targetX = outX;
            }
        } 
        else if (state == PanelState.Out) {
            rectTransform.anchoredPosition = new Vector2(outX, rectTransform.anchoredPosition.y);
            // move back in after delay
            elapsedTime += TimeSpan.FromSeconds(Time.deltaTime);
            if (elapsedTime > delay && !infinite) {
                state = PanelState.MovingIn;
                targetX = inX;
                elapsedTime = TimeSpan.FromSeconds(0.6f);
            }
        }
        else if (state == PanelState.MovingIn) {
            float elasped = elapsedTime.Seconds;
            float stepAmount = Mathf.Pow(elasped * speedIn, exp);
            var x = Mathf.SmoothStep(rectTransform.anchoredPosition.x, targetX, stepAmount);
            rectTransform.anchoredPosition = new Vector2(x, rectTransform.anchoredPosition.y);
            elapsedTime += TimeSpan.FromSeconds(Time.deltaTime);
            if (Mathf.Abs(x - targetX) < thresh) {
                state = PanelState.In;
                elapsedTime = TimeSpan.FromSeconds(0.6f);
                targetX = inX;
                ResetData();
            }
        }
    }

    public void SetConnectedText(string text) {
        connectedText.text = text;
    }

    public void SetAttackInfo_Save(bool hitStatus, bool? shipDestroyed, string attkIdent, int? ammoRemain) {
        if (hitStatus) {
            persistingHit = true;
        }
        // if nothing is set yet
        if (persistingShipDestroyed == null && shipDestroyed != null) {
            persistingShipDestroyed = false;
        }
        if (shipDestroyed != null && (bool)shipDestroyed) {
            persistingShipDestroyed = true;
        }
        SetAttackInfo(persistingHit, persistingShipDestroyed, attkIdent, ammoRemain);
    }

    public void ResetData() {
        persistingHit = false;
        persistingShipDestroyed = null;
    }

    public void SetAttackInfo(bool hitStatus, bool? shipDestroyed, string attkIdent, int? ammoRemain) {
        if (!hasAttackIntent) return;

        this.connectedText.text = hitStatus ? "> HIT <" : "> MISS <";
        this.connectedText.color = hitStatus ? new Color(204f/255f, 24f/255f, 11f/255f) : new Color(0.9f, 0.9f, 0.9f);
        this.shipStatus.text = shipDestroyed == null ? "N/A" : ((bool)shipDestroyed ? "DESTROYED" : "ALIVE");
        this.fleetStatus.text = "ACTIVE";
        this.attkIdent.text = attkIdent == null ? "UNKNOWN" : attkIdent;
        this.ammoRemain.text = ammoRemain == null ? "UNKNOWN" : (ammoRemain <= 0 ? "FALSE" : (ammoRemain > 100 ? "UNLIMITED" : ammoRemain.ToString()));

        CameraManager.instance.Shake(hitStatus);
    }
}
