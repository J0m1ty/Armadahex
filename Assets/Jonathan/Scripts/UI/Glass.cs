using UnityEngine;
using UnityEngine.UIElements;

// the lit look of a home control: a soft glow around it, and behind it the blurred scene through frosted glass
public static class Glass
{
    public static void Add(VisualElement element) {
        if (element == null) return;

        var glow = new VisualElement { pickingMode = PickingMode.Ignore };
        glow.AddToClassList("glass__glow");
        element.Insert(0, glow);

        var frost = new Frost();
        frost.AddToClassList("glass__frost");
        element.Insert(1, frost);

        // pointing at a control selects it, so only one is ever lit
        element.RegisterCallback<PointerEnterEvent>(_ => {
            if (element.enabledInHierarchy) element.Focus();
        });
    }
}

// shows the part of the blurred scene that sits behind it, so it reads as glass
public class Frost : VisualElement
{
    public Frost() {
        pickingMode = PickingMode.Ignore;
        schedule.Execute(Track).Every(0);
    }

    private void Track() {
        var texture = MenuBackdrop.blurred;
        if (texture == null || panel == null) {
            style.backgroundImage = StyleKeyword.Null;
            return;
        }

        // the texture covers the whole panel, so size it to the panel and slide it back by where this sits
        var root = panel.visualTree.layout;
        var at = worldBound.position - panel.visualTree.worldBound.position;
        style.backgroundImage = Background.FromRenderTexture(texture);
        style.backgroundSize = new BackgroundSize(root.width, root.height);
        style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left, -at.x);
        style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Top, -at.y);
        MarkDirtyRepaint();
    }
}
