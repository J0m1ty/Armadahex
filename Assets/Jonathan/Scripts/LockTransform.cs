using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockTransform : MonoBehaviour
{
    Quaternion initRot;
    Vector3 initPos;

    public bool lockRotation = true;
    public bool lockX = false;
    public bool lockY = false;
    public bool lockZ = false;

    // Start is called before the first frame update
    void Start()
    {
        initRot = transform.rotation;
        initPos = transform.position;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.rotation = initRot;
        
        if (lockX) {
            transform.position = new Vector3(initPos.x, transform.position.y, transform.position.z);
        }
        if (lockY) {
            transform.position = new Vector3(transform.position.x, initPos.y, transform.position.z);
        }
        if (lockZ) {
            transform.position = new Vector3(transform.position.x, transform.position.y, initPos.z);
        }
    }
}
