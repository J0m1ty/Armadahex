using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using MyBox;

public enum PanelState {
    In,
    Out,
    MovingIn,
    MovingOut,
}

// slides a panel of the document in from its side of the screen and back out
public class PanelSlider : MonoBehaviour
{
    [SerializeField]
    private UIDocument document;

    // the panel's name, its texts are found inside it by name
    [SerializeField]
    private string element;

    private VisualElement panel;

    private float width => float.IsNaN(panel.resolvedStyle.width) ? 0 : panel.resolvedStyle.width;

    // the panel sits at its edge, x is how far it is pushed off
    private float x;
    private Vector2 anchoredPosition {
        get => new Vector2(x, 0);
        set {
            x = value.x;
            panel.style.translate = new Translate(x, 0);
        }
    }

    [SerializeField]
    [MyBox.DefinedValues("left", "right")]
    private string direction = "left";

    [SerializeField]
    private float exp = 10;
    
    public Label connectedText { get; private set; }

    [SerializeField]
    private bool hasAttackIntent;
    private Label shipStatus;
    private Label fleetStatus;
    private Label attkIdent;
    private Label ammoRemain;

    [Header("Colors")]
    [SerializeField] private Color hitColor = new Color(0.8f, 0.1f, 0.1f);
    [SerializeField] private Color missColor = new Color(0.4f, 0.4f, 0.4f);
    [SerializeField] private Color sunkColor = new Color(0.85f, 0.45f, 0f);
    [SerializeField] private Color contactColor = new Color(0.1f, 0.4f, 0.9f);

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

    public bool persistingHit { get; set; }
    public bool? persistingShipDestroyed { get; set; }

    private void Awake() {
        panel = document.rootVisualElement.Q(element);
        connectedText = panel.Q<Label>("connected");
        shipStatus = panel.Q<Label>("shipStatus");
        fleetStatus = panel.Q<Label>("fleetStatus");
        attkIdent = panel.Q<Label>("attkIdent");
        ammoRemain = panel.Q<Label>("ammoRemain");

        state = PanelState.In;
        // hidden until it has a width to slide by
        panel.style.visibility = Visibility.Hidden;
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

    public void QuickActivate(bool resetText = true, float? overrideDuration = null) {
        anchoredPosition = new Vector2(inX, 0);
        
        this.targetX = outX;
        this.elapsedTime = TimeSpan.FromSeconds(0.6f);
        this.delay = TimeSpan.FromSeconds(overrideDuration ?? duration);

        state = PanelState.MovingOut;
    }

    public void Update() {
        if (width <= 0) return;
        panel.style.visibility = Visibility.Visible;

        var thresh = 0.5f;
        // move the panel to the correct anchoredPosition
        if (state == PanelState.In) {
            anchoredPosition = new Vector2(inX, 0);
        }
        else if (state == PanelState.MovingOut) {
            float elasped = elapsedTime.Seconds;
            float stepAmount = Mathf.Pow(elasped * speedOut, exp);
            var x = Mathf.SmoothStep(anchoredPosition.x, targetX, stepAmount);
            anchoredPosition = new Vector2(x, 0);
            elapsedTime += TimeSpan.FromSeconds(Time.deltaTime);
            if (Mathf.Abs(x - targetX) < thresh) {
                state = PanelState.Out;
                elapsedTime = TimeSpan.FromSeconds(0.6f);
                targetX = outX;
            }
        } 
        else if (state == PanelState.Out) {
            anchoredPosition = new Vector2(outX, 0);
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
            var x = Mathf.SmoothStep(anchoredPosition.x, targetX, stepAmount);
            anchoredPosition = new Vector2(x, 0);
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

    // a sonar sweep reports clear or contact rather than hit or miss
    public void SetScanInfo(bool contact, string attkIdent, int? ammoRemain) {
        SetAttackInfo(false, null, attkIdent, ammoRemain);
        if (!hasAttackIntent) return;

        this.connectedText.text = contact ? "CONTACT" : "CLEAR";
        this.connectedText.style.color = contact ? contactColor : missColor;
        this.shipStatus.text = contact ? "SHIP IN THE AREA" : "NOTHING FOUND";
    }

    // the headline is HIT, MISS or SUNK, the rows underneath say what fired and what's left
    public void SetAttackInfo(bool hitStatus, bool? shipDestroyed, string attkIdent, int? ammoRemain) {
        if (!hasAttackIntent) return;

        var sunk = shipDestroyed == true;
        this.connectedText.text = sunk ? "SUNK" : (hitStatus ? "HIT" : "MISS");
        this.connectedText.style.color = sunk ? sunkColor : (hitStatus ? hitColor : missColor);
        this.shipStatus.text = shipDestroyed == null ? "NO SHIP" : (sunk ? "SHIP DESTROYED" : "SHIP DAMAGED");
        this.fleetStatus.text = "FLEET ACTIVE";
        this.attkIdent.text = attkIdent == null ? "ENEMY FIRE" : attkIdent.ToUpper();
        this.ammoRemain.text = ammoRemain == null ? "" : (ammoRemain <= 0 ? "NONE LEFT" : (ammoRemain > 100 ? "UNLIMITED" : ammoRemain + " LEFT"));
    }
}
