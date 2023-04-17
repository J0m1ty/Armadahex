using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MyBox;

public enum RandomTextOption {
    Constant,
    Break,
    RandomNumber,
    RandomFromList,
}

[System.Serializable]
public class RandomString {
    [SerializeField]
    private RandomTextOption option;

    [ConditionalField(nameof(option), false, RandomTextOption.Constant)]
    [SerializeField]
    private string constant;
    
    [ConditionalField(nameof(option), false, RandomTextOption.RandomNumber)]
    [SerializeField]
    private int digits;
    
    [ConditionalField(nameof(option), false, RandomTextOption.RandomNumber)]
    [SerializeField]
    private int decimalPlaces;

    [ConditionalField(nameof(option), false, RandomTextOption.RandomNumber)]
    [SerializeField]
    private MinMaxInt range;
    
    [ConditionalField(nameof(option), false, RandomTextOption.RandomFromList)]
    [SerializeField]
    private string[] list;

    public string GetRandomString() {
        switch (option) {
            case RandomTextOption.Constant:
                return constant;
            case RandomTextOption.Break:    
                return "\n";
            case RandomTextOption.RandomNumber:
                var lower = range.Min == 0 ? Mathf.Pow(10, digits - 1) : Mathf.Min(Mathf.Pow(10, digits - 1), range.Min);
                var upper = range.Max == 0 ? Mathf.Pow(10, digits) : Mathf.Min(Mathf.Pow(10, digits), range.Max);
                return Random.Range(lower, upper).ToString("F" + decimalPlaces);
            case RandomTextOption.RandomFromList:
                return list[Random.Range(0, list.Length)];
            default:
                return "";
        }
    }
}

[RequireComponent(typeof(TMP_Text))]
public class RandomText : MonoBehaviour
{
    [SerializeField]
    private List<RandomString> randomStrings; 

    void Awake() {
        TMP_Text text = GetComponent<TMP_Text>();
        string newText = "";
        foreach (RandomString randomString in randomStrings) {
            newText += randomString.GetRandomString();
        }
        text.text = newText;
    }

    public void ResetText() {
        Awake();
    }

    public static void ResetText(Transform transform) {
        // go through children
        foreach (Transform child in transform) {
            // if child has RandomText component, reset text
            if (child.TryGetComponent<RandomText>(out RandomText randomText)) {
                randomText.ResetText();
            }
            // if child has children, recurse
            if (child.childCount > 0) {
                ResetText(child);
            }
        }
    }
}
