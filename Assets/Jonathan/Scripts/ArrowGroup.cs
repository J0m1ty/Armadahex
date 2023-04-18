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

    public void SetPatternPreview(AttackPattern pattern) {
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
        
        foreach (AttackDirection dir in pattern.directions) {
            var obj = Instantiate(arrowPrefab, transform);
            var arrow = obj.GetComponent<Arrow>();
            arrow.offset = arrowOffset;
            var rev = dir.reverse;
            var rangeInDirection = 5;
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
                var activeHexOnly = selectedTarget.hexRenderer.gridRef;
                while (true) {
                    var next = originHex.GetNeighbor(AttackUIManager.Convert(dir.rotation, !rev));
                    if (next != null) {
                        originHex = next;

                        if (next.hexRenderer.gameObject.activeSelf) {
                            activeHexOnly = next;
                        }
                    }
                    else {
                        break;
                    }
                }

                var overallLength = Vector3.Magnitude(transform.position - activeHexOnly.hexRenderer.transform.position);

                transform.position = new Vector3(activeHexOnly.hexRenderer.transform.position.x, height, activeHexOnly.hexRenderer.transform.position.z);

                // color each hex
                rangeInDirection = 0;
                while (true) {
                    var thisDistance = Vector3.Magnitude(activeHexOnly.hexRenderer.transform.position - originHex.hexRenderer.transform.position);

                    if (originHex != selectedTarget) {
                        originHex.hexRenderer.SetCustomFogColor(Color.Lerp(FogState.friendlySelected, FogState.fogNormal, Mathf.Clamp01(LODMeshGenerator.Map(Mathf.Abs(overallLength - thisDistance), 0, overallLength * 2f, 0, 1))));
                    }

                    var next = originHex.GetNeighbor(AttackUIManager.Convert(dir.rotation, rev));
                    if (next != null) {
                        rangeInDirection++;
                        originHex = next;
                    }
                    else {
                        break;
                    }

                    if (!dir.noRange && rangeInDirection > dir.length) {
                        break;
                    }
                }

                // go back and subtract from rangeInDirection if squares are not active
                while (true) {
                    if (!originHex.hexRenderer.gameObject.activeSelf) {
                        rangeInDirection--;

                        if (rangeInDirection < 0) {
                            rangeInDirection = 0;
                        }

                        var next = originHex.GetNeighbor(AttackUIManager.Convert(dir.rotation, !rev));
                        
                        if (next != null) {
                            originHex = next;
                        }
                        else {
                            break;
                        }
                    }
                    else {
                        break;
                    }
                }

                s = Vector3.Magnitude(originHex.hexRenderer.transform.position - activeHexOnly.hexRenderer.transform.position);
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

            arrow.SetArrow(dir.rotation, rev, s, SliderPos.Back);
            arrows.Add(arrow);
        }

        SetColor(FogState.friendlySelected);
    }
}
