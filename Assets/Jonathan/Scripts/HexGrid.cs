using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MyBox;
using Random = UnityEngine.Random;

[Serializable]
public class GridUnit {
    public CoordinateSystem coords;
    public HexRenderer hexRenderer;
    public ShipSegment shipSegment;

    public bool isEdge;
    public bool preventShips;

    public GridUnit(CoordinateSystem coords, HexRenderer hex) {
        this.coords = coords;
        this.hexRenderer = hex;
        this.shipSegment = null;

        hexRenderer.gridRef = this;
    }

    public GridUnit GetNeighbor(int dir, bool reverse = false) {
        return hexRenderer.hexMap.FromCoordinatesBrute(coords.GetNeighbor(dir));
    }

    public int RangeInDirection(int dir, bool activeOnly) {
        int range = 0;
        GridUnit current = this;
        while (current != null) {
            current = current.GetNeighbor(dir);

            if (current == null) break;

            if (!(activeOnly && current.hexRenderer.gameObject.activeSelf == false)) {
                range++;
            }
        }
        return range;
    }
}

public class HexGrid : MonoBehaviour
{
    [Serializable]
    public enum GridType
    {
        Square,
        Hexagon
    }

    [Header("Grid Settings")]
    public GridType gridType;
    [ConditionalField("gridType", false, GridType.Square)]
    public Vector2Int gridSize;
    [ConditionalField("gridType", false, GridType.Hexagon)]
    public int gridRadius;
    
    [Header("Culling Settings")]
    public int cullDepth;

    [Header("Hex Settings")]
    public float size;
    public bool isFlatTopped;
    public Material material;
    [Layer]
    public int layer;
    public bool hasHeight;
    [ConditionalField("hasHeight", false, true)]
    public float height;
    [ConditionalField("hasHeight", false, true)]
    public float noiseScale;
    [ConditionalField("hasHeight", false, true)]
    public MinMaxFloat heightVariation;

    [Header("Hexes")]
    public List<GridUnit> hexes;

    [Header("Team Integration")]
    public TeamBase teamBase;

    [Header("VFX Integration")]
    public bool useFog;
    [ConditionalField("useFog", false, true)]
    public MinMaxFloat fogHeight;
    [ConditionalField("useFog", false, true)]
    public GameObject fogPrefab;
    public bool useFlames;
    [ConditionalField("useFlames", false, true)]
    public int flameCount;
    [ConditionalField("useFlames", false, true)]
    public GameObject flamePrefab;
    [ConditionalField("useFlames", false, true)]
    public int flameHeight;
    [ConditionalField("useFlames", false, true)]
    public MinMaxFloat fireSize;

    private void Awake() {
        Generate();
    }

    public void Generate() {
        hexes = new List<GridUnit>();

        switch (gridType) {
            case GridType.Square:
                SquareGrid();
                break;
            case GridType.Hexagon:
                HexagonGrid();
                break;
        }
    }

    private void SquareGrid() {
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                int i = x + y * gridSize.x;
                AddHex($"Tile {x}, {y}", i, GetPositionForHexFromCoordinates(new Vector2Int(x, y)));
            }
        }
    }

    private Vector3 GetPositionForHexFromCoordinates(Vector2Int coordinate) {
        int column = coordinate.x;
        int row = coordinate.y;

        float width;
        float height;
        float xPosition;
        float yPosition;
        bool shouldOffset;
        float horizontalDistance;
        float verticalDistance;
        float offset;

        if (!isFlatTopped) {
            shouldOffset = (row % 2) == 0;
            width = Mathf.Sqrt(3) * size;
            height = 2f * size;

            horizontalDistance = width;
            verticalDistance = height * (3f / 4f);

            offset = (shouldOffset) ? width / 2f : 0f;

            xPosition = (horizontalDistance * column) + offset;
            yPosition = verticalDistance * row;
        }
        else {
            shouldOffset = (column % 2) == 0;
            width = 2f * size;
            height = Mathf.Sqrt(3) * size;

            horizontalDistance = width * (3f / 4f);
            verticalDistance = height;

            offset = (shouldOffset) ? height / 2f : 0f;

            xPosition = horizontalDistance * column;
            yPosition = (verticalDistance * row) + offset;
        }

        return new Vector3(xPosition, 0f, -yPosition);
    }

    private void HexagonGrid() {
        Vector2 drawPointer = new Vector2(0, 0);
        int direction = 5;
        for (int layer = 0; layer < gridRadius; layer++) {
            int layerSize = layer == 0 ? 1 : 6 * layer;
            for (int pos = 0; pos < layerSize; pos++) {
                int i = layer == 0 ? 0 : Spiral.FromPolar(new Polar(layer, pos)).index;
                int r = layer == 0 ? 0 : Polar.FromSpiral(new Spiral(i)).layer;
                int p = layer == 0 ? 0 : Polar.FromSpiral(new Spiral(i)).position;

                bool corner = layer <= 1 ? true : (CoordinateSystem.Mod(p, r) == r - 1);
                
                if (p == layerSize - 1) {
                    direction = 5;
                }
                else if (corner && direction != 5) {
                    direction = CoordinateSystem.Mod(direction + 1, 6);
                }

                AddHex($"Hex {i}", i, new Vector3(drawPointer.x, 0f, drawPointer.y), r == gridRadius - 1);

                float theta = (Mathf.PI * 2f) - (direction * Mathf.PI / 3f + (isFlatTopped ? Mathf.PI / 6f : 0f));
                drawPointer.x += size * Mathf.Cos(theta) * Mathf.Sqrt(3);
                drawPointer.y += size * Mathf.Sin(theta) * Mathf.Sqrt(3);

                if (p == layerSize - 1) {
                    direction = 0;
                }
            }
        }
    }

    private void AddHex(string name, int index, Vector3 position, bool isEdge = false) {
        float noise = LODMeshGenerator.Map(Mathf.PerlinNoise(position.x * noiseScale, position.z * noiseScale), 0f, 1f, heightVariation.Min, heightVariation.Max);

        GameObject tile = new GameObject(name, typeof(HexRenderer));
        tile.transform.SetParent(transform, true);
        tile.transform.position = transform.position + position + Vector3.up * noise;
        tile.layer = layer;

        HexRenderer hex = tile.GetComponent<HexRenderer>();
        var m = new Material(material);
        m.name = "Hex Material";
        m.color = Color.white; //placeholder, gets set by team type in teamManager
        hex.SetMaterial(m);

        if (useFog) {
            var fog = Instantiate(fogPrefab, tile.transform);
            fog.transform.localPosition = Vector3.zero + Vector3.up * (UnityEngine.Random.Range(fogHeight.Min, fogHeight.Max));
            fog.transform.localRotation = Quaternion.identity * Quaternion.Euler(90f, 0f, 0f);
            fog.transform.localScale = Vector3.one;
            hex.fog = new Fog(fog.GetComponent<ParticleSystem>());
        }

        if (hasHeight) {
            var border = new GameObject("Height", typeof(HexBorder));
            border.transform.SetParent(tile.transform, true);
            border.transform.localPosition = new Vector3(0f, -height, 0f);

            var borderRenderer = border.GetComponent<HexBorder>();
            borderRenderer.hexGrid = this;
            borderRenderer.height = height;
            borderRenderer.SetMaterial(m);
            borderRenderer.SetHeight(height);
        }

        hex.coords = new CoordinateSystem(new Spiral(index));
        hex.size = size;
        hex.isFlatTopped = isFlatTopped;
        hex.hexMap = this;
        hex.fire = tile.AddComponent<FlameMaker>();
        hex.GenerateMesh();

        MeshCollider meshCollider = tile.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = hex.mesh;

        var gridUnit = new GridUnit(new CoordinateSystem(new Spiral(index)), hex);
        gridUnit.isEdge = isEdge;
        hexes.Add(gridUnit);
    }

    public void CheckTerrain() {
        foreach (GridUnit hex in hexes) {
            CheckTerrain(hex);
        }
    }

    private void CheckTerrain(GridUnit hex) {
        var pos = hex.hexRenderer.transform.position;
        
        var distToCheck = 150f;
        var allow = true;

        var vertices = hex.hexRenderer.GetVerticesInWorld();

        foreach (var vertex in vertices) {
            if (Physics.Linecast(vertex, vertex + Vector3.down * distToCheck, out RaycastHit hitPoint, 1 << 3)) {
                if (hitPoint.distance < cullDepth) {
                    allow = false;
                    break;
                }
            }
            else {
                allow = false;
                break;
            }
        }

        if (!allow) {
            hex.preventShips = true;
            hex.hexRenderer.gameObject.SetActive(false);
        }
    }

    public GridUnit FromCoordinates(CoordinateSystem coords) {
        if (coords.index >= hexes.Count || coords.index < 0)
            return null;

        return hexes[coords.index];
    }
    
    public GridUnit FromCoordinatesBrute(CoordinateSystem coords) {
        return hexes.Find(hex => hex.coords.index == coords.index);
    }


    public GridUnit FromPosition(Vector3 position) {
        return hexes.Find(hex => hex.hexRenderer.transform.position == position);
    }

    public GridUnit ClosestTo(Vector3 position, out float closestDistance) {
        var posXZ = new Vector2(position.x, position.z);

        GridUnit closest = null;
        closestDistance = float.MaxValue;
        foreach (GridUnit hex in hexes) {
            var hexXZ = new Vector2(hex.hexRenderer.transform.position.x, hex.hexRenderer.transform.position.z);

            float distance = Vector2.Distance(posXZ, hexXZ);
            if (distance < closestDistance) {
                closest = hex;
                closestDistance = distance;
            }
        }
        return closest;
    }
}