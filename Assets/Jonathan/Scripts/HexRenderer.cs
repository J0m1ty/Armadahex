using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FogState {
    public static Color fogNormal = new Color(0.82f, 0.86f, 0.92f); // cool grey so the smoke texture doesn't read as cream
    public static Color fogHit = new Color(0.8f, 0.0f, 0.0f);
    public static Color fogScan = new Color(0.0f, 0.0f, 0.8f);
    public static Color fogMiss = new Color(1.0f, 1.0f, 1.0f);
    public static Color friendlySelected = new Color(0.94f, 0.38f, 0.0f);
}

public enum FogColor {
    Normal,
    Hit,
    Scan,
    Miss,
    Selected,
    Custom
}

// per hex cloud state, eased and drawn by the board's CloudFog
public class Fog {
    public FogColor baseColor = FogColor.Normal;
    public FogColor? overrideColor = null;
    public Color customColor = Color.white;
    public bool cleared;

    public HexRenderer hex { get; private set; }
    public Vector2Int axial { get; private set; }

    public float cover = 1;
    public Color glow = Color.clear;
    public float hover;
    public float hoverTarget;

    public Fog(HexRenderer hex, Vector2Int axial) {
        this.hex = hex;
        this.axial = axial;
    }

    public FogColor current {
        get { return overrideColor ?? baseColor; }
    }

    // how much the clouds still cover this hex, 0 is open water
    public float TargetCover() {
        if (cleared || !hex.gameObject.activeSelf) return 0;

        // an override can thin the clouds but never thicken them past what the hex already shows
        var cover = Cover(baseColor);
        return overrideColor.HasValue ? Mathf.Min(cover, Cover(overrideColor.Value)) : cover;
    }

    private float Cover(FogColor color) {
        switch (color) {
            case FogColor.Hit:
                return 0.15f;
            case FogColor.Scan:
                return 0.6f;
            case FogColor.Selected:
                return 0.6f;
            case FogColor.Custom:
                return 1 - 0.4f * CustomStrength();
            default:
                return 1;
        }
    }

    // light inside the clouds, hdr so it blooms
    public Color TargetGlow() {
        if (!hex.gameObject.activeSelf) return Color.clear;

        switch (current) {
            case FogColor.Hit:
                return FogState.fogHit * 3f;
            case FogColor.Scan:
                return FogState.fogScan * 2f;
            case FogColor.Miss:
                return FogState.fogMiss * 0.6f;
            case FogColor.Selected:
                return FogState.friendlySelected * 1.1f;
            case FogColor.Custom:
                return customColor * 1.1f * CustomStrength();
            default:
                return Color.clear;
        }
    }

    // attack previews lerp between selected and normal, so read back how far along they are
    private float CustomStrength() {
        var full = Vector4.Distance(FogState.friendlySelected, FogState.fogNormal);
        return Mathf.Clamp01(Vector4.Distance(customColor, FogState.fogNormal) / full);
    }
}

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
    public Fog fog;
    public FlameMaker fire;

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

    public void SetColor(Color color) {
        material.SetColor("_BaseColor", color);
    }

    public void SetFogColorSlow(FogColor color, bool isOverride = false) {
        if (fog == null || !gameObject.activeSelf) return;
        
        if (isOverride) {
            fog.overrideColor = color;
        }
        else {
            fog.baseColor = color;
        }
    }

    public void SetFogColorInstant(FogColor color, bool isOverride = false) {
        if (fog == null || !gameObject.activeSelf) return;
        
        SetFogColorSlow(color, isOverride);
        fog.glow = fog.TargetGlow();
    }

    public void SetCustomFogColor(Color color) {
        if (fog == null || !gameObject.activeSelf) return;
        
        fog.customColor = color;
        fog.overrideColor = FogColor.Custom;
        fog.glow = fog.TargetGlow();
    }

    public void ClearFog() {
        if (fog == null || !gameObject.activeSelf) return;

        fog.cleared = true;
    }

    public void RemoveFogOverrideColor() {
        if (fog == null || !gameObject.activeSelf) return;

        fog.overrideColor = null;
    }

    public void EnableFlames() {
        fire.EnableFlames();
    }

    public void DisableFlames() {
        fire.DisableFlames();
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