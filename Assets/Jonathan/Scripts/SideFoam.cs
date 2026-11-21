using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// tints the shoreline foam with the side color of whichever board the camera is over, neutral in between
[RequireComponent(typeof(MeshRenderer))]
public class SideFoam : MonoBehaviour
{
    [SerializeField]
    private TeamManager teamManager;

    [SerializeField]
    private float claimedRadius = 350f;

    [SerializeField]
    private float fadeDistance = 300f;

    [SerializeField]
    [Range(0f, 1f)]
    private float tintAmount = 0.7f;

    private static readonly int foamColorId = Shader.PropertyToID("_edgeFalloffColor");

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock block;
    private Color neutralColor;

    void Start() {
        meshRenderer = GetComponent<MeshRenderer>();
        block = new MaterialPropertyBlock();
        neutralColor = meshRenderer.sharedMaterial.GetColor(foamColorId);
    }

    void LateUpdate() {
        if (teamManager == null || Camera.main == null) return;

        var cam = Camera.main.transform.position;
        cam.y = 0;

        var color = neutralColor;
        var closest = float.MaxValue;

        foreach (var team in teamManager.teams) {
            if (team.teamBase == null) continue;

            var basePos = team.teamBase.transform.position;
            basePos.y = 0;

            var dist = Vector3.Distance(cam, basePos);
            if (dist >= closest) continue;
            closest = dist;

            var claimed = 1f - Mathf.Clamp01((dist - claimedRadius) / fadeDistance);
            var sideColor = Color.Lerp(neutralColor, TeamColor.GetSideColor(team), tintAmount);
            color = Color.Lerp(neutralColor, sideColor, claimed);
        }

        meshRenderer.GetPropertyBlock(block);
        block.SetColor(foamColorId, color);
        meshRenderer.SetPropertyBlock(block);
    }
}
