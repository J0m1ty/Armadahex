using UnityEngine;
using UnityEngine.UIElements;

// two sheets, one over the other, drawn in the element's text color; a check for a moment once something's copied
[UxmlElement]
public partial class CopyIcon : VisualElement
{
    private bool copied;

    public CopyIcon() {
        AddToClassList("copy-icon");
        generateVisualContent += Draw;
        RegisterCallback<CustomStyleResolvedEvent>(_ => MarkDirtyRepaint());
    }

    public void ShowCopied() {
        copied = true;
        MarkDirtyRepaint();
        schedule.Execute(() => {
            copied = false;
            MarkDirtyRepaint();
        }).StartingIn(1500);
    }

    private void Draw(MeshGenerationContext context) {
        var rect = contentRect;
        if (rect.width <= 0 || rect.height <= 0) return;

        var painter = context.painter2D;
        painter.strokeColor = resolvedStyle.color;
        painter.lineWidth = 2;
        painter.lineJoin = LineJoin.Round;

        if (copied) {
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.width * 0.12f, rect.height * 0.52f));
            painter.LineTo(new Vector2(rect.width * 0.4f, rect.height * 0.8f));
            painter.LineTo(new Vector2(rect.width * 0.9f, rect.height * 0.22f));
            painter.Stroke();
            return;
        }

        // the back sheet shows only where the front one doesn't cover it
        var sheet = rect.width * 0.68f;
        var offset = rect.width - sheet;
        painter.BeginPath();
        painter.MoveTo(new Vector2(offset, offset - 1));
        painter.LineTo(new Vector2(offset, 1));
        painter.LineTo(new Vector2(rect.width - 1, 1));
        painter.LineTo(new Vector2(rect.width - 1, sheet));
        painter.LineTo(new Vector2(sheet + 1, sheet));
        painter.Stroke();

        painter.BeginPath();
        painter.MoveTo(new Vector2(1, offset));
        painter.LineTo(new Vector2(sheet, offset));
        painter.LineTo(new Vector2(sheet, rect.height - 1));
        painter.LineTo(new Vector2(1, rect.height - 1));
        painter.ClosePath();
        painter.Stroke();
    }
}
