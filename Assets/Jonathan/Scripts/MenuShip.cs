using UnityEngine;

// a ship steaming along for the menu: the model of a game ship without its game scripts, bobbing on the swell,
// with smoke from its funnels, a wake behind it and spray at the bow
public class MenuShip : MonoBehaviour
{
    [SerializeField] private Material smokeMaterial;
    [SerializeField] private Material foamMaterial;

    [Header("Bobbing")]
    [SerializeField] private float bobHeight = 0.25f;
    [SerializeField] private float bobSpeed = 0.12f;
    [SerializeField] private float rockAngle = 1.2f;

    private Transform hull;
    private float speed;

    // the point the camera follows: on the water, not bobbing
    public Transform anchor => transform;

    // builds the ship from a scenery's settings
    public void Build(MenuBackdrop.Scenery scenery) {
        speed = scenery.speed;

        hull = new GameObject("Hull").transform;
        hull.SetParent(transform, false);

        // under an inactive parent nothing on the copy wakes up, so its game scripts can go before they run
        var holder = new GameObject("Holder");
        holder.SetActive(false);
        var model = Instantiate(scenery.ship, holder.transform);
        foreach (var behaviour in model.GetComponentsInChildren<MonoBehaviour>(true)) DestroyImmediate(behaviour);
        foreach (var collider in model.GetComponentsInChildren<Collider>(true)) DestroyImmediate(collider);
        foreach (var effect in model.GetComponentsInChildren<ParticleSystem>(true)) {
            if (effect != null) DestroyImmediate(effect.gameObject);
        }
        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true)) renderer.receiveShadows = false;
        model.transform.SetParent(hull, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        Destroy(holder);

        foreach (var stack in scenery.stacks) {
            Smoke(hull, stack, scenery.smokeSize);
        }

        var size = scenery.beam;
        Wake(transform, scenery.stern, size);
        BowWave(transform, scenery.bow, size, 1);
        BowWave(transform, scenery.bow, size, -1);
        Spray(hull, scenery.bow, size, 1);
        Spray(hull, scenery.bow, size, -1);
    }

    void Update() {
        transform.position += transform.forward * speed * Time.deltaTime;

        if (hull == null) return;
        var t = Time.time * bobSpeed * Mathf.PI * 2;
        hull.localPosition = Vector3.up * Mathf.Sin(t) * bobHeight;
        hull.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.7f) * rockAngle * 0.5f, 0f, Mathf.Sin(t * 0.9f + 1f) * rockAngle);
    }

    private ParticleSystem Emitter(string name, Transform parent, Vector3 at, Material material) {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        var system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = system.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;
        main.loop = true;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        return system;
    }

    private static Gradient Fade(float peak, float hold) {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(peak, 0.1f), new GradientAlphaKey(peak * 0.6f, hold), new GradientAlphaKey(0f, 1f) });
        return gradient;
    }

    // a column of smoke that the ship steams out from under
    private void Smoke(Transform parent, Vector3 at, float size) {
        var system = Emitter("Smoke", parent, at, smokeMaterial);
        system.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        var main = system.main;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(12f, 16f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(size * 0.5f, size * 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 1f, size * 1.6f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2);
        main.startColor = new Color(0.5f, 0.5f, 0.53f);
        main.maxParticles = 600;

        var emission = system.emission;
        emission.rateOverTime = 26f;

        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 6f;
        shape.radius = size * 0.2f;

        // it slows as it rises and drifts off with the wind
        var limit = system.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = size * 0.3f;
        limit.dampen = 0.05f;

        var force = system.forceOverLifetime;
        force.enabled = true;
        force.space = ParticleSystemSimulationSpace.World;
        force.x = size * 0.08f;
        force.y = size * 0.05f;
        force.z = 0f;

        var sizeOver = system.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.5f, 1f, 5f));

        var rotation = system.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);

        var color = system.colorOverLifetime;
        color.enabled = true;
        color.color = Fade(0.5f, 0.4f);

        system.Play();
    }

    // foam flat on the water behind the stern, spreading and fading as it falls behind
    private void Wake(Transform parent, Vector3 at, float size) {
        var system = Emitter("Wake", parent, at, foamMaterial);

        var main = system.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 12f);
        main.startSpeed = 0f;
        // seen from low over the water, foam is a wide flat streak
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(size * 1.4f, size * 2f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(size * 0.3f, size * 0.45f);
        main.maxParticles = 1000;

        var emission = system.emission;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 1.5f;

        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(size * 0.6f, 0f, size * 0.5f);

        var sizeOver = system.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.8f, 1f, 3.5f));

        var color = system.colorOverLifetime;
        color.enabled = true;
        color.color = Fade(0.9f, 0.3f);

        system.Play();
    }

    // the waves the bow pushes aside, rolling outward into a V
    private void BowWave(Transform parent, Vector3 at, float size, float side) {
        var system = Emitter("Bow Wave", parent, at + Vector3.right * side * size * 0.4f, foamMaterial);
        system.transform.localRotation = Quaternion.Euler(0f, side * 70f, 0f);

        var main = system.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(size * 0.25f, size * 0.35f);
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(size * 0.6f, size * 1f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(size * 0.15f, size * 0.25f);
        main.maxParticles = 800;

        var emission = system.emission;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 1.2f;

        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 4f;
        shape.radius = size * 0.1f;

        var sizeOver = system.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.7f, 1f, 2.5f));

        var color = system.colorOverLifetime;
        color.enabled = true;
        color.color = Fade(0.85f, 0.3f);

        system.Play();
    }

    // water thrown up where the bow meets the swell, it rides up and down with the hull
    private void Spray(Transform parent, Vector3 at, float size, float side) {
        var system = Emitter("Spray", parent, at + Vector3.right * side * size * 0.25f, foamMaterial);
        system.transform.localRotation = Quaternion.Euler(-50f, side * 60f, 0f);

        var main = system.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(size * 0.4f, size * 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.15f, size * 0.4f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2);
        main.gravityModifier = size * 0.12f;
        main.maxParticles = 200;

        var emission = system.emission;
        emission.rateOverTime = 10f;
        // and more in bursts, once a swell, as the bow dips
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, new ParticleSystem.MinMaxCurve(8, 16), 0, 1f / bobSpeed) });

        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = size * 0.15f;

        var sizeOver = system.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.8f));

        var color = system.colorOverLifetime;
        color.enabled = true;
        color.color = Fade(0.8f, 0.3f);

        system.Play();
    }
}
