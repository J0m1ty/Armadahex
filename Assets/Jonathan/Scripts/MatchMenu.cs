using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// esc during a match: resume, skip, mute and a held surrender; arrows, enter and esc move through it
public class MatchMenu : MonoBehaviour
{
    public static MatchMenu instance { get; private set; }

    private const string selectedClass = "selected";

    [SerializeField] private UIDocument document;
    [SerializeField] private AttackUIManager attackManager;
    [SerializeField] private MatchChat matchChat;

    private VisualElement menu;
    private Button skip;
    private HoldButton surrender;
    private readonly List<VisualElement> items = new List<VisualElement>();
    private int selected;
    private bool surrendered;

    private InputAction submit;

    public bool isOpen => UIKit.IsShown(menu);

    void Awake() {
        instance = this;
    }

    void Start() {
        var root = document.rootVisualElement;
        menu = root.Q("matchMenu");
        skip = root.Q<Button>("skip");
        surrender = root.Q<HoldButton>("surrender");
        var mute = root.Q<Button>("mute");

        UIKit.OnClick(root.Q("resume"), () => Open(false));
        UIKit.OnClick(skip, () => {
            Open(false);
            attackManager.SkipTurn();
        });
        UIKit.OnClick(mute, () => matchChat.DisableChat(true));

        // there's no one to mute in a bot match
        if (GameModeInfo.instance.IsSingleplayer) mute.RemoveFromHierarchy();

        // the menu stays up and says so while the match ends behind it
        surrender.held += () => {
            if (surrendered) return;
            surrendered = true;
            surrender.text = "SURRENDERING...";
            foreach (var item in items) {
                if (item != surrender) item.SetEnabled(false);
            }
            GameOver.instance.Surrender();
        };

        menu.Query<Button>(className: "menu-item").ForEach(item => {
            var index = items.Count;
            items.Add(item);
            item.focusable = true;
            item.RegisterCallback<PointerEnterEvent>(_ => Select(index));
        });

        // the arrows move the selection here, not unity's own focus order
        menu.RegisterCallback<NavigationMoveEvent>(e => {
            e.StopPropagation();
            menu.focusController?.IgnoreEvent(e);
        }, TrickleDown.TrickleDown);
        // enter on surrender is a hold, not a click
        surrender.RegisterCallback<NavigationSubmitEvent>(e => {
            e.StopPropagation();
            surrender.focusController?.IgnoreEvent(e);
        }, TrickleDown.TrickleDown);

        submit = GameInput.Get("UI/Submit");
        UIKit.Show(menu, false);
    }

    void Update() {
        if (surrendered) return;

        if (!isOpen) {
            surrender.progress = 0;
            return;
        }

        // only your turn can be skipped
        var tm = TurnManager.instance;
        skip.SetEnabled(tm != null && tm.gameActive && tm.currentTeam != null && tm.currentTeam.isPlayer);

        var keyboard = Keyboard.current;
        if (keyboard != null && !GameInput.typing) {
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) Step(-1);
            if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) Step(1);
        }

        var holdingKey = items[selected] == surrender && submit != null && submit.IsPressed();
        surrender.Step(surrender.pointerHeld || holdingKey, Time.unscaledDeltaTime);

        // a click on nothing drops focus, so give it back and enter still works
        if (UIKit.Focused(document) == null) items[selected].Focus();
    }

    private void Step(int by) {
        var index = selected;
        for (var i = 0; i < items.Count; i++) {
            index = (index + by + items.Count) % items.Count;
            if (items[index].enabledInHierarchy) break;
        }
        Select(index);
    }

    private void Select(int index) {
        selected = index;
        for (var i = 0; i < items.Count; i++) {
            items[i].EnableInClassList(selectedClass, i == index);
        }
        items[index].Focus();
    }

    public void Toggle() {
        Open(!isOpen);
    }

    public void Open(bool open) {
        if (surrendered) return;

        UIKit.Show(menu, open);
        surrender.progress = 0;
        if (open) {
            Select(0);
        }
        else {
            UIKit.Blur(document);
        }
    }
}
