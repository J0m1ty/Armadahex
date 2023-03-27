using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CopyTransform : MonoBehaviour
{
    public Transform target;

    public bool copyRotation = false;
    public bool copyX = false;
    public bool copyY = false;
    public bool copyZ = false;
    
    void LateUpdate()
    {
        if (target == null) return;
        
        Vector3 pos = transform.position;
        if (copyX) pos.x = target.position.x;
        if (copyY) pos.y = target.position.y;
        if (copyZ) pos.z = target.position.z;
        transform.position = pos;
        
        if (copyRotation) {
            transform.rotation = target.rotation;
        }
    }
}
