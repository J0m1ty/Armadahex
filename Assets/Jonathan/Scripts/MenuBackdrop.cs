using System;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

// the home page's background: a ship steaming across one of a few seas, picked at random, with the camera alongside.
// the camera draws into a texture the page shows, and a blurred copy of that is the glass behind the controls
public class MenuBackdrop : MonoBehaviour
{
    [Serializable]
    public class Scenery {
        public string name;
        public Material skybox;
        public Vector3 sunAngle = new Vector3(12f, 250f, 0f);
        public Color sunColor = Color.white;
        public float sunIntensity = 1f;
        public Color fogColor = new Color(0.62f, 0.7f, 0.74f);
        // the ship's heading; the waves run one way, so it changes the look
        public float heading;

        [Header("Ship")]
        public GameObject ship;
        public float speed = 4f;
        // from the ship's root: each funnel's top, the bow and stern at the waterline, and half the width
        public Vector3[] stacks;
        public float smokeSize = 2f;
        public Vector3 bow;
        public Vector3 stern;
        public float beam = 3f;

        [Header("Camera")]
        // where the camera sits and looks, from the ship and turned with it
        public Vector3 cameraOffset = new Vector3(-40f, 12f, -30f);
        public Vector3 lookOffset = new Vector3(0f, 4f, 10f);
        public float fieldOfView = 40f;
    }

    public static RenderTexture view { get; private set; }
    public static RenderTexture blurred { get; private set; }

    [SerializeField] private UIDocument document;
    [SerializeField] private string home = "home";
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private Light sun;
    [SerializeField] private MenuShip ship;
    [SerializeField] private Scenery[] sceneries;
    // halvings before the glass's blur, more is softer
    [SerializeField] private int blurSteps = 4;

    private Scenery scenery;
    private VisualElement homeElement;

    void Start() {
        homeElement = document.rootVisualElement.Q(home);
        if (sceneries.Length == 0) return;

        scenery = sceneries[Random.Range(0, sceneries.Length)];
        if (scenery.skybox != null) RenderSettings.skybox = scenery.skybox;
        RenderSettings.fogColor = scenery.fogColor;
        sun.transform.rotation = Quaternion.Euler(scenery.sunAngle);
        sun.color = scenery.sunColor;
        sun.intensity = scenery.sunIntensity;
        DynamicGI.UpdateEnvironment();

        ship.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, scenery.heading, 0f));
        ship.Build(scenery);
        sceneCamera.fieldOfView = scenery.fieldOfView;
        Fit();
        Follow();
    }

    void LateUpdate() {
        // the other pages have their own background, so only draw while home is up
        var shown = UIKit.IsShown(homeElement);
        sceneCamera.enabled = shown;
        if (!shown || scenery == null) return;

        Fit();
        Follow();
        Blur();
    }

    private void Follow() {
        var anchor = ship.anchor;
        sceneCamera.transform.position = anchor.TransformPoint(scenery.cameraOffset);
        sceneCamera.transform.LookAt(anchor.TransformPoint(scenery.lookOffset));
    }

    // the camera draws at the screen's size
    private void Fit() {
        if (view != null && view.width == Screen.width && view.height == Screen.height) return;

        Free(view);
        Free(blurred);
        view = new RenderTexture(Screen.width, Screen.height, 24) { name = "Menu Scene" };
        blurred = new RenderTexture(Mathf.Max(1, Screen.width >> (blurSteps - 1)), Mathf.Max(1, Screen.height >> (blurSteps - 1)), 0) { name = "Menu Glass" };
        sceneCamera.targetTexture = view;
    }

    // halving with bilinear filtering averages each 2x2, and one step back up smooths the blocks
    private void Blur() {
        var active = RenderTexture.active;
        var source = view;
        RenderTexture last = null;
        for (var i = 1; i <= blurSteps; i++) {
            var step = RenderTexture.GetTemporary(Mathf.Max(1, view.width >> i), Mathf.Max(1, view.height >> i), 0);
            Graphics.Blit(source, step);
            if (last != null) RenderTexture.ReleaseTemporary(last);
            source = last = step;
        }
        Graphics.Blit(source, blurred);
        if (last != null) RenderTexture.ReleaseTemporary(last);
        RenderTexture.active = active;
    }

    void OnDestroy() {
        if (sceneCamera != null) sceneCamera.targetTexture = null;
        Free(view);
        Free(blurred);
        view = null;
        blurred = null;
    }

    private static void Free(RenderTexture texture) {
        if (texture == null) return;
        if (RenderTexture.active == texture) RenderTexture.active = null;
        texture.Release();
        Destroy(texture);
    }
}
