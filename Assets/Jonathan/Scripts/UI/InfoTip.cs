using UnityEngine;
using UnityEngine.UIElements;

// a small (i) beside a name; pointing at it shows what the name means just to its right, over the page
[UxmlElement]
public partial class InfoTip : VisualElement
{
    [UxmlAttribute]
    public string text {
        get => bubble.text;
        set => bubble.text = value;
    }

    private readonly Label bubble = new Label();
    private VisualElement layer;

    public InfoTip() {
        AddToClassList("info-tip");
        bubble.AddToClassList("info-tip__bubble");
        bubble.pickingMode = PickingMode.Ignore;
        generateVisualContent += Draw;

        RegisterCallback<PointerEnterEvent>(_ => Open());
        RegisterCallback<PointerLeaveEvent>(_ => Close());
        RegisterCallback<DetachFromPanelEvent>(_ => Close());
        RegisterCallback<CustomStyleResolvedEvent>(_ => MarkDirtyRepaint());
    }

    private void Open() {
        layer = GetFirstAncestorOfType<TemplateContainer>() ?? panel?.visualTree;
        if (layer == null) return;

        var bounds = worldBound;
        var at = layer.WorldToLocal(new Vector2(bounds.xMax, bounds.center.y));
        bubble.style.left = at.x;
        bubble.style.top = at.y;
        layer.Add(bubble);
    }

    private void Close() {
        bubble.RemoveFromHierarchy();
    }

    private void Draw(MeshGenerationContext context) {
        var rect = contentRect;
        if (rect.width <= 0 || rect.height <= 0) return;

        var color = resolvedStyle.color;
        var painter = context.painter2D;
        var center = rect.center;
        var radius = Mathf.Min(rect.width, rect.height) / 2 - 1;

        painter.strokeColor = color;
        painter.lineWidth = 2;
        painter.BeginPath();
        painter.Arc(center, radius, Angle.Degrees(0), Angle.Degrees(360));
        painter.Stroke();

        // the i: a dot over a short line
        painter.BeginPath();
        painter.MoveTo(new Vector2(center.x, center.y - radius * 0.1f));
        painter.LineTo(new Vector2(center.x, center.y + radius * 0.5f));
        painter.Stroke();

        painter.fillColor = color;
        painter.BeginPath();
        painter.Arc(new Vector2(center.x, center.y - radius * 0.45f), 1.4f, Angle.Degrees(0), Angle.Degrees(360));
        painter.Fill();
    }
}
