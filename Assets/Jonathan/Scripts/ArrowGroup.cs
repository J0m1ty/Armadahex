using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowGroup : MonoBehaviour
{
    private List<Arrow> arrows;

    [SerializeField]
    private GameObject arrowPrefab;

    private GridUnit selectedTarget;
    private Attack selectedOption;

    // what the arrows show now, so confirming the same preview doesn't grow them again
    private AttackPattern shownPattern;
    private GridUnit shownTarget;

    public float height;
    public float arrowOffset;

    public bool disableButtons;

    public void Awake() {
        arrows = new List<Arrow>();
    }

    public void SetInfo(GridUnit selectedTarget, Attack selectedOption) {
        this.selectedTarget = selectedTarget;
        this.selectedOption = selectedOption;
    }

    // For buttons
    public void SetPatternPreview(int i) {
        if (disableButtons) {
            return;
        }

        var pattern = selectedOption.info.options[i];
        SetPatternPreview(pattern);
    }

    // For scripting
    public void SetPatternPreview(GridUnit selectedTarget, Attack selectedOption, AttackPattern selectedPattern) {
        SetInfo(selectedTarget, selectedOption);
        SetPatternPreview(selectedPattern);
    }

    public void LockIn() {
        disableButtons = true;
    }

    public void SetColor(Color color) {
        foreach (var arrow in arrows) {
            arrow.SetColor(color);
        }
    }

    // a shape whose cells all sit in straight lines from the target (a star, a v) gets an arrow per line;
    // other shapes and sweeps just light their hexes
    private AttackDirection[] Directions(AttackPattern pattern) {
        if (pattern.cells == null || pattern.cells.Length == 0) return pattern.directions;
        if (selectedOption != null && selectedOption.info.isScan) return new AttackDirection[0];

        var steps = new[] { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(1, -1) };
        var rotations = new[] { Rotation.One, Rotation.Two, Rotation.Three };
        var arrows = new List<AttackDirection>();
        foreach (var cell in pattern.cells) {
            if (cell == Vector2Int.zero) continue;

            var found = false;
            for (var k = 0; k < 6 && !found; k++) {
                for (var n = 1; n <= 8 && !found; n++) {
                    if (steps[k] * n == cell) {
                        arrows.Add(new AttackDirection { rotation = rotations[k % 3], reverse = k >= 3, length = n });
                        found = true;
                    }
                }
            }
            if (!found) return new AttackDirection[0];
        }
        return arrows.ToArray();
    }

    public void SetPatternPreview(AttackPattern pattern) {
        Debug.Log("Setting pattern preview to " + pattern.name);

        for (int i = 0; i < arrows.Count; i++) {
            Destroy(arrows[i].gameObject);
        }
        arrows.Clear();

        if (pattern == null || selectedTarget == null || selectedOption == null) {
            return;
        }

        foreach (var hex in selectedTarget.hexRenderer.hexMap.hexes) {
            if (hex == selectedTarget) {
                continue;
            }
            hex.hexRenderer.RemoveFogOverrideColor();
        }

        transform.position = new Vector3(selectedTarget.hexRenderer.transform.position.x, height, selectedTarget.hexRenderer.transform.position.z);

        var grow = pattern != shownPattern || selectedTarget != shownTarget;
        shownPattern = pattern;
        shownTarget = selectedTarget;
        
        foreach (AttackDirection dir in Directions(pattern)) {
            var obj = Instantiate(arrowPrefab, transform);
            var arrow = obj.GetComponent<Arrow>();
            arrow.offset = arrowOffset;
            var rev = dir.reverse;
            var rangeInDirection = 5;
            if (dir.noRange && !pattern.doOffset) {
                // count the hexes to the edge, so the arrow runs the whole way across
                rangeInDirection = 0;
                var edge = selectedTarget;
                while ((edge = edge.GetNeighbor(AttackUIManager.Convert(dir.rotation, rev))) != null) rangeInDirection++;
            }
            var s = TurnManager.instance.enemyTeam.teamBase.hexMap.size * (dir.noRange ? rangeInDirection : dir.length + 1);

            if (pattern.doOffset) {
                arrow.offset = 0;

                if (pattern.autoOptimize) {
                    var testOne = selectedTarget.RangeInDirection(AttackUIManager.Convert(dir.rotation, rev), true);
                    var testTwo = selectedTarget.RangeInDirection(AttackUIManager.Convert(dir.rotation, !rev), true);

                    if (testOne < testTwo) {
                        rev = !rev;
                    }
                }

                // do offset
                var originHex = selectedTarget.hexRenderer.gridRef;
                var originActive = selectedTarget.hexRenderer.gridRef;
                while (true) {
                    var next = originHex.GetNeighbor(AttackUIManager.Convert(dir.rotation, !rev));
                    if (next != null) {
                        originHex = next;

                        if (next.hexRenderer.gameObject.activeSelf) {
                            originActive = next;
                        }
                    }
                    else {
                        break;
                    }
                }

                // find furthest hex in direction, keep origin the same
                var furthest = originHex;
                var furthestActive = originHex;
                while (true) {
                    var next = furthest.GetNeighbor(AttackUIManager.Convert(dir.rotation, rev));
                    if (next != null) {
                        furthest = next;

                        if (next.hexRenderer.gameObject.activeSelf) {
                            furthestActive = next;
                        }
                    }
                    else {
                        break;
                    }
                }

                var overallLength = Vector3.Magnitude(furthestActive.hexRenderer.transform.position - originActive.hexRenderer.transform.position);

                transform.position = new Vector3(originActive.hexRenderer.transform.position.x, height, originActive.hexRenderer.transform.position.z);

                // color each hex
                var colorHex = originHex;
                while (true) {
                    var thisDistance = Vector3.Magnitude(selectedTarget.hexRenderer.transform.position - colorHex.hexRenderer.transform.position);

                    if (colorHex != selectedTarget) {
                        colorHex.hexRenderer.SetCustomFogColor(Color.Lerp(FogState.friendlySelected, FogState.fogNormal, Mathf.Clamp01(LODMeshGenerator.Map(Mathf.Abs(overallLength - thisDistance), 0, overallLength, 1, 0))));
                    }

                    var next = colorHex.GetNeighbor(AttackUIManager.Convert(dir.rotation, rev));
                    if (next != null) {
                        colorHex = next;
                    }
                    else {
                        break;
                    }
                }

                s = Vector3.Magnitude(originActive.hexRenderer.transform.position - furthestActive.hexRenderer.transform.position);
                Debug.DrawLine(originActive.hexRenderer.transform.position, furthestActive.hexRenderer.transform.position, Color.red, 100f);
            }
            else {
                // color each hex
                var originHex = selectedTarget.hexRenderer.gridRef;
                for (int i = 0; i < dir.length; i++) {
                    var next = originHex.GetNeighbor(AttackUIManager.Convert(dir.rotation, rev));
                    if (next != null) {
                        originHex = next;
                        originHex.hexRenderer.SetCustomFogColor(Color.Lerp(FogState.friendlySelected, FogState.fogNormal, 0.5f));
                    }
                    else {
                        break;
                    }
                }
            }

            arrow.SetArrow(dir.rotation, rev, s, SliderPos.Back, grow);
            arrows.Add(arrow);
        }

        SetColor(FogState.friendlySelected);
    }
}
