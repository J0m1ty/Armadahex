using System.Collections.Generic;
using UnityEngine;

// One raymarched cloud layer over a whole board, replacing the old particle per hex.
// Every hex owns a texel in a small state map (rgb = glow, a = cloud cover) that the
// shader finds by the hex's axial coordinate, so C# and the shader always agree.
public class CloudFog : MonoBehaviour
{
    const int MAP_SIZE = 32;
    const int MAP_OFFSET = 16;
    const float SQRT3 = 1.7320508f;

    [Header("Shape")]
    public float cloudBase = -2f;
    public float cloudTop = 44f;
    public float margin = 40f;

    [Header("Easing (seconds)")]
    public float coverTime = 0.35f;
    public float clearTime = 1.6f;
    public float glowTime = 0.15f;
    public float hoverInTime = 0.06f;
    public float hoverOutTime = 0.5f;

    [Header("Grid")]
    [Range(0, 1)]
    public float idleGrid = 0.2f;
    [Range(0, 1)]
    public float pickingGrid = 0.6f;
    public float gridTime = 0.4f;

    [Header("Hover")]
    // a softer take on the lock-in gold, so hovering never reads as a red hit
    public Color hoverGlow = new Color(1.2f, 0.6f, 0.12f);
    [Range(0, 1)]
    public float hoverCarve = 0.15f;

    private HexGrid grid;
    private Material material;
    private Transform box;

    private readonly List<Fog> fogs = new List<Fog>();
    private Texture2D map;
    private Color[] pixels;

    private static Texture3D noise;

    private readonly Vector4[] ripples = new Vector4[4];
    private int nextRipple;
    private float gridGlow;

    public void Init(HexGrid grid, Material fogMaterial) {
        this.grid = grid;

        map = new Texture2D(MAP_SIZE, MAP_SIZE, TextureFormat.RGBAHalf, false, true);
        map.name = "Cloud Map";
        map.filterMode = FilterMode.Point;
        map.wrapMode = TextureWrapMode.Clamp;
        pixels = new Color[MAP_SIZE * MAP_SIZE];

        // a plain cube is enough to cover the board, the shader does the rest
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(cube.GetComponent<Collider>());
        cube.name = "Clouds";
        cube.layer = 2; // ignore raycast, so it never eats the hover ray
        box = cube.transform;
        box.SetParent(transform, false);

        var meshRenderer = cube.GetComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        material = new Material(fogMaterial);
        material.name = "Cloud Fog";
        meshRenderer.sharedMaterial = material;

        material.SetTexture("_CloudNoise", GetNoise());
        material.SetTexture("_CloudMap", map);
    }

    public Fog Add(HexRenderer hex) {
        var local = hex.transform.position - transform.position;
        var fog = new Fog(hex, AxialFromLocal(local.x, local.z));
        fogs.Add(fog);
        return fog;
    }

    void Start() {
        // size the box once every hex exists
        float extent = 0;
        foreach (var fog in fogs) {
            var local = fog.hex.transform.position - transform.position;
            extent = Mathf.Max(extent, Mathf.Abs(local.x), Mathf.Abs(local.z));
        }
        extent += grid.size + margin;

        box.localPosition = new Vector3(0, (cloudBase + cloudTop) * 0.5f, 0);
        box.localScale = new Vector3(extent * 2, cloudTop - cloudBase, extent * 2);

        material.SetVector("_BoxMin", new Vector4(-extent, cloudBase, -extent, 0));
        material.SetVector("_BoxMax", new Vector4(extent, cloudTop, extent, 0));
        material.SetVector("_MapParams", new Vector4(MAP_SIZE, MAP_OFFSET, grid.size, 0));

        foreach (var fog in fogs) {
            fog.cover = fog.TargetCover();
            fog.glow = fog.TargetGlow();
        }
        WriteMap();
    }

    // a ring that rolls outward through the clouds from a hit
    public void Ripple(Vector3 worldPosition) {
        var local = worldPosition - transform.position;
        ripples[nextRipple] = new Vector4(local.x, local.z, Time.timeSinceLevelLoad, 1);
        nextRipple = (nextRipple + 1) % ripples.Length;
        material.SetVectorArray("_Ripples", ripples);
    }

    public void SetHover(HexRenderer hex) {
        foreach (var fog in fogs) {
            fog.hoverTarget = fog.hex == hex ? 1 : 0;
        }
    }

    void Update() {
        float dt = Time.deltaTime;
        foreach (var fog in fogs) {
            var coverTarget = fog.TargetCover();
            var time = coverTarget < fog.cover && fog.cleared ? clearTime : coverTime;
            fog.cover = Ease(fog.cover, coverTarget, time, dt);
            fog.glow = Color.Lerp(fog.glow, fog.TargetGlow(), 1 - Mathf.Exp(-dt / glowTime));
            fog.hover = Ease(fog.hover, fog.hoverTarget, fog.hoverTarget > fog.hover ? hoverInTime : hoverOutTime, dt);
        }
        WriteMap();
    }

    void LateUpdate() {
        material.SetVector("_GridOrigin", transform.position);

        // the hex grid shows faintly in the clouds, and lights up while you pick a target
        var picking = Selector.instance != null && Selector.instance.allowSelectingGrids && TurnManager.instance != null && TurnManager.instance.gameActive;
        gridGlow = Ease(gridGlow, picking ? pickingGrid : idleGrid, gridTime, Time.deltaTime);
        material.SetFloat("_GridGlow", gridGlow);

        // the shader parts the clouds softly around the visible hover column
        var border = Selector.instance != null ? Selector.instance.border : null;
        if (border != null && border.isVisible) {
            var local = border.transform.position - transform.position;
            material.SetVector("_Column", new Vector4(local.x, local.z, local.y + border.height, 1));
        }
        else {
            material.SetVector("_Column", Vector4.zero);
        }
    }

    private static float Ease(float value, float target, float time, float dt) {
        return Mathf.Lerp(value, target, 1 - Mathf.Exp(-dt / Mathf.Max(time, 0.001f)));
    }

    private void WriteMap() {
        System.Array.Clear(pixels, 0, pixels.Length);
        foreach (var fog in fogs) {
            int x = fog.axial.x + MAP_OFFSET;
            int y = fog.axial.y + MAP_OFFSET;
            if (x < 0 || y < 0 || x >= MAP_SIZE || y >= MAP_SIZE) continue;

            // hovering only half parts the clouds, locking in opens them all the way
            // (and the locked hex already glows, so the hover glow on top would blow it out)
            var locked = fog.current == FogColor.Selected;
            var cover = fog.cover * (1 - (locked ? 1f : hoverCarve) * fog.hover);
            var glow = fog.glow + hoverGlow * fog.hover * (locked ? 0.25f : 1f);
            pixels[x + y * MAP_SIZE] = new Color(glow.r, glow.g, glow.b, cover);
        }
        map.SetPixels(pixels);
        map.Apply(false);
    }

    // flat topped lattice with neighbours sqrt(3) * size apart, same maths as the shader
    private Vector2Int AxialFromLocal(float x, float z) {
        float q = (2f / 3f * x) / grid.size;
        float r = (-1f / 3f * x + SQRT3 / 3f * z) / grid.size;
        float s = -q - r;

        int rq = Mathf.RoundToInt(q);
        int rr = Mathf.RoundToInt(r);
        int rs = Mathf.RoundToInt(s);

        float dq = Mathf.Abs(rq - q);
        float dr = Mathf.Abs(rr - r);
        float ds = Mathf.Abs(rs - s);

        if (dq > dr && dq > ds) {
            rq = -rr - rs;
        }
        else if (dr > ds) {
            rr = -rq - rs;
        }
        return new Vector2Int(rq, rr);
    }

    // tiling 3d noise, built once and shared by every board
    // r = perlin-worley billows, g = worley detail, b = value noise for warping edges
    private static Texture3D GetNoise() {
        if (noise != null) return noise;

        const int n = 48;
        var colors = new Color32[n * n * n];
        for (int z = 0; z < n; z++) {
            for (int y = 0; y < n; y++) {
                for (int x = 0; x < n; x++) {
                    var p = new Vector3(x, y, z) / n;

                    float value = ValueFbm(p, 4, 3);
                    float worley = Worley(p, 4) * 0.625f + Worley(p, 8) * 0.25f + Worley(p, 16) * 0.125f;
                    float billow = Mathf.Clamp01((value - worley + 1f) / (2f - worley));
                    billow = Mathf.Clamp01(billow * 0.5f + worley * 0.6f);

                    float detail = Worley(p, 8) * 0.625f + Worley(p, 16) * 0.25f + Worley(p, 24) * 0.125f;
                    float warp = ValueFbm(p, 6, 2);

                    colors[x + y * n + z * n * n] = new Color(billow, detail, warp, 1);
                }
            }
        }

        noise = new Texture3D(n, n, n, TextureFormat.RGBA32, false);
        noise.name = "Cloud Noise";
        noise.wrapMode = TextureWrapMode.Repeat;
        noise.filterMode = FilterMode.Bilinear;
        noise.SetPixels32(colors);
        noise.Apply(false, true);
        return noise;
    }

    private static float Hash(int x, int y, int z, int seed) {
        unchecked {
            uint h = (uint)(x * 374761393 + y * 668265263 + z * 1274126177 + seed * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0xffffff) / (float)0xffffff;
        }
    }

    private static int Wrap(int i, int period) {
        return ((i % period) + period) % period;
    }

    private static float ValueNoise(Vector3 p, int period) {
        var f = p * period;
        int x0 = Mathf.FloorToInt(f.x), y0 = Mathf.FloorToInt(f.y), z0 = Mathf.FloorToInt(f.z);
        float tx = f.x - x0, ty = f.y - y0, tz = f.z - z0;
        tx = tx * tx * (3 - 2 * tx);
        ty = ty * ty * (3 - 2 * ty);
        tz = tz * tz * (3 - 2 * tz);

        float Corner(int dx, int dy, int dz) {
            return Hash(Wrap(x0 + dx, period), Wrap(y0 + dy, period), Wrap(z0 + dz, period), period);
        }

        float a = Mathf.Lerp(Corner(0, 0, 0), Corner(1, 0, 0), tx);
        float b = Mathf.Lerp(Corner(0, 1, 0), Corner(1, 1, 0), tx);
        float c = Mathf.Lerp(Corner(0, 0, 1), Corner(1, 0, 1), tx);
        float d = Mathf.Lerp(Corner(0, 1, 1), Corner(1, 1, 1), tx);
        return Mathf.Lerp(Mathf.Lerp(a, b, ty), Mathf.Lerp(c, d, ty), tz);
    }

    private static float ValueFbm(Vector3 p, int period, int octaves) {
        float sum = 0, amp = 0.5f, total = 0;
        for (int i = 0; i < octaves; i++) {
            sum += ValueNoise(p, period) * amp;
            total += amp;
            period *= 2;
            amp *= 0.5f;
        }
        return sum / total;
    }

    // inverted, so 1 sits on the feature points
    private static float Worley(Vector3 p, int cells) {
        var f = p * cells;
        int cx = Mathf.FloorToInt(f.x), cy = Mathf.FloorToInt(f.y), cz = Mathf.FloorToInt(f.z);
        float best = 10f;
        for (int dz = -1; dz <= 1; dz++) {
            for (int dy = -1; dy <= 1; dy++) {
                for (int dx = -1; dx <= 1; dx++) {
                    int x = cx + dx, y = cy + dy, z = cz + dz;
                    int wx = Wrap(x, cells), wy = Wrap(y, cells), wz = Wrap(z, cells);
                    var point = new Vector3(
                        x + Hash(wx, wy, wz, 11),
                        y + Hash(wx, wy, wz, 23),
                        z + Hash(wx, wy, wz, 37));
                    best = Mathf.Min(best, (point - f).sqrMagnitude);
                }
            }
        }
        return 1f - Mathf.Clamp01(Mathf.Sqrt(best));
    }
}
