using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CanvasRenderer))]
public class MatchmakingShaderUpdater : MonoBehaviour
{
    public float distance = 0f;

    public void Update() {
        distance = Mathf.PingPong(Time.time * 1000f, 1980f);

        GetComponent<CanvasRenderer>().GetMaterial().SetFloat("_Distance", distance);
    }
}
