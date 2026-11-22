using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using ThisOtherThing.UI.Shapes;
using ShadowProperties = ThisOtherThing.UI.GeoUtils.ShadowProperties;

public enum ButtonStyle {
    Primary,
    Secondary,
    Friendly,
    Danger,
    Ghost,
    Nav,
    Key,
    Fire,
    Tab
}

public enum ClickSound {
    Normal,
    Light,
    Heavy,
    None
}

// a button painted from the theme: hover and keyboard focus go gold, lit means picked (a tab, a ship, a pattern)
public class UIButton : Button
{
    public ButtonStyle style = ButtonStyle.Secondary;
    public ClickSound sound = ClickSound.Normal;
    public Rectangle background;
    public TMP_Text label;
    // a bar or underline that follows the state, optional
    public Graphic accent;
    [SerializeField] private bool lit;

    public bool isLit => lit;

    protected override void Awake() {
        base.Awake();
        transition = Transition.None;
    }

    public void SetLit(bool lit) {
        this.lit = lit;
        Repaint();
    }

    public void Repaint() {
        Paint(currentSelectionState);
    }

    protected override void DoStateTransition(SelectionState state, bool instant) {
        base.DoStateTransition(state, instant);
        Paint(state);
    }

    // a mouse click leaves the button selected, which would keep it gold after the pointer leaves
    public override void OnPointerExit(PointerEventData eventData) {
        base.OnPointerExit(eventData);
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject) {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    // wired as a persistent listener, so code that clears the runtime listeners keeps the sound
    public void PlaySound() {
        if (ButtonClick.instance == null) return;

        switch (sound) {
            case ClickSound.Normal: ButtonClick.instance.PlayClick(); break;
            case ClickSound.Light: ButtonClick.instance.PlayClickLight(); break;
            case ClickSound.Heavy: ButtonClick.instance.PlayClickHeavy(); break;
        }
    }

    private void Paint(SelectionState state) {
        var t = UITheme.instance;
        if (t == null) return;

        var hot = state == SelectionState.Highlighted || state == SelectionState.Selected || state == SelectionState.Pressed;
        var pressed = state == SelectionState.Pressed;
        var off = state == SelectionState.Disabled;

        var fill = t.Get(ThemeColor.Raised);
        var outline = t.Get(ThemeColor.LineStrong);
        var text = t.Get(ThemeColor.Ink);
        var accentColor = t.Get(ThemeColor.Friendly);
        Color? glow = null;

        switch (style) {
            case ButtonStyle.Primary:
                fill = off ? t.Get(ThemeColor.Raised) : (pressed ? t.gold * 0.85f : t.gold);
                fill.a = 1;
                outline = off ? t.Get(ThemeColor.Line) : fill;
                text = off ? t.Get(ThemeColor.Ink3) : t.goldInk;
                if (hot && !off) glow = t.gold;
                break;
            case ButtonStyle.Secondary:
                fill = t.Get(ThemeColor.Raised, 0.9f);
                outline = off ? t.Get(ThemeColor.Line) : (hot || lit ? t.gold : t.lineStrong);
                text = off ? t.Get(ThemeColor.Ink3) : (hot ? t.gold : t.ink);
                if ((hot || lit) && !off) glow = t.gold;
                break;
            case ButtonStyle.Friendly:
                fill = lit ? t.Get(ThemeColor.Friendly, 0.25f) : t.Get(ThemeColor.Deep, 0.9f);
                outline = hot ? t.gold : (lit ? t.friendly : t.Get(ThemeColor.LineStrong));
                text = off ? t.Get(ThemeColor.Ink3) : (lit || hot ? t.friendlyInk : t.ink2);
                if (hot) glow = t.gold;
                else if (lit) glow = t.friendly;
                break;
            case ButtonStyle.Danger:
                fill = hot ? t.Get(ThemeColor.Enemy, 0.22f) : t.Get(ThemeColor.Deep, 0.9f);
                outline = t.Get(ThemeColor.Enemy, hot ? 1 : 0.6f);
                text = off ? t.Get(ThemeColor.Ink3) : t.enemyInk;
                if (hot && !off) glow = t.enemy;
                break;
            case ButtonStyle.Ghost:
                fill = new Color(0, 0, 0, 0.001f);
                outline = Color.clear;
                text = off ? t.Get(ThemeColor.Ink3) : (hot ? t.gold : (lit ? t.ink : t.ink2));
                break;
            case ButtonStyle.Nav:
                fill = lit ? t.Get(ThemeColor.Gold, 0.14f) : t.Get(ThemeColor.Raised, 0.6f);
                outline = hot ? t.gold : (lit ? t.Get(ThemeColor.Gold, 0.5f) : Color.clear);
                text = off ? t.Get(ThemeColor.Ink3) : t.ink;
                accentColor = hot || lit ? t.gold : t.friendly;
                if (hot) glow = t.gold;
                break;
            case ButtonStyle.Key:
                fill = off ? t.Get(ThemeColor.Deep, 0.9f) : (lit ? t.Get(ThemeColor.Gold, 0.12f) : t.raised);
                outline = off ? t.Get(ThemeColor.Line) : (lit ? t.gold : (hot ? t.Get(ThemeColor.Gold, 0.7f) : t.lineStrong));
                text = off ? t.Get(ThemeColor.Ink3) : t.ink;
                if (lit && !off) glow = t.gold;
                break;
            case ButtonStyle.Fire:
                fill = off ? t.Get(ThemeColor.Raised, 0.9f) : t.Get(ThemeColor.Enemy, hot ? 0.4f : 0.25f);
                outline = off ? t.Get(ThemeColor.Line) : (hot ? t.gold : t.enemy);
                text = off ? t.Get(ThemeColor.Ink3) : t.enemyInk;
                if (!off) glow = t.enemy;
                break;
            case ButtonStyle.Tab:
                fill = new Color(0, 0, 0, 0.001f);
                outline = Color.clear;
                text = lit ? t.ink : (hot ? t.gold : t.ink2);
                accentColor = lit ? t.gold : Color.clear;
                break;
        }

        if (background != null) {
            background.ShapeProperties.DrawFill = true;
            background.ShapeProperties.FillColor = fill;
            background.ShapeProperties.DrawOutline = outline.a > 0;
            background.ShapeProperties.OutlineColor = outline;

            if (glow != null) {
                var g = glow.Value;
                g.a = 0.4f;
                background.ShadowProperties.ShowShadows = true;
                background.ShadowProperties.Shadows = new[] { new ShadowProperties { Color = g, Size = t.glowSize, Softness = 1 } };
            }
            else {
                background.ShadowProperties.ShowShadows = false;
            }
            background.ForceMeshUpdate();
        }

        if (label != null) label.color = text;
        if (accent != null) {
            var shape = accent as Rectangle;
            if (shape != null) {
                shape.ShapeProperties.FillColor = accentColor;
                shape.ForceMeshUpdate();
            }
            else {
                accent.color = accentColor;
            }
        }
    }
}
