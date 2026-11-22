using System;
using UnityEngine;
using UnityEngine.InputSystem;

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

    public static void Rebind(InputAction action, int bindingIndex, Action onDone = null) {
        action.Disable();
        action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .OnComplete(operation => {
                operation.Dispose();
                action.Enable();
                SaveBindings();
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
    }

    private static void LoadBindings() {
        var json = PlayerPrefs.GetString(Constants.INPUT_BINDINGS_PREF_KEY, "");
        if (!string.IsNullOrEmpty(json)) {
            asset.LoadBindingOverridesFromJson(json);
        }
    }
}
