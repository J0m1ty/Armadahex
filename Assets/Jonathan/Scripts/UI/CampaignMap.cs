using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// the campaign as a graph that climbs from the bottom: a hex per mission and a line to each mission it opens up.
// lines between cleared missions are solid, a line to the next available one is dashed, the rest are faint
[UxmlElement]
public partial class CampaignMap : VisualElement
{
    private static readonly CustomStyleProperty<Color> accentProperty = new CustomStyleProperty<Color>("--accent");
    private static readonly Color faint = new Color(0.78f, 0.86f, 1f, 0.14f);

    [UxmlAttribute]
    public float rowHeight { get; set; } = 184;

    [UxmlAttribute]
    public float columnWidth { get; set; } = 300;

    // space above the top row and under the bottom one
    [UxmlAttribute]
    public float padding { get; set; } = 120;

    [UxmlAttribute]
    public float hexSize { get; set; } = 96;

    // lines go up, across and up again; off, they run straight from hex to hex
    [UxmlAttribute]
    public bool stepped { get; set; } = true;

    public event Action<Mission> picked;

    private readonly Dictionary<Mission, MissionNode> nodes = new Dictionary<Mission, MissionNode>();
    private Color accent = Color.white;
    private int rows;

    public CampaignMap() {
        generateVisualContent += Draw;
        RegisterCallback<CustomStyleResolvedEvent>(_ => {
            if (customStyle.TryGetValue(accentProperty, out var color)) accent = color;
        });
    }

    public void Fill(IReadOnlyList<Mission> missions) {
        Clear();
        nodes.Clear();

        rows = 0;
        foreach (var mission in missions) rows = Mathf.Max(rows, mission.row + 1);
        style.height = padding * 2 + Mathf.Max(0, rows - 1) * rowHeight;

        foreach (var mission in missions) {
            var node = new MissionNode(mission, hexSize);
            node.style.left = Length.Percent(50);
            node.style.marginLeft = mission.column * columnWidth - hexSize / 2;
            node.style.top = Center(mission).y - hexSize / 2;
            node.AddManipulator(new Clickable(() => picked?.Invoke(mission)));
            nodes[mission] = node;
            Add(node);
        }

        Refresh();
    }

    // states change after a win, and the picked one is outlined
    public void Refresh(Mission selected = null) {
        foreach (var pair in nodes) {
            pair.Value.Show(Missions.State(pair.Key), pair.Key == selected);
        }
        MarkDirtyRepaint();
    }

    public VisualElement NodeOf(Mission mission) {
        return mission != null && nodes.TryGetValue(mission, out var node) ? node : null;
    }

    // x from the middle of the map, y from the top
    private Vector2 Center(Mission mission) {
        var height = padding * 2 + Mathf.Max(0, rows - 1) * rowHeight;
        return new Vector2(mission.column * columnWidth, height - padding - mission.row * rowHeight);
    }

    private void Draw(MeshGenerationContext context) {
        var painter = context.painter2D;
        var middle = contentRect.width / 2;

        foreach (var pair in nodes) {
            var from = pair.Key;
            var fromState = Missions.State(from);
            foreach (var to in from.unlocks) {
                if (to == null || !nodes.ContainsKey(to)) continue;
                var toState = Missions.State(to);

                var a = Center(from) + new Vector2(middle, 0);
                var b = Center(to) + new Vector2(middle, 0);

                var path = Route(a, b);
                if (fromState == MissionState.Cleared && toState == MissionState.Cleared) {
                    Stroke(painter, path, new Color(accent.r, accent.g, accent.b, 0.85f), 3, 0);
                }
                else if (fromState == MissionState.Cleared) {
                    Stroke(painter, path, new Color(accent.r, accent.g, accent.b, 0.6f), 3, 10);
                }
                else {
                    Stroke(painter, path, faint, 2, 0);
                }
            }
        }
    }

    // the points a line passes through, trimmed so it starts and ends at the hexes' edges
    private List<Vector2> Route(Vector2 a, Vector2 b) {
        var points = new List<Vector2> { a };
        var dx = b.x - a.x;
        var dy = b.y - a.y;

        if (stepped && Mathf.Abs(dx) > 1) {
            var turn = a.y + dy / 2;
            points.Add(new Vector2(a.x, turn));
            points.Add(new Vector2(b.x, turn));
        }
        points.Add(b);

        var trim = hexSize / 2 + 8;
        points[0] = Toward(points[0], points[1], trim);
        points[points.Count - 1] = Toward(points[points.Count - 1], points[points.Count - 2], trim);
        return points;
    }

    private static Vector2 Toward(Vector2 from, Vector2 to, float distance) {
        var length = Vector2.Distance(from, to);
        return length <= distance ? from : from + (to - from) / length * distance;
    }

    private static void Stroke(Painter2D painter, List<Vector2> points, Color color, float width, float dash) {
        painter.strokeColor = color;
        painter.lineWidth = width;
        painter.lineJoin = LineJoin.Round;
        painter.lineCap = LineCap.Butt;
        painter.BeginPath();

        if (dash <= 0) {
            painter.MoveTo(points[0]);
            for (var i = 1; i < points.Count; i++) painter.LineTo(points[i]);
            painter.Stroke();
            return;
        }

        // dashes run on across the corners
        var on = true;
        var left = dash;
        painter.MoveTo(points[0]);
        for (var i = 1; i < points.Count; i++) {
            var from = points[i - 1];
            var to = points[i];
            var length = Vector2.Distance(from, to);
            var done = 0f;
            while (length - done > left) {
                done += left;
                var point = Vector2.Lerp(from, to, done / length);
                if (on) painter.LineTo(point);
                else painter.MoveTo(point);
                on = !on;
                left = dash;
            }
            left -= length - done;
            if (on) painter.LineTo(to);
            else painter.MoveTo(to);
        }
        painter.Stroke();
    }
}

// one mission on the map: its hex with the number (or a lock), its name beside it, clear of the lines
public class MissionNode : VisualElement
{
    public const float width = 320;

    private readonly MissionHex hex;
    private readonly Label number;
    private readonly LockIcon lockIcon;

    public MissionNode(Mission mission, float size) {
        AddToClassList("mission-node");
        style.position = Position.Absolute;
        style.width = width;

        hex = new MissionHex();
        hex.AddToClassList("mission-hex");
        hex.style.width = size;
        hex.style.height = size;
        Add(hex);

        number = new Label(mission.number.ToString("D2"));
        number.AddToClassList("mission-number");
        number.pickingMode = PickingMode.Ignore;
        hex.Add(number);

        lockIcon = new LockIcon();
        lockIcon.AddToClassList("mission-lock");
        hex.Add(lockIcon);

        var name = new Label(mission.title.ToUpper());
        name.AddToClassList("mission-name");
        name.pickingMode = PickingMode.Ignore;
        Add(name);

        RegisterCallback<PointerEnterEvent>(_ => hex.hovered = true);
        RegisterCallback<PointerLeaveEvent>(_ => hex.hovered = false);
    }

    public void Show(MissionState state, bool selected) {
        EnableInClassList("mission-node--locked", state == MissionState.Locked);
        EnableInClassList("mission-node--available", state == MissionState.Available);
        EnableInClassList("mission-node--cleared", state == MissionState.Cleared);
        EnableInClassList("lit", selected);
        UIKit.Show(number, state != MissionState.Locked);
        UIKit.Show(lockIcon, state == MissionState.Locked);
        hex.Show(state, selected);
    }
}

// a flat topped hex: filled in the accent once cleared, outlined in it while available (with a slow ring
// going out from it), faint while locked; a second outline around it when it's picked
public class MissionHex : VisualElement
{
    private static readonly CustomStyleProperty<Color> accentProperty = new CustomStyleProperty<Color>("--accent");
    private static readonly Color fill = new Color(0.055f, 0.086f, 0.157f, 0.96f);
    private static readonly Color lockedEdge = new Color(0.78f, 0.86f, 1f, 0.18f);
    private const float pulsePeriod = 2.4f;

    private Color accent = Color.white;
    private MissionState state;
    private bool selected;
    private bool hover;
    private IVisualElementScheduledItem tick;

    public bool hovered {
        set { hover = value; MarkDirtyRepaint(); }
    }

    public MissionHex() {
        pickingMode = PickingMode.Position;
        generateVisualContent += Draw;
        RegisterCallback<CustomStyleResolvedEvent>(_ => {
            if (customStyle.TryGetValue(accentProperty, out var color)) accent = color;
        });
        RegisterCallback<DetachFromPanelEvent>(_ => tick?.Pause());
    }

    public void Show(MissionState state, bool selected) {
        this.state = state;
        this.selected = selected;

        // only an available mission pulses
        if (state == MissionState.Available) {
            if (tick == null) tick = schedule.Execute(MarkDirtyRepaint).Every(16);
            else tick.Resume();
        }
        else tick?.Pause();

        MarkDirtyRepaint();
    }

    private void Draw(MeshGenerationContext context) {
        var rect = contentRect;
        if (rect.width <= 0) return;

        var painter = context.painter2D;
        var center = rect.center;
        var radius = rect.width / 2;
        var lift = hover ? 1f : 0.8f;

        if (state == MissionState.Available) {
            var t = Time.realtimeSinceStartup / pulsePeriod % 1f;
            var pulse = new Color(accent.r, accent.g, accent.b, 0.45f * (1 - t));
            Hex(painter, center, radius + 4 + t * 22);
            painter.strokeColor = pulse;
            painter.lineWidth = 2;
            painter.Stroke();
        }

        Hex(painter, center, radius);
        painter.fillColor = state == MissionState.Cleared ? Fade(accent, hover ? 1 : 0.9f) : fill;
        painter.Fill();

        if (state != MissionState.Cleared) {
            painter.strokeColor = state == MissionState.Available ? Fade(accent, lift) : hover ? Fade(lockedEdge, 2.2f) : lockedEdge;
            painter.lineWidth = 3;
            painter.Stroke();
        }

        if (selected) {
            Hex(painter, center, radius + 10);
            painter.strokeColor = accent;
            painter.lineWidth = 2;
            painter.Stroke();
        }
    }

    private static Color Fade(Color color, float amount) {
        return new Color(color.r, color.g, color.b, Mathf.Clamp01(color.a * amount));
    }

    private static void Hex(Painter2D painter, Vector2 center, float radius) {
        painter.BeginPath();
        for (var i = 0; i < 6; i++) {
            var angle = i * 60 * Mathf.Deg2Rad;
            var point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            if (i == 0) painter.MoveTo(point);
            else painter.LineTo(point);
        }
        painter.ClosePath();
    }
}
