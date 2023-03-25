using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class NameGroup {
    public TMP_Text name;
    public TMP_Text divider;

    public void SetEnabled(bool enabled) {
        name.gameObject.SetActive(enabled);
        divider.gameObject.SetActive(enabled);
    }
}

public class StateManager : MonoBehaviour
{
    [Serializable]
    public struct MenuState {
        public GameMode gameMode;

        public string name;
        public bool showName;

        [TextArea(3, 10)]
        public string description;

        public Button select;

        public string buttonText;
        public bool showButton;
        
        public bool showInput;

        public bool isDefault;
    }

    public MenuState currentState { get; private set; }

    [SerializeField]
    private MenuState[] states;

    [SerializeField]
    private NameGroup stateName;

    [SerializeField]
    private TMP_Text stateDescription;

    [SerializeField]
    private Button stateButton;

    [SerializeField]
    private TMP_InputField stateInput;

    private void Start() {
        int i = 0;
        foreach (MenuState state in states) {
            if (state.isDefault) {
                SetState(i);
            }

            if (state.select != null) {
                state.select.onClick.AddListener(() => {
                    SetState(state.name);
                    Debug.Log("Clicked " + state.name);
                });
            }

            i++;
        }
    }

    public void SetState(int index) {
        if (index < 0 || index >= states.Length) {
            Debug.LogError("Invalid state index!");
            return;
        }

        if (currentState.name == states[index].name) {
            int i = 0;
            foreach (MenuState state in states) {
                if (state.isDefault) {
                    SetState(i);
                    break;
                }
                i++;
            }
            return;
        }

        currentState = states[index];
        
        stateName.name.text = currentState.name;
        stateName.SetEnabled(currentState.showName);
        
        stateDescription.text = currentState.description;

        stateButton.GetComponentInChildren<TMP_Text>().text = currentState.buttonText;
        stateButton.gameObject.SetActive(currentState.showButton);
        
        stateInput.text = "";
        stateInput.gameObject.SetActive(currentState.showInput);
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

        Debug.LogError("Invalid state name!");
    }
}
