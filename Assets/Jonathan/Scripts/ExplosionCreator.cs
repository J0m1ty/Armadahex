using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionCreator : MonoBehaviour
{
    public static ExplosionCreator instance;

    public GameObject explosionPrefab;

    public float scale;

    private void Awake()
    {
        instance = this;
    }

    public void CreateExplosion(Vector3 atPosition)
    {
        var obj = Instantiate(explosionPrefab, atPosition, Quaternion.identity);
        obj.transform.localScale = Vector3.one * scale;
    }
    
    public void CreateExplosion(ShipSegment segment)
    {
        var obj = Instantiate(explosionPrefab, segment.transform.position, Quaternion.identity);
        obj.transform.localScale = Vector3.one * scale;
    }
}