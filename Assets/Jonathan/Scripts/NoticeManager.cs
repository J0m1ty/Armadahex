using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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
        // the label's name in the document
        public string element;
        public NoticeState state;
        public NoticeState[] paralells;
        public bool disableForm;
    }

    public NoticeState state { get; private set; }

    [SerializeField]
    private UIDocument document;

    [SerializeField]
    private Notice[] notices;

    public bool fatal => state == NoticeState.Fatal;

    private Label Text(Notice notice) {
        return document.rootVisualElement.Q<Label>(notice.element);
    }

    public void SetState(NoticeState state) {
        this.state = state;

        bool disable = false;
        foreach (Notice notice in notices) {
            if (notice.state == state || Array.IndexOf(notice.paralells, state) != -1) {
                UIKit.Show(Text(notice), true);

                if (notice.disableForm) {
                    disable = true;
                }
            }
            else {
                UIKit.Show(Text(notice), false);
            }
        }

        FormManager formManager = GetComponent<FormManager>();
        formManager.isEnabled = !disable;
    }

    public void SetError(string error, bool fatal = false) {
        SetState(fatal ? NoticeState.Fatal : NoticeState.Error);
        GetComponent<FormManager>().Shake();

        foreach (Notice notice in notices) {
            if ((fatal && notice.state == NoticeState.Fatal) || (!fatal && notice.state == NoticeState.Error)) {
                var text = Text(notice);
                if (text != null) text.text = error;
            }
        }
    }
}
