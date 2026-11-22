using UnityEngine;
using UnityEngine.EventSystems;
using ThisOtherThing.UI.Shapes;

// a text field's border goes gold while you type in it
public class FieldFocus : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Rectangle border;

    void OnEnable() {
        Paint(false);
    }

    public void OnSelect(BaseEventData eventData) {
        Paint(true);
    }

    public void OnDeselect(BaseEventData eventData) {
        Paint(false);
    }

    private void Paint(bool focused) {
        var t = UITheme.instance;
        if (t == null || border == null) return;

        border.ShapeProperties.DrawOutline = true;
        border.ShapeProperties.OutlineColor = focused ? t.gold : t.lineStrong;
        border.ForceMeshUpdate();
    }
}
