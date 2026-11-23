using UnityEngine;
using UnityEngine.UIElements;

// a short line just outside each of a hex plate button's four corners: they tighten on hover,
// and an echo pulses outward and fades every few seconds, like the miss marker
[UxmlElement]
public partial class Brackets : VisualElement
{
    private static readonly CustomStyleProperty<Color> accentProperty = new CustomStyleProperty<Color>("--accent");
    private static readonly float slope = Mathf.Tan(30 * Mathf.Deg2Rad);

    // how far outside the plate the lines sit, and how long they are
    [UxmlAttribute]
    public float gap { get; set; } = 8;

    [UxmlAttribute]
    public float length { get; set; } = 20;

    // false: along the flat top and bottom edges, true: along the cut ends
    [UxmlAttribute]
    public bool angled { get; set; }

    // seconds between echoes
    [UxmlAttribute]
    public float period { get; set; } = 2.4f;

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

        // the echo: out and away, then quiet until the next one
        var t = Mathf.Repeat(Time.realtimeSinceStartup, period) / period;
        var pulse = Mathf.Clamp01(t / 0.45f);
        if (pulse < 1) {
            var ease = 1 - (1 - pulse) * (1 - pulse);
            Draw(painter, width, height, gap + ease * 18, (1 - pulse) * 0.6f);
        }
    }

    private void Draw(Painter2D painter, float width, float height, float offset, float alpha) {
        var run = height / 2 * slope;
        var plate = new[] {
            new Vector2(0, height / 2), new Vector2(run, 0), new Vector2(width - run, 0),
            new Vector2(width, height / 2), new Vector2(width - run, height), new Vector2(run, height)
        };
        var outline = Outset(plate, offset);
        painter.strokeColor = new Color(color.r, color.g, color.b, color.a * alpha);

        // each corner where a flat edge meets a cut one: 1 top left, 2 top right, 4 bottom right, 5 bottom left
        foreach (var corner in new[] { 1, 2, 4, 5 }) {
            var at = outline[corner];
            var next = angled
                ? outline[corner == 1 || corner == 5 ? 0 : 3]
                : outline[corner == 1 ? 2 : corner == 2 ? 1 : corner == 4 ? 5 : 4];
            painter.BeginPath();
            painter.MoveTo(at);
            painter.LineTo(at + (next - at).normalized * length);
            painter.Stroke();
        }
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
