using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private KeyCode scoreboardKey = KeyCode.Space;

    [Header("Interactions")]
    [SerializeField] private AttackUIManager attackManager;
    [SerializeField] private KeyCode switchShip = KeyCode.Tab;

    [SerializeField] private Button fireButton;
    [SerializeField] private KeyCode fire = KeyCode.Return;

    [SerializeField] private Button cancelButton;
    [SerializeField] private KeyCode cancel = KeyCode.Escape;

    void Start() {
        if (hideScoreboardOnStart) {
            scoreboard.SetActive(false);
        }
    }
    
    void Update() {
        if (Input.GetKeyDown(scoreboardKey)) {
            if (scoreboardMode == KeyMode.Toggle) {
                scoreboard.SetActive(!scoreboard.activeSelf);
            } else {
                scoreboard.SetActive(true);
            }
        }

        if (Input.GetKeyUp(scoreboardKey) && scoreboardMode == KeyMode.Hold) {
            scoreboard.SetActive(false);
        }

        if (Input.GetKeyDown(switchShip)) {
            attackManager.SelectRandomShip();
        }
        
        if (Input.GetKeyDown(fire)) {
            if (fireButton.isActiveAndEnabled) {
                fireButton.onClick.Invoke();
            }
        }

        if (Input.GetKeyDown(cancel)) {
            if (cancelButton.isActiveAndEnabled) {
                cancelButton.onClick.Invoke();
            }
        }
    }
}
