using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// the login and sign up form: tab moves between fields, enter submits
public class FormManager : MonoBehaviour
{
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;

    [Header("Sign up")]
    [SerializeField] private TMP_InputField confirmField;
    [SerializeField] private UIButton loginTab;
    [SerializeField] private UIButton signupTab;
    [SerializeField] private TMP_Text submitText;
    [SerializeField] private TMP_Text modeNote;

    public string username => usernameField.text;
    public string password => passwordField.text;

    // the server makes an account the first time it sees a name, so both modes log in, sign up only adds the checks
    public bool signingUp { get; private set; }

    private bool _isEnabled;

    public bool isEnabled {
        get => _isEnabled;
        set {
            _isEnabled = value;

            if (!(usernameField == null && passwordField == null)) {
                usernameField.interactable = value;
                passwordField.interactable = value;
                if (confirmField != null) confirmField.interactable = value;
            }
        }
    }

    void Start() {
        if (usernameField == null) return;

        if (loginTab != null) loginTab.onClick.AddListener(() => SetMode(false));
        if (signupTab != null) signupTab.onClick.AddListener(() => SetMode(true));
        SetMode(false);
    }

    void Update() {
        if (usernameField == null || !isEnabled) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.tabKey.wasPressedThisFrame) {
            Step(keyboard.shiftKey.isPressed ? -1 : 1);
        }

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) {
            GetComponent<ConnectionManager>().Login();
        }
    }

    public void SetMode(bool signUp) {
        signingUp = signUp;

        if (loginTab != null) loginTab.SetLit(!signUp);
        if (signupTab != null) signupTab.SetLit(signUp);
        if (confirmField != null) {
            confirmField.text = "";
            confirmField.transform.parent.gameObject.SetActive(signUp);
        }
        if (submitText != null) submitText.text = signUp ? "CREATE ACCOUNT" : "LOG IN";
        if (modeNote != null) {
            modeNote.text = signUp
                ? "Pick a name (4-16 letters, numbers or _) and a password of 6 or more."
                : "New here? Switch to Sign up.";
        }

        GetComponent<NoticeManager>()?.SetState(NoticeState.Default);
        Focus(usernameField);
    }

    // checks the server can't do for us
    public bool Check(out string error) {
        error = null;
        if (usernameField == null) return true;

        if (username.Length == 0) {
            error = "Enter a username.";
        }
        else if (password.Length == 0) {
            error = "Enter a password.";
        }
        else if (signingUp && confirmField != null && confirmField.text != password) {
            error = "The passwords don't match.";
            confirmField.text = "";
            Focus(confirmField);
        }
        return error == null;
    }

    private void Step(int direction) {
        var fields = new List<TMP_InputField> { usernameField, passwordField };
        if (signingUp && confirmField != null) fields.Add(confirmField);

        var current = fields.FindIndex(f => f.isFocused);
        var next = current < 0 ? 0 : (current + direction + fields.Count) % fields.Count;
        Focus(fields[next]);
    }

    private void Focus(TMP_InputField field) {
        if (field == null || EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(field.gameObject);
        field.ActivateInputField();
    }

    public void Clear() {
        usernameField.text = "";
        passwordField.text = "";
        if (confirmField != null) confirmField.text = "";
    }

    public void Incorrect() {
        passwordField.text = "";
        if (confirmField != null) confirmField.text = "";
        Focus(passwordField);
    }
}
