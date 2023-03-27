using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipSegment : MonoBehaviour {
    public Ship parent;

    public GridUnit gridRef;

    private bool _isAlive = true;
    public bool isAlive {
        get {
            return _isAlive;
        }
        set {
            _isAlive = value;
            if (value) {
                gameObject.SetActive(false);
            } else {
                gameObject.SetActive(true);
            }
        }
    }
}