using UnityEngine;
using UnityEngine.UIElements;

// one ship's row in the picker: its key, silhouette and name; the picked one opens a box under it
public class FleetKey
{
    public Button button { get; private set; }
    // the picked ship's weapons and angles go in here
    public VisualElement sub { get; private set; }

    private KeyLabel number;
    private VisualElement icon;
    private Label shipName;

    public FleetKey(VisualElement parent) {
        button = new Button { focusable = false };
        button.AddToClassList("fleet-row");

        number = new KeyLabel { format = "{0}" };
        number.AddToClassList("key-chip");
        icon = new VisualElement { pickingMode = PickingMode.Ignore };
        icon.AddToClassList("fleet-row__ship");
        shipName = new Label { pickingMode = PickingMode.Ignore };
        shipName.AddToClassList("fleet-row__name");

        button.Add(number);
        button.Add(icon);
        button.Add(shipName);
        parent.Add(button);

        sub = new VisualElement { pickingMode = PickingMode.Ignore };
        sub.AddToClassList("fleet-sub");
        parent.Add(sub);
        UIKit.Show(sub, false);
    }

    public void Set(int slot, Texture2D image, string name, bool alive) {
        number.action = "Gameplay/Choice " + slot;
        icon.style.backgroundImage = image != null ? new StyleBackground(image) : StyleKeyword.None;

        shipName.text = alive ? name : "<s>" + name + "</s>";
        shipName.EnableInClassList("sunk", !alive);
        button.SetEnabled(alive);
    }

    public void SetSelected(bool selected) {
        UIKit.SetLit(button, selected);
        UIKit.Show(sub, selected && sub.childCount > 0);
    }

    // the number only means this ship while the fleet is what's numbered
    public void ShowNumber(bool show) {
        number.style.visibility = show ? Visibility.Visible : Visibility.Hidden;
    }
}
