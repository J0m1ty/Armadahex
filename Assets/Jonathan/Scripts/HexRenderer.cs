using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public static class FogState {
    public static Color fogNormal = new Color(0.9f, 0.9f, 0.9f);
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

public class FogOptions {
    public FogColor? setBaseColor = null;
    public FogColor? setOverrideColor = null;
    public Color? setCustomColor = null;
    public bool instant = false;
    public bool modifyStart = false;
    public float? addVelocity = null;
    public int? lifetime = null;
    public int? rate = null;
    public float? simulationSpeed = null;
}

public class Fog {
    private FogColor _baseColor;
    private FogColor? _overrideColor;
    public FogColor baseColor {
        get { return _baseColor; }
        set {
            _baseColor = value;
            UpdateSystem();
        }
    }
    public FogColor? overrideColor {
        get { return _overrideColor; }
        set {
            _overrideColor = value;
            UpdateSystem();
        }
    }

    public Color customColor = Color.white;
    
    public ParticleSystem ps { get; private set; }

    public Fog(ParticleSystem ps) {
        this.ps = ps;
        baseColor = FogColor.Normal;
        overrideColor = null;
    }

    public Color GetColor(FogColor color) {
        switch (color) {
            case FogColor.Normal:
                return FogState.fogNormal;
            case FogColor.Hit:
                return FogState.fogHit;
            case FogColor.Scan: 
                return FogState.fogScan;
            case FogColor.Miss:
                return FogState.fogMiss;
            case FogColor.Selected: 
                return FogState.friendlySelected;
            case FogColor.Custom:
                return customColor;
            default:
                return Color.white;
        }
    }

    public void UpdateSystem(FogOptions options = null) {
        if (options == null) {
            options = new FogOptions();
        }

        if (options.setBaseColor != null) {
            _baseColor = (FogColor)options.setBaseColor;
        }

        if (options.setOverrideColor != null) {
            _overrideColor = (FogColor)options.setOverrideColor;
        }

        if (options.setCustomColor != null) {
            customColor = (Color)options.setCustomColor;
        }

        Color color = overrideColor != null ? GetColor((FogColor)overrideColor) : GetColor(baseColor);

        if (options.instant) {
            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[ps.particleCount];
            int num = ps.GetParticles(particles);
            for (int i = 0; i < num; i++) {
                #pragma warning disable 618
                particles[i].color = color;
                #pragma warning restore 618

                if (options.addVelocity != null) {
                    // get vector from center to particle position, and update velocity away from center
                    Vector3 dir = particles[i].position - ps.transform.position;
                    particles[i].velocity = dir.normalized * (float)options.addVelocity;
                }

                if (options.lifetime != null) {
                    particles[i].remainingLifetime = (int)options.lifetime;
                }
            }
            ps.SetParticles(particles, num);
        }

        if (options.modifyStart) {
            var main = ps.main;
            main.startColor = color;
        }

        if (options.lifetime != null) {
            var main = ps.main;
            main.startLifetime = (int)options.lifetime;
        }

        if (options.rate != null) {
            var emission = ps.emission;
            emission.rateOverTime = (int)options.rate;
        }

        if (options.simulationSpeed != null) {
            var main = ps.main;
            main.simulationSpeed = (float)options.simulationSpeed;
        }
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

    public void LoadFog(ParticleSystem fogEffect) {
        if (fog != null) return;
        fog = new Fog(fogEffect);
    }

    public void SetFogColorSlow(FogColor color, bool isOverride = false) {
        if (fog == null || !gameObject.activeSelf) return;
        
        fog.UpdateSystem(new FogOptions {
            setBaseColor = isOverride ? null : color,
            setOverrideColor = isOverride ? color : null,
            instant = false,
            modifyStart = true
        });
    }

    public void SetFogColorInstant(FogColor color, bool isOverride = false) {
        if (fog == null || !gameObject.activeSelf) return;
        
        fog.UpdateSystem(new FogOptions {
            setBaseColor = isOverride ? null : color,
            setOverrideColor = isOverride ? color : null,
            instant = true,
            modifyStart = true
        });
    }

    public void SetCustomFogColor(Color color) {
        if (fog == null || !gameObject.activeSelf) return;
        
        fog.UpdateSystem(new FogOptions {
            setCustomColor = color,
            setOverrideColor = FogColor.Custom,
            instant = true,
            modifyStart = true
        });
    }

    public void ClearFog() {
        if (fog == null || !gameObject.activeSelf) return;

        fog.UpdateSystem(new FogOptions {
            rate = 0,
            simulationSpeed = 10
        });
        
        StartCoroutine(TurnOffFog((fog.ps.main.startLifetime.constant * 5f) / fog.ps.main.simulationSpeed));
    }

    public void RemoveFogOverrideColor() {
        if (fog == null || !gameObject.activeSelf) return;

        fog.overrideColor = null;
        
        fog.UpdateSystem(new FogOptions {
            instant = true,
            modifyStart = true
        });
    }
    
    public IEnumerator TurnOffFog(float delay) {
        yield return new WaitForSeconds(delay);
        fog.ps.gameObject.SetActive(false);
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