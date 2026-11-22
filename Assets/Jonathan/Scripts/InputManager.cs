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

    void Start() {
        scoreboardAction = GameInput.Get("Gameplay/Scoreboard");
        switchShipAction = GameInput.Get("Gameplay/Switch Ship");
        fireAction = GameInput.Get("Gameplay/Fire");
        cancelAction = GameInput.Get("Gameplay/Cancel");

        if (hideScoreboardOnStart) {
            scoreboard.SetActive(false);
        }
    }
    
    void Update() {
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
        
        if (fireAction.WasPressedThisFrame()) {
            if (fireButton.isActiveAndEnabled) {
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
