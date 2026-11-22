using System.Collections;
using UnityEngine;

// Hit and miss feedback built from glowing HexBorder walls, so it shares the hover column's additive look:
// a peg left on the hex (tall red for a hit, short white for a miss, like battleship pegs),
// plus a one-off splash column and shockwave ring on the water.
public class ShotEffects : MonoBehaviour
{
    private static ShotEffects instance;

    public static Color hitColor = new Color(2.5f, 0.3f, 0.12f);
    public static Color hitWaveColor = new Color(1f, 0.45f, 0.12f);
    public static Color missColor = new Color(0.75f, 0.85f, 1f);

    private static ShotEffects Get() {
        if (instance == null) {
            instance = new GameObject("Shot Effects").AddComponent<ShotEffects>();
        }
        return instance;
    }

    public static void Shot(HexRenderer hex, bool hit) {
        if (hex == null || Selector.instance == null) return;
        var effects = Get();
        var size = hex.size;

        if (hit) {
            // peg, ring that stays on the water, and the blast
            effects.Spawn(hex, hitColor, size * 0.24f, size * 0.24f, 0f, 62f, 0.45f, false);
            effects.Spawn(hex, hitColor, size * 0.8f, size * 0.8f, 0f, 7f, 0.3f, false);
            effects.Spawn(hex, hitWaveColor, size * 0.5f, size * 3.2f, 14f, 2f, 0.9f, true);
            effects.Spawn(hex, hitWaveColor, size * 0.3f, size * 0.7f, 90f, 10f, 0.6f, true);

            // your own ships already explode from ShipSegment, so only blow up the fogged board here
            if (hex.fog != null) {
                if (ExplosionCreator.instance != null) {
                    // up at the cloud tops, so it bursts out of the fog
                    ExplosionCreator.instance.CreateExplosion(hex.transform.position + Vector3.up * 40f);
                }
                hex.hexMap.clouds.Ripple(hex.transform.position);
            }
        }
        else {
            effects.Spawn(hex, missColor, size * 0.16f, size * 0.16f, 0f, 24f, 0.35f, false);
            effects.Spawn(hex, missColor, size * 0.25f, size * 0.45f, 40f, 6f, 0.6f, true);
            effects.Spawn(hex, missColor, size * 0.4f, size * 2f, 6f, 1f, 0.8f, true);
        }
    }

    // grows from (startSize, startHeight) to (endSize, endHeight); one-shots fade out and go away
    private void Spawn(HexRenderer hex, Color color, float startSize, float endSize, float startHeight, float endHeight, float duration, bool oneShot) {
        var obj = new GameObject(oneShot ? "Shot Splash" : "Shot Marker", typeof(HexBorder));
        obj.layer = 2; // ignore raycast, so it never blocks hex hover
        obj.transform.SetParent(hex.transform, false);

        var border = obj.GetComponent<HexBorder>();
        border.isFlatTopped = hex.isFlatTopped;
        var material = new Material(Selector.instance.border.borderMaterial);
        material.name = "Shot Effect";
        // pegs use the lock-in beam look, splashes the short hover look
        material.SetFloat("_Scale", oneShot ? 0.04f : 0.2f);
        border.SetMaterial(material);
        border.SetVisibility(true);

        StartCoroutine(Animate(border, color, startSize, endSize, startHeight, endHeight, duration, oneShot));
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
