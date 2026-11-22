using UnityEngine;
using TMPro;

public enum ThemeColor {
    None,
    Abyss,
    Deep,
    Raised,
    Line,
    LineStrong,
    Ink,
    Ink2,
    Ink3,
    Friendly,
    Enemy,
    Gold,
    GoldInk,
    Miss,
    Sonar,
    FriendlyInk,
    EnemyInk
}

public enum ThemeFont {
    None,
    Display,
    DisplayBold,
    Body,
    Mono
}

// the design tokens every screen reads (Resources/UI Theme), blue is your side, red is theirs, gold is what you're pointing at
[CreateAssetMenu(fileName = "UI Theme", menuName = "Armadahex/UI Theme")]
public class UITheme : ScriptableObject
{
    private static UITheme _instance;
    public static UITheme instance {
        get {
            if (_instance == null) {
                _instance = Resources.Load<UITheme>("UI Theme");
            }
            return _instance;
        }
    }

    [Header("Surfaces")]
    public Color abyss = Hex("05090F");
    public Color deep = Hex("0A121D");
    public Color raised = Hex("111C2B");
    public Color line = Hex("1F3047");
    public Color lineStrong = Hex("2D4565");

    [Header("Text")]
    public Color ink = Hex("E8EEF6");
    public Color ink2 = Hex("97A9C0");
    public Color ink3 = Hex("5A6D85");

    [Header("Sides")]
    public Color friendly = Hex("3B8CFF");
    public Color friendlyInk = Hex("CFE2FF");
    public Color enemy = Hex("FF4B3A");
    public Color enemyInk = Hex("FFC9C2");

    [Header("Accents")]
    public Color gold = Hex("FFC23D");
    public Color goldInk = Hex("1A1203");
    public Color miss = Hex("E9EDF2");
    public Color sonar = Hex("6CB6FF");

    [Header("Fonts")]
    public TMP_FontAsset display;
    public TMP_FontAsset displayBold;
    public TMP_FontAsset body;
    public TMP_FontAsset mono;

    [Header("Shape")]
    public float radiusSmall = 3;
    public float radius = 6;
    public float radiusLarge = 10;
    public float glowSize = 14;

    public Color Get(ThemeColor color, float alpha = 1) {
        Color c;
        switch (color) {
            case ThemeColor.Abyss: c = abyss; break;
            case ThemeColor.Deep: c = deep; break;
            case ThemeColor.Raised: c = raised; break;
            case ThemeColor.Line: c = line; break;
            case ThemeColor.LineStrong: c = lineStrong; break;
            case ThemeColor.Ink: c = ink; break;
            case ThemeColor.Ink2: c = ink2; break;
            case ThemeColor.Ink3: c = ink3; break;
            case ThemeColor.Friendly: c = friendly; break;
            case ThemeColor.FriendlyInk: c = friendlyInk; break;
            case ThemeColor.Enemy: c = enemy; break;
            case ThemeColor.EnemyInk: c = enemyInk; break;
            case ThemeColor.Gold: c = gold; break;
            case ThemeColor.GoldInk: c = goldInk; break;
            case ThemeColor.Miss: c = miss; break;
            case ThemeColor.Sonar: c = sonar; break;
            default: c = Color.white; break;
        }
        c.a *= alpha;
        return c;
    }

    public TMP_FontAsset Get(ThemeFont font) {
        switch (font) {
            case ThemeFont.Display: return display;
            case ThemeFont.DisplayBold: return displayBold != null ? displayBold : display;
            case ThemeFont.Body: return body;
            case ThemeFont.Mono: return mono;
            default: return null;
        }
    }

    // hex colours for rich text, e.g. "<color=#" + theme.Html(ThemeColor.Gold) + ">"
    public string Html(ThemeColor color) {
        return ColorUtility.ToHtmlStringRGB(Get(color));
    }

    private static Color Hex(string hex) {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }

    private void OnValidate() {
        Themed.RefreshAll();
    }
}
