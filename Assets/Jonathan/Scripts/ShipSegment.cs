using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipSegment : MonoBehaviour {
    public Ship parent;

    public GridUnit gridRef;
    
    public bool isAlive;

    public GameObject flames;

    public void Destroy() {
        isAlive = false;
        parent.UpdateVisibility();
        if (parent.team.isPlayer && flames) {
            flames.SetActive(true);
        }
    }
}