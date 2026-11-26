using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

// a small drawing of the hexes an attack pattern hits around its target, centred in the element
public class PatternShape : VisualElement
{
    // cube offsets of the six neighbours, the same order as CoordinateSystem.GetNeighbor
    private static readonly Vector3Int[] neighbours = {
        new Vector3Int(1, -1, 0), new Vector3Int(1, 0, -1), new Vector3Int(0, 1, -1),
        new Vector3Int(-1, 1, 0), new Vector3Int(-1, 0, 1), new Vector3Int(0, -1, 1)
    };

    // a pattern that runs to the board's edge is drawn this long each way
    private const int edgeLength = 3;

    private List<Vector3Int> cells = new List<Vector3Int>();
    private Color color = Color.white;

    public PatternShape() {
        AddToClassList("angle__shape");
        pickingMode = PickingMode.Ignore;
        generateVisualContent += Draw;
    }

    public void Set(AttackPattern pattern, Color color) {
        this.color = color;
        cells = Cells(pattern);
        MarkDirtyRepaint();
    }

    public void SetColor(Color color) {
        this.color = color;
        MarkDirtyRepaint();
    }

    private static List<Vector3Int> Cells(AttackPattern pattern) {
        var found = new List<Vector3Int>();
        if (pattern == null) {
            found.Add(Vector3Int.zero);
            return found;
        }

        // fixed shapes: x along neighbour 0, y along neighbour 1 (as AttackUIManager.Offset)
        if (pattern.cells != null && pattern.cells.Length > 0) {
            foreach (var cell in pattern.cells) {
                found.Add(new Vector3Int(cell.x + cell.y, -cell.x, -cell.y));
            }
            return found.Distinct().ToList();
        }

        if (pattern.attackCenter || pattern.doOffset) found.Add(Vector3Int.zero);

        foreach (var direction in pattern.directions) {
            var index = AttackUIManager.Convert(direction.rotation, direction.reverse);
            var step = Mathf.Max(1, direction.step);
            var length = pattern.doOffset || direction.noRange ? edgeLength : direction.length;

            // across the whole board: both ways from the target
            var ways = pattern.doOffset ? new[] { index, (index + 3) % 6 } : new[] { index };
            foreach (var way in ways) {
                var at = Vector3Int.zero;
                for (var n = 0; n < length; n++) {
                    at += neighbours[way] * step;
                    found.Add(at);
                }
            }
        }
        return found.Distinct().ToList();
    }

    private void Draw(MeshGenerationContext context) {
        if (cells.Count == 0) return;

        // flat-topped hexes; scale them so the whole shape fits
        var centres = cells.Select(c => new Vector2(1.5f * c.x, Mathf.Sqrt(3) * (c.y + c.x / 2f))).ToList();
        var min = new Vector2(centres.Min(p => p.x) - 1, centres.Min(p => p.y) - 0.87f);
        var max = new Vector2(centres.Max(p => p.x) + 1, centres.Max(p => p.y) + 0.87f);
        var size = max - min;
        var rect = contentRect;
        var scale = Mathf.Min(rect.width / size.x, rect.height / size.y, rect.height / 4f);
        var offset = rect.center - (min + size / 2) * scale;

        var painter = context.painter2D;
        painter.fillColor = color;
        foreach (var centre in centres) {
            var at = offset + centre * scale;
            var radius = scale * 0.92f;
            painter.BeginPath();
            for (var i = 0; i < 6; i++) {
                var angle = Mathf.PI / 3 * i;
                var point = at + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                if (i == 0) painter.MoveTo(point);
                else painter.LineTo(point);
            }
            painter.ClosePath();
            painter.Fill();
        }
    }
}
