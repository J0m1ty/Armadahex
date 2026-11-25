using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// picks one name from a list: a field showing the picked one, and a list that opens over the page.
// it looks like a ColorDropdown without the swatches, and shares its classes
[UxmlElement]
public partial class ListDropdown : VisualElement
{
    private readonly Button field = new Button();
    private readonly Label current = new Label();
    private readonly VisualElement list = new VisualElement();
    private VisualElement layer;

    private List<string> choices = new List<string>();
    private int _index = -1;

    // only when the player picks one, not when it's set from code
    public event Action<int> changed;

    public int index {
        get => _index;
        set {
            _index = value;
            current.text = value >= 0 && value < choices.Count ? choices[value] : "";
            for (var i = 0; i < list.childCount; i++) {
                list[i].EnableInClassList("color-dropdown__item--picked", i == value);
            }
        }
    }

    public ListDropdown() {
        AddToClassList("color-dropdown");

        field.AddToClassList("color-dropdown__field");
        current.AddToClassList("color-dropdown__name");
        var arrow = new VisualElement();
        arrow.AddToClassList("color-dropdown__arrow");
        arrow.generateVisualContent += DrawArrow;
        field.Add(current);
        field.Add(arrow);
        field.clicked += Toggle;
        Add(field);

        list.AddToClassList("color-dropdown__list");

        RegisterCallback<DetachFromPanelEvent>(_ => Close());
    }

    public void SetChoices(List<string> names) {
        choices = names;
        list.Clear();
        for (var i = 0; i < choices.Count; i++) {
            var picked = i;
            var item = new VisualElement();
            item.AddToClassList("color-dropdown__item");
            var name = new Label(choices[i]);
            name.AddToClassList("color-dropdown__name");
            item.Add(name);
            item.AddManipulator(new Clickable(() => Pick(picked)));
            list.Add(item);
        }
        index = _index;
    }

    private void Pick(int picked) {
        Close();
        index = picked;
        changed?.Invoke(picked);
    }

    private void Toggle() {
        if (list.parent != null) Close();
        else Open();
    }

    // the list goes on top of everything in the page, just under the field
    private void Open() {
        layer = GetFirstAncestorOfType<TemplateContainer>() ?? panel?.visualTree;
        if (layer == null) return;

        var bounds = field.worldBound;
        var at = layer.WorldToLocal(new Vector2(bounds.xMin, bounds.yMax));
        list.style.left = at.x;
        list.style.top = at.y;
        list.style.width = bounds.width;
        layer.Add(list);
        layer.RegisterCallback<PointerDownEvent>(OutsideClick, TrickleDown.TrickleDown);
        AddToClassList("color-dropdown--open");
    }

    private void Close() {
        layer?.UnregisterCallback<PointerDownEvent>(OutsideClick, TrickleDown.TrickleDown);
        list.RemoveFromHierarchy();
        RemoveFromClassList("color-dropdown--open");
    }

    private void OutsideClick(PointerDownEvent e) {
        var target = e.target as VisualElement;
        if (target != null && (list.Contains(target) || field.Contains(target))) return;
        Close();
    }

    private void DrawArrow(MeshGenerationContext context) {
        var rect = context.visualElement.contentRect;
        var painter = context.painter2D;
        painter.strokeColor = context.visualElement.resolvedStyle.color;
        painter.lineWidth = 2;
        painter.BeginPath();
        painter.MoveTo(new Vector2(0, 0));
        painter.LineTo(new Vector2(rect.width / 2, rect.height));
        painter.LineTo(new Vector2(rect.width, 0));
        painter.Stroke();
    }
}
