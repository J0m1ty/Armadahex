using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum KeyMode {
    Toggle,
    Hold
}

public class InputManager : MonoBehaviour
{   
    [Header("Scoreboard")]
    [SerializeField] private KeyMode scoreboardMode = KeyMode.Hold;

    [Header("Interactions")]
    [SerializeField] private AttackUIManager attackManager;

    // bindings live in Resources/Controls, under Gameplay
    private InputAction scoreboardAction;
    private InputAction switchShipAction;
    private InputAction fireAction;
    private InputAction cancelAction;
    // the numbered rows on screen: ships, then weapons, then angles
    private InputAction[] choiceActions = new InputAction[5];

    void Start() {
        for (var i = 0; i < choiceActions.Length; i++) {
            choiceActions[i] = GameInput.Get("Gameplay/Choice " + (i + 1));
        }

        scoreboardAction = GameInput.Get("Gameplay/Scoreboard");
        switchShipAction = GameInput.Get("Gameplay/Switch Ship");
        fireAction = GameInput.Get("Gameplay/Fire");
        cancelAction = GameInput.Get("Gameplay/Cancel");
    }
    
    void Update() {
        if (GameInput.typing) return;

        // nothing works before the first turn, the intro is still playing
        var tm = TurnManager.instance;
        if (tm == null || !tm.gameActive) return;

        var menu = MatchMenu.instance;

        // esc steps back while there's something to step back from, otherwise it opens or closes the menu
        if (cancelAction.WasPressedThisFrame()) {
            if (menu.isOpen || !attackManager.StepBack()) {
                MatchHud.instance.ShowScoreboard(false);
                menu.Toggle();
            }
        }

        if (menu.isOpen) return;

        var hud = MatchHud.instance;
        if (scoreboardAction.WasPressedThisFrame()) {
            hud.ShowScoreboard(scoreboardMode == KeyMode.Hold || !hud.scoreboardShown);
        }

        if (scoreboardAction.WasReleasedThisFrame() && scoreboardMode == KeyMode.Hold) {
            hud.ShowScoreboard(false);
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
            attackManager.Fire();
        }
    }
}
