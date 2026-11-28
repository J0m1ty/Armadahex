using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// the intro's own camera: a wide shot of both boards, then a spiral out over each board in turn
[RequireComponent(typeof(Camera))]
public class IntroCamera : MonoBehaviour
{
    [SerializeField]
    private TeamManager teamManager;

    [Header("Framing")]
    // starts out to sea beside the player's base, looking across at both boards
    // distances are relative to the distance between the two bases
    [SerializeField]
    private float back = -0.1f;
    [SerializeField]
    private float across = 0.55f;
    [SerializeField]
    private float height = 420f;
    // where between the bases it looks, 0 is the player's base and 1 the enemy's
    [SerializeField]
    private float focus = 0.55f;

    // the wide shot drifts round the boards so it's never still
    [SerializeField]
    private float overviewDrift = 1.5f;

    [Header("Spiral")]
    // most of the way up over a board, then a little further out and up until the whole board shows,
    // turning about as slowly as the wide shot drifts
    [SerializeField]
    private float startPitch = 50f;
    [SerializeField]
    private float endPitch = 64f;
    // the start distance is a share of the board's radius
    [SerializeField]
    private float startDistance = 1.6f;
    // the end distance fits the board, with this much room
    [SerializeField]
    private float fitPadding = 1.05f;
    [SerializeField]
    private float turn = 24f;
    [SerializeField]
    private float minHeight = 14f;
    [SerializeField]
    private float endYaw = 0f;

    private Camera cam;
    private bool overview;
    private Vector3 overviewTarget;
    private List<Camera> hidden = new List<Camera>();
    private Dictionary<TeamBase, float> radii = new Dictionary<TeamBase, float>();

    void Awake() {
        cam = GetComponent<Camera>();
    }

    void Start() {
        Overview();

        foreach (var other in Camera.allCameras) {
            if (other == cam || other.targetTexture != null) continue;

            other.enabled = false;
            hidden.Add(other);
        }
    }

    public void Overview() {
        Vector3? friendly = null;
        Vector3? enemy = null;

        foreach (var team in teamManager.teams) {
            if (team.isPlayer) friendly = team.teamBase.transform.position;
            else enemy = team.teamBase.transform.position;
        }

        if (friendly == null || enemy == null) return;

        var a = friendly.Value;
        var b = enemy.Value;
        var forward = b - a;
        forward.y = 0;
        var span = forward.magnitude;
        forward /= span;
        var side = Vector3.Cross(Vector3.up, forward);

        transform.position = a - forward * span * back + side * span * across + Vector3.up * height;
        overviewTarget = Vector3.Lerp(a, b, focus);
        transform.LookAt(overviewTarget);
        overview = true;
    }

    void Update() {
        // reduced motion holds the wide shot still
        if (!overview || GameSettings.reducedMotion) return;

        transform.RotateAround(overviewTarget, Vector3.up, overviewDrift * Time.deltaTime);
        transform.LookAt(overviewTarget);
    }

    // t from 0 to 1 over the spiral; it turns and climbs the whole way, easing off gently as it opens out
    public void Spiral(Team team, float t) {
        overview = false;
        var center = team.teamBase.transform.position;
        var radius = Radius(team.teamBase);

        var ease = t < 1 ? 1 - Mathf.Pow(1 - t, 2) : 1 + (t - 1) * 0.02f;
        var pitch = Mathf.Lerp(startPitch, endPitch, ease);
        var fit = radius * fitPadding / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        var distance = Mathf.Lerp(radius * startDistance, fit, ease);

        // it ends looking the way the top view does
        var yaw = endYaw + turn * (ease - 1);

        var position = center + Quaternion.Euler(pitch, yaw, 0) * Vector3.back * distance;
        position.y = Mathf.Max(position.y, center.y + minHeight);
        transform.position = position;
        transform.LookAt(center);
    }

    // the farthest active hex from the board's centre
    private float Radius(TeamBase teamBase) {
        if (radii.TryGetValue(teamBase, out var known)) return known;

        var grid = teamBase.hexMap;
        var center = teamBase.transform.position;
        var radius = 0f;
        foreach (var unit in grid.hexes) {
            if (unit.hexRenderer == null || !unit.hexRenderer.gameObject.activeInHierarchy) continue;
            var offset = unit.hexRenderer.transform.position - center;
            offset.y = 0;
            radius = Mathf.Max(radius, offset.magnitude + grid.size);
        }
        radii[teamBase] = radius;
        return radius;
    }

    public void End() {
        foreach (var other in hidden) {
            if (other != null) other.enabled = true;
        }

        hidden.Clear();
        gameObject.SetActive(false);
    }
}
