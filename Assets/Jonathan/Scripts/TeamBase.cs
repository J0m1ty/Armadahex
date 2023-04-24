using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TeamBase : MonoBehaviour {
    public HexGrid hexMap;
    public GameObject terrainContainer;
    public Vector2 offset;
    public Team team;

    public TerrainBlock terrainBlock { get; private set; }

    void Start() {
        hexMap.teamBase = this;
    }

    private void ClearTerrain() {
        foreach (Transform child in terrainContainer.transform) {
            Destroy(child.gameObject);
        }
    }

    public void SetTerrain(TerrainBlock terrain) {
        terrainBlock = terrain;

        ClearTerrain();

        var obj = Instantiate(terrain.terrainPrefab, terrainContainer.transform) as GameObject;
        obj.transform.localPosition = new Vector3(offset.x, obj.transform.localPosition.y, offset.y);

        hexMap.CheckTerrain();
    }
}