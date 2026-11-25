using UnityEngine;
using UnityEngine.UIElements;

// what's new on the menu, from Resources/Changelog.txt for now (it'll come from the auth server later).
// "# version | date" starts an update, "## heading" a group inside it, "- text" a line.
// the newest update's version and date sit in the card's bar, its picture leads the list,
// and everything fades in and out near the top and bottom of the list as it scrolls
public static class Changelog
{
    // how far from the list's edges things fade out
    private const float fade = 72f;

    public static void Fill(ScrollView list, Label version, Label date, Texture2D image) {
        if (list == null) return;

        var file = Resources.Load<TextAsset>("Changelog");
        if (file == null) return;

        list.Clear();
        // each update's notes sit in a body
        VisualElement body = null;
        foreach (var raw in file.text.Split('\n')) {
            var line = raw.Trim();
            if (line.StartsWith("# ")) {
                var parts = line.Substring(2).Split('|');
                var number = parts[0].Trim();
                var when = parts.Length > 1 ? parts[1].Trim().ToUpper() : "";

                if (body == null) {
                    // the newest goes in the bar, with its picture first in the list
                    if (version != null) version.text = "PATCH NOTES " + number;
                    if (date != null) date.text = when;
                    if (image != null) {
                        var frame = new VisualElement();
                        frame.AddToClassList("changelog__image");
                        var picture = new FadeImage { texture = image };
                        picture.AddToClassList("changelog__picture");
                        frame.Add(picture);
                        list.Add(frame);
                    }
                    body = Body(list);
                }
                else {
                    list.Add(Gap());
                    body = Body(list);
                    body.Add(Label(when == "" ? number : number + "  ·  " + when, "changelog__version"));
                }
            }
            else if (body == null) {
                continue;
            }
            else if (line.StartsWith("## ")) {
                body.Add(Label(line.Substring(3).ToUpper(), "changelog__group"));
            }
            else if (line.StartsWith("- ")) {
                var row = new VisualElement();
                row.AddToClassList("changelog__line");
                var dot = new VisualElement();
                dot.AddToClassList("changelog__dot");
                row.Add(dot);
                row.Add(Label(line.Substring(2), "changelog__text"));
                body.Add(row);
            }
        }

        list.schedule.Execute(() => Fade(list)).Every(0);
    }

    // a side only fades once there's more to scroll that way, so nothing fades at rest
    private static void Fade(ScrollView list) {
        var view = list.contentViewport.worldBound;
        var range = list.contentContainer.layout.height - list.contentViewport.layout.height;
        var topFade = Mathf.Min(list.scrollOffset.y, fade);
        var bottomFade = Mathf.Min(range - list.scrollOffset.y, fade);

        foreach (var child in list.contentContainer.Children()) {
            if (child.childCount > 0 && child[0] is FadeImage picture) {
                picture.SetView(view.yMin, view.yMax, topFade, bottomFade);
                continue;
            }
            // a body's lines fade one by one
            if (child.ClassListContains("changelog__body")) {
                foreach (var line in child.Children()) line.style.opacity = Visible(line.worldBound.center.y, view.yMin, view.yMax, topFade, bottomFade);
                continue;
            }
            child.style.opacity = Visible(child.worldBound.center.y, view.yMin, view.yMax, topFade, bottomFade);
        }
    }

    // how visible something at y is, 0 at an edge that fades, 1 a fade's width inside it
    public static float Visible(float y, float top, float bottom, float topFade, float bottomFade) {
        var fromTop = topFade < 1f ? 1f : (y - top) / topFade;
        var fromBottom = bottomFade < 1f ? 1f : (bottom - y) / bottomFade;
        return Mathf.Clamp01(Mathf.Min(fromTop, fromBottom));
    }

    private static VisualElement Body(ScrollView list) {
        var body = new VisualElement();
        body.AddToClassList("changelog__body");
        list.Add(body);
        return body;
    }

    private static Label Label(string text, string className) {
        var label = new Label(text);
        label.AddToClassList(className);
        return label;
    }

    private static VisualElement Gap() {
        var gap = new VisualElement();
        gap.AddToClassList("changelog__gap");
        return gap;
    }
}

// a picture cropped to fill its box, drawn as a strip of rows so it can fade smoothly where it meets the list's edges
public class FadeImage : VisualElement
{
    private const int rows = 32;

    public Texture2D texture;

    private float viewTop = float.MinValue;
    private float viewBottom = float.MaxValue;
    private float topFade;
    private float bottomFade;

    public FadeImage() {
        pickingMode = PickingMode.Ignore;
        generateVisualContent += Draw;
    }

    public void SetView(float top, float bottom, float fadeTop, float fadeBottom) {
        viewTop = top;
        viewBottom = bottom;
        topFade = fadeTop;
        bottomFade = fadeBottom;
        MarkDirtyRepaint();
    }

    private void Draw(MeshGenerationContext context) {
        var box = contentRect;
        if (texture == null || box.width <= 0 || box.height <= 0) return;

        // crop the picture to the box's shape, keeping its middle
        var boxAspect = box.width / box.height;
        var imageAspect = (float)texture.width / texture.height;
        var u = new Vector2(0f, 1f);
        var v = new Vector2(0f, 1f);
        if (imageAspect > boxAspect) {
            var keep = boxAspect / imageAspect;
            u = new Vector2((1f - keep) / 2f, (1f + keep) / 2f);
        }
        else {
            var keep = imageAspect / boxAspect;
            v = new Vector2((1f - keep) / 2f, (1f + keep) / 2f);
        }

        var mesh = context.Allocate((rows + 1) * 2, rows * 6, texture);
        var top = worldBound.yMin;
        for (var i = 0; i <= rows; i++) {
            var t = (float)i / rows;
            var y = box.yMin + t * box.height;
            var worldY = top + t * worldBound.height;
            var alpha = Changelog.Visible(worldY, viewTop, viewBottom, topFade, bottomFade);
            var tint = new Color32(255, 255, 255, (byte)(alpha * alpha * (3 - 2 * alpha) * 255));
            var uvY = Mathf.Lerp(v.y, v.x, t);
            mesh.SetNextVertex(new Vertex { position = new Vector3(box.xMin, y, Vertex.nearZ), tint = tint, uv = new Vector2(u.x, uvY) });
            mesh.SetNextVertex(new Vertex { position = new Vector3(box.xMax, y, Vertex.nearZ), tint = tint, uv = new Vector2(u.y, uvY) });
        }
        for (var i = 0; i < rows; i++) {
            var a = (ushort)(i * 2);
            mesh.SetNextIndex(a);
            mesh.SetNextIndex((ushort)(a + 1));
            mesh.SetNextIndex((ushort)(a + 2));
            mesh.SetNextIndex((ushort)(a + 1));
            mesh.SetNextIndex((ushort)(a + 3));
            mesh.SetNextIndex((ushort)(a + 2));
        }
    }
}
