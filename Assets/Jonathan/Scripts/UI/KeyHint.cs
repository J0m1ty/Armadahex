using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// a key label that follows the bindings, so a rebind on the controls page shows up everywhere
[RequireComponent(typeof(TMP_Text))]
public class KeyHint : MonoBehaviour
{
    // "Map/Action", e.g. "Gameplay/Fire"
    public string action;
    // text either side, e.g. "" and " BACK"
    public string prefix;
    public string suffix;

    private TMP_Text text;

    void Awake() {
        text = GetComponent<TMP_Text>();
    }

    void OnEnable() {
        GameInput.bindingsChanged += Refresh;
        Refresh();
    }

    void OnDisable() {
        GameInput.bindingsChanged -= Refresh;
    }

    public void Set(string action) {
        this.action = action;
        Refresh();
    }

    public void Refresh() {
        if (text == null || string.IsNullOrEmpty(action)) return;
        text.text = prefix + Display(action) + suffix;
    }

    // the first keyboard binding's name, e.g. "Enter" or "1"
    public static string Display(string action) {
        var found = GameInput.actions.FindAction(action);
        if (found == null) return "?";

        for (var i = 0; i < found.bindings.Count; i++) {
            var binding = found.bindings[i];
            if (binding.isComposite || binding.isPartOfComposite) continue;
            if (binding.effectivePath.StartsWith("<Keyboard>")) {
                return found.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontIncludeInteractions).ToUpper();
            }
        }
        return found.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions).ToUpper();
    }

    // an inline keycap for rich text, e.g. Key("Gameplay/Cancel") + " BACK"
    public static string Key(string action) {
        var theme = UITheme.instance;
        var line = theme != null ? ColorUtility.ToHtmlStringRGB(theme.lineStrong) : "2D4565";
        return "<mark=#" + line + "AA padding=\"10,10,6,6\">" + Display(action) + "</mark>";
    }
}
