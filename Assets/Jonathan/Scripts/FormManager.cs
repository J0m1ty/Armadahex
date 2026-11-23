using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// the login and sign up form: tab moves between fields, enter submits
public class FormManager : MonoBehaviour
{
    [SerializeField] private UIDocument document;

    private TextField usernameField;
    private TextField passwordField;
    private TextField confirmField;
    private Button loginTab;
    private Button signupTab;
    private Button submitButton;

    public string username => usernameField.value;
    public string password => passwordField.value;

    // the server makes an account the first time it sees a name, so both modes log in, sign up only adds the checks
    public bool signingUp { get; private set; }

    private bool _isEnabled;

    public bool isEnabled {
        get => _isEnabled;
        set {
            _isEnabled = value;

            Find();
            if (usernameField != null) {
                usernameField.SetEnabled(value);
                passwordField.SetEnabled(value);
                confirmField?.SetEnabled(value);
                submitButton?.SetEnabled(value);
            }
        }
    }

    // the title has no form, only the account screen does
    private void Find() {
        if (usernameField != null || document == null) return;

        var root = document.rootVisualElement;
        if (root == null) return;
        usernameField = root.Q<TextField>("username");
        passwordField = root.Q<TextField>("password");
        confirmField = root.Q<TextField>("confirm");
        loginTab = root.Q<Button>("loginTab");
        signupTab = root.Q<Button>("signupTab");
        submitButton = root.Q<Button>("submit");
    }

    void Start() {
        Find();
        if (usernameField == null) return;

        // names and passwords only take letters, numbers and _
        foreach (var field in new[] { usernameField, passwordField, confirmField }) {
            if (field == null) continue;
            var f = field;
            f.RegisterValueChangedCallback(e => {
                var clean = Regex.Replace(e.newValue ?? "", "[^a-zA-Z0-9_]", "");
                if (clean != e.newValue) f.SetValueWithoutNotify(clean);
            });
        }

        if (loginTab != null) UIKit.OnClick(loginTab, () => SetMode(false), ClickSound.Light);
        if (signupTab != null) UIKit.OnClick(signupTab, () => SetMode(true), ClickSound.Light);
        if (submitButton != null) UIKit.OnClick(submitButton, () => GetComponent<ConnectionManager>().Login(), ClickSound.Heavy);
        SetMode(false);
    }

    void Update() {
        if (usernameField == null || !isEnabled) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) {
            // a focused button clicks itself
            if (UIKit.Focused(document) is Button) return;
            GetComponent<ConnectionManager>().Login();
        }
    }

    public void SetMode(bool signUp) {
        signingUp = signUp;

        UIKit.SetLit(loginTab, !signUp);
        UIKit.SetLit(signupTab, signUp);
        if (confirmField != null) {
            confirmField.value = "";
            UIKit.Show(confirmField, signUp);
        }
        if (submitButton != null) submitButton.text = signUp ? "CREATE ACCOUNT" : "ENTER";

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
        else if (signingUp && confirmField != null && confirmField.value != password) {
            error = "Passwords don't match.";
            confirmField.value = "";
            Focus(confirmField);
        }
        return error == null;
    }

    private void Focus(TextField field) {
        if (field == null) return;
        // focus waits a frame, a field that was just shown can't take it yet
        field.schedule.Execute(() => field.Focus());
    }

    public void Clear() {
        usernameField.value = "";
        passwordField.value = "";
        if (confirmField != null) confirmField.value = "";
    }

    public void Incorrect() {
        passwordField.value = "";
        if (confirmField != null) confirmField.value = "";
        Focus(passwordField);
    }
}
