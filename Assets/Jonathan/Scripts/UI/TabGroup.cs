using UnityEngine;

// tabs over a set of panels, one open at a time
public class TabGroup : MonoBehaviour
{
    [SerializeField] private UIButton[] tabs;
    [SerializeField] private GameObject[] panels;
    [SerializeField] private int current;

    void Awake() {
        for (var i = 0; i < tabs.Length; i++) {
            var index = i;
            tabs[i].onClick.AddListener(() => Select(index));
        }
    }

    void OnEnable() {
        Select(current);
    }

    public void Select(int index) {
        current = index;
        for (var i = 0; i < tabs.Length; i++) {
            tabs[i].SetLit(i == index);
        }
        for (var i = 0; i < panels.Length; i++) {
            panels[i].SetActive(i == index);
        }
    }
}
