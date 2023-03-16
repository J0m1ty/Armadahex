using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FormManager : MonoBehaviour
{
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;

    public string username => usernameField.text;
    public string password => passwordField.text;
    
    private bool _isEnabled;

    public bool isEnabled {
        get => _isEnabled;
        set {
            _isEnabled = value;

            if (!(usernameField == null && passwordField == null)) {
                usernameField.interactable = value;
                passwordField.interactable = value;
            }
        }
    }

    public void Clear() {
        usernameField.text = "";
        passwordField.text = "";
    }

    public void Incorrect() {
        passwordField.text = "";
    }
}
