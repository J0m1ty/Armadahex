using UnityEngine;
using UnityEngine.UIElements;

// the home page's 3D scene, which the menu's camera draws into a texture
[UxmlElement]
public partial class SceneView : VisualElement
{
    public SceneView() {
        pickingMode = PickingMode.Ignore;
        schedule.Execute(Track).Every(0);
    }

    private void Track() {
        var texture = MenuBackdrop.view;
        style.backgroundImage = texture != null ? Background.FromRenderTexture(texture) : (StyleBackground)StyleKeyword.Null;
        MarkDirtyRepaint();
    }
}
