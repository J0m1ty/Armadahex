using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class HexRenderer : MonoBehaviour
{
    public Mesh mesh { get; private set; }
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    
    [Header("Hex Settings")]
    public Material material;
    public float size;
    public bool isFlatTopped;

    [Header("Map Integration")]
    public GridUnit gridRef;

    private void Awake() {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        mesh = new Mesh();
        mesh.name = "Hex Mesh";

        meshFilter.mesh = mesh;
        
        SetMaterial(material ?? new Material(Shader.Find("Universal Render Pipeline/Lit")));
    }

    public void SetMaterial(Material material) {
        this.material = material;
        meshRenderer.material = material;
    }

    public void GenerateMesh() {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        
        vertices.Add(Vector3.zero);

        for (int i = 0; i < 6; i++) {
            Vector3 vertex = GetVertex(i);
            vertices.Add(vertex);

            triangles.Add(0);
            triangles.Add(Polar.Mod(i, 6) + 1);
            triangles.Add(Polar.Mod(i + 1, 6) + 1);
        }

        triangles.Reverse();

        mesh.Clear();

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private Vector3 GetVertex(int i) {
        float angle = Mathf.Deg2Rad * (60 * i - (isFlatTopped ? 0 : 30));
        return new Vector3(size * Mathf.Cos(angle), 0, size * Mathf.Sin(angle));
    }
}