using UnityEngine;
using UnityEngine.UIElements;

// picks one side's color from SideColors: a field showing the current color, and a list that opens over the page.
// the color the other side has is shown as taken and can't be picked
[UxmlElement]
public partial class ColorDropdown : VisualElement
{
    // false: your own side, true: the enemy's
    [UxmlAttribute]
    public bool enemy { get; set; }

    private readonly Button field = new Button();
    private readonly VisualElement swatch = new VisualElement();
    private readonly Label current = new Label();
    private readonly VisualElement list = new VisualElement();
    private VisualElement layer;

    public ColorDropdown() {
        AddToClassList("color-dropdown");

        field.AddToClassList("color-dropdown__field");
        swatch.AddToClassList("color-swatch");
        current.AddToClassList("color-dropdown__name");
        var arrow = new VisualElement();
        arrow.AddToClassList("color-dropdown__arrow");
        arrow.generateVisualContent += DrawArrow;
        field.Add(swatch);
        field.Add(current);
        field.Add(arrow);
        field.clicked += Toggle;
        Add(field);

        list.AddToClassList("color-dropdown__list");
        for (var i = 0; i < SideColors.colors.Length; i++) {
            var index = i;
            var item = new VisualElement();
            item.AddToClassList("color-dropdown__item");
            var itemSwatch = new VisualElement();
            itemSwatch.AddToClassList("color-swatch");
            itemSwatch.style.backgroundColor = SideColors.colors[i];
            var name = new Label(SideColors.names[i].ToUpper());
            name.AddToClassList("color-dropdown__name");
            item.Add(itemSwatch);
            item.Add(name);
            item.AddManipulator(new Clickable(() => Pick(index)));
            list.Add(item);
        }

        RegisterCallback<AttachToPanelEvent>(_ => {
            SideColors.changed += Refresh;
            Refresh();
        });
        RegisterCallback<DetachFromPanelEvent>(_ => {
            SideColors.changed -= Refresh;
            Close();
        });
    }

    private int picked => enemy ? SideColors.enemy : SideColors.player;
    private int taken => enemy ? SideColors.player : SideColors.enemy;

    private void Refresh() {
        swatch.style.backgroundColor = SideColors.colors[picked];
        current.text = SideColors.names[picked].ToUpper();
        for (var i = 0; i < list.childCount; i++) {
            list[i].EnableInClassList("color-dropdown__item--picked", i == picked);
            list[i].EnableInClassList("color-dropdown__item--taken", i == taken);
        }
    }

    private void Pick(int index) {
        if (index == taken) return;
        SideColors.Set(!enemy, index);
        Close();
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
