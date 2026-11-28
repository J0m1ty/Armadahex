using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Hit and miss feedback built from glowing HexBorder walls, so it shares the hover column's additive look.
// Enemy board: pegs left in the clouds (tall red for a hit, short white for a miss, like battleship pegs)
// until a sunk ship is revealed, which then glows red all over.
// Your board: miss pegs too, but hits just make the hull glow red over the hit hex, since you can see your ships.
// Both boards get a splash per hex and one shockwave ring per volley.
public class ShotEffects : MonoBehaviour
{
    private static ShotEffects instance;

    public static Color hitColor = new Color(2.5f, 0.3f, 0.12f);
    public static Color hitWaveColor = new Color(1f, 0.45f, 0.12f);
    public static Color missColor = new Color(0.75f, 0.85f, 1f);

    // shots in one volley land in the same frame, so only the first gets a shockwave
    private const float VOLLEY_TIME = 0.3f;
    private static float lastWave = -1;

    // a hex keeps one peg: a hit stays, a miss and a sonar contact replace each other, so the latest one shows
    private enum PegKind { Contact, Miss, Hit }

    private readonly Dictionary<HexRenderer, GameObject> pegs = new Dictionary<HexRenderer, GameObject>();
    private readonly Dictionary<HexRenderer, PegKind> pegKinds = new Dictionary<HexRenderer, PegKind>();
    private readonly Dictionary<Ship, Material> glows = new Dictionary<Ship, Material>();
    private readonly Dictionary<Ship, List<Vector4>> glowPoints = new Dictionary<Ship, List<Vector4>>();

    private static ShotEffects Get() {
        if (instance == null) {
            instance = new GameObject("Shot Effects").AddComponent<ShotEffects>();
        }
        return instance;
    }

    public static void Shot(HexRenderer hex, bool hit) {
        if (hex == null || Selector.instance == null) return;
        if (CameraManager.instance != null) CameraManager.instance.NoteShot(hex.transform.position);
        var effects = Get();
        var size = hex.size;
        var fogged = hex.fog != null;

        var wave = Time.time - lastWave > VOLLEY_TIME;
        if (wave) {
            lastWave = Time.time;
        }

        if (hit) {
            if (fogged) {
                effects.Peg(hex, PegKind.Hit, hitColor, size * 0.24f, 62f, 0.45f);
            }
            else if (hex.gridRef.shipSegment != null) {
                effects.GlowHex(hex.gridRef.shipSegment.parent, hex.transform.position);
            }

            effects.Spawn(hex, hitWaveColor, size * 0.3f, size * 0.7f, 90f, 10f, 0.6f, true);
            if (wave) {
                effects.Spawn(hex, hitWaveColor, size * 0.5f, size * 3.2f, 14f, 2f, 0.9f, true);
            }

            // your own ships already explode from ShipSegment, so only blow up the fogged board here
            if (fogged) {
                if (ExplosionCreator.instance != null) {
                    // up at the cloud tops, so it bursts out of the fog
                    ExplosionCreator.instance.CreateExplosion(hex.transform.position + Vector3.up * 40f);
                }
                hex.hexMap.clouds.Ripple(hex.transform.position);
            }
        }
        else {
            effects.Peg(hex, PegKind.Miss, missColor, size * 0.16f, 24f, 0.35f);

            effects.Spawn(hex, missColor, size * 0.25f, size * 0.45f, 40f, 6f, 0.6f, true);
            if (wave) {
                effects.Spawn(hex, missColor, size * 0.4f, size * 2f, 6f, 1f, 0.8f, true);
            }
        }
    }

    public static Color contactColor = new Color(0.3f, 0.6f, 2.5f);

    // one of your ships went down: the hit glow covers the whole hull instead of the hit hexes
    public static void Sunk(Ship ship) {
        Get().GetGlow(ship).SetFloat("_GlowAll", 1);
    }

    // a sonar contact: a blue peg in the middle of the sweep, nothing was hit
    public static void Contact(HexRenderer hex) {
        if (hex == null || Selector.instance == null) return;
        var effects = Get();
        var size = hex.size;

        effects.Peg(hex, PegKind.Contact, contactColor, size * 0.2f, 40f, 0.4f);
        effects.Spawn(hex, contactColor, size * 0.5f, size * 3f, 10f, 2f, 0.9f, true);
    }

    // an enemy ship that just sank shows itself: drop its pegs and light the whole hull
    public static void Revealed(Ship ship) {
        var effects = Get();
        foreach (var segment in ship.segments) {
            var hex = segment.gridRef != null ? segment.gridRef.hexRenderer : null;
            if (hex != null && effects.pegs.TryGetValue(hex, out var peg)) {
                if (peg != null) Destroy(peg);
                effects.pegs.Remove(hex);
                effects.pegKinds.Remove(hex);
            }
        }
        effects.GetGlow(ship).SetFloat("_GlowAll", 1);
    }

    // takes away the peg it replaces; nothing replaces a hit, and the same peg again stays as it is
    private void Peg(HexRenderer hex, PegKind kind, Color color, float size, float height, float duration) {
        if (pegKinds.TryGetValue(hex, out var current) && (current == PegKind.Hit || current == kind)) return;

        if (pegs.TryGetValue(hex, out var old) && old != null) Destroy(old);
        pegs[hex] = Spawn(hex, color, size, size, 0f, height, duration, false);
        pegKinds[hex] = kind;
    }

    private void GlowHex(Ship ship, Vector3 position) {
        var material = GetGlow(ship);
        var points = glowPoints[ship];
        if (points.Count >= 8) return;

        points.Add(new Vector4(position.x, position.y, position.z, 1));
        var array = new Vector4[8];
        points.CopyTo(array);
        material.SetVectorArray("_HitPoints", array);
    }

    // copies every mesh in the ship with the glow material, so the glow follows the bob and the sink
    private Material GetGlow(Ship ship) {
        if (glows.TryGetValue(ship, out var existing)) return existing;

        var material = new Material(Resources.Load<Material>("Ship Hit Glow"));
        var hexSize = ship.segments.Length > 0 && ship.segments[0].gridRef != null ? ship.segments[0].gridRef.hexRenderer.size : 25f;
        material.SetFloat("_HexSize", hexSize);
        material.SetVectorArray("_HitPoints", new Vector4[8]);
        glows[ship] = material;
        glowPoints[ship] = new List<Vector4>();

        ShipAura.CopyMeshes(ship, material, "Hit Glow");
        return material;
    }

    // grows from (startSize, startHeight) to (endSize, endHeight); one-shots fade out and go away
    private GameObject Spawn(HexRenderer hex, Color color, float startSize, float endSize, float startHeight, float endHeight, float duration, bool oneShot) {
        var obj = new GameObject(oneShot ? "Shot Splash" : "Shot Peg", typeof(HexBorder));
        obj.layer = 2; // ignore raycast, so it never blocks hex hover
        obj.transform.SetParent(hex.transform, false);

        var border = obj.GetComponent<HexBorder>();
        border.isFlatTopped = hex.isFlatTopped;
        // pegs get a glowing base at the water line, so they still show from straight above
        border.capped = !oneShot;
        border.capHeight = 0.4f - hex.transform.position.y;
        var material = new Material(Selector.instance.border.borderMaterial);
        material.name = "Shot Effect";
        // pegs use the lock-in beam look, splashes the short hover look
        material.SetFloat("_Scale", oneShot ? 0.04f : 0.2f);
        border.SetMaterial(material);
        border.SetVisibility(true);

        StartCoroutine(Animate(border, color, startSize, endSize, startHeight, endHeight, duration, oneShot));
        return obj;
    }

    private IEnumerator Animate(HexBorder border, Color color, float startSize, float endSize, float startHeight, float endHeight, float duration, bool oneShot) {
        float t = 0;
        while (t < 1) {
            if (border == null) yield break;
            t = Mathf.Clamp01(t + Time.deltaTime / duration);
            float ease = 1 - Mathf.Pow(1 - t, 3);

            border.GenerateMesh(Mathf.Lerp(startHeight, endHeight, ease), Mathf.Lerp(startSize, endSize, ease), border.isFlatTopped);
            border.SetColor(oneShot ? color * Mathf.Pow(1 - t, 2) * 2f : color);
            yield return null;
        }

        if (oneShot && border != null) {
            Destroy(border.gameObject);
        }
    }
}
