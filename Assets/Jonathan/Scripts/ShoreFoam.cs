using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// bakes how close the sea is to each pillar into a map the water shader draws shoreline foam from
// depth based foam barely shows against straight pillar walls, so this works from above instead
public class ShoreFoam : MonoBehaviour
{
    [Header("Bake")]
    // foam reaches out this many times the pillar's size, up to the max width
    [SerializeField]
    private float widthScale = 1.2f;
    [SerializeField]
    private float width = 30f;
    [SerializeField]
    private float texelSize = 1.5f;
    [SerializeField]
    private int maxResolution = 4096;

    [Header("Look")]
    [SerializeField]
    [Range(0f, 1f)]
    private float tintAmount = 0.85f;
    [SerializeField]
    private float intensity = 0.6f;
    [SerializeField]
    private float bandCount = 2f;
    [SerializeField]
    private float bandSpeed = 0.6f;
    [SerializeField]
    [Range(0f, 1f)]
    private float edgeWidth = 0.3f;

    private struct Pillar {
        public Vector2 center;
        public float radius;
        public Color color;
    }

    private static readonly List<Pillar> pillars = new List<Pillar>();
    private static bool dirty;

    private static readonly int mapId = Shader.PropertyToID("_ShoreFoamMap");
    private static readonly int rectId = Shader.PropertyToID("_ShoreFoamRect");
    private static readonly int paramsId = Shader.PropertyToID("_ShoreFoamParams");

    private Texture2D map;

    // a flat sided hex column poking out of the water
    public static void Add(Vector3 center, float radius, Color color) {
        pillars.Add(new Pillar { center = new Vector2(center.x, center.z), radius = radius, color = color });
        dirty = true;
    }

    void Awake() {
        pillars.Clear();
        dirty = false;
        Shader.SetGlobalVector(rectId, Vector4.zero);
    }

    void OnDestroy() {
        pillars.Clear();
        if (map != null) Destroy(map);
    }

    void LateUpdate() {
        Shader.SetGlobalVector(paramsId, new Vector4(intensity, bandCount, bandSpeed, edgeWidth));

        if (!dirty) return;
        dirty = false;
        Bake();
    }

    private float Reach(Pillar p) {
        return Mathf.Min(width, p.radius * widthScale);
    }

    private void Bake() {
        if (pillars.Count == 0) {
            Shader.SetGlobalVector(rectId, Vector4.zero);
            return;
        }

        var min = new Vector2(float.MaxValue, float.MaxValue);
        var max = new Vector2(float.MinValue, float.MinValue);
        foreach (var p in pillars) {
            var reach = Vector2.one * (p.radius + Reach(p));
            min = Vector2.Min(min, p.center - reach);
            max = Vector2.Max(max, p.center + reach);
        }

        // one empty texel around the edge so clamped samples outside read as no foam
        var texel = texelSize;
        int w, h;
        while (true) {
            w = Mathf.CeilToInt((max.x - min.x) / texel) + 2;
            h = Mathf.CeilToInt((max.y - min.y) / texel) + 2;
            if (w <= maxResolution && h <= maxResolution) break;
            texel *= 1.25f;
        }
        min -= Vector2.one * texel;

        var foam = new float[w * h];
        var pixels = new Color32[w * h];

        foreach (var p in pillars) {
            var foamWidth = Reach(p);
            var reach = p.radius + foamWidth;
            var x0 = Mathf.Max(0, Mathf.FloorToInt((p.center.x - reach - min.x) / texel));
            var x1 = Mathf.Min(w - 1, Mathf.CeilToInt((p.center.x + reach - min.x) / texel));
            var y0 = Mathf.Max(0, Mathf.FloorToInt((p.center.y - reach - min.y) / texel));
            var y1 = Mathf.Min(h - 1, Mathf.CeilToInt((p.center.y + reach - min.y) / texel));
            var color = (Color32)Color.Lerp(Color.white, p.color, tintAmount);

            for (var y = y0; y <= y1; y++) {
                for (var x = x0; x <= x1; x++) {
                    var dx = Mathf.Abs(min.x + (x + 0.5f) * texel - p.center.x);
                    var dy = Mathf.Abs(min.y + (y + 0.5f) * texel - p.center.y);

                    // distance to a hex with corners on the x axis
                    var d = Mathf.Max(dy, dx * 0.8660254f + dy * 0.5f) - p.radius * 0.8660254f;
                    if (d > foamWidth) continue;

                    var f = 1f - Mathf.Max(d, 0f) / foamWidth;
                    var i = y * w + x;
                    if (f <= foam[i]) continue;

                    foam[i] = f;
                    color.a = (byte)(f * 255f);
                    pixels[i] = color;
                }
            }
        }

        if (map != null) Destroy(map);
        map = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
        map.name = "Shore Foam";
        map.wrapMode = TextureWrapMode.Clamp;
        map.filterMode = FilterMode.Bilinear;
        map.SetPixels32(pixels);
        map.Apply(false, true);

        Shader.SetGlobalTexture(mapId, map);
        Shader.SetGlobalVector(rectId, new Vector4(min.x, min.y, 1f / (w * texel), 1f / (h * texel)));
    }
}
