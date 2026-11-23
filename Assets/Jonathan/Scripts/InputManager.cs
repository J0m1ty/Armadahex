using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public enum KeyMode {
    Toggle,
    Hold
}

public class InputManager : MonoBehaviour
{   
    [Header("Scoreboard")]
    [SerializeField] private bool hideScoreboardOnStart = true;
    [SerializeField] private KeyMode scoreboardMode = KeyMode.Hold;
    [SerializeField] private UIDocument document;
    private VisualElement scoreboard;

    [Header("Interactions")]
    [SerializeField] private AttackUIManager attackManager;
    private Button fireButton;
    private Button cancelButton;

    // bindings live in Resources/Controls, under Gameplay
    private InputAction scoreboardAction;
    private InputAction switchShipAction;
    private InputAction fireAction;
    private InputAction cancelAction;
    // the numbered buttons on screen: ships, then weapons, then patterns
    private InputAction[] choiceActions = new InputAction[5];

    void Start() {
        var root = document.rootVisualElement;
        scoreboard = root.Q("scoreboard");
        fireButton = root.Q<Button>("fire");
        cancelButton = root.Q<Button>("back");

        for (var i = 0; i < choiceActions.Length; i++) {
            choiceActions[i] = GameInput.Get("Gameplay/Choice " + (i + 1));
        }

        scoreboardAction = GameInput.Get("Gameplay/Scoreboard");
        switchShipAction = GameInput.Get("Gameplay/Switch Ship");
        fireAction = GameInput.Get("Gameplay/Fire");
        cancelAction = GameInput.Get("Gameplay/Cancel");

        if (hideScoreboardOnStart) {
            UIKit.Show(scoreboard, false);
        }
    }
    
    void Update() {
        if (GameInput.typing) return;

        if (scoreboardAction.WasPressedThisFrame()) {
            if (scoreboardMode == KeyMode.Toggle) {
                UIKit.Show(scoreboard, !UIKit.IsShown(scoreboard));
            } else {
                UIKit.Show(scoreboard, true);
            }
        }

        if (scoreboardAction.WasReleasedThisFrame() && scoreboardMode == KeyMode.Hold) {
            UIKit.Show(scoreboard, false);
        }

        if (switchShipAction.WasPressedThisFrame()) {
            attackManager.SelectRandomShip();
        }

        for (var i = 0; i < choiceActions.Length; i++) {
            if (choiceActions[i].WasPressedThisFrame()) {
                attackManager.ChooseNumber(i + 1);
            }
        }

        if (fireAction.WasPressedThisFrame()) {
            if (UIKit.IsShown(fireButton) && fireButton.enabledSelf) {
                UIKit.Click(fireButton);
            }
        }

        if (cancelAction.WasPressedThisFrame()) {
            if (UIKit.IsShown(cancelButton)) {
                UIKit.Click(cancelButton);
            }
        }
    }
}
