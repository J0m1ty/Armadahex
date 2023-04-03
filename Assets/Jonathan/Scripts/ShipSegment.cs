using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipSegment : MonoBehaviour {
    public Ship parent;

    public GridUnit gridRef;
    
    public bool isAlive;

    public GameObject fireEffect;

    public void Destroy() {
        isAlive = false;
        fireEffect.SetActive(true);
        parent.UpdateStatus();
    }
}