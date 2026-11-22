using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// shifts the sky and the horizon haze toward the color of whichever side the camera has been over
[RequireComponent(typeof(TeamManager))]
public class SideSky : MonoBehaviour
{
    [SerializeField]
    private Color friendlyTop = new Color(0.13f, 0.33f, 0.58f);
    [SerializeField]
    private Color enemyTop = new Color(0.42f, 0.14f, 0.18f);
    [SerializeField]
    private Color friendlyHorizon = new Color(1f, 0.72f, 0.4f);
    [SerializeField]
    private Color enemyHorizon = new Color(1f, 0.38f, 0.22f);
    // the haze along the water and the bottom of the sky share one color so they read as one band
    [SerializeField]
    private Color friendlyHaze = new Color(0.42f, 0.6f, 0.86f);
    [SerializeField]
    private Color enemyHaze = new Color(0.9f, 0.48f, 0.42f);

    // the part of the way between the bases the blend happens over
    [SerializeField]
    private float blendStart = 0.3f;
    [SerializeField]
    private float blendEnd = 0.7f;
    // the sky drifts toward the color of where the camera is, so quick moves don't snap it
    [SerializeField]
    private float settleTime = 6f;

    private static readonly int topId = Shader.PropertyToID("_SkyGradientTop");
    private static readonly int horizonId = Shader.PropertyToID("_HorizonLineColor");
    private static readonly int bottomId = Shader.PropertyToID("_SkyGradientBottom");

    private TeamManager teamManager;
    private Material sky;
    private float blend = -1f;

    void Start() {
        teamManager = GetComponent<TeamManager>();

        // work on a copy so play never edits the sky asset
        if (RenderSettings.skybox != null) {
            sky = new Material(RenderSettings.skybox);
            RenderSettings.skybox = sky;
        }
    }

    void OnDestroy() {
        if (sky != null) Destroy(sky);
    }

    void LateUpdate() {
        if (sky == null || Camera.main == null) return;

        Vector3? friendly = null;
        Vector3? enemy = null;
        foreach (var team in teamManager.teams) {
            if (team.teamBase == null) continue;
            if (team.isPlayer) friendly = team.teamBase.transform.position;
            else enemy = team.teamBase.transform.position;
        }
        if (friendly == null || enemy == null) return;

        var a = friendly.Value;
        var axis = enemy.Value - a;
        axis.y = 0;

        var cam = Camera.main.transform.position - a;
        cam.y = 0;

        var along = Vector3.Dot(cam, axis) / axis.sqrMagnitude;
        var target = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(blendStart, blendEnd, along));
        blend = blend < 0f ? target : Mathf.Lerp(blend, target, 1f - Mathf.Exp(-Time.deltaTime / settleTime));
        var t = blend;

        sky.SetColor(topId, Color.Lerp(friendlyTop, enemyTop, t));
        sky.SetColor(horizonId, Color.Lerp(friendlyHorizon, enemyHorizon, t));

        var haze = Color.Lerp(friendlyHaze, enemyHaze, t);
        sky.SetColor(bottomId, haze);
        RenderSettings.fogColor = haze;
    }
}
