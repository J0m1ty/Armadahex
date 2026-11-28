using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// a preset over resolution scale and detail; custom is any other mix of the two
public enum GraphicsQuality {
    High = 0,
    // full resolution, lighter clouds and water
    Medium = 1,
    // medium at a lower resolution
    Low = 2,
    Custom = 3
}

// the player's display and comfort settings, picked on the options page and applied live
public static class GameSettings
{
    private const float lowRenderScale = 0.75f;
    private const int lowShadowResolution = 1024;

    public static readonly int[] antiAliasingSamples = { 1, 2, 4 };

    public static event Action changed;

    // a copy of the project's pipeline asset made at start, the settings change it so the asset file never does
    private static UniversalRenderPipelineAsset pipeline;
    private static int fullShadowResolution;
    private static bool projectVSync = true;

    // cuts and fades instead of camera moves, no shake, a still intro
    public static bool reducedMotion {
        get => GetBool(Constants.REDUCED_MOTION_PREF_KEY, false);
        set => Set(Constants.REDUCED_MOTION_PREF_KEY, value);
    }

    // off draws at a lower resolution and scales it up; webgl starts with it off
    public static bool fullResolution {
        get => GetBool(Constants.FULL_RESOLUTION_PREF_KEY, !webgl);
        set => Set(Constants.FULL_RESOLUTION_PREF_KEY, value);
    }

    // off has lighter clouds, water and shadows; webgl starts with it off
    public static bool highDetail {
        get => GetBool(Constants.HIGH_DETAIL_PREF_KEY, !webgl);
        set => Set(Constants.HIGH_DETAIL_PREF_KEY, value);
    }

    // msaa samples, 1 is off; webgl starts with it off
    public static int antiAliasing {
        get {
            var samples = PlayerPrefs.GetInt(Constants.ANTI_ALIASING_PREF_KEY, webgl ? 1 : 4);
            return Array.IndexOf(antiAliasingSamples, samples) >= 0 ? samples : 4;
        }
        set {
            PlayerPrefs.SetInt(Constants.ANTI_ALIASING_PREF_KEY, value);
            Changed();
        }
    }

    // starts as the quality level has it; the browser always syncs on webgl
    public static bool vSync {
        get => GetBool(Constants.VSYNC_PREF_KEY, projectVSync);
        set => Set(Constants.VSYNC_PREF_KEY, value);
    }

    public static GraphicsQuality quality {
        get {
            if (fullResolution && highDetail) return GraphicsQuality.High;
            if (fullResolution) return GraphicsQuality.Medium;
            if (!highDetail) return GraphicsQuality.Low;
            return GraphicsQuality.Custom;
        }
        // picking custom keeps whatever is set
        set {
            if (value == GraphicsQuality.Custom) return;
            PlayerPrefs.SetInt(Constants.FULL_RESOLUTION_PREF_KEY, value != GraphicsQuality.Low ? 1 : 0);
            PlayerPrefs.SetInt(Constants.HIGH_DETAIL_PREF_KEY, value == GraphicsQuality.High ? 1 : 0);
            Changed();
        }
    }

    // the clouds and water read this
    public static bool lighterEffects => !highDetail;

    private static bool webgl => Application.platform == RuntimePlatform.WebGLPlayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot() {
        projectVSync = QualitySettings.vSyncCount > 0;

        var project = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if (project != null) {
            pipeline = UnityEngine.Object.Instantiate(project);
            pipeline.name = project.name + " (settings)";
            fullShadowResolution = project.mainLightShadowmapResolution;
            QualitySettings.renderPipeline = pipeline;
        }

        Apply();
    }

    private static bool GetBool(string key, bool fallback) {
        return PlayerPrefs.GetInt(key, fallback ? 1 : 0) == 1;
    }

    private static void Set(string key, bool value) {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        Changed();
    }

    private static void Changed() {
        Apply();
        changed?.Invoke();
    }

    private static void Apply() {
        QualitySettings.vSyncCount = vSync ? 1 : 0;

        if (pipeline == null) return;
        pipeline.renderScale = fullResolution ? 1f : lowRenderScale;
        pipeline.mainLightShadowmapResolution = highDetail ? fullShadowResolution : lowShadowResolution;
        pipeline.msaaSampleCount = antiAliasing;
    }
}
