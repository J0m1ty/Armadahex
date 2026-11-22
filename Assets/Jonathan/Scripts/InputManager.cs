using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
    [SerializeField] private GameObject scoreboard;

    [Header("Interactions")]
    [SerializeField] private AttackUIManager attackManager;
    [SerializeField] private Button fireButton;
    [SerializeField] private Button cancelButton;

    // bindings live in Resources/Controls, under Gameplay
    private InputAction scoreboardAction;
    private InputAction switchShipAction;
    private InputAction fireAction;
    private InputAction cancelAction;
    private InputAction previousAction;
    private InputAction nextAction;
    // each ship type has its own action (1 carrier, 2 battleship, 3 sub, 4 destroyer, 5 cruiser by default)
    private static readonly ShipType[] shipTypes = { ShipType.Carrier, ShipType.Battleship, ShipType.Submarine, ShipType.Destroyer, ShipType.Cruiser };
    private InputAction[] shipActions = new InputAction[5];

    void Start() {
        previousAction = GameInput.Get("Gameplay/Previous");
        nextAction = GameInput.Get("Gameplay/Next");
        for (var i = 0; i < shipActions.Length; i++) {
            shipActions[i] = GameInput.Get("Gameplay/" + shipTypes[i]);
        }

        scoreboardAction = GameInput.Get("Gameplay/Scoreboard");
        switchShipAction = GameInput.Get("Gameplay/Switch Ship");
        fireAction = GameInput.Get("Gameplay/Fire");
        cancelAction = GameInput.Get("Gameplay/Cancel");

        if (hideScoreboardOnStart) {
            scoreboard.SetActive(false);
        }
    }
    
    void Update() {
        if (GameInput.typing) return;

        if (scoreboardAction.WasPressedThisFrame()) {
            if (scoreboardMode == KeyMode.Toggle) {
                scoreboard.SetActive(!scoreboard.activeSelf);
            } else {
                scoreboard.SetActive(true);
            }
        }

        if (scoreboardAction.WasReleasedThisFrame() && scoreboardMode == KeyMode.Hold) {
            scoreboard.SetActive(false);
        }

        if (switchShipAction.WasPressedThisFrame()) {
            attackManager.SelectNextShip();
        }

        // the ship keys always pick that ship, even after a target is chosen
        for (var i = 0; i < shipActions.Length; i++) {
            if (shipActions[i].WasPressedThisFrame()) {
                attackManager.SelectShipOfType(shipTypes[i]);
            }
        }

        if (previousAction.WasPressedThisFrame()) {
            attackManager.CycleChoice(-1);
        }
        if (nextAction.WasPressedThisFrame()) {
            attackManager.CycleChoice(1);
        }

        // enter picks the highlighted weapon or pattern, or fires once it's confirmed, never both in one press
        if (fireAction.WasPressedThisFrame()) {
            if (!attackManager.ChooseHighlighted() && fireButton.isActiveAndEnabled) {
                fireButton.onClick.Invoke();
            }
        }

        if (cancelAction.WasPressedThisFrame()) {
            if (cancelButton.isActiveAndEnabled) {
                cancelButton.onClick.Invoke();
            }
        }
    }
}
