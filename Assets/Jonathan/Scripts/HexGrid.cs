using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MyBox;

public class GridUnit {
    public CoordinateSystem coords;
    public HexRenderer hexRenderer;

    public GridUnit(CoordinateSystem coords, HexRenderer hex) {
        this.coords = coords;
        this.hexRenderer = hex;

        hexRenderer.gridRef = this;
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

    [Header("Hex Settings")]
    public float size;
    public bool isFlatTopped;
    public Material material;

    [Header("Hexes")]
    public List<GridUnit> hexes;

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

                bool corner = layer <= 1 ? true : (Polar.Mod(p, r) == r - 1);
                
                if (p == layerSize - 1) {
                    direction = 5;
                }
                else if (corner && direction != 5) {
                    direction = Polar.Mod(direction + 1, 6);
                }

                AddHex($"Hex {i}", i, new Vector3(drawPointer.x, 0f, drawPointer.y));

                float theta = direction * Mathf.PI / 3f + (isFlatTopped ? Mathf.PI / 6f : 0f);
                drawPointer.x += size * Mathf.Cos(theta) * Mathf.Sqrt(3);
                drawPointer.y += size * Mathf.Sin(theta) * Mathf.Sqrt(3);

                if (p == layerSize - 1) {
                    direction = 0;
                }
            }
        }
    }

    private void AddHex(string name, int index, Vector3 position) {
        GameObject tile = new GameObject(name, typeof(HexRenderer));
        tile.transform.SetParent(transform, true);
        tile.transform.position = transform.position + position;
        
        HexRenderer hex = tile.GetComponent<HexRenderer>();
        var m = new Material(material);
        m.color = Color.HSVToRGB(UnityEngine.Random.Range(0f, 1f), 1f, 1f);
        hex.SetMaterial(m);
        hex.size = size;
        hex.isFlatTopped = isFlatTopped;
        hex.GenerateMesh();

        MeshCollider meshCollider = tile.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = hex.mesh;

        hexes.Add(new GridUnit(new CoordinateSystem(new Spiral(index)), hex));
    }

    public GridUnit FromCoordinates(CoordinateSystem coords) {
        return hexes.Find(hex => hex.coords.Equals(coords));
    }
}