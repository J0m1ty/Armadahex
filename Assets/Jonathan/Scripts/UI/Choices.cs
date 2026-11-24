using System;
using UnityEngine.UIElements;

// a pill of a few options side by side, the picked one filled in, like the ON / OFF switch
[UxmlElement]
public partial class Choices : VisualElement
{
    private string _choices = "";
    private int _value;

    public event Action<int> changed;

    // the options' names, comma separated
    [UxmlAttribute]
    public string choices {
        get => _choices;
        set {
            _choices = value;
            Build();
        }
    }

    public int value {
        get => _value;
        set {
            _value = value;
            for (var i = 0; i < childCount; i++) {
                ElementAt(i).EnableInClassList("switch__side--picked", i == value);
            }
        }
    }

    public Choices() {
        AddToClassList("switch");
    }

    private void Build() {
        Clear();
        var names = _choices.Split(',');
        for (var i = 0; i < names.Length; i++) {
            var index = i;
            var side = new Label(names[i].Trim());
            side.AddToClassList("switch__side");
            side.AddToClassList("choices__side");
            side.AddManipulator(new Clickable(() => {
                value = index;
                changed?.Invoke(index);
            }));
            Add(side);
        }
        value = _value;
    }
}
