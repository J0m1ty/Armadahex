using UnityEngine;
using UnityEngine.UIElements;

// one ship's key in the fleet tray: number, silhouette, name, specials left, sunk
public class FleetKey
{
    public Button button { get; private set; }

    private KeyLabel number;
    private Image icon;
    private Label shipName;
    private Label info;
    private Label sunkTag;

    public FleetKey(VisualElement parent) {
        button = new Button { focusable = false };
        button.AddToClassList("fleet-key");

        number = new KeyLabel();
        icon = new Image { scaleMode = ScaleMode.ScaleToFit };
        icon.AddToClassList("fleet-icon");
        shipName = new Label();
        info = new Label();
        sunkTag = new Label("SUNK");

        button.Add(number);
        button.Add(icon);
        button.Add(shipName);
        button.Add(info);
        button.Add(sunkTag);
        parent.Add(button);
    }

    public void Set(int slot, Sprite sprite, string name, bool alive, int specials) {
        number.action = "Gameplay/Choice " + slot;
        icon.sprite = sprite;
        UIKit.Show(icon, sprite != null);

        shipName.text = alive ? name : "<s>" + name + "</s>";

        if (!alive) {
            info.text = "";
        }
        else if (specials > 0) {
            info.text = specials + " SPECIAL" + (specials > 1 ? "S" : "");
        }
        else {
            info.text = "PLAIN SHOT";
        }

        UIKit.Show(sunkTag, !alive);
        button.SetEnabled(alive);
    }

    public void SetSelected(bool selected) {
        UIKit.SetLit(button, selected);
    }

    // the number only means this ship while the fleet is what's numbered
    public void ShowNumber(bool show) {
        number.style.visibility = show ? Visibility.Visible : Visibility.Hidden;
    }
}
