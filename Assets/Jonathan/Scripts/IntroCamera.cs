using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// a wide shot of both boards from an angle, shown until the countdown ends
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

    private Camera cam;
    private List<Camera> hidden = new List<Camera>();

    void Awake() {
        cam = GetComponent<Camera>();
    }

    void Start() {
        Frame();

        foreach (var other in Camera.allCameras) {
            if (other == cam || other.targetTexture != null) continue;

            other.enabled = false;
            hidden.Add(other);
        }
    }

    private void Frame() {
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
        transform.LookAt(Vector3.Lerp(a, b, focus));
    }

    public void End() {
        foreach (var other in hidden) {
            if (other != null) other.enabled = true;
        }

        hidden.Clear();
        gameObject.SetActive(false);
    }
}
