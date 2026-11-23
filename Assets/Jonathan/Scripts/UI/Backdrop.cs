using UnityEngine;
using UnityEngine.UIElements;

// the shared menu background: Resources/Backdrop.mat drawn into a texture at the element's size every frame
[UxmlElement]
public partial class Backdrop : VisualElement
{
    private static readonly int aspectId = Shader.PropertyToID("_Aspect");
    private static readonly int effectId = Shader.PropertyToID("_Effect");

    // 0 hex lattice, 1 caustics, 2 sonar
    [UxmlAttribute]
    public int effect { get; set; }

    private Material material;
    private RenderTexture texture;
    private IVisualElementScheduledItem tick;

    public Backdrop() {
        pickingMode = PickingMode.Ignore;
        RegisterCallback<AttachToPanelEvent>(_ => Attach());
        RegisterCallback<DetachFromPanelEvent>(_ => Detach());
        RegisterCallback<GeometryChangedEvent>(_ => Resize());
    }

    private void Attach() {
        if (!Application.isPlaying) return;

        material = new Material(Resources.Load<Material>("Backdrop"));
        tick = schedule.Execute(Draw).Every(0);
    }

    private void Detach() {
        tick?.Pause();
        Release();
        if (material != null) Object.Destroy(material);
    }

    private void Resize() {
        if (material == null) return;

        // the panel's scale turns layout points into screen pixels, half res is plenty for soft light
        var scale = panel != null ? panel.scaledPixelsPerPoint : 1f;
        var width = Mathf.Max(1, Mathf.RoundToInt(layout.width * scale * 0.5f));
        var height = Mathf.Max(1, Mathf.RoundToInt(layout.height * scale * 0.5f));
        if (texture != null && texture.width == width && texture.height == height) return;

        // draw into the new texture and show it before letting go of the old one
        var old = texture;
        texture = new RenderTexture(width, height, 0);
        Draw();
        style.backgroundImage = Background.FromRenderTexture(texture);
        Free(old);
    }

    private void Draw() {
        if (texture == null || material == null) return;

        material.SetFloat(aspectId, (float)texture.width / texture.height);
        material.SetFloat(effectId, effect);
        var active = RenderTexture.active;
        Graphics.Blit(null, texture, material);
        RenderTexture.active = active;
        MarkDirtyRepaint();
    }

    private void Release() {
        if (texture == null) return;

        style.backgroundImage = StyleKeyword.Null;
        Free(texture);
        texture = null;
    }

    private static void Free(RenderTexture old) {
        if (old == null) return;

        if (RenderTexture.active == old) RenderTexture.active = null;
        old.Release();
        Object.Destroy(old);
    }
}
