using UnityEngine.UIElements;

public enum Setting {
    ReducedMotion,
    // on is full, off is reduced
    Graphics
}

// a Switch for one of GameSettings, it reads and saves the setting itself so it can sit on any page
[UxmlElement]
public partial class SettingSwitch : Switch
{
    [UxmlAttribute]
    public Setting setting { get; set; }

    public SettingSwitch() {
        changed += Set;
        RegisterCallback<AttachToPanelEvent>(_ => {
            GameSettings.changed += Refresh;
            Refresh();
        });
        RegisterCallback<DetachFromPanelEvent>(_ => GameSettings.changed -= Refresh);
    }

    private void Refresh() {
        value = setting == Setting.ReducedMotion ? GameSettings.reducedMotion : !GameSettings.reducedGraphics;
    }

    private void Set(bool on) {
        if (setting == Setting.ReducedMotion) GameSettings.reducedMotion = on;
        else GameSettings.graphics = on ? GraphicsQuality.Full : GraphicsQuality.Reduced;
    }
}
