using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum State
{
    None,
    Default,
    Connecting,
    Error,
    Fatal
}

[RequireComponent(typeof(FormManager))]
public class NoticeManager : MonoBehaviour
{
    [Serializable]
    public struct Notice {
        public TMP_Text text;
        public State state;
        public State[] paralells;
        public bool disableForm;
    }

    public State state { get; private set; }

    [SerializeField]
    private Notice[] notices;

    public bool fatal => state == State.Fatal;

    public void SetState(State state) {
        this.state = state;
        
        bool disable = false;
        foreach (Notice notice in notices) {
            if (notice.state == state || Array.IndexOf(notice.paralells, state) != -1) {
                notice.text.gameObject.SetActive(true);

                if (notice.disableForm) {
                    disable = true;
                }
            }
            else {
                notice.text.gameObject.SetActive(false);
            }
        }

        FormManager formManager = GetComponent<FormManager>();
        formManager.isEnabled = !disable;
    }

    public void SetError(string error, bool fatal = false) {
        SetState(fatal ? State.Fatal : State.Error);

        foreach (Notice notice in notices) {
            if ((fatal && notice.state == State.Fatal) || (!fatal && notice.state == State.Error)) {
                notice.text.text = error;
            }
        }
    }
}
