using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// every rebindable key in the Camera and Gameplay maps, click one and press the new key (esc cancels)
public class ControlsPage : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private string list = "controlsList";
    [SerializeField] private string resetButton = "resetControls";
    [SerializeField] private string saveButton = "saveControls";
    [SerializeField] private string[] maps = { "Gameplay", "Camera" };

    // true while waiting for a key, and for a frame after, so the esc that cancels doesn't also leave the page
    public static bool busy => listening || Time.frameCount - doneFrame <= 1;
    private static bool listening;
    private static int doneFrame = -10;

    // a map name (action null) or one binding of an action
    private class Row {
        public string name;
        public InputAction action;
        public int index;
    }

    private List<Row> rows = new List<Row>();
    private ListView view;
    private Button save;
    private Row waiting;

    void Start() {
        var root = document.rootVisualElement;
        view = root.Q<ListView>(list);
        view.makeItem = MakeRow;
        view.bindItem = BindRow;
        view.itemsSource = rows;
        view.selectionType = SelectionType.None;
        // the wheel still scrolls, the bar is just noise
        view.Q<ScrollView>().verticalScrollerVisibility = ScrollerVisibility.Hidden;

        // a new key applies straight away, it's kept for next time once saved
        save = root.Q<Button>(saveButton);
        UIKit.OnClick(save, () => {
            GameInput.SaveBindings();
            Unsaved(false);
        });

        UIKit.OnClick(root.Q(resetButton), () => {
            GameInput.ResetBindings();
            Unsaved(false);
            Build();
        });
        Unsaved(false);
        Build();
    }

    private void Build() {
        rows.Clear();

        foreach (var mapName in maps) {
            var map = GameInput.actions.FindActionMap(mapName);
            if (map == null) continue;

            rows.Add(new Row { name = mapName.ToUpper() });

            foreach (var action in map.actions) {
                for (var i = 0; i < action.bindings.Count; i++) {
                    var binding = action.bindings[i];
                    if (binding.isComposite || !Rebindable(binding)) continue;

                    var name = action.name + (binding.isPartOfComposite ? " " + Pretty(binding.name) : "");
                    rows.Add(new Row { name = name, action = action, index = i });
                }
            }
        }
        view.RefreshItems();
    }

    // pointer movement and the wheel aren't keys
    private static bool Rebindable(InputBinding binding) {
        var path = binding.path;
        if (string.IsNullOrEmpty(path)) return false;
        return !path.Contains("position") && !path.Contains("delta") && !path.Contains("scroll") && !path.StartsWith("<Gamepad>");
    }

    private VisualElement MakeRow() {
        var row = new VisualElement();
        row.AddToClassList("row");
        var name = new Label { name = "name" };
        name.AddToClassList("control-name");
        row.Add(name);
        // a key bound to more than one action gets a red mark; it still saves, the player may mean it
        var conflict = new Label("!") { name = "conflict" };
        conflict.AddToClassList("conflict");
        row.Add(conflict);
        var bind = new Button { name = "bind" };
        bind.AddToClassList("control-key");
        row.Add(bind);
        UIKit.OnClick(bind, () => StartRebind(row.userData as Row, bind), ClickSound.Light);
        return row;
    }

    private void BindRow(VisualElement element, int index) {
        var row = rows[index];
        element.userData = row;

        var name = element.Q<Label>("name");
        var bind = element.Q<Button>("bind");
        var conflict = element.Q<Label>("conflict");
        name.text = row.name;
        name.EnableInClassList("h2", row.action == null);
        UIKit.Show(bind, row.action != null);
        UIKit.Show(conflict, row.action != null && Duplicated(row));
        if (row.action == null) return;

        var listeningHere = waiting == row;
        bind.text = listeningHere ? "PRESS A KEY..." : row.action.GetBindingDisplayString(row.index).ToUpper();
        UIKit.SetLit(bind, listeningHere);
    }

    private void StartRebind(Row row, Button button) {
        if (listening || row == null || row.action == null) return;
        listening = true;
        waiting = row;
        view.RefreshItems();

        var keyboard = row.action.bindings[row.index].path.StartsWith("<Keyboard>");
        GameInput.Rebind(row.action, row.index, () => {
            listening = false;
            waiting = null;
            doneFrame = Time.frameCount;
            view.RefreshItems();
            Unsaved(true);
            // enter or space on a still-focused button would start another rebind
            UIKit.Blur(document);
        }, keyboard, false);
    }

    // the save button only shows while there's something to save, reset slides over for it
    private void Unsaved(bool unsaved) {
        save?.parent.EnableInClassList("unsaved", unsaved);
    }

    private static string Path(Row row) {
        return row.action.bindings[row.index].effectivePath;
    }

    // only keys count: the mouse buttons are shared on purpose (click selects, drag pans)
    private bool Duplicated(Row row) {
        var path = Path(row);
        if (!path.StartsWith("<Keyboard>")) return false;
        foreach (var other in rows) {
            if (other != row && other.action != null && string.Equals(Path(other), path, System.StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static string Pretty(string part) {
        return string.IsNullOrEmpty(part) ? "" : char.ToUpper(part[0]) + part.Substring(1);
    }

    void OnDisable() {
        listening = false;
    }
}
