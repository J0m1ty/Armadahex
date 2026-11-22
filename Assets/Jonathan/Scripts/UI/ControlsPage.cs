using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

// every rebindable key in the Camera and Gameplay maps, click one and press the new key (esc cancels)
public class ControlsPage : MonoBehaviour
{
    [SerializeField] private Transform list;
    // templates, hidden: a row has a "Name" text and a UIButton "Bind", a header has one text
    [SerializeField] private GameObject rowTemplate;
    [SerializeField] private GameObject headerTemplate;
    [SerializeField] private Button resetButton;
    [SerializeField] private string[] maps = { "Gameplay", "Camera" };

    // true while waiting for a key, and for a frame after, so the esc that cancels doesn't also leave the page
    public static bool busy => listening || Time.frameCount - doneFrame <= 1;
    private static bool listening;
    private static int doneFrame = -10;

    private List<GameObject> rows = new List<GameObject>();

    void Start() {
        rowTemplate.SetActive(false);
        headerTemplate.SetActive(false);
        resetButton.onClick.AddListener(() => {
            GameInput.ResetBindings();
            Build();
        });
        Build();
    }

    private void Build() {
        foreach (var row in rows) Destroy(row);
        rows.Clear();

        foreach (var mapName in maps) {
            var map = GameInput.actions.FindActionMap(mapName);
            if (map == null) continue;

            var header = Instantiate(headerTemplate, list);
            header.SetActive(true);
            header.GetComponentInChildren<TMP_Text>().text = mapName.ToUpper();
            rows.Add(header);

            foreach (var action in map.actions) {
                for (var i = 0; i < action.bindings.Count; i++) {
                    var binding = action.bindings[i];
                    if (binding.isComposite || !Rebindable(binding)) continue;

                    var name = action.name + (binding.isPartOfComposite ? " " + Pretty(binding.name) : "");
                    AddRow(action, i, name);
                }
            }
        }
    }

    // pointer movement and the wheel aren't keys
    private static bool Rebindable(InputBinding binding) {
        var path = binding.path;
        if (string.IsNullOrEmpty(path)) return false;
        return !path.Contains("position") && !path.Contains("delta") && !path.Contains("scroll") && !path.StartsWith("<Gamepad>");
    }

    private void AddRow(InputAction action, int index, string name) {
        var row = Instantiate(rowTemplate, list);
        row.SetActive(true);
        rows.Add(row);

        row.transform.Find("Name").GetComponent<TMP_Text>().text = name;
        var button = row.transform.Find("Bind").GetComponent<UIButton>();
        var label = button.label;
        label.text = action.GetBindingDisplayString(index).ToUpper();

        var keyboard = action.bindings[index].path.StartsWith("<Keyboard>");
        button.onClick.AddListener(() => {
            if (listening) return;
            listening = true;
            label.text = "PRESS A KEY...";
            button.SetLit(true);

            GameInput.Rebind(action, index, () => {
                listening = false;
                doneFrame = Time.frameCount;
                button.SetLit(false);
                label.text = action.GetBindingDisplayString(index).ToUpper();
                // enter or space on a still-selected button would start another rebind
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            }, keyboard);
        });
    }

    private static string Pretty(string part) {
        return string.IsNullOrEmpty(part) ? "" : char.ToUpper(part[0]) + part.Substring(1);
    }

    void OnDisable() {
        listening = false;
    }
}
