using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(HexRenderer))]
public class FlameMaker : MonoBehaviour
{
    public HexRenderer hexRenderer;

    public List<GameObject> flames;

    void Start()
    {
        hexRenderer = GetComponent<HexRenderer>();

        flames = new List<GameObject>();
    }

    public void GenerateFlames() {
        for (int i = 0; i < hexRenderer.hexMap.flameCount; i++) {
            GameObject flame = Instantiate(hexRenderer.hexMap.flamePrefab, transform);
            flame.transform.localPosition = Random.insideUnitCircle * hexRenderer.hexMap.size * 0.8f + Vector2.up * hexRenderer.hexMap.flameHeight;
            flame.GetComponent<VisualEffect>().SetFloat("Fire Size", UnityEngine.Random.Range(hexRenderer.hexMap.fireSize.Min, hexRenderer.hexMap.fireSize.Max));
            flames.Add(flame);
        }
    }

    public void EnableFlames() {
        if (flames.Count == 0)
            GenerateFlames();
    }

    public void DisableFlames() {
        foreach (GameObject flame in flames) {
            Destroy(flame);
        }
        flames.Clear();
    }
}
