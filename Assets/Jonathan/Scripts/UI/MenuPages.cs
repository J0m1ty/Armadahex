using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// the menu's home and its pages (settings, controls, progression), esc goes back home
public class MenuPages : MonoBehaviour
{
    [SerializeField] private GameObject home;
    [SerializeField] private GameObject[] pages;

    private InputAction cancelAction;

    void Start() {
        cancelAction = GameInput.Get("UI/Cancel");
        Show(-1);
    }

    void Update() {
        if (cancelAction.WasPressedThisFrame() && !ControlsPage.busy && !GameInput.typing) {
            Show(-1);
        }
    }

    // -1 is home
    public void Show(int index) {
        home.SetActive(index < 0);
        for (var i = 0; i < pages.Length; i++) {
            pages[i].SetActive(i == index);
        }

        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }
}
