using UnityEngine;
using UnityEngine.UIElements;

// what's new on the menu, from Resources/Changelog.txt for now (it'll come from the auth server later).
// "# title | version | date" starts an update, "## heading" a group inside it, "- text" a line.
// everything fades in and out near the top and bottom of the list as it scrolls
public static class Changelog
{
    // how far from the list's edges things fade out
    private const float fade = 72f;

    public static void Fill(ScrollView list, Texture2D image) {
        if (list == null) return;

        var file = Resources.Load<TextAsset>("Changelog");
        if (file == null) return;

        list.Clear();
        var first = true;
        foreach (var raw in file.text.Split('\n')) {
            var line = raw.Trim();
            if (line.StartsWith("## ")) {
                list.Add(Label(line.Substring(3).ToUpper(), "changelog__group"));
            }
            else if (line.StartsWith("# ")) {
                if (!first) list.Add(Gap());
                // the newest update leads with its picture
                if (first && image != null) {
                    var picture = new FadeImage { texture = image };
                    picture.AddToClassList("changelog__image");
                    list.Add(picture);
                }
                first = false;

                var parts = line.Substring(2).Split('|');
                list.Add(Label(parts[0].Trim().ToUpper(), "changelog__title"));
                if (parts.Length > 1) {
                    var version = parts[1].Trim();
                    if (parts.Length > 2) version += "  ·  " + parts[2].Trim().ToUpper();
                    list.Add(Label(version, "changelog__version"));
                }
            }
            else if (line.StartsWith("- ")) {
                var row = new VisualElement();
                row.AddToClassList("changelog__line");
                var dot = new VisualElement();
                dot.AddToClassList("changelog__dot");
                row.Add(dot);
                row.Add(Label(line.Substring(2), "changelog__text"));
                list.Add(row);
            }
        }

        list.schedule.Execute(() => Fade(list)).Every(0);
    }

    // each line fades by how close it is to an edge; at the very top or bottom of the list nothing fades on that side
    private static void Fade(ScrollView list) {
        var view = list.contentViewport.worldBound;
        var range = list.contentContainer.layout.height - list.contentViewport.layout.height;
        var topFade = Mathf.Clamp(list.scrollOffset.y, 0.01f, fade);
        var bottomFade = Mathf.Clamp(range - list.scrollOffset.y, 0.01f, fade);

        foreach (var child in list.contentContainer.Children()) {
            if (child is FadeImage picture) {
                picture.SetView(view.yMin, view.yMax, topFade, bottomFade);
                continue;
            }
            var center = child.worldBound.center.y;
            child.style.opacity = Mathf.Clamp01(Mathf.Min((center - view.yMin) / topFade, (view.yMax - center) / bottomFade));
        }
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
    private float topFade = 1f;
    private float bottomFade = 1f;

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
            var alpha = Mathf.Clamp01(Mathf.Min((worldY - viewTop) / topFade, (viewBottom - worldY) / bottomFade));
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
