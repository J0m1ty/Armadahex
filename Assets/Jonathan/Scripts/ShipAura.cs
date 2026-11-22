using UnityEngine;

// Overlays a ship with extra copies of its meshes in another material.
// Used for your fleet's blue aura (Resources/Ship Aura.mat, shared, so it tunes live in the Inspector)
// and for the red hit glow in ShotEffects.
public static class ShipAura
{
    public static void Apply(Ship ship) {
        var material = Resources.Load<Material>("Ship Aura");
        if (material != null) {
            CopyMeshes(ship, material, "Aura");
        }
    }

    // the copies sit under each mesh, so they follow the bob and the sink
    public static void CopyMeshes(Ship ship, Material material, string name) {
        foreach (var filter in ship.internals.shipMesh.GetComponentsInChildren<MeshFilter>(true)) {
            if (filter.sharedMesh == null || filter.GetComponent<MeshRenderer>() == null) continue;

            var copy = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            copy.layer = 2; // not the ship or outline layer, so the outline and selection ignore it
            copy.transform.SetParent(filter.transform, false);
            copy.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;

            var copyRenderer = copy.GetComponent<MeshRenderer>();
            copyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            copyRenderer.receiveShadows = false;
            var materials = new Material[Mathf.Max(1, filter.sharedMesh.subMeshCount)];
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            copyRenderer.sharedMaterials = materials;
        }
    }
}
