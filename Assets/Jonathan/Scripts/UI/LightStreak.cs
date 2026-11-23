using UnityEngine;
using UnityEngine.UIElements;

// a streak of light along the element's top edge that grows in weight and peaks under part of a label, e.g. the "HEX" in the logo
[UxmlElement]
public partial class LightStreak : VisualElement
{
    // the label, and the part of its text the streak peaks under
    [UxmlAttribute]
    public string target { get; set; }

    [UxmlAttribute]
    public string peakText { get; set; }

    [UxmlAttribute]
    public Color color { get; set; } = Color.white;

    public LightStreak() {
        pickingMode = PickingMode.Ignore;
        generateVisualContent += Draw;
        RegisterCallback<AttachToPanelEvent>(_ => parent?.RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint()));
    }

    private float Peak() {
        var label = parent?.Q<TextElement>(target);
        var start = label != null && !string.IsNullOrEmpty(peakText) ? label.text.LastIndexOf(peakText) : -1;
        if (start < 0 || layout.width <= 0) return 0.8f;

        var before = Measure(label, label.text.Substring(0, start));
        var center = label.contentRect.xMin + before + Measure(label, peakText) * 0.8f;
        var x = label.ChangeCoordinatesTo(this, new Vector2(center, 0)).x;
        return Mathf.Clamp01(x / layout.width);
    }

    private static float Measure(TextElement label, string text) {
        return label.MeasureTextSize(text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).x;
    }

    // a wedge with a flat top: a hairline on the left, the element's full height at the peak, then a sharp tip
    private void Draw(MeshGenerationContext context) {
        var width = contentRect.width;
        var height = contentRect.height;
        if (width <= 0 || height <= 0) return;

        var peak = width * Peak();
        var end = Mathf.Min(width, peak + width * 0.06f);

        var painter = context.painter2D;
        painter.fillGradient = FillGradient.MakeLinearGradient(Fade(peak / width), Vector2.zero, new Vector2(width, 0), AddressMode.Clamp);
        painter.BeginPath();
        painter.MoveTo(new Vector2(0, 0));
        painter.LineTo(new Vector2(end, 0));
        painter.LineTo(new Vector2(peak, height));
        painter.LineTo(new Vector2(0, 0.5f));
        painter.ClosePath();
        painter.Fill();
    }

    private Gradient Fade(float peak) {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0), new GradientColorKey(color, 1) },
            new[] {
                new GradientAlphaKey(0, 0),
                new GradientAlphaKey(color.a * 0.3f, peak * 0.5f),
                new GradientAlphaKey(color.a, peak),
                new GradientAlphaKey(color.a, 1)
            });
        return gradient;
    }
}
