using System;
using UnityEngine.UIElements;

// an ON / OFF pill: the side that's picked is filled in, a click flips it; the two words can be changed
[UxmlElement]
public partial class Switch : VisualElement
{
    private readonly Label on = new Label("ON");
    private readonly Label off = new Label("OFF");
    private bool _value;

    public event Action<bool> changed;

    [UxmlAttribute]
    public string onText {
        get => on.text;
        set => on.text = value;
    }

    [UxmlAttribute]
    public string offText {
        get => off.text;
        set => off.text = value;
    }

    [UxmlAttribute]
    public bool value {
        get => _value;
        set {
            _value = value;
            on.EnableInClassList("switch__side--picked", value);
            off.EnableInClassList("switch__side--picked", !value);
        }
    }

    public Switch() {
        AddToClassList("switch");
        on.AddToClassList("switch__side");
        off.AddToClassList("switch__side");
        Add(on);
        Add(off);
        value = true;

        this.AddManipulator(new Clickable(() => {
            value = !value;
            changed?.Invoke(value);
        }));
    }
}
