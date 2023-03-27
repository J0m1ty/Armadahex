using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableOnPlay : MonoBehaviour
{
    public List<GameObject> objectsToEnable;

    void Awake() {
        foreach (var obj in objectsToEnable) {
            obj.SetActive(true);
        }
    }
}