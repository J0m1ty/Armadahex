using UnityEngine;
using UnityEngine.UI;
using TMPro;

// one ship's key in the fleet tray: number chip, silhouette, name, specials left, sunk
[RequireComponent(typeof(UIButton))]
public class FleetKey : MonoBehaviour
{
    [SerializeField] private KeyHint number;
    [SerializeField] private GameObject numberChip;
    // the silhouette masks a tint, so a black sprite can show in any colour
    [SerializeField] private Image icon;
    [SerializeField] private Graphic iconTint;
    [SerializeField] private TMP_Text shipName;
    [SerializeField] private TMP_Text info;
    [SerializeField] private GameObject sunkTag;

    public UIButton button => GetComponent<UIButton>();

    public void Set(int slot, Sprite sprite, string name, bool alive, int specials) {
        var t = UITheme.instance;

        number.Set("Gameplay/Choice " + slot);
        icon.sprite = sprite;
        icon.enabled = sprite != null;
        iconTint.color = alive ? t.Get(ThemeColor.FriendlyInk, 0.9f) : t.Get(ThemeColor.Enemy, 0.6f);

        shipName.text = alive ? name : "<s>" + name + "</s>";
        shipName.color = alive ? t.ink : t.ink3;

        if (!alive) {
            info.text = "";
        }
        else if (specials > 0) {
            info.text = "<color=#" + t.Html(ThemeColor.Gold) + ">" + specials + "</color> SPECIAL" + (specials > 1 ? "S" : "");
        }
        else {
            info.text = "PLAIN SHOT";
        }
        info.color = t.ink2;

        sunkTag.SetActive(!alive);
        button.interactable = alive;
    }

    public void SetSelected(bool selected) {
        button.SetLit(selected);
    }

    // the number only means this ship while the fleet is what's numbered
    public void ShowNumber(bool show) {
        numberChip.SetActive(show);
    }
}
