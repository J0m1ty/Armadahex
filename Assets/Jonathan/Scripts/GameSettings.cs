using System;
using UnityEngine;
using UnityEngine.Rendering;

public enum GraphicsQuality {
    Full = 0,
    Reduced = 1
}

// the player's comfort and performance settings, picked on the options page and applied live
public static class GameSettings
{
    // a lighter copy of the URP asset: lower render scale, no msaa, smaller shadow map
    private const string reducedPipeline = "URP Asset Reduced";

    public static event Action changed;

    // cuts and fades instead of camera moves, no shake, a still intro
    public static bool reducedMotion {
        get => PlayerPrefs.GetInt(Constants.REDUCED_MOTION_PREF_KEY, 0) == 1;
        set {
            PlayerPrefs.SetInt(Constants.REDUCED_MOTION_PREF_KEY, value ? 1 : 0);
            changed?.Invoke();
        }
    }

    // webgl starts on reduced, it has the least to spare
    public static GraphicsQuality graphics {
        get => (GraphicsQuality)PlayerPrefs.GetInt(Constants.GRAPHICS_QUALITY_PREF_KEY, (int)defaultGraphics);
        set {
            PlayerPrefs.SetInt(Constants.GRAPHICS_QUALITY_PREF_KEY, (int)value);
            ApplyGraphics();
            changed?.Invoke();
        }
    }

    public static bool reducedGraphics => graphics == GraphicsQuality.Reduced;

    private static GraphicsQuality defaultGraphics => Application.platform == RuntimePlatform.WebGLPlayer ? GraphicsQuality.Reduced : GraphicsQuality.Full;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot() {
        ApplyGraphics();
    }

    // full is the project's own pipeline asset, reduced swaps in the lighter one for the current quality level
    private static void ApplyGraphics() {
        QualitySettings.renderPipeline = reducedGraphics ? Resources.Load<RenderPipelineAsset>(reducedPipeline) : null;
    }
}
