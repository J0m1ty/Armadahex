using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TeamBase : MonoBehaviour {
    public HexGrid hexMap;
    public GameObject terrainContainer;
    public Vector2 offset;
    public Team team;

    public ScreenshotCamera screenshotCamera { get; private set; }

    private TerrainBlock _terrainBlock;
    public TerrainBlock terrainBlock { 
        get {
            return _terrainBlock;
        } 
        private set {
            _terrainBlock = value;
            screenshotCamera = new ScreenshotCamera();
            screenshotCamera.obj = Instantiate(_terrainBlock.screenshotCameraPrefab, transform, false) as GameObject;
        }
    }

    void Start() {
        hexMap.teamBase = this;
    }

    private void ClearTerrain() {
        foreach (Transform child in terrainContainer.transform) {
            Destroy(child.gameObject);
        }
    }

    public void SetTerrain(TerrainBlock terrain) {
        Debug.Log("Setting terrain to " + terrain.name);
        terrainBlock = terrain;

        ClearTerrain();

        var obj = Instantiate(terrain.terrainPrefab, terrainContainer.transform) as GameObject;
        obj.transform.localPosition = new Vector3(offset.x, obj.transform.localPosition.y, offset.y);

        hexMap.CheckTerrain();
    }
}