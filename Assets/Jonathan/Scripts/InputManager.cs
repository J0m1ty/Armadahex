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
    // the numbered buttons on screen: ships, then weapons, then patterns
    private InputAction[] choiceActions = new InputAction[5];

    void Start() {
        for (var i = 0; i < choiceActions.Length; i++) {
            choiceActions[i] = GameInput.Get("Gameplay/Choice " + (i + 1));
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
            attackManager.SelectRandomShip();
        }

        for (var i = 0; i < choiceActions.Length; i++) {
            if (choiceActions[i].WasPressedThisFrame()) {
                attackManager.ChooseNumber(i + 1);
            }
        }

        if (fireAction.WasPressedThisFrame()) {
            if (fireButton.isActiveAndEnabled && fireButton.interactable) {
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
