using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class StateManager : MonoBehaviour
{
    [Serializable]
    public struct MenuState {
        public GameMode gameMode;

        public string name;

        [TextArea(3, 10)]
        public string description;

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
    private UIDocument document;

    // every mode but the default gets a button in here
    private VisualElement modes;
    private Label stateName;
    private Label stateHeader;
    private Label stateDescription;
    private Button stateButton;
    public TextField stateInput { get; private set; }

    private Dictionary<string, Button> modeButtons = new Dictionary<string, Button>();

    private void Awake() {
        var root = document.rootVisualElement;
        modes = root.Q("modes");
        stateName = root.Q<Label>("stateName");
        stateHeader = root.Q<Label>("stateHeader");
        stateDescription = root.Q<Label>("stateDescription");
        stateButton = root.Q<Button>("join");
        stateInput = root.Q<TextField>("code");
    }

    private void Start() {
        foreach (MenuState state in states) {
            if (!state.isDefault) {
                var name = state.name;
                var button = new Button { text = name.ToUpper(), name = name };
                UIKit.OnClick(button, () => SetState(name), ClickSound.Light);
                modes.Add(button);
                modeButtons[name] = button;
            }
        }

        foreach (MenuState state in states) {
            if (state.isDefault) {
                SetState(state.name);
            }
        }

        stateInput.RegisterValueChangedCallback(e => {
            DisplayButton(e.newValue.Length == 4);
        });
    }

    public void DisplayButton(bool display) {
        if (currentState.hideButtonUntilValidInput) {
            UIKit.Show(stateButton, currentState.isDefault ? false : display);
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

        foreach (var pair in modeButtons) {
            UIKit.SetLit(pair.Value, pair.Key == currentState.name);
        }

        if (currentState.isDefault) {
            UIKit.Show(stateName, false);
            UIKit.Show(stateDescription, false);
            UIKit.Show(stateButton, false);

            stateHeader.text = "SELECT GAME MODE";
        }
        else {
            UIKit.Show(stateName, true);
            UIKit.Show(stateDescription, true);
            UIKit.Show(stateButton, true);

            stateHeader.text = "DETAILS";
        }
        
        stateName.text = currentState.name;
        
        stateDescription.text = currentState.description;

        stateButton.text = currentState.buttonText;
        
        stateInput.value = "";
        UIKit.Show(stateInput, currentState.showInput);

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
