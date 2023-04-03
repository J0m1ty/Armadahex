using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class HexBorder : MonoBehaviour
{
    public Mesh mesh { get; private set; }
    [MyBox.AutoProperty]
    private MeshFilter meshFilter;
    [MyBox.AutoProperty]
    private MeshRenderer meshRenderer;
    
    public Material borderMaterial;

    public float height;
    public HexGrid hexGrid;

    private void Awake() {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        mesh = new Mesh();
        mesh.name = "Hex Border Mesh";

        meshFilter.mesh = mesh;
        
        SetMaterial(borderMaterial ?? new Material(Shader.Find("Universal Render Pipeline/Lit")));

        GenerateMesh();
    }

    public void SetMaterial(Material material) {
        this.borderMaterial = material;
        meshRenderer.material = material;
    }

    public void SetColor(Color color) {
        borderMaterial.SetColor("_Color", color);
    }

    public void SetHeight(float height) {
        this.height = height;
        //GenerateMesh();
    }

    public void GenerateMesh() {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i < 6; i++) {
            vertices.Add(GetVertex(hexGrid.size, 0, i, hexGrid.isFlatTopped));
            vertices.Add(GetVertex(hexGrid.size, height, i, hexGrid.isFlatTopped));

            triangles.Add(i * 2);
            triangles.Add(i * 2 + 1);
            triangles.Add(((i + 1) * 2) % 12);

            triangles.Add(i * 2 + 1);
            triangles.Add(((i + 1) * 2 + 1) % 12);
            triangles.Add(((i + 1) * 2) % 12);
            
            // interior triangles
            // triangles.Add(i * 2);
            // triangles.Add(((i + 1) * 2) % 12);
            // triangles.Add(i * 2 + 1);

            // triangles.Add(i * 2 + 1);
            // triangles.Add(((i + 1) * 2) % 12);
            // triangles.Add(((i + 1) * 2 + 1) % 12);

            var progress = LODMeshGenerator.Map(i, 0, 6, 0, 1);

            uvs.Add(new Vector2(0, progress));
            uvs.Add(new Vector2(1, progress));
        }

        mesh.Clear();

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private Vector3 GetVertex(float size, float height, float i, bool isFlatTopped = false) {
        float angle = Mathf.Deg2Rad * (60 * i - (isFlatTopped ? 0 : 30));
        return new Vector3(size * Mathf.Cos(angle), height, size * Mathf.Sin(angle));
    }
}
