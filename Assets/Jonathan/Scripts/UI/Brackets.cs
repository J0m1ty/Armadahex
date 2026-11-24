using UnityEngine;
using UnityEngine.UIElements;

// two lines just outside a hex plate button, one over its top and one under its bottom, each running
// down into the cut ends: they tighten on hover
[UxmlElement]
public partial class Brackets : VisualElement
{
    private static readonly CustomStyleProperty<Color> accentProperty = new CustomStyleProperty<Color>("--accent");
    private static readonly float slope = Mathf.Tan(30 * Mathf.Deg2Rad);

    // how far outside the plate the lines sit, and how far down each cut end they reach (0 to 1)
    [UxmlAttribute]
    public float gap { get; set; } = 8;

    [UxmlAttribute]
    public float reach { get; set; } = 0.6f;


    private Color color = Color.white;
    private float hover;
    private bool hovered;
    private IVisualElementScheduledItem tick;

    public Brackets() {
        pickingMode = PickingMode.Ignore;
        generateVisualContent += Draw;
        RegisterCallback<CustomStyleResolvedEvent>(_ => {
            if (customStyle.TryGetValue(accentProperty, out var accent)) color = accent;
        });
        RegisterCallback<AttachToPanelEvent>(_ => Attach());
        RegisterCallback<DetachFromPanelEvent>(_ => tick?.Pause());
    }

    private void Attach() {
        if (parent != null) {
            parent.RegisterCallback<PointerEnterEvent>(_ => hovered = true);
            parent.RegisterCallback<PointerLeaveEvent>(_ => hovered = false);
            parent.RegisterCallback<FocusInEvent>(_ => hovered = true);
            parent.RegisterCallback<FocusOutEvent>(_ => hovered = false);
        }
        tick = schedule.Execute(() => {
            hover = Mathf.MoveTowards(hover, hovered ? 1 : 0, 0.12f);
            MarkDirtyRepaint();
        }).Every(16);
    }

    private void Draw(MeshGenerationContext context) {
        var width = contentRect.width;
        var height = contentRect.height;
        if (width <= 0 || height <= 0 || parent == null || !parent.enabledInHierarchy) return;

        var painter = context.painter2D;
        painter.lineWidth = 2;
        painter.lineJoin = LineJoin.Miter;

        // the resting lines, closer and brighter while hovered
        Draw(painter, width, height, Mathf.Lerp(gap, gap * 0.5f, hover), Mathf.Lerp(0.45f, 1, hover));

    }

    private void Draw(Painter2D painter, float width, float height, float offset, float alpha) {
        var run = height / 2 * slope;
        var plate = new[] {
            new Vector2(0, height / 2), new Vector2(run, 0), new Vector2(width - run, 0),
            new Vector2(width, height / 2), new Vector2(width - run, height), new Vector2(run, height)
        };
        var outline = Outset(plate, offset);
        painter.strokeColor = new Color(color.r, color.g, color.b, color.a * alpha);

        // outline: 0 left tip, 1 top left, 2 top right, 3 right tip, 4 bottom right, 5 bottom left
        Rail(painter, outline[1], outline[2], outline[0], outline[3]);
        Rail(painter, outline[5], outline[4], outline[0], outline[3]);
    }

    // along a flat edge from corner to corner, with each end bending down its cut edge toward the tip
    private void Rail(Painter2D painter, Vector2 left, Vector2 right, Vector2 leftTip, Vector2 rightTip) {
        painter.BeginPath();
        painter.MoveTo(Vector2.Lerp(left, leftTip, reach));
        painter.LineTo(left);
        painter.LineTo(right);
        painter.LineTo(Vector2.Lerp(right, rightTip, reach));
        painter.Stroke();
    }

    // pushes each edge away from the middle by offset and intersects neighbouring edges
    private static Vector2[] Outset(Vector2[] polygon, float offset) {
        var count = polygon.Length;
        var center = Vector2.zero;
        foreach (var point in polygon) center += point / count;

        var result = new Vector2[count];
        for (var i = 0; i < count; i++) {
            var a = Shift(polygon[(i + count - 1) % count], polygon[i], center, offset, out var da);
            var b = Shift(polygon[i], polygon[(i + 1) % count], center, offset, out var db);
            var cross = da.x * db.y - da.y * db.x;
            var t = ((b.x - a.x) * db.y - (b.y - a.y) * db.x) / cross;
            result[i] = a + da * t;
        }
        return result;
    }

    private static Vector2 Shift(Vector2 from, Vector2 to, Vector2 center, float offset, out Vector2 direction) {
        direction = (to - from).normalized;
        var normal = new Vector2(-direction.y, direction.x);
        if (Vector2.Dot(normal, (from + to) / 2 - center) < 0) normal = -normal;
        return from + normal * offset;
    }
}
