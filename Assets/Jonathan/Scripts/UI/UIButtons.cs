using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// wires the document's buttons to the scene by name, and presses them from a key so a key on screen does what it says
[RequireComponent(typeof(UIDocument))]
public class UIButtons : MonoBehaviour
{
    [Serializable]
    public class Binding {
        // the element's name in the uxml, any element can be clicked
        public string element;
        public ClickSound sound = ClickSound.Normal;
        // "Map/Action" that presses it too, e.g. "UI/Cancel", empty for none
        public string key;
        public UnityEvent onClick;

        [NonSerialized] public VisualElement target;
        [NonSerialized] public InputAction input;
    }

    [SerializeField] private Binding[] buttons;

    private UIDocument document;

    void OnEnable() {
        document = GetComponent<UIDocument>();
        var root = document.rootVisualElement;

        foreach (var binding in buttons) {
            binding.target = root.Q(binding.element);
            if (binding.target == null) {
                Debug.LogWarning("No element named " + binding.element);
                continue;
            }

            var b = binding;
            UIKit.OnClick(b.target, () => b.onClick.Invoke(), b.sound);
            b.input = string.IsNullOrEmpty(b.key) ? null : GameInput.Get(b.key);
        }
    }

    void Update() {
        if (GameInput.typing || ControlsPage.busy) return;

        foreach (var binding in buttons) {
            if (binding.input == null || binding.target == null || !binding.input.WasPressedThisFrame()) continue;
            if (!binding.target.enabledInHierarchy || !UIKit.IsShown(binding.target)) continue;

            // a focused element already gets submit from the event system
            if (binding.key == "UI/Submit" && UIKit.Focused(document) == binding.target) continue;

            UIKit.Click(binding.target);
        }
    }

    // shows or hides an element, e.g. the in-game menu
    public void Toggle(string element) {
        var target = document.rootVisualElement.Q(element);
        if (target != null) UIKit.Show(target, target.ClassListContains(UIKit.hiddenClass));
    }
}
