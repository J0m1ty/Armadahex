using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class Toggler : MonoBehaviour
{
    [SerializeField]
    private GameObject[] targets;

    void Awake() {
        GetComponent<Button>().onClick.AddListener(Toggle);
    }

    public void Toggle() {
        foreach (var target in targets) {
            target.SetActive(!target.activeSelf);
        }
    }
}
