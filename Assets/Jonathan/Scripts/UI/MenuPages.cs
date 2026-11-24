using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// the menu's home and its pages (settings, controls, progression), the progression page has tabs,
// and the options pages (gameplay, audio, controls) share a row of tabs that switches between them
public class MenuPages : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private string home = "home";
    [SerializeField] private string[] pages = { "settingsPage", "controlsPage", "progressionPage" };

    [Header("Options tabs")]
    [SerializeField] private string[] optionTabs = { "gameplayTab", "audioTab", "controlsTab" };
    [SerializeField] private string[] optionPages = { "gameplayPage", "settingsPage", "controlsPage" };
    // the gameplay page is new, so its back button is wired here rather than in UIButtons
    [SerializeField] private string gameplayBack = "gameplayBack";

    [Header("Progression tabs")]
    [SerializeField] private string[] tabs = { "captainsTab", "achievementsTab", "statisticsTab" };
    [SerializeField] private string[] panels = { "captains", "achievements", "statistics" };

    void Start() {
        var root = document.rootVisualElement;

        // every options page has the same tab row, so each tab name appears once per page
        for (var i = 0; i < optionTabs.Length; i++) {
            var page = optionPages[i];
            root.Query<Button>(optionTabs[i]).ForEach(tab => UIKit.OnClick(tab, () => ShowPage(page), ClickSound.Light));
        }
        UIKit.OnClick(root.Q(gameplayBack), () => Show(-1));

        for (var i = 0; i < tabs.Length; i++) {
            var index = i;
            UIKit.OnClick(root.Q(tabs[i]), () => SelectTab(index), ClickSound.Light);
        }

        SelectTab(0);
        Show(-1);
    }

    void Update() {
        // esc backs out of the gameplay page, the others have it through UIButtons
        var cancel = GameInput.Get("UI/Cancel");
        if (cancel != null && cancel.WasPressedThisFrame() && UIKit.IsShown(document.rootVisualElement.Q(optionPages[0]))) {
            Show(-1);
        }
    }

    // -1 is home
    public void Show(int index) {
        ShowPage(index < 0 || index >= pages.Length ? null : pages[index]);
    }

    // null is home
    public void ShowPage(string page) {
        var root = document.rootVisualElement;
        UIKit.Show(root.Q(home), page == null);
        foreach (var name in AllPages()) {
            UIKit.Show(root.Q(name), name == page);
        }

        UIKit.Blur(document);
    }

    private IEnumerable<string> AllPages() {
        var all = new HashSet<string>(pages);
        all.UnionWith(optionPages);
        return all;
    }

    public void SelectTab(int index) {
        var root = document.rootVisualElement;
        for (var i = 0; i < tabs.Length; i++) {
            UIKit.SetLit(root.Q(tabs[i]), i == index);
        }
        for (var i = 0; i < panels.Length; i++) {
            UIKit.Show(root.Q(panels[i]), i == index);
        }
    }
}
