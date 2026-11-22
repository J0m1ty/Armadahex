using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

// presses a button from a key, so a key chip on screen always does what it says
[RequireComponent(typeof(Button))]
public class KeyButton : MonoBehaviour
{
    // "Map/Action", e.g. "UI/Cancel"
    public string action = "UI/Cancel";

    private InputAction input;
    private Button button;

    void Awake() {
        button = GetComponent<Button>();
    }

    void OnEnable() {
        input = GameInput.Get(action);
    }

    void Update() {
        if (input == null || !input.WasPressedThisFrame()) return;
        if (GameInput.typing || ControlsPage.busy) return;
        if (!button.isActiveAndEnabled || !button.IsInteractable()) return;

        // a selected button already gets submit from the event system
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected == gameObject) return;

        // an open dropdown takes esc to close itself
        foreach (var dropdown in FindObjectsByType<TMP_Dropdown>(FindObjectsSortMode.None)) {
            if (dropdown.IsExpanded) return;
        }

        button.onClick.Invoke();
    }
}
