using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class GameModeCard {
    public TMP_Text name;
    public Image overlay;

    public void SetEnabled(bool enabled) {
        if (name == null) {
            return;
        }

        name.gameObject.SetActive(enabled);
    }

    public void SetOverlay(float alpha) {
        if (overlay == null) {
            return;
        }

        overlay.color = new Color(overlay.color.r, overlay.color.g, overlay.color.b, alpha);
    }
}

public class StateManager : MonoBehaviour
{
    public const float NO_OVERLAY = 0f;
    public const float UNSELECTED_OVERLAY = 0.5f;

    [Serializable]
    public struct MenuState {
        public GameMode gameMode;

        public string name;

        [TextArea(3, 10)]
        public string description;

        public GameModeCard card;

        public Button select;
        public string buttonText;
        
        public bool showInput;
        public bool hideButtonUntilValidInput;

        public bool isDefault;
    }

    public MenuState currentState { get; private set; }

    public bool allowLocking;
    public bool lockedIn;

    [SerializeField]
    private MenuState[] states;

    [SerializeField]
    private TMP_Text stateName;

    [SerializeField]
    private TMP_Text stateHeader;

    [SerializeField]
    private TMP_Text stateDescription;

    [SerializeField]
    private Button stateButton;

    [SerializeField]
    public TMP_InputField stateInput;

    private void Start() {
        foreach (MenuState state in states) {
            if (state.isDefault) {
                SetState(state.name);
            }

            if (state.select != null) {
                state.select.onClick.AddListener(() => {
                    SetState(state.name);
                });
            }
        }

        stateInput.onValueChanged.AddListener((string value) => {
            DisplayButton(value.Length == 4);
        });
    }

    public void DisplayButton(bool display) {
        if (currentState.hideButtonUntilValidInput) {
            stateButton.gameObject.SetActive(currentState.isDefault ? false : display);
        }
    }

    public void SetState(int index) {
        if (allowLocking && lockedIn) {
            return;
        }
        
        if (index < 0 || index >= states.Length) {
            Debug.LogError("Invalid state index!");
            return;
        }
        
        if (currentState.name == states[index].name) {
            foreach (MenuState state in states) {
                if (state.isDefault) {
                    SetState(state.name);
                    break;
                }
            }
            return;
        }

        currentState = states[index];

        if (currentState.isDefault) {
            foreach (MenuState state in states) {
                state.card.SetOverlay(NO_OVERLAY);
            }

            stateName.gameObject.SetActive(false);
            stateDescription.gameObject.SetActive(false);
            stateButton.gameObject.SetActive(false);

            stateHeader.text = "SELECT GAME MODE";
        }
        else {
            foreach (MenuState state in states) {
                state.card.SetOverlay(UNSELECTED_OVERLAY);
            }

            stateName.gameObject.SetActive(true);
            stateDescription.gameObject.SetActive(true);
            stateButton.gameObject.SetActive(true);

            stateHeader.text = "DETAILS";

            currentState.card.SetOverlay(NO_OVERLAY);
        }
        
        stateName.text = currentState.name;
        
        stateDescription.text = currentState.description;

        stateButton.GetComponentInChildren<TMP_Text>().text = currentState.buttonText;
        
        stateInput.text = "";
        stateInput.gameObject.SetActive(currentState.showInput);

        DisplayButton(false);
    }

    public void SetState(string name) {
        int i = 0;
        foreach (MenuState state in states) {
            if (state.name == name) {
                SetState(i);
                return;
            }
            i++;
        }
    }
}
