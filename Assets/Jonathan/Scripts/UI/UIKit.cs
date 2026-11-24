using UnityEngine;
using UnityEngine.UIElements;

public enum ClickSound {
    Normal,
    Light,
    Heavy,
    None
}

// small helpers for the ui toolkit screens: showing, picking, clicking from code
public static class UIKit
{
    public const string hiddenClass = "hidden";
    public const string litClass = "lit";

    public static void Show(VisualElement element, bool show) {
        if (element == null) return;
        element.EnableInClassList(hiddenClass, !show);
    }

    public static bool IsShown(VisualElement element) {
        if (element == null) return false;
        for (var e = element; e != null; e = e.parent) {
            if (e.ClassListContains(hiddenClass) || e.resolvedStyle.display == DisplayStyle.None) return false;
        }
        return element.panel != null;
    }

    // marks what's picked: a tab, a ship, a pattern
    public static void SetLit(VisualElement element, bool lit) {
        if (element == null) return;
        element.EnableInClassList(litClass, lit);
    }

    // presses a button from code, the same as a click
    public static void Click(VisualElement element) {
        if (element == null || !element.enabledInHierarchy) return;
        using (var e = NavigationSubmitEvent.GetPooled()) {
            e.target = element;
            element.SendEvent(e);
        }
    }

    public static void PlaySound(ClickSound sound) {
        if (ButtonClick.instance == null) return;

        switch (sound) {
            case ClickSound.Normal: ButtonClick.instance.PlayClick(); break;
            case ClickSound.Light: ButtonClick.instance.PlayClickLight(); break;
            case ClickSound.Heavy: ButtonClick.instance.PlayClickHeavy(); break;
        }
    }

    // a click on any element, a button or not, with its sound
    public static void OnClick(VisualElement element, System.Action action, ClickSound sound = ClickSound.Normal) {
        if (element == null) return;

        System.Action run = () => {
            PlaySound(sound);
            action?.Invoke();
        };

        if (element is Button button) {
            button.clicked += run;
        }
        else {
            element.AddManipulator(new Clickable(run));
            // submit (enter on a focused element) clicks it too
            element.RegisterCallback<NavigationSubmitEvent>(_ => run());
        }
    }

    // the focused element of a document, if any
    public static Focusable Focused(UIDocument document) {
        if (document == null || document.rootVisualElement == null) return null;
        return document.rootVisualElement.focusController?.focusedElement;
    }

    public static void Blur(UIDocument document) {
        var focused = Focused(document) as VisualElement;
        focused?.Blur();
    }

    // a quick side to side shake, for a button whose action just failed
    public static void Shake(VisualElement element) {
        if (element == null) return;

        var start = Time.realtimeSinceStartup;
        IVisualElementScheduledItem shake = null;
        shake = element.schedule.Execute(() => {
            var t = (Time.realtimeSinceStartup - start) / 0.4f;
            if (t >= 1) {
                element.style.translate = StyleKeyword.Null;
                shake.Pause();
                return;
            }
            var x = Mathf.Sin(t * Mathf.PI * 6) * 10 * (1 - t);
            element.style.translate = new Translate(x, 0);
        }).Every(16);
    }
}
