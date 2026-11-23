using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// key names that follow the bindings, so a rebind on the controls page shows up everywhere
public static class KeyHint
{
    // the first keyboard binding's name, e.g. "ENTER" or "1"
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

    // an inline key, e.g. Key("Gameplay/Cancel") + " BACK" reads "[ESC] BACK"
    public static string Key(string action) {
        return "[" + Display(action) + "]";
    }
}

// a label that shows an action's key, e.g. <KeyLabel action="Gameplay/Fire" format="[{0}] FIRE" />
[UxmlElement]
public partial class KeyLabel : Label
{
    private string _action;
    private string _format = "[{0}]";

    [UxmlAttribute]
    public string action {
        get => _action;
        set { _action = value; Refresh(); }
    }

    // {0} is the key's name; "{0}" alone suits a key chip
    [UxmlAttribute]
    public string format {
        get => _format;
        set { _format = string.IsNullOrEmpty(value) ? "[{0}]" : value; Refresh(); }
    }

    public KeyLabel() {
        RegisterCallback<AttachToPanelEvent>(_ => {
            GameInput.bindingsChanged += Refresh;
            Refresh();
        });
        RegisterCallback<DetachFromPanelEvent>(_ => GameInput.bindingsChanged -= Refresh);
    }

    public void Refresh() {
        if (string.IsNullOrEmpty(_action)) return;
        // the editor's ui builder has no input actions to read
        var key = Application.isPlaying ? KeyHint.Display(_action) : _action;
        text = string.Format(_format, key);
    }
}
