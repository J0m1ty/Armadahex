using UnityEngine;
using UnityEngine.UIElements;

// draws a BoardSnapshot as a flat hex map fitted to the element: water, land, ships in the side's color,
// sunk ships darkened, a dark mark on every hit (it reads on any side color) and a white one on every miss
[UxmlElement]
public partial class BoardView : VisualElement
{
    private static readonly Color water = new Color(0.08f, 0.13f, 0.22f);
    private static readonly Color land = new Color(0.12f, 0.13f, 0.15f);
    private static readonly Color hitMark = new Color(0.04f, 0.05f, 0.09f, 0.85f);
    private static readonly Color missMark = new Color(0.85f, 0.9f, 1f, 0.7f);

    private BoardSnapshot snapshot;
    private Color side = Color.white;

    public BoardView() {
        generateVisualContent += Draw;
    }

    public void Show(BoardSnapshot board, Color sideColor) {
        snapshot = board;
        side = sideColor;
        MarkDirtyRepaint();
    }

    private void Draw(MeshGenerationContext context) {
        var rect = contentRect;
        if (snapshot == null || snapshot.cells.Count == 0 || rect.width <= 0 || rect.height <= 0) return;

        // fit the board's extent into the element, keeping hexes regular
        var min = new Vector2(float.MaxValue, float.MaxValue);
        var max = new Vector2(float.MinValue, float.MinValue);
        foreach (var cell in snapshot.cells) {
            min = Vector2.Min(min, cell.position);
            max = Vector2.Max(max, cell.position);
        }
        var extent = (max - min) + Vector2.one * snapshot.hexSize * 2;
        var scale = Mathf.Min(rect.width / extent.x, rect.height / extent.y);
        var middle = (min + max) / 2;
        var radius = snapshot.hexSize * scale * 0.94f;

        var painter = context.painter2D;
        var sunk = Color.Lerp(side, Color.black, 0.65f);

        foreach (var cell in snapshot.cells) {
            // board z runs away from the camera, so it's flipped to read top-down
            var offset = cell.position - middle;
            var center = rect.center + new Vector2(offset.x, -offset.y) * scale;

            var fill = cell.land ? land : cell.ship >= 0 ? (cell.shipSunk ? sunk : side) : water;
            Hex(painter, center, radius, fill);

            if (cell.hit) Dot(painter, center, radius * 0.34f, hitMark);
            else if (cell.miss) Dot(painter, center, radius * 0.18f, missMark);
        }
    }

    // flat topped, like the board
    private static void Hex(Painter2D painter, Vector2 center, float radius, Color color) {
        painter.fillColor = color;
        painter.BeginPath();
        for (var i = 0; i < 6; i++) {
            var angle = Mathf.Deg2Rad * 60 * i;
            var point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            if (i == 0) painter.MoveTo(point);
            else painter.LineTo(point);
        }
        painter.ClosePath();
        painter.Fill();
    }

    private static void Dot(Painter2D painter, Vector2 center, float radius, Color color) {
        painter.fillColor = color;
        painter.BeginPath();
        painter.Arc(center, radius, Angle.Degrees(0), Angle.Degrees(360));
        painter.Fill();
    }
}
