using System.Collections.Generic;
using UnityEngine.UIElements;

// a ListDropdown for one of GameSettings' choices, it reads and saves the setting itself so it can sit on any page
[UxmlElement]
public partial class SettingDropdown : ListDropdown
{
    [UxmlAttribute]
    public Setting setting { get; set; }

    private static readonly List<string> qualities = new List<string> { "HIGH", "MEDIUM", "LOW", "CUSTOM" };
    private static readonly List<string> antiAliasing = new List<string> { "OFF", "2X", "4X" };

    public SettingDropdown() {
        changed += Set;
        RegisterCallback<AttachToPanelEvent>(_ => {
            SetChoices(setting == Setting.AntiAliasing ? antiAliasing : qualities);
            GameSettings.changed += Refresh;
            Refresh();
        });
        RegisterCallback<DetachFromPanelEvent>(_ => GameSettings.changed -= Refresh);
    }

    private void Refresh() {
        if (setting == Setting.Quality) index = (int)GameSettings.quality;
        else if (setting == Setting.AntiAliasing) index = System.Array.IndexOf(GameSettings.antiAliasingSamples, GameSettings.antiAliasing);
    }

    private void Set(int picked) {
        if (setting == Setting.Quality) {
            GameSettings.quality = (GraphicsQuality)picked;
            // custom changes nothing, so show what the rows add up to again
            Refresh();
        }
        else if (setting == Setting.AntiAliasing) GameSettings.antiAliasing = GameSettings.antiAliasingSamples[picked];
    }
}
