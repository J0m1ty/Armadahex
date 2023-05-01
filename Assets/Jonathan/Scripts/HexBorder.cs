using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class HexBorder : MonoBehaviour
{
    public Mesh mesh { get; private set; }
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    
    public Material borderMaterial;

    public float height;
    public float size;
    public bool isFlatTopped;

    public bool isVisible;

    private void Awake() {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        mesh = new Mesh();
        mesh.name = "Hex Border Mesh";

        meshFilter.mesh = mesh;
        
        SetMaterial(borderMaterial ?? new Material(Shader.Find("Universal Render Pipeline/Lit")));

        if (height != 0 && size != 0) {
            GenerateMesh(height, size, isFlatTopped);
        }
    }

    public void SetVisibility(bool isVisible) {
        this.isVisible = isVisible;
        meshRenderer.enabled = isVisible;
    }

    public void SetMaterial(Material material) {
        this.borderMaterial = material;
        meshRenderer.material = this.borderMaterial;
    }

    private Color setColor;

    public void SetColor(Color color) {
        if (borderMaterial.HasColor("_Color")) {
            borderMaterial.SetColor("_Color", color);
        }
        setColor = color;
    }

    public Color GetColor() {
        return setColor;
    }

    public void SetHeight(float height) {
        if (this.height != height) {
            this.height = height;

            if (borderMaterial != null && Selector.instance != null && borderMaterial.HasFloat("_Scale")) {
                var amount = LODMeshGenerator.Map(height, Selector.instance.lockedInHeight, Selector.instance.highlightHeight, 0.2f, 0.04f);
                borderMaterial.SetFloat("_Scale", amount);
            }

            if (mesh != null) {
                GenerateMesh(height, size, isFlatTopped);
            }
        }
    }

    public void GenerateMesh(float height, float size, bool isFlatTopped) {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i < 6; i++) {
            vertices.Add(GetVertex(size, 0, i, isFlatTopped));
            vertices.Add(GetVertex(size, height, i, isFlatTopped));

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
