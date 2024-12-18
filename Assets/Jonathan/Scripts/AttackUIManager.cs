using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public static class AttackColor {
    // grey is default
    public static Color none = new Color(0.6f, 0.6f, 0.6f);
    // dark red hit
    public static Color hit = new Color(0.8f, 0.0f, 0.0f);
    // dark blue for scan
    public static Color scan = new Color(0.0f, 0.0f, 0.8f);
    // white miss
    public static Color miss = new Color(1.0f, 1.0f, 1.0f);
}

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
    [SerializeField]
    private ShipManager shipManager;
    [SerializeField]
    private Countdown countdown;
    [SerializeField]
    private PanelSlider attackPanel;

    [Header("Multi Use")]
    public TMP_Text directiveText;
    public Image selectionDisplay;
    public GameObject selectionUI;
    public GameObject backButtonParent;
    public Button backButton;
    public GameObject bottomUI;

    [Header("Error Text")]
    public TMP_Text errorText;

    [Header("Directive Text")]
    private Ship selectedShip;

    [Header("Target Text")]
    private GridUnit selectedTarget;

    [Header("Attack Options")]
    public TMP_Text selectedShipText;
    public GameObject optionsParent;
    public GameObject optionPrefab;
    private Attack selectedOption;

    [Header("Attack Patterns")]
    public TMP_Text selectedOptionText;
    public GameObject patternsParent;
    public GameObject patternPrefab;
    private AttackPattern selectedPattern;
    [SerializeField]
    private ArrowGroup arrowGroup;

    [Header("Confirmation Text")]
    public TMP_Text infoText;
    public GameObject confirmUI;
    public Button confirmButton;

    [Header("Game Stats")]
    public int shotsFired;
    public int shotsHit;
    public int advancedShotsFired;

    [Header("Other")]
    public GameObject skipButton;
    public audioScript gameAudio;

    void Awake() {
        selector = GetComponent<Selector>();

        shotsFired = 0;
        shotsHit = 0;
        advancedShotsFired = 0;
    }

    public void SetState(AttackState state) {
        attackState = state;

        backButton.onClick.RemoveAllListeners();
        backButtonParent.GetComponentInChildren<TMP_Text>().text = "UNDO";

        if (state != AttackState.Error) {
            directiveText.transform.parent.gameObject.SetActive(false);
            errorText.gameObject.SetActive(false);
            selectionUI.GetComponent<PanelSlider>().SetState(PanelState.In);
            selectedOptionText.gameObject.SetActive(false);
            confirmUI.SetActive(false);
            arrowGroup.gameObject.SetActive(false);
            backButtonParent.SetActive(false);
            skipButton.SetActive(false);

            confirmButton.onClick.RemoveAllListeners();
            backButton.onClick.RemoveAllListeners();
        }

        if (state != AttackState.None) {
            selector.allowSelectingGrids = false;
            selector.allowSelectingShips = false;

            skipButton.SetActive(state != AttackState.AttackOver);

            infoText.gameObject.SetActive(state == AttackState.Confirm);

            bottomUI.SetActive(true);
        }
        else {
            bottomUI.SetActive(false);
        }

        switch (state) {
            case AttackState.SelectShip:
                if (!GameModeInfo.instance.IsAdvancedCombat) {
                    SelectRandomShip();
                }
                else {
                    directiveText.transform.parent.gameObject.SetActive(true);
                    directiveText.text = "SELECT A SHIP";
                    selector.allowSelectingShips = true;
                    CameraManager.instance.MoveToOnce(TurnManager.instance.playerTeam.teamBase.transform.position);
                }
                break;
            case AttackState.Error:
                errorText.gameObject.SetActive(true);
                break;
            case AttackState.SelectTarget:
                directiveText.transform.parent.gameObject.gameObject.SetActive(true);
                directiveText.text = "SELECT A TARGET";
                selector.allowSelectingGrids = true;
                selector.SetTeam(TurnManager.instance.playerTeam, TurnManager.instance.enemyTeam.teamBase);
                
                if (GameModeInfo.instance.IsAdvancedCombat) {
                    backButton.onClick.AddListener(() => {
                        GoBackToSelection();
                    });
                    backButtonParent.SetActive(true);
                    CameraManager.instance.MoveToOnce(TurnManager.instance.enemyTeam.teamBase.transform.position);
                }
                else {
                    backButtonParent.SetActive(false);
                    CameraManager.instance.MoveToInstant(TurnManager.instance.enemyTeam.teamBase.transform.position);
                }
                break;
            case AttackState.SelectAttackOption:
                directiveText.transform.parent.gameObject.gameObject.SetActive(true);
                directiveText.text = "SELECT AN ATTACK OPTION";
                selectionUI.SetActive(true);
                backButton.onClick.AddListener(() => {
                    GoBackToTarget();
                });
                backButtonParent.SetActive(true);
                selectionUI.GetComponent<PanelSlider>().SetState(PanelState.Out);
                break;
            case AttackState.SelectAttackPattern:
                directiveText.transform.parent.gameObject.gameObject.SetActive(true);
                directiveText.text = "SELECT AN ATTACK PATTERN";
                selectionUI.SetActive(true);
                selectedOptionText.gameObject.SetActive(true);
                backButton.onClick.AddListener(() => {
                    GoBackToAttackOption();
                });
                backButtonParent.SetActive(true);
                selectionUI.GetComponent<PanelSlider>().SetState(PanelState.Out);
                break;
            case AttackState.Confirm:
                confirmUI.SetActive(true);
                confirmButton.onClick.AddListener(() => {
                    ConfirmAttack();
                });
                backButton.onClick.AddListener(() => {
                    GoBackToAttackPattern();
                });
                backButtonParent.GetComponentInChildren<TMP_Text>().text = "CANCEL";
                backButtonParent.SetActive(true);
                break;
            case AttackState.AttackOver:
                bottomUI.SetActive(false);
                break;
            default:
                break;
        }
    }

    public void SelectRandomShip() {
        if (attackState == AttackState.SelectShip) {
            List<Ship> ships = new List<Ship>();
            foreach (Ship ship in shipManager.ships) {
                if (ship.team.isPlayer && ship.hasAmmoLeft && ship.isAlive) {
                    ships.Add(ship);
                }
            }
            
            if (ships.Count > 0) {
                selectedShip = ships[Random.Range(0, ships.Count)];

                Debug.Log("Randomly selected ship: " + selectedShip.name);
                
                SetState(AttackState.SelectTarget);
            }
            else {
                SetState(AttackState.Error);
                errorText.text = "No ships have ammo left";
            }
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
                if (selectedShip.remainingAttacks.Length == 1) {
                    SelectAttackOption(selectedShip.remainingAttacks[0]);
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
        selectionDisplay.sprite = selectedShip.shipModel.display;
        selectedShipText.text = selectedShip.shipModel.name;

        foreach (Transform child in optionsParent.transform) {
            Destroy(child.gameObject);
        }

        foreach (Attack attack in selectedShip.attacks) {
            if ((attack.ammoLeft <= 0 && !attack.info.unlimited) || !attack.info.isAllowed) {
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

            arrowGroup.SetInfo(selectedTarget, selectedOption);

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
        Debug.Log("Generating attack patterns");

        selectionDisplay.sprite = selectedShip.shipModel.display;
        selectedShipText.text = selectedShip.shipModel.name;
        selectedOptionText.text = selectedOption.info.name;

        foreach (Transform child in patternsParent.transform) {
            Destroy(child.gameObject);
        }

        arrowGroup.SetInfo(selectedTarget, selectedOption);
        arrowGroup.disableButtons = false;

        var i = 0;
        foreach (AttackPattern pattern in selectedOption.info.options) {
            GameObject patternObj = Instantiate(patternPrefab, patternsParent.transform);
            patternObj.GetComponentInChildren<TMP_Text>().text = pattern.name;

            var bl = patternObj.AddComponent<ButtonListener>();
            bl.index = i;

            patternObj.GetComponent<Button>().onClick.AddListener(() => {
                arrowGroup.disableButtons = true;
                SelectAttackPattern(bl.index);
            });
            
            bl.OnMouseEnter += () => {
                arrowGroup.gameObject.SetActive(true);
                arrowGroup.SetPatternPreview(bl.index);
            };
            bl.OnMouseExit += () => {
                arrowGroup.gameObject.SetActive(false);
            };

            i++;
        }
    }

    public void SelectAttackPattern(int index) {
        if (attackState == AttackState.SelectAttackPattern) {
            if (index >= 0 && index < selectedOption.info.options.Length) {
                SelectAttackPattern(selectedOption.info.options[index]);
            }
        }
    }

    public void SelectAttackPattern(AttackPattern pattern) {
        if (attackState == AttackState.SelectAttackPattern) {
            selectedPattern = pattern;
            SetState(AttackState.Confirm);

            GenerateConfirmationInfo();

            if (pattern != null) {
                arrowGroup.gameObject.SetActive(true);
                arrowGroup.SetPatternPreview(selectedTarget, selectedOption, pattern);
            }
        }
    }

    public void GenerateConfirmationInfo() {
        if (attackState == AttackState.Confirm) {
            // dont show shots left if unlimited
            if (!infoText) return;

            infoText.gameObject.SetActive(!selectedOption.info.unlimited);
            infoText.text = selectedOption.ammoLeft + " shots left";
        }
    }

    public void SkipTurn() {
        if (TurnManager.instance.currentTeam.isPlayer && !countdown.isPaused) {
            RandomAttack();
        }
    }

    public void RandomAttack() {
        RandomAttack(AttackState.SelectShip);
    }

    public void RandomAttack(AttackState startAt) {
        if (attackState == AttackState.None || attackState == AttackState.AttackOver) {
            return;
        }

        if (startAt == AttackState.SelectShip && attackState != startAt) {
            startAt = attackState;
        }

        attackState = startAt;

        if (startAt == AttackState.SelectShip) {
            var ships = shipManager.playerShips;
            var ship = ships[Random.Range(0, ships.Count)];
            var valid = ship.hasAmmoLeft && ship.team.isPlayer && ship.isAlive;
            var n = 0;
            while (!valid) {
                ship = ships[Random.Range(0, ships.Count)];
                valid = ship.hasAmmoLeft && ship.team.isPlayer && ship.isAlive;
                n++;
                if (n > 10) {
                    return;
                }
            }
            selectedShip = ship;
            RandomAttack(AttackState.SelectTarget);
        }
        else if (startAt == AttackState.SelectTarget) {
            var targets = TurnManager.instance.enemyTeam.teamBase.hexMap.hexes;
            var target = targets[Random.Range(0, targets.Count)];
            var valid = (target.shipSegment?.isAlive ?? true) && target.hexRenderer.gameObject.activeSelf;
            while (!valid) {
                target = targets[Random.Range(0, targets.Count)];
                valid = (target.shipSegment?.isAlive ?? true) && target.hexRenderer.gameObject.activeSelf;
            }
            selectedTarget = target;
            RandomAttack(AttackState.SelectAttackOption);
        }
        else if (startAt == AttackState.SelectAttackOption) {
            var options = selectedShip.remainingAttacks;
            if (options.Length == 0) {
                RandomAttack(AttackState.SelectShip);
                return;
            }
            else if (options.Length == 1) {
                selectedOption = options[0];
                RandomAttack(AttackState.SelectAttackPattern);
                return;
            }
            var option = options[Random.Range(0, options.Length)];
            selectedOption = option;
            RandomAttack(AttackState.SelectAttackPattern);
        }
        else if (startAt == AttackState.SelectAttackPattern) {
            var patterns = selectedOption.info.options;
            if (patterns.Length == 0) {
                RandomAttack(AttackState.Confirm);
                return;
            }
            else if (patterns.Length == 1) {
                selectedPattern = patterns[0];
                RandomAttack(AttackState.Confirm);
                return;
            }
            var pattern = patterns[Random.Range(0, patterns.Length)];
            selectedPattern = pattern;
            RandomAttack(AttackState.Confirm);
        }
        else if (startAt == AttackState.Confirm) {
            ConfirmAttack();
        }
    }

    public void ConfirmAttack() {
        if (attackState == AttackState.Confirm) {
            countdown.StopCountdown();

            //AudioManager.instance?.PlayActionSound(ActionType.Fire);

            // do and show the attach, then end turn

            // if the attack is not unlimited, remove ammo
            if (!selectedOption.info.unlimited) {
                selectedOption.ammoLeft--;

                if (selectedOption.ammoLeft <= 0) {
                    //Debug.LogError("Out of ammo");
                }
            }

            var stopAtHit = false;

            // calculate each hex that will be hit
            List<HexRenderer> targets = new List<HexRenderer>();
            if (selectedPattern == null) {
                targets.Add(selectedTarget.hexRenderer);
            }
            else {
                if (selectedPattern.attackCenter && !selectedPattern.doOffset) {
                    targets.Add(selectedTarget.hexRenderer);
                }

                foreach (AttackDirection direction in selectedPattern.directions) {
                    if (direction.stopAtHit) {
                        stopAtHit = true;
                    }

                    var origin = selectedTarget;

                    bool reverse = direction.reverse;

                    if (selectedPattern.doOffset) {
                        if (selectedPattern.autoOptimize) {
                            var testOne = selectedTarget.RangeInDirection(Convert(direction.rotation, reverse), true);
                            var testTwo = selectedTarget.RangeInDirection(Convert(direction.rotation, !reverse), true);

                            if (testOne < testTwo) {
                                reverse = !reverse;
                            }
                        }

                        // go back until we reach get null from .GetNeighbor
                        while (true) {
                            var next = origin.GetNeighbor(Convert(direction.rotation, !reverse));
                            if (next != null) {
                                origin = next;
                            }
                            else {
                                break;
                            }
                        }

                        targets.Add(origin.hexRenderer);
                    }
                    
                    var n = 0;
                    while (true) {
                        origin = origin.GetNeighbor(Convert(direction.rotation, reverse));

                        bool isHit = origin?.shipSegment?.isAlive ?? false;
                        
                        if (origin != null) {
                            targets.Add(origin.hexRenderer);
                        }
                        else {
                            break;
                        }

                        if ((!direction.noRange && ++n >= direction.length) || (direction.stopAtHit && isHit)) {
                            break;
                        }
                    }
                }
            }

            var hit = false;
            bool? destroyed = null;
            
            for (int i = 0; i < targets.Count; i++) {
                var target = targets[i];
                
                if (target.gridRef.shipSegment == null) {
                    target.SetColor(AttackColor.miss);
                    target.SetFogColorInstant(FogColor.Miss, false);
                    target.ClearFog();
                    OnAttack?.Invoke(TurnManager.instance.enemyTeam, false, target.coords.index, i == targets.Count - 1);
                }
                else if (target.gridRef.shipSegment.isAlive) {
                    target.SetColor(AttackColor.hit);
                    target.SetFogColorInstant(FogColor.Hit, false);
                    target.EnableFlames();
                    destroyed = target.gridRef.shipSegment.Destroy();
                    OnAttack?.Invoke(TurnManager.instance.enemyTeam, true, target.coords.index, i == targets.Count - 1);

                    hit = true;

                    if (stopAtHit) {
                        Debug.Log("Stopping at hit");
                        break;
                    }
                }
            }

            Debug.Log("Shot was " + (hit ? "successful" : "unsuccessful"));

            shotsFired++;

            if (hit) {
                shotsHit++;
            }

            if (selectedOption.info.name != "Single Shot") {
                advancedShotsFired++;
            }

            foreach (var hex in selectedTarget.hexRenderer.hexMap.hexes) {
                hex.hexRenderer.RemoveFogOverrideColor();
            }

            if (!GameOver.instance.CheckIfGameOver()) {
                attackPanel.ResetData();
                attackPanel.SetAttackInfo(hit, destroyed, selectedShip.shipModel.attackName, selectedOption.info.unlimited ? 1000 : selectedOption.ammoLeft );
                CameraManager.instance.Shake(hit);
                AudioManager.instance?.PlayActionSound(ActionType.Explosion);
                AudioManager.instance?.PlayHitSound(hit, 3f);
                attackPanel.QuickActivate();

                gameAudio.RemainingShips(shipManager.enemyShips.FindAll(s => s.isAlive && s.hasAmmoLeft).Count);
            }

            SetState(AttackState.AttackOver);

            var shipsRemaining = shipManager.playerShips.FindAll(s => s.isAlive && s.hasAmmoLeft).Count;
            
            if (GameModeInfo.instance.IsBonus && hit) {
                TurnManager.instance.ContinueTurnDelay(false);
            }
            else if (GameModeInfo.instance.IsSalvo && TurnManager.instance.consecutiveTurns < shipsRemaining) {
                TurnManager.instance.ContinueTurnDelay(false);
            }
            else {
                TurnManager.instance.TurnOver();

                selectedShip = null;
                selectedTarget = null;
                selectedOption = null;
                selectedPattern = null;
            }
        }
    }

    public void GetAttackFromEnemy(TeamType target, int hexIndex, bool finalAttack) {
        var targetTeam = TurnManager.instance.playerTeam;
        var grid = targetTeam.teamBase.hexMap;
        var hex = grid.FromCoordinates(new CoordinateSystem(hexIndex)).hexRenderer;
        
        var hit = false;
        bool? destroyed = null;
        if (hex.gridRef.shipSegment == null) {
            hex.SetColor(AttackColor.miss);
        }
        else if (hex.gridRef.shipSegment.isAlive) {
            destroyed = hex.gridRef.shipSegment.Destroy();
            hex.SetColor(AttackColor.hit);
            hit = true;
        }

        if (GameModeInfo.instance.IsSingleplayer) {
            if (GameModeInfo.instance.IsBonus) {
                if (hit) {
                    TurnManager.instance.ContinueTurnDelay(true);
                }
                else {
                    TurnManager.instance.TurnOver();
                }
            }
            else if (GameModeInfo.instance.IsSalvo) {
                var shipsRemaining = shipManager.playerShips.FindAll(s => s.isAlive && s.hasAmmoLeft).Count;
            
                if (TurnManager.instance.consecutiveTurns >= shipsRemaining) {
                    TurnManager.instance.TurnOver();
                }
                else {
                    TurnManager.instance.ContinueTurnDelay(true);
                }
            }
            else {
                TurnManager.instance.TurnOver();
            }

            attackPanel.ResetData();
        }

        GameOver.instance.CheckIfGameOver(TurnManager.instance.playerTeam, hit, hex.coords.index, finalAttack);

        attackPanel.SetAttackInfo_Save(hit, destroyed, null, null);
        
        if (finalAttack && !GameOver.instance.CheckIfGameOver()) {
            CameraManager.instance.Shake(attackPanel.persistingHit);
            AudioManager.instance?.PlayActionSound(ActionType.Explosion);
            AudioManager.instance?.PlayHitSound(attackPanel.persistingHit, 3f);
            attackPanel.QuickActivate(true, GameModeInfo.instance.IsSingleplayer ? (attackPanel.persistingHit ? 4f : null) : null);
            
            // wait until next turn or continue turn to restart or resume the countdown
            countdown.isPaused = true;

            // reset hit data
            attackPanel.ResetData();
        }
    }
    
    public void RandomBotAttack() {
        Debug.Log("Random bot attack");

        // var botShips = shipManager.enemyShips;
        // var botShip = botShips[Random.Range(0, botShips.Count)];
        // while (!botShip.hasAmmoLeft || !botShip.isAlive) {
        //     botShip = botShips[Random.Range(0, botShips.Count)];
        // }

        var botTargets = TurnManager.instance.playerTeam.teamBase.hexMap.hexes;
        var botTarget = botTargets[Random.Range(0, botTargets.Count)];
        while (!((botTarget.shipSegment?.isAlive ?? true) && botTarget.hexRenderer.gameObject.activeSelf)) {
            botTarget = botTargets[Random.Range(0, botTargets.Count)];
        }
        
        GetAttackFromEnemy(TurnManager.instance.playerTeam.teamType, botTarget.coords.index, true);
    }

    public delegate void AttackEvent(Team against, bool hit, int hexIndex, bool finalAttack = false);

    public event AttackEvent OnAttack;

    public void GoBackToAttackPattern() {
        if (attackState == AttackState.Confirm) {
            if (selectedTarget != null) {
                foreach (var hex in selectedTarget.hexRenderer.hexMap.hexes) {
                    if (hex == selectedTarget) continue;
                    hex.hexRenderer.RemoveFogOverrideColor();
                }
            }

            selectedPattern = null;
            
            SetState(AttackState.SelectAttackPattern);

            // go back further if there is only one or no attack options
            // or if the current attack option has no ammo left
            if (selectedOption.info.options.Length == 1 || selectedOption.info.options.Length == 0 || (selectedOption.ammoLeft <= 0 && !selectedOption.info.unlimited) || !selectedOption.info.isAllowed) {
                GoBackToAttackOption();
            }
            else {
                GenerateAttackPatterns();
            }
        }
    }

    public void GoBackToAttackOption() {
        if (attackState == AttackState.SelectAttackPattern) {
            if (selectedTarget != null) {
                foreach (var hex in selectedTarget.hexRenderer.hexMap.hexes) {
                    if (hex == selectedTarget) continue;
                    hex.hexRenderer.RemoveFogOverrideColor();
                }
            }

            selectedOption = null;
            selectedPattern = null;

            SetState(AttackState.SelectAttackOption);

            // go back further if there is only one or none attack option
            if (selectedShip.remainingAttacks.Length == 1 || selectedShip.remainingAttacks.Length == 0) {
                GoBackToTarget();
            }
            else {
                GenerateAttackOptions();
            }
        }
    }

    public void GoBackToTarget() {
        if (attackState == AttackState.SelectAttackOption) {
            if (selectedTarget != null) {
                foreach (var hex in selectedTarget.hexRenderer.hexMap.hexes) {
                    hex.hexRenderer.RemoveFogOverrideColor();
                }
            }

            selectedTarget = null;
            selectedOption = null;
            selectedPattern = null;

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
            selectedTarget = null;
            selectedOption = null;
            selectedPattern = null;
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
