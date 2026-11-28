using UnityEngine.UIElements;

public enum Setting {
    ReducedMotion,
    Quality,
    FullResolution,
    HighDetail,
    AntiAliasing,
    VSync
}

// a Switch for one of GameSettings' on/off settings, it reads and saves the setting itself so it can sit on any page
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
        switch (setting) {
            case Setting.ReducedMotion: value = GameSettings.reducedMotion; break;
            case Setting.FullResolution: value = GameSettings.fullResolution; break;
            case Setting.HighDetail: value = GameSettings.highDetail; break;
            case Setting.VSync: value = GameSettings.vSync; break;
        }
    }

    private void Set(bool on) {
        switch (setting) {
            case Setting.ReducedMotion: GameSettings.reducedMotion = on; break;
            case Setting.FullResolution: GameSettings.fullResolution = on; break;
            case Setting.HighDetail: GameSettings.highDetail = on; break;
            case Setting.VSync: GameSettings.vSync = on; break;
        }
    }
}
