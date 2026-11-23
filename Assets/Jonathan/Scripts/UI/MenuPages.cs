using UnityEngine;
using UnityEngine.UIElements;

// the menu's home and its pages (settings, controls, progression), the progression page has tabs
public class MenuPages : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private string home = "home";
    [SerializeField] private string[] pages = { "settingsPage", "controlsPage", "progressionPage" };

    // tabs that open a page: tab i opens page i (the options page's audio and controls tabs sit on both of its pages)
    [SerializeField] private string[] pageTabs = { "audioTab", "controlsTab" };

    [Header("Progression tabs")]
    [SerializeField] private string[] tabs = { "captainsTab", "achievementsTab", "statisticsTab" };
    [SerializeField] private string[] panels = { "captains", "achievements", "statistics" };

    void Start() {
        var root = document.rootVisualElement;
        for (var i = 0; i < pageTabs.Length; i++) {
            var page = i;
            root.Query<Button>(pageTabs[i]).ForEach(tab => UIKit.OnClick(tab, () => Show(page), ClickSound.Light));
        }
        for (var i = 0; i < tabs.Length; i++) {
            var index = i;
            UIKit.OnClick(root.Q(tabs[i]), () => SelectTab(index), ClickSound.Light);
        }

        SelectTab(0);
        Show(-1);
    }

    // -1 is home
    public void Show(int index) {
        var root = document.rootVisualElement;
        UIKit.Show(root.Q(home), index < 0);
        for (var i = 0; i < pages.Length; i++) {
            UIKit.Show(root.Q(pages[i]), i == index);
        }

        UIKit.Blur(document);
    }

    public void SelectTab(int index) {
        var root = document.rootVisualElement;
        for (var i = 0; i < pageTabs.Length; i++) {
            var page = i;
            root.Query<Button>(pageTabs[i]).ForEach(tab => UIKit.OnClick(tab, () => Show(page), ClickSound.Light));
        }
        for (var i = 0; i < tabs.Length; i++) {
            UIKit.SetLit(root.Q(tabs[i]), i == index);
        }
        for (var i = 0; i < panels.Length; i++) {
            UIKit.Show(root.Q(panels[i]), i == index);
        }
    }
}
