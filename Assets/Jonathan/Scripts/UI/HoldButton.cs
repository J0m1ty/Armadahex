using System;
using UnityEngine;
using UnityEngine.UIElements;

// a button that only goes off once it's been held: an outline draws itself around it while it fills
[UxmlElement]
public partial class HoldButton : Button
{
    [UxmlAttribute]
    public float holdTime { get; set; } = 1.2f;

    [UxmlAttribute]
    public Color strokeColor { get; set; } = new Color(1f, 0.43f, 0.39f);

    [UxmlAttribute]
    public float strokeWidth { get; set; } = 3f;

    // held down with the pointer, the menu also holds it from enter
    public bool pointerHeld { get; private set; }

    public event Action held;

    private float _progress;
    public float progress {
        get => _progress;
        set {
            var next = Mathf.Clamp01(value);
            if (Mathf.Approximately(next, _progress)) return;
            _progress = next;
            EnableInClassList("holding", _progress > 0);
            MarkDirtyRepaint();
        }
    }

    public HoldButton() {
        generateVisualContent += Draw;
        RegisterCallback<PointerDownEvent>(e => {
            if (e.button != 0) return;
            pointerHeld = true;
            this.CapturePointer(e.pointerId);
        }, TrickleDown.TrickleDown);
        RegisterCallback<PointerUpEvent>(e => {
            pointerHeld = false;
            if (this.HasPointerCapture(e.pointerId)) this.ReleasePointer(e.pointerId);
        });
        RegisterCallback<PointerCaptureOutEvent>(_ => pointerHeld = false);
    }

    // fills by dt while pressed and drains when let go, going off once at the top
    public void Step(bool pressed, float dt) {
        if (!enabledInHierarchy) pressed = false;

        var before = progress;
        progress = pressed ? progress + dt / Mathf.Max(0.01f, holdTime) : progress - dt / Mathf.Max(0.01f, holdTime) * 3f;

        if (before < 1 && progress >= 1) {
            held?.Invoke();
        }
    }

    // the outline starts top centre and runs clockwise round the edge
    private void Draw(MeshGenerationContext context) {
        if (progress <= 0) return;

        var r = contentRect;
        var inset = strokeWidth / 2;
        var left = inset;
        var top = inset;
        var right = r.width - inset;
        var bottom = r.height - inset;
        var mid = r.width / 2;

        var points = new[] {
            new Vector2(mid, top), new Vector2(right, top), new Vector2(right, bottom),
            new Vector2(left, bottom), new Vector2(left, top), new Vector2(mid, top)
        };

        var length = 0f;
        for (var i = 1; i < points.Length; i++) length += Vector2.Distance(points[i - 1], points[i]);
        var remaining = length * progress;

        var painter = context.painter2D;
        painter.strokeColor = strokeColor;
        painter.lineWidth = strokeWidth;
        painter.lineJoin = LineJoin.Miter;
        painter.BeginPath();
        painter.MoveTo(points[0]);
        for (var i = 1; i < points.Length && remaining > 0; i++) {
            var segment = Vector2.Distance(points[i - 1], points[i]);
            if (segment <= remaining) {
                painter.LineTo(points[i]);
                remaining -= segment;
            }
            else {
                painter.LineTo(Vector2.Lerp(points[i - 1], points[i], remaining / segment));
                remaining = 0;
            }
        }
        painter.Stroke();
    }
}
