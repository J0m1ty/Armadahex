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

    public static LODMeshGenerator tb;
    
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

        var sample = tb.step.Evaluate(Mathf.Clamp01(LODMeshGenerator.Map(dist, 0, gridSize / 2, 0, 1))).a;
        var expected = Mathf.Clamp(LODMeshGenerator.Map(sample, 0, 1, tb.min, tb.scale), tb.max, tb.min);

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
public class LODMeshGenerator : MonoBehaviour
{
    public Mesh mesh { get; private set; }
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    [Header("Saved Mesh")]
    public Mesh savedMesh;
    
    [Header("Mesh Settings")]
    public Material material;
    public Gradient step;
    public int exp = 9;
    public int max = 9;
    public int min = 5;
    public int scale = 1;
    
    [MyBox.DefinedValues(0, 1)]
    public int uvType = 0;

    [Header("Follow Settings")]
    public bool followCamera = true;
    // must be a multiple of the finest cell and move every texture by whole tiles (uv = world / 2, foam patterns are scaled down further)
    public int snapSize = 64;
    public float maxFocusDistance = 600f;

    private Dictionary<Vector3, int> vertexIndices;

    private void Awake() {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (savedMesh) {
            mesh = savedMesh;
            meshFilter.mesh = mesh;
            meshRenderer.material = material;
            return;
        }

        mesh = new Mesh();
        mesh.name = "LOD Mesh";

        meshFilter.mesh = mesh;
        
        meshRenderer.material = material;
    }

    private void OnEnable() {
        if (savedMesh) return;
        
        GenerateCells();
    }

    private void LateUpdate() {
        if (!followCamera || uvType != 0 || Camera.main == null) return;

        // snap so vertices and textures line up in world space
        var gridSize = 1 << exp;
        var snap = snapSize;

        // center the detail on where the camera is looking, not where it sits
        var cam = Camera.main.transform;
        var target = cam.position;
        if (cam.forward.y < -0.01f) {
            var distance = Mathf.Min((cam.position.y - transform.position.y) / -cam.forward.y, maxFocusDistance);
            target = cam.position + cam.forward * distance;
        }

        var x = Mathf.Round(target.x / snap) * snap;
        var z = Mathf.Round(target.z / snap) * snap;

        transform.position = new Vector3(x - gridSize / 2, transform.position.y, z - gridSize / 2);
    }

    private int FindVertex(Vector3 vertex) {
        return vertexIndices.TryGetValue(vertex, out var index) ? index : -1;
    }

    private void GenerateCells() {
        Cell.tb = this;
        
        var rootCell = new Cell(Vector2Int.zero, 1, 1 << exp, null, null);

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        vertexIndices = new Dictionary<Vector3, int>();

        void AddVertex(Vector3 vertex, Vector2 uv) {
            if (vertexIndices.ContainsKey(vertex)) return;

            vertexIndices[vertex] = vertices.Count;
            vertices.Add(vertex);
            uvs.Add(uv);
        }

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
            
            var gridSize = 1 << exp;
            var toSize = uvType == 0 ? gridSize / 2 : 1;

            var uvx = Map(x, 0, gridSize, 0, toSize);
            var uvy = Map(y, 0, gridSize, 0, toSize);
            var uvS = Map(s, 0, gridSize, 0, toSize);
            
            AddVertex(v0, new Vector2(uvx, uvy));
            AddVertex(v1, new Vector2(uvx + uvS, uvy));
            AddVertex(v2, new Vector2(uvx + uvS, uvy + uvS));
            AddVertex(v3, new Vector2(uvx, uvy + uvS));
            AddVertex(v4, new Vector2(uvx + uvS / 2f, uvy + uvS / 2f));
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
            
            var NW = FindVertex(searchNW);
            var NE = FindVertex(searchNE);
            var SE = FindVertex(searchSE);
            var SW = FindVertex(searchSW);
            var center = FindVertex(searchCenter);
            
            // north
            if (cell.position.y != 0) {
                var searchMid = new Vector3(cell.position.x + cell.size/2, 0, cell.position.y);
                var mid = FindVertex(searchMid);

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
                var mid = FindVertex(searchMid);

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
                var mid = FindVertex(searchMid);

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
                var mid = FindVertex(searchMid);

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
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

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
