using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum P {
    NW,
    NE,
    SE,
    SW
}

public class Cell {
    public Vector2Int position;
    public int depth;
    public int size;
    public Cell parent;
    public Cell[] children;
    public P? posInParent;

    public static TerrainBuilder tb;
    
    public Cell(Vector2Int position, int depth, int size, Cell parent, P? posInParent) {
        this.position = position;
        this.depth = depth;
        this.size = size;
        this.parent = parent;
        this.children = null;
        this.posInParent = posInParent;

        var gridSize = 1 << tb.exp;

        var dx = ((float)position.x + (float)size / 2f) - ((float)gridSize / 2f);
        var dy = ((float)position.y + (float)size / 2f) - ((float)gridSize / 2f);
        var dist = Mathf.Sqrt(dx * dx + dy * dy);

        var sample = tb.step.Evaluate(Mathf.Clamp01(TerrainBuilder.Map(dist, 0, gridSize / 2, 0, 1))).a;
        var expected = Mathf.Clamp(TerrainBuilder.Map(sample, 0, 1, tb.exp, tb.scale), tb.max, tb.exp);

        //var expected = Mathf.Clamp(TerrainBuilder.Map(Mathf.Log(dist, 2), 0, Mathf.Log(gridSize / 2), tb.exp, tb.scale), tb.max, tb.exp);

        if (depth < expected && depth < tb.min) {
            Subdivide();
        }
    }

    public void Subdivide() {
        var halfSize = size / 2;
        
        children = new Cell[4] {
            new Cell(position, depth + 1, halfSize, this, P.NW),
            new Cell(position + new Vector2Int(halfSize, 0), depth + 1, halfSize, this, P.NE),
            new Cell(position + new Vector2Int(halfSize, halfSize), depth + 1, halfSize, this, P.SE),
            new Cell(position + new Vector2Int(0, halfSize), depth + 1, halfSize, this, P.SW)
        };
    }

    public List<Cell> Collect() {
        List<Cell> allCells = new List<Cell>();

        if (children != null) {
            foreach (var child in children) {
                allCells.AddRange(child.Collect());
            }
        }
        else {
            allCells.Add(this);
        }

        return allCells;
    }
}

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TerrainBuilder : MonoBehaviour
{
    public Mesh mesh { get; private set; }
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    
    [Header("Terrain Settings")]
    public Material material;
    public Gradient step;
    public int exp = 9;
    public int max = 9;
    public int min = 5;
    public int scale = 1;

    private void Awake() {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        mesh = new Mesh();
        mesh.name = "Terrain Mesh";

        meshFilter.mesh = mesh;
        
        meshRenderer.material = material;
    }

    private void OnEnable() {
        GenerateCells();
    }

    private void GenerateCells() {
        Cell.tb = this;
        
        var rootCell = new Cell(Vector2Int.zero, 1, 1 << exp, null, null);

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        
        var cells = rootCell.Collect().ToArray();

        // Generate vertices
        for (var i = 0; i < cells.Length; i++) {
            var cell = cells[i];
            
            var x = cell.position.x;
            var y = cell.position.y;
            var s = cell.size;

            var v0 = new Vector3(x, 0, y);
            var v1 = new Vector3(x + s, 0, y);
            var v2 = new Vector3(x + s, 0, y + s);
            var v3 = new Vector3(x, 0, y + s);
            var v4 = new Vector3(x + s / 2f, 0, y + s / 2f);
            
            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);
            vertices.Add(v4);

            var gridSize = 1 << exp;

            var uvx = Map(x, 0, gridSize, 0, gridSize / 2);
            var uvy = Map(y, 0, gridSize, 0, gridSize / 2);
            var uvS = Map(s, 0, gridSize, 0, gridSize / 2);
            
            uvs.Add(new Vector2(uvx, uvy));
            uvs.Add(new Vector2(uvx + uvS, uvy));
            uvs.Add(new Vector2(uvx + uvS, uvy + uvS));
            uvs.Add(new Vector2(uvx, uvy + uvS));
            uvs.Add(new Vector2(uvx + uvS / 2f, uvy + uvS / 2f));
        }

        // Remove duplicates
        for (var i = 0; i < vertices.Count; i++) {
            for (var j = i + 1; j < vertices.Count; j++) {
                if (vertices[i] == vertices[j]) {
                    vertices.RemoveAt(j);
                    uvs.RemoveAt(j);
                    j--;
                }
            }
        }

        // Remove seams and calculate triangles
        for (var i = 0; i < cells.Length; i++) {
            var cell = cells[i];

            var x = cell.position.x;
            var y = cell.position.y;
            var s = cell.size;

            var searchNW = new Vector3(cell.position.x, 0, cell.position.y);
            var searchNE = new Vector3(cell.position.x + cell.size, 0, cell.position.y);
            var searchSE = new Vector3(cell.position.x + cell.size, 0, cell.position.y + cell.size);
            var searchSW = new Vector3(cell.position.x, 0, cell.position.y + cell.size);
            var searchCenter = new Vector3(cell.position.x + cell.size/2, 0, cell.position.y + cell.size/2);
            
            var NW = vertices.IndexOf(searchNW);
            var NE = vertices.IndexOf(searchNE);
            var SE = vertices.IndexOf(searchSE);
            var SW = vertices.IndexOf(searchSW);
            var center = vertices.IndexOf(searchCenter);
            
            // north
            if (cell.position.y != 0) {
                var searchMid = new Vector3(cell.position.x + cell.size/2, 0, cell.position.y);
                var mid = vertices.IndexOf(searchMid);

                // check if mid exists
                if (mid == -1) {
                    triangles.Add(NW);
                    triangles.Add(NE);
                    triangles.Add(center);
                }
                else {
                    triangles.Add(NW);
                    triangles.Add(mid);
                    triangles.Add(center);

                    triangles.Add(mid);
                    triangles.Add(NE);
                    triangles.Add(center);
                }
            }
            else {
                triangles.Add(NW);
                triangles.Add(NE);
                triangles.Add(center);
            }

            // east
            if (cell.position.x + cell.size != 1 << exp) {
                var searchMid = new Vector3(cell.position.x + cell.size, 0, cell.position.y + cell.size/2);
                var mid = vertices.IndexOf(searchMid);

                // check if mid exists
                if (mid == -1) {
                    triangles.Add(NE);
                    triangles.Add(SE);
                    triangles.Add(center);
                }
                else {
                    triangles.Add(NE);
                    triangles.Add(mid);
                    triangles.Add(center);

                    triangles.Add(mid);
                    triangles.Add(SE);
                    triangles.Add(center);
                }
            }
            else {
                triangles.Add(NE);
                triangles.Add(SE);
                triangles.Add(center);
            }

            // south
            if (cell.position.y + cell.size != 1 << exp) {
                var searchMid = new Vector3(cell.position.x + cell.size/2, 0, cell.position.y + cell.size);
                var mid = vertices.IndexOf(searchMid);

                // check if mid exists
                if (mid == -1) {
                    triangles.Add(SW);
                    triangles.Add(center);
                    triangles.Add(SE);
                }
                else {
                    triangles.Add(SW);
                    triangles.Add(center);
                    triangles.Add(mid);
                    
                    triangles.Add(mid);
                    triangles.Add(center);
                    triangles.Add(SE);
                }
            }
            else {
                triangles.Add(SW);
                triangles.Add(center);
                triangles.Add(SE);
            }

            // west
            if (cell.position.x != 0) {
                var searchMid = new Vector3(cell.position.x, 0, cell.position.y + cell.size/2);
                var mid = vertices.IndexOf(searchMid);

                // check if mid exists
                if (mid == -1) {
                    triangles.Add(NW);
                    triangles.Add(center);
                    triangles.Add(SW);
                }
                else {
                    triangles.Add(NW);
                    triangles.Add(center);
                    triangles.Add(mid);

                    triangles.Add(mid);
                    triangles.Add(center);
                    triangles.Add(SW);
                }
            }
            else {
                triangles.Add(NW);
                triangles.Add(center);
                triangles.Add(SW);
            }
        }

        mesh.Clear();

        Debug.Log(vertices.Count + " " + uvs.Count);

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    public static float Map(float value, float from1, float to1, float from2, float to2) {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}
