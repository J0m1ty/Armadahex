using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

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
    public HexGrid hexMap;
    public CoordinateSystem coords {
        get { return gridRef.coords; }
        set {
            q = value.q;
            r = value.r;
            s = value.s;
            layer = value.layer;
            position = value.position;
            index =value.index;
        }
    }
    public int q;
    public int r;
    public int s;
    public int layer;
    public int position;
    public int index;

    [Header("VFX Integration")]
    public ParticleSystem fogEffect;
    public FlameMaker fire;

    private void Awake() {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        mesh = new Mesh();
        mesh.name = "Hex Mesh";

        meshFilter.mesh = mesh;
        
        SetMaterial(material ?? new Material(Shader.Find("Universal Render Pipeline/Lit")));
    }

    void Start() {
        fogEffect = GetComponentInChildren<ParticleSystem>();
    }

    public void SetMaterial(Material material) {
        this.material = material;
        meshRenderer.material = material;
    }

    public void ClearFog() {
        if (!fogEffect || !gameObject.activeSelf) return;
        
        var emission = fogEffect.emission;
        emission.rateOverTime  = 0;

        // increase speed of all particles
        var main = fogEffect.main;
        main.simulationSpeed = 10;

        // start coroutine to turn off fog
        StartCoroutine(TurnOffFog());
    }

    public void EnableFlames() {
        fire.EnableFlames();
    }

    public void DisableFlames() {
        fire.DisableFlames();
    }
    
    public IEnumerator TurnOffFog() {
        yield return new WaitForSeconds(3);
        fogEffect.gameObject.SetActive(false);
    }

    public void GenerateMesh() {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        
        vertices.Add(Vector3.zero);

        for (int i = 0; i < 6; i++) {
            Vector3 vertex = GetVertex(i);
            vertices.Add(vertex);

            triangles.Add(0);
            triangles.Add(CoordinateSystem.Mod(i, 6) + 1);
            triangles.Add(CoordinateSystem.Mod(i + 1, 6) + 1);
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

    public Vector3[] GetVerticesInWorld() {
        Vector3[] vertices = new Vector3[6];
        for (int i = 0; i < 6; i++) {
            vertices[i] = transform.TransformPoint(GetVertex(i));
        }
        return vertices;
    }
}