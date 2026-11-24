using UnityEngine;
using UnityEngine.UIElements;

// a padlock drawn in the element's text color: a shackle over a body
[UxmlElement]
public partial class LockIcon : VisualElement
{
    public LockIcon() {
        pickingMode = PickingMode.Ignore;
        generateVisualContent += Draw;
    }

    private void Draw(MeshGenerationContext context) {
        var rect = contentRect;
        if (rect.width <= 0 || rect.height <= 0) return;

        var color = resolvedStyle.color;
        var painter = context.painter2D;
        var body = rect.height * 0.55f;
        var line = Mathf.Max(2, rect.width * 0.12f);

        // the shackle: an arch over the top
        var radius = rect.width * 0.3f;
        var center = new Vector2(rect.width / 2, rect.height - body);
        painter.strokeColor = color;
        painter.lineWidth = line;
        painter.BeginPath();
        painter.MoveTo(new Vector2(center.x - radius, center.y + line / 2));
        painter.LineTo(new Vector2(center.x - radius, radius + line / 2));
        painter.Arc(new Vector2(center.x, radius + line / 2), radius, Angle.Degrees(180), Angle.Degrees(360));
        painter.LineTo(new Vector2(center.x + radius, center.y + line / 2));
        painter.Stroke();

        // the body
        painter.fillColor = color;
        painter.BeginPath();
        painter.MoveTo(new Vector2(0, rect.height - body));
        painter.LineTo(new Vector2(rect.width, rect.height - body));
        painter.LineTo(new Vector2(rect.width, rect.height));
        painter.LineTo(new Vector2(0, rect.height));
        painter.ClosePath();
        painter.Fill();
    }
}
