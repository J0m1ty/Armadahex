using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ThisOtherThing.UI.Shapes;

// paints a graphic (rectangle, text or image) from the theme, so changing a token restyles every screen
[ExecuteAlways]
public class Themed : MonoBehaviour
{
    public ThemeColor fill = ThemeColor.None;
    [Range(0, 1)] public float alpha = 1;
    public ThemeColor outline = ThemeColor.None;
    [Range(0, 1)] public float outlineAlpha = 1;
    public ThemeFont font = ThemeFont.None;

    void OnEnable() {
        Apply();
    }

    void OnValidate() {
        Apply();
    }

    public void Apply() {
        var theme = UITheme.instance;
        if (theme == null) return;

        var rect = GetComponent<Rectangle>();
        if (rect != null) {
            rect.ShapeProperties.DrawFill = fill != ThemeColor.None;
            if (fill != ThemeColor.None) rect.ShapeProperties.FillColor = theme.Get(fill, alpha);
            rect.ShapeProperties.DrawOutline = outline != ThemeColor.None;
            if (outline != ThemeColor.None) rect.ShapeProperties.OutlineColor = theme.Get(outline, outlineAlpha);
            rect.ForceMeshUpdate();
            return;
        }

        var text = GetComponent<TMP_Text>();
        if (text != null) {
            if (fill != ThemeColor.None) text.color = theme.Get(fill, alpha);
            var asset = theme.Get(font);
            if (asset != null) text.font = asset;
            return;
        }

        var graphic = GetComponent<Graphic>();
        if (graphic != null && fill != ThemeColor.None) {
            graphic.color = theme.Get(fill, alpha);
        }
    }

    public static void RefreshAll() {
        foreach (var themed in FindObjectsByType<Themed>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
            themed.Apply();
        }
        foreach (var button in FindObjectsByType<UIButton>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
            button.Repaint();
        }
    }
}
