using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(Selector))]
public class AttackUIManager : MonoBehaviour
{
    public enum AttackState {
        None,
        Error,
        SelectShip,
        SelectTarget,
        SelectAttackOption,
        SelectAttackPattern,
        Confirm,
        AttackOver
    }
    
    public AttackState attackState;
    private Selector selector;
    public CameraController cameraRig;
    [SerializeField]
    private TeamManager teamManager;

    [Header("Error Text")]
    public TMP_Text errorText;

    [Header("Selection Text")]
    public TMP_Text selectText;
    private Ship selectedShip;

    [Header("Target Text")]
    public GameObject targetUI;
    public TMP_Text targetText;
    public Button backToSelectionButton;
    private GridUnit selectedTarget;

    [Header("Attack Options")]
    public GameObject attackOptionsUI;
    public TMP_Text selectedShipText;
    public GameObject optionsParent;
    public GameObject optionPrefab;
    public Button backToTargetButton;
    private Attack selectedOption;

    [Header("Attack Patterns")]
    public GameObject attackPatternsUI;
    public TMP_Text selectedOptionText;
    public GameObject patternsParent;
    public GameObject patternPrefab;
    public Button backToOptionsButton;
    private AttackPattern selectedPattern;

    [Header("Confirmation Text")]
    public GameObject confirmUI;
    public Button confirmButton;
    public TMP_Text infoText;
    public Button cancelButton;

    void Awake() {
        selector = GetComponent<Selector>();
    }

    public void SetState(AttackState state) {
        attackState = state;

        if (state != AttackState.Error) {
            selectText.gameObject.SetActive(false);
            errorText.gameObject.SetActive(false);
            targetUI.SetActive(false);
            attackOptionsUI.SetActive(false);
            attackPatternsUI.SetActive(false);
            confirmUI.SetActive(false);
        }

        if (state != AttackState.None) {
            selector.allowSelectingGrids = false;
            selector.allowSelectingShips = false;
        }

        switch (state) {
            case AttackState.SelectShip:
                selectText.gameObject.SetActive(true);
                selector.allowSelectingShips = true;
                selector.SetTeam(TurnManager.instance.currentTeam);
                cameraRig.MoveTo(TurnManager.instance.currentTeam.teamBase.transform.position);
                break;
            case AttackState.Error:
                errorText.gameObject.SetActive(true);
                break;
            case AttackState.SelectTarget:
                targetUI.SetActive(true);
                selector.allowSelectingGrids = true;
                selector.SetTeam(TurnManager.instance.otherTeam);
                cameraRig.MoveTo(TurnManager.instance.otherTeam.teamBase.transform.position);
                break;
            case AttackState.SelectAttackOption:
                attackOptionsUI.SetActive(true);
                break;
            case AttackState.SelectAttackPattern:
                attackPatternsUI.SetActive(true);
                break;
            case AttackState.Confirm:
                confirmUI.SetActive(true);
                break;
            case AttackState.AttackOver:
                break;
            default:
                break;
        }
    }

    public void SelectShip(Ship ship) {
        if (attackState == AttackState.SelectShip) {
            if (ship.team.isPlayer) {
                if (ship.hasAmmoLeft) {
                    selectedShip = ship;
                    SetState(AttackState.SelectTarget);
                }
                else {
                    SetState(AttackState.Error);
                    errorText.text = "This ship has no ammo left";
                }
            }
            else {
                SetState(AttackState.Error);
                errorText.text = "You can't select an enemy ship";
            }
        }
    }

    public void SelectTarget(GridUnit target) {
        if (attackState == AttackState.SelectTarget) {
            if (!target.hexRenderer.hexMap.teamBase.team.isPlayer) {
                selectedTarget = target;
                SetState(AttackState.SelectAttackOption);
                
                // dont generate attack options if there is only one
                if (selectedShip.attacks.Length == 1) {
                    SelectAttackOption(selectedShip.attacks[0]);
                }
                else if (selectedShip.attacks.Length == 0) {
                    SetState(AttackState.Error);
                    errorText.text = "This ship has no attacks remaining";
                }
                else {
                    GenerateAttackOptions();
                }
            }
            else {
                SetState(AttackState.Error);
                errorText.text = "You can't select a friendly target";
            }
        }
    }
    
    public void GenerateAttackOptions() {
        selectedShipText.text = selectedShip.shipModel.name + " selected : choose weapon";

        foreach (Transform child in optionsParent.transform) {
            Destroy(child.gameObject);
        }

        foreach (Attack attack in selectedShip.attacks) {
            if (attack.ammoLeft <= 0 && !attack.info.unlimited) {
                continue;
            }

            GameObject option = Instantiate(optionPrefab, optionsParent.transform);
            option.GetComponentInChildren<TMP_Text>().text = attack.info.name;
            option.GetComponent<Button>().onClick.AddListener(() => SelectAttackOption(attack));
        }
    }

    public void SelectAttackOption(Attack attack) {
        if (attackState == AttackState.SelectAttackOption) {
            selectedOption = attack;
            SetState(AttackState.SelectAttackPattern);

            // dont generate attack patterns if there is only one
            if (selectedOption.info.options.Length == 1) {
                SelectAttackPattern(selectedOption.info.options[0]);
            }
            else if (selectedOption.info.options.Length == 0) {
                SelectAttackPattern(null);
            }
            else {
                GenerateAttackPatterns();
            }
        }
    }

    public void GenerateAttackPatterns() {
        selectedOptionText.text = selectedOption.info.name + " selected : choose firing pattern";

        foreach (Transform child in patternsParent.transform) {
            Destroy(child.gameObject);
        }

        foreach (AttackPattern pattern in selectedOption.info.options) {
            GameObject patternObj = Instantiate(patternPrefab, patternsParent.transform);
            patternObj.GetComponentInChildren<TMP_Text>().text = pattern.name;
            patternObj.GetComponent<Button>().onClick.AddListener(() => SelectAttackPattern(pattern));
        }
    }

    public void SelectAttackPattern(AttackPattern pattern) {
        if (attackState == AttackState.SelectAttackPattern) {
            selectedPattern = pattern;
            SetState(AttackState.Confirm);

            GenerateConfirmationInfo();
        }
    }

    public void GenerateConfirmationInfo() {
        if (attackState == AttackState.Confirm) {
            // dont show shots left if unlimited
            infoText.gameObject.SetActive(!selectedOption.info.unlimited);
            infoText.text = selectedOption.ammoLeft + " shots left";
        }
    }

    public void ConfirmAttack() {
        if (attackState == AttackState.Confirm) {
            // do and show the attach, then end turn

            // if the attack is not unlimited, remove ammo
            if (!selectedOption.info.unlimited) {
                selectedOption.ammoLeft--;
            }

            // calculate each hex that will be hit
            List<HexRenderer> targets = new List<HexRenderer>();
            if (selectedPattern == null) {
                targets.Add(selectedTarget.hexRenderer);
            }
            else {
                if (selectedPattern.attackCenter) {
                    targets.Add(selectedTarget.hexRenderer);
                }

                foreach (AttackDirection direction in selectedPattern.directions) {
                    var origin = selectedTarget;

                    if (selectedPattern.doOffset) {
                        // go back until we reach get null from .GetNeighbor
                        while (true) {
                            var next = origin.GetNeighbor(Convert(direction.rotation, !direction.reverse));
                            if (next != null) {
                                origin = next;
                            }
                            else {
                                break;
                            }
                        }
                    }
                    
                    var n = 0;
                    while (true) {
                        origin = origin.GetNeighbor(Convert(direction.rotation, direction.reverse));
                        
                        if (origin != null) {
                            targets.Add(origin.hexRenderer);
                        }
                        else {
                            break;
                        }

                        if (!direction.noRange && ++n >= direction.length) {
                            break;
                        }
                    }
                }
            }
            
            for (int i = 0; i < targets.Count; i++) {
                var target = targets[i];
                
                if (target.gridRef.shipSegment == null) {
                    target.ClearFog();
                    OnAttack?.Invoke(TurnManager.instance.enemyTeam, false, target.coords.index);
                }
                else if (target.gridRef.shipSegment.isAlive) {
                    target.EnableFlames();
                    target.gridRef.shipSegment.Destroy();
                    OnAttack?.Invoke(TurnManager.instance.enemyTeam, true, target.coords.index);
                }
            }

            SetState(AttackState.AttackOver);
            
            TurnManager.instance.TurnOver();
        }
    }

    public void GetAttackFromEnemy(TeamType target, int hexIndex) {
        var targetTeam = TurnManager.instance.playerTeam;
        var grid = targetTeam.teamBase.hexMap;
        var hex = grid.FromCoordinates(new CoordinateSystem(hexIndex)).hexRenderer;
        
        if (hex.gridRef.shipSegment == null) {
            
        }
        else if (hex.gridRef.shipSegment.isAlive) {
            hex.gridRef.shipSegment.Destroy();
        }
    }

    public delegate void AttackEvent(Team against, bool hit, int hexIndex);

    public event AttackEvent OnAttack;

    public void GoBackToAttackPattern() {
        if (attackState == AttackState.Confirm) {
            selectedPattern = null;
            
            SetState(AttackState.SelectAttackPattern);

            // go back further if there is only one or no attack options
            // or if the current attack option has no ammo left
            if (selectedOption.info.options.Length == 1 || selectedOption.info.options.Length == 0 || (selectedOption.ammoLeft <= 0 && !selectedOption.info.unlimited)) {
                GoBackToAttackOption();
            }
            else {
                GenerateAttackPatterns();
            }
        }
    }

    public void GoBackToAttackOption() {
        if (attackState == AttackState.SelectAttackPattern) {
            selectedOption = null;

            SetState(AttackState.SelectAttackOption);

            // go back further if there is only one or none attack option
            if (selectedShip.remainingAttacks.Length == 1) {
                GoBackToTarget();
            }
            else {
                GenerateAttackOptions();
            }
        }
    }

    public void GoBackToTarget() {
        if (attackState == AttackState.SelectAttackOption) {
            selectedTarget = null;

            SetState(AttackState.SelectTarget);

            // go back further if the ship is out of ammo completely
            if (selectedShip.remainingAttacks.Length == 0) {
                GoBackToSelection();
            }
        }
    }

    public void GoBackToSelection() {
        if (attackState == AttackState.SelectTarget) {
            SetState(AttackState.SelectShip);
            selectedShip = null;
        }
    }

    public static int Convert(Rotation r, bool reverse) {
        var num = 0;

        switch (r) {
            case Rotation.One:
                num = 0;
                break;
            case Rotation.Two:
                num = 1;
                break;
            case Rotation.Three:    
                num = 2;
                break;
            default:
                break;
        }

        if (reverse) {
            num += 3;
        }

        return num;
    }
}
