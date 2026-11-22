using UnityEngine;
using UnityEngine.UI;
using ThisOtherThing.UI.Shapes;

// draws a toggle as a sliding switch, blue when on
[RequireComponent(typeof(Toggle))]
public class SwitchToggle : MonoBehaviour
{
    [SerializeField] private Rectangle track;
    [SerializeField] private RectTransform knob;
    [SerializeField] private Rectangle knobShape;

    private Toggle toggle;

    void Awake() {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(_ => Paint());
    }

    void OnEnable() {
        Paint();
    }

    public void Paint() {
        var t = UITheme.instance;
        if (t == null || toggle == null) return;

        var on = toggle.isOn;
        var off = !toggle.IsInteractable();

        track.ShapeProperties.FillColor = on ? t.Get(ThemeColor.Friendly, off ? 0.4f : 1) : t.Get(ThemeColor.LineStrong, off ? 0.5f : 1);
        track.ForceMeshUpdate();

        knobShape.ShapeProperties.FillColor = on ? Color.white : t.ink2;
        knobShape.ForceMeshUpdate();

        var x = on ? 8f : -8f;
        knob.anchoredPosition = new Vector2(x, 0);
    }
}
