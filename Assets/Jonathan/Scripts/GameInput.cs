using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// the game's input actions (Resources/Controls), shared by every script so a rebind reaches all of them
public static class GameInput
{
    private static InputActionAsset asset;

    public static InputActionAsset actions {
        get {
            if (asset == null) {
                asset = Resources.Load<InputActionAsset>("Controls");
                LoadBindings();
                asset.Enable();
            }
            return asset;
        }
    }

    // "Map/Action", e.g. "Camera/Move"
    public static InputAction Get(string action) {
        return actions.FindAction(action, true);
    }

    // true while a text field has focus, so typing (chat, names) doesn't trigger hotkeys
    public static bool typing {
        get {
            // looked up once a frame, a scene has one or two documents
            if (Time.frameCount != documentsFrame) {
                documentsFrame = Time.frameCount;
                documents = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            }

            foreach (var document in documents) {
                var focused = UIKit.Focused(document) as VisualElement;
                if (focused == null) continue;
                var field = focused as TextField ?? focused.GetFirstAncestorOfType<TextField>();
                if (field != null && !field.isReadOnly) return true;
            }
            return false;
        }
    }

    private static UIDocument[] documents = new UIDocument[0];
    private static int documentsFrame = -1;

    // key labels on screen listen for this
    public static event Action bindingsChanged;

    // keyboardOnly keeps the mouse out, so the click that started the rebind can't become the new key
    public static void Rebind(InputAction action, int bindingIndex, Action onDone = null, bool keyboardOnly = false) {
        action.Disable();
        var rebind = action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .WithControlsExcluding("<Pointer>/position")
            .WithControlsExcluding("<Pointer>/delta")
            .WithControlsExcluding("<Mouse>/scroll");
        if (keyboardOnly) {
            rebind.WithControlsExcluding("<Mouse>");
        }
        rebind
            .OnComplete(operation => {
                operation.Dispose();
                action.Enable();
                SaveBindings();
                bindingsChanged?.Invoke();
                onDone?.Invoke();
            })
            .OnCancel(operation => {
                operation.Dispose();
                action.Enable();
                onDone?.Invoke();
            })
            .Start();
    }

    public static void SaveBindings() {
        PlayerPrefs.SetString(Constants.INPUT_BINDINGS_PREF_KEY, actions.SaveBindingOverridesAsJson());
    }

    public static void ResetBindings() {
        actions.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(Constants.INPUT_BINDINGS_PREF_KEY);
        bindingsChanged?.Invoke();
    }

    private static void LoadBindings() {
        var json = PlayerPrefs.GetString(Constants.INPUT_BINDINGS_PREF_KEY, "");
        if (!string.IsNullOrEmpty(json)) {
            asset.LoadBindingOverridesFromJson(json);
        }
    }
}
