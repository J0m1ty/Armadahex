using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum NoticeState
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
        public NoticeState state;
        public NoticeState[] paralells;
        public bool disableForm;
    }

    public NoticeState state { get; private set; }

    [SerializeField]
    private Notice[] notices;

    public bool fatal => state == NoticeState.Fatal;

    public void SetState(NoticeState state) {
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
        SetState(fatal ? NoticeState.Fatal : NoticeState.Error);

        foreach (Notice notice in notices) {
            if ((fatal && notice.state == NoticeState.Fatal) || (!fatal && notice.state == NoticeState.Error)) {
                notice.text.text = error;
            }
        }
    }
}
