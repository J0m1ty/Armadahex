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
        Confirm
    }
    
    public AttackState attackState;
    private Selector selector;
    public CameraController cameraRig;

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

    void Start() {
        selector = GetComponent<Selector>();

        TurnManager.instance.OnTurnChange += ChangeTurn;
    }

    public void ChangeTurn(Team team) {
        if (team.isPlayer) {
            SetState(AttackState.SelectShip);
        }
        else {
            SetState(AttackState.None);
        }
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
        selector.allowSelectingGrids = false;
        selector.allowSelectingShips = false;

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
        selectedShipText.text = selectedShip.name + " selected : choose weapon";

        foreach (Transform child in optionsParent.transform) {
            Destroy(child.gameObject);
        }

        foreach (Attack attack in selectedShip.attacks) {
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
        }
    }

    public void GoBackToAttackPattern() {
        if (attackState == AttackState.Confirm) {
            selectedPattern = null;

            // go back further if there is only one attack option
            SetState(AttackState.SelectAttackPattern);

            if (selectedOption.info.options.Length == 1 || selectedOption.info.options.Length == 0) {
                GoBackToAttackOption();
            }
        }
    }

    public void GoBackToAttackOption() {
        if (attackState == AttackState.SelectAttackPattern) {
            selectedOption = null;

            SetState(AttackState.SelectAttackOption);

            // go back further if there is only one attack option
            if (selectedShip.attacks.Length == 1) {
                GoBackToTarget();
            }
        }
    }

    public void GoBackToTarget() {
        if (attackState == AttackState.SelectAttackOption) {
            SetState(AttackState.SelectTarget);
            selectedTarget = null;
        }
    }

    public void GoBackToSelection() {
        if (attackState == AttackState.SelectTarget) {
            SetState(AttackState.SelectShip);
            selectedShip = null;
        }
    }
}
