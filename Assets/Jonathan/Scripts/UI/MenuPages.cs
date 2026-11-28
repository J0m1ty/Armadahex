using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// the menu's home and its pages (settings, controls, progression), the progression page has tabs,
// and the options pages (gameplay, graphics, audio, controls) share a row of tabs that switches between them
public class MenuPages : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private string home = "home";
    [SerializeField] private string[] pages = { "settingsPage", "controlsPage", "progressionPage" };

    [Header("Options tabs")]
    [SerializeField] private string[] optionTabs = { "gameplayTab", "graphicsTab", "audioTab", "controlsTab" };
    [SerializeField] private string[] optionPages = { "gameplayPage", "graphicsPage", "settingsPage", "controlsPage" };
    // the gameplay and graphics pages are new, so their back buttons are wired here rather than in UIButtons
    [SerializeField] private string gameplayBack = "gameplayBack";
    [SerializeField] private string graphicsBack = "graphicsBack";
    // the browser always syncs to the screen
    [SerializeField] private string vSyncRow = "vSyncRow";

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
        UIKit.OnClick(root.Q(graphicsBack), () => Show(-1));
        if (Application.platform == RuntimePlatform.WebGLPlayer) UIKit.Show(root.Q(vSyncRow), false);

        for (var i = 0; i < tabs.Length; i++) {
            var index = i;
            UIKit.OnClick(root.Q(tabs[i]), () => SelectTab(index), ClickSound.Light);
        }

        SelectTab(0);
        Show(-1);
    }

    void Update() {
        // esc backs out of the gameplay and graphics pages, the others have it through UIButtons
        var cancel = GameInput.Get("UI/Cancel");
        if (cancel != null && cancel.WasPressedThisFrame() && (UIKit.IsShown(PageOf(gameplayBack)) || UIKit.IsShown(PageOf(graphicsBack)))) {
            Show(-1);
        }
    }

    private VisualElement PageOf(string back) {
        return document.rootVisualElement.Q(back)?.parent;
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
