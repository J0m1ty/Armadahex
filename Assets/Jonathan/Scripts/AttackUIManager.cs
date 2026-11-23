using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
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

    [Header("UI")]
    [SerializeField]
    private UIDocument document;

    // multi use
    private Label directiveText;
    // the keys that work right now, under the directive
    private Label hintText;
    private Image selectionDisplay;
    private VisualElement selectionUI;
    private VisualElement backButtonParent;
    public Button backButton { get; private set; }
    private VisualElement bottomUI;

    private Label errorText;

    private Ship selectedShip;

    private GridUnit selectedTarget;

    // the ship keys along the bottom, always in fleet order
    private VisualElement fleetParent;
    private List<FleetKey> fleetKeys = new List<FleetKey>();

    // attack options
    private Label selectedShipText;
    private VisualElement optionsParent;
    private Attack selectedOption;

    // attack patterns
    private Label selectedOptionText;
    private VisualElement patternsParent;
    private AttackPattern selectedPattern;
    [SerializeField]
    private ArrowGroup arrowGroup;

    // confirmation
    private Label infoText;
    // the fire key stays in the tray and only lights up once a hex is locked
    public Button confirmButton { get; private set; }

    // what the back and fire buttons do in the current state
    private System.Action backAction;
    private System.Action confirmAction;

    [Header("Game Stats")]
    public int shotsFired;
    public int shotsHit;
    public int advancedShotsFired;

    private BotAI bot;

    // ships always sit on their cards in this order, so a number key always means the same ship
    private static readonly ShipType[] fleetOrder = { ShipType.Carrier, ShipType.Battleship, ShipType.Submarine, ShipType.Cruiser, ShipType.Destroyer };

    // the hex the mouse is over while aiming, previewed until a click locks it in
    private GridUnit aimedTarget;

    // the plain shot when no ship is picked, the fleet's rather than any one ship's
    private Attack fleetShot = new Attack {
        info = new AttackInfo { name = "Single Shot", unlimited = true, options = new AttackPattern[0], isAllowed = true },
        ammoLeft = -1
    };
    [Header("Other")]
    public audioScript gameAudio;
    private VisualElement skipButton;

    void Awake() {
        selector = GetComponent<Selector>();

        shotsFired = 0;
        shotsHit = 0;
        advancedShotsFired = 0;

        var root = document.rootVisualElement;
        directiveText = root.Q<Label>("directiveText");
        hintText = root.Q<Label>("hintText");
        selectionDisplay = root.Q<Image>("shipImage");
        selectionUI = root.Q("selection");
        backButtonParent = root.Q("backContainer");
        backButton = root.Q<Button>("back");
        bottomUI = root.Q("bottom");
        errorText = root.Q<Label>("errorText");
        fleetParent = root.Q("fleet");
        selectedShipText = root.Q<Label>("shipName");
        optionsParent = root.Q("weapons");
        selectedOptionText = root.Q<Label>("optionName");
        patternsParent = root.Q("angles");
        infoText = root.Q<Label>("fireInfo");
        confirmButton = root.Q<Button>("fire");
        skipButton = root.Q("skip");

        UIKit.OnClick(backButton, () => backAction?.Invoke(), ClickSound.Light);
        UIKit.OnClick(confirmButton, () => confirmAction?.Invoke(), ClickSound.Heavy);
    }

    public void SetState(AttackState state) {
        attackState = state;

        // a button clicked earlier would otherwise take the enter key too
        UIKit.Blur(document);

        backAction = null;
        backButton.text = "BACK";

        if (state != AttackState.Error) {
            UIKit.Show(directiveText.parent, false);
            UIKit.Show(errorText, false);
            UIKit.Show(selectionUI, false);
            UIKit.Show(selectedOptionText, false);
            confirmButton.SetEnabled(false);
            UIKit.Show(optionsParent, state == AttackState.SelectAttackOption);
            if (state != AttackState.SelectAttackPattern && state != AttackState.Confirm) ClearChoices(patternsParent);
            arrowGroup.gameObject.SetActive(false);
            UIKit.Show(backButtonParent, false);
            UIKit.Show(skipButton, false);

            confirmAction = null;
            backAction = null;
        }

        var aiming = state == AttackState.SelectShip || state == AttackState.SelectAttackPattern || state == AttackState.Confirm;
        if (!aiming) {
            selector.ClearSelection();
        }

        if (state != AttackState.None) {
            selector.allowSelectingGrids = false;
            selector.allowSelectingShips = false;

            UIKit.Show(skipButton, state != AttackState.AttackOver);

            UIKit.Show(infoText, state == AttackState.Confirm);

            UIKit.Show(bottomUI, true);
        }
        else {
            UIKit.Show(bottomUI, false);
        }

        switch (state) {
            case AttackState.SelectShip:
                // nothing picked: the fleet's plain shot follows the mouse, or pick a ship from the cards
                UIKit.Show(directiveText.parent, true);
                directiveText.text = GameModeInfo.instance.IsAdvancedCombat ? "CLICK A HEX TO AIM, OR PICK A SHIP" : "CLICK A HEX TO AIM";
                hintText.text = GameModeInfo.instance.IsAdvancedCombat ? Range(FleetInOrder().Count) + " SHIP" : "";
                AimingUI();
                if (GameModeInfo.instance.IsAdvancedCombat) {
                    GenerateShipCards();
                }
                break;
            case AttackState.Error:
                UIKit.Show(errorText, true);
                break;
            case AttackState.SelectAttackOption:
                // a ship with more than one special picks which first
                UIKit.Show(directiveText.parent, true);
                directiveText.text = "PICK A WEAPON";
                hintText.text = Range(Specials(selectedShip).Count) + " WEAPON   " + KeyHint.Key("Gameplay/Cancel") + " BACK";
                AimingUI();
                backAction = Back;
                UIKit.Show(backButtonParent, true);
                break;
            case AttackState.SelectAttackPattern:
                UIKit.Show(directiveText.parent, true);
                directiveText.text = "CLICK A HEX TO LOCK";
                hintText.text = AngleHint() + KeyHint.Key("Gameplay/Cancel") + " BACK";
                AimingUI();
                backAction = Back;
                UIKit.Show(backButtonParent, true);
                break;
            case AttackState.Confirm:
                UIKit.Show(directiveText.parent, true);
                directiveText.text = "TARGET LOCKED";
                hintText.text = KeyHint.Key("Gameplay/Fire") + " FIRE   " + AngleHint() + KeyHint.Key("Gameplay/Cancel") + " UNLOCK";
                AimingUI();
                confirmButton.SetEnabled(true);
                confirmAction = ConfirmAttack;
                backAction = Back;
                backButton.text = "UNLOCK";
                UIKit.Show(backButtonParent, true);
                break;
            case AttackState.AttackOver:
                UIKit.Show(bottomUI, false);
                break;
            default:
                break;
        }

        MarkFleet();
    }

    // "[1]-[5]", the keys for the numbers on screen
    private static string Range(int count) {
        if (count <= 1) return KeyHint.Key("Gameplay/Choice 1");
        return KeyHint.Key("Gameplay/Choice 1") + "-" + KeyHint.Key("Gameplay/Choice " + Mathf.Min(count, 5));
    }

    private string AngleHint() {
        var count = selectedOption != null ? selectedOption.info.options.Length : 0;
        return count > 1 ? Range(count) + " ANGLE   " : "";
    }

    // every aiming state keeps the enemy board clickable, and the weapon strip shows once a ship is picked
    // (the selector's team is set once a turn, setting it again would drop a locked target)
    private void AimingUI() {
        selector.allowSelectingGrids = true;

        if (GameModeInfo.instance.IsAdvancedCombat) {
            UIKit.Show(selectionUI, selectedShip != null);
            UIKit.Show(selectedOptionText, selectedShip != null && selectedOption != null);
        }
    }

    // the picked ship's key stays lit, and the numbers only show while they pick ships
    private void MarkFleet() {
        var aiming = attackState == AttackState.SelectShip || attackState == AttackState.SelectAttackOption
            || attackState == AttackState.SelectAttackPattern || attackState == AttackState.Confirm;
        UIKit.Show(fleetParent, aiming && GameModeInfo.instance.IsAdvancedCombat);

        var fleet = FleetInOrder();
        for (var i = 0; i < fleetKeys.Count && i < fleet.Count; i++) {
            if (fleetKeys[i] == null) continue;
            fleetKeys[i].SetSelected(fleet[i] == selectedShip);
            fleetKeys[i].ShowNumber(attackState == AttackState.SelectShip);
        }
    }

    private List<Ship> FleetInOrder() {
        var fleet = new List<Ship>();
        foreach (var type in fleetOrder) {
            var ship = shipManager.playerShips.Find(s => s.shipModel.type == type);
            if (ship != null) fleet.Add(ship);
        }
        // anything without a slot goes on the end
        foreach (var ship in shipManager.playerShips) {
            if (!fleet.Contains(ship)) fleet.Add(ship);
        }
        return fleet;
    }

    // a ship's specials: everything but its plain shot
    private List<Attack> Specials(Ship ship) {
        return ship.attacks.Where(a => !a.info.unlimited && a.info.isAllowed).ToList();
    }

    private static bool Ready(Attack attack) {
        return attack.info.unlimited || attack.ammoLeft > 0;
    }

    // the ship's own plain shot, or the fleet's when it has none
    private Attack PlainShot(Ship ship) {
        var own = ship != null ? ship.attacks.FirstOrDefault(a => a.info.unlimited) : null;
        return own ?? fleetShot;
    }

    // the old tab key: a random ship that still has a special
    public void SelectRandomShip() {
        var fleet = FleetInOrder().FindAll(s => s.isAlive && Specials(s).Any(Ready));
        if (fleet.Count > 0) PickShip(fleet[Random.Range(0, fleet.Count)]);
    }

    public void SelectShip(Ship ship) {
        PickShip(ship);
    }

    // one special goes straight to aiming it, several get a list first, none just fires plain shots
    private void PickShip(Ship ship) {
        if (attackState != AttackState.SelectShip || !GameModeInfo.instance.IsAdvancedCombat) return;

        if (!ship.isAlive) {
            UIKit.Show(errorText, true);
            errorText.text = "This ship is sunk";
            return;
        }

        selectedShip = ship;
        var specials = Specials(ship);
        var ready = specials.Where(Ready).ToList();

        if (ready.Count == 0) {
            ChooseAttack(PlainShot(ship));
        }
        else if (specials.Count == 1) {
            ChooseAttack(ready[0]);
        }
        else {
            SetState(AttackState.SelectAttackOption);
            GenerateAttackOptions();
        }
    }

    // number keys pick whatever is numbered on screen: ships, then weapons, then angles
    public void ChooseNumber(int number) {
        var index = number - 1;
        if (index < 0) return;

        switch (attackState) {
            case AttackState.SelectShip:
                var fleet = FleetInOrder();
                if (index < fleet.Count) PickShip(fleet[index]);
                break;
            case AttackState.SelectAttackOption:
                var buttons = ChoiceButtons(optionsParent);
                if (index < buttons.Count && buttons[index].enabledSelf) UIKit.Click(buttons[index]);
                break;
            case AttackState.SelectAttackPattern:
            case AttackState.Confirm:
                if (selectedOption != null && index < selectedOption.info.options.Length) SetPattern(index);
                break;
        }
    }

    // esc steps back one level: unlock, then the weapon list, then the fleet
    public void Back() {
        switch (attackState) {
            case AttackState.Confirm:
                Unlock();
                break;
            case AttackState.SelectAttackPattern:
                Preview(null);
                aimedTarget = null;
                if (selectedShip != null && Specials(selectedShip).Count > 1 && Specials(selectedShip).Any(Ready)) {
                    selectedOption = null;
                    selectedPattern = null;
                    SetState(AttackState.SelectAttackOption);
                    GenerateAttackOptions();
                }
                else {
                    BackToFleet();
                }
                break;
            case AttackState.SelectAttackOption:
                BackToFleet();
                break;
        }
    }

    public void BackToFleet() {
        Preview(null);
        selectedShip = null;
        selectedOption = null;
        selectedPattern = null;
        aimedTarget = null;
        SetState(AttackState.SelectShip);
    }

    // only a sunk ship is greyed out, an empty one still fires plain shots
    public void GenerateShipCards() {
        ClearChoices(fleetParent);
        fleetKeys.Clear();

        var fleet = FleetInOrder();
        for (var i = 0; i < fleet.Count; i++) {
            var ship = fleet[i];
            var key = new FleetKey(fleetParent);

            // the specials it has left
            var left = Specials(ship).Where(Ready).Sum(a => a.ammoLeft);
            key.Set(i + 1, ship.shipModel.display, ShortName(ship.shipModel.type), ship.isAlive, left);

            // clicking another ship swaps to it, clicking the picked one puts it back
            UIKit.OnClick(key.button, () => {
                var swap = attackState != AttackState.SelectShip;
                var same = ship == selectedShip;
                if (swap) BackToFleet();
                if (!same) PickShip(ship);
            });
            fleetKeys.Add(key);
        }
        MarkFleet();
    }

    private static string ShortName(ShipType type) {
        switch (type) {
            case ShipType.Carrier: return "CARRIER";
            case ShipType.Submarine: return "SUB";
            default: return type.ToString().ToUpper();
        }
    }

    // the weapon list for a ship with more than one special, used ones say spent
    public void GenerateAttackOptions() {
        ShowShip();
        UIKit.Show(selectedOptionText, false);

        ClearChoices(optionsParent);
        var number = 1;
        foreach (var attack in Specials(selectedShip)) {
            var ready = Ready(attack);
            var label = attack.info.name.ToUpper();
            var chosen = attack;
            AddChoice(optionsParent, number++, label, ready ? attack.ammoLeft + " LEFT" : "SPENT", ready, () => ChooseAttack(chosen));
        }
    }

    private void ShowShip() {
        UIKit.Show(selectionDisplay.parent, true);
        selectionDisplay.sprite = selectedShip.shipModel.display;
        selectedShipText.text = selectedShip.shipModel.name;
    }

    // a weapon goes to its angles, or straight to aiming when it only has the one shape
    private void ChooseAttack(Attack attack) {
        selectedOption = attack;
        selectedPattern = null;
        aimedTarget = null;
        SetState(AttackState.SelectAttackPattern);
        GenerateAttackPatterns();
        SetPattern(0);
    }

    public void GenerateAttackPatterns() {
        ClearChoices(patternsParent);

        if (selectedShip == null) return;

        ShowShip();
        selectedOptionText.text = selectedOption.info.name.ToUpper() + (selectedOption.info.unlimited ? "" : "  ·  " + selectedOption.ammoLeft + " LEFT");
        UIKit.Show(selectedOptionText, true);

        // one shape needs no buttons
        var patterns = selectedOption.info.options;
        if (patterns.Length < 2) return;

        for (var i = 0; i < patterns.Length; i++) {
            var index = i;
            AddChoice(patternsParent, i + 1, patterns[i].name.ToUpper(), "", true, () => SetPattern(index));
        }
    }

    // an angle only changes the aim, nothing fires until a hex is locked in and confirmed
    public void SetPattern(int index) {
        if (selectedOption == null) return;

        var patterns = selectedOption.info.options;
        selectedPattern = index >= 0 && index < patterns.Length ? patterns[index] : null;

        var buttons = ChoiceButtons(patternsParent);
        for (var i = 0; i < buttons.Count; i++) {
            MarkChosen(buttons[i], i == index);
        }

        Preview(selectedTarget ?? aimedTarget);
        GenerateConfirmationInfo();
    }

    // the selector calls this as the mouse moves over the enemy board
    public void AimAt(GridUnit target) {
        if (attackState != AttackState.SelectShip && attackState != AttackState.SelectAttackPattern) return;
        if (target == aimedTarget) return;

        aimedTarget = target;
        Preview(target);
    }

    private void Preview(GridUnit at) {
        ClearPreviewFog(at);

        if (at == null || selectedPattern == null) {
            arrowGroup.gameObject.SetActive(false);
            return;
        }

        arrowGroup.gameObject.SetActive(true);
        arrowGroup.SetPatternPreview(at, selectedOption, selectedPattern);

        // shapes have no arrows and spaced patterns have gaps, so both light the exact hexes they'll hit;
        // a sweep doesn't hit anything, so it shows in blue
        var scan = selectedOption.info.isScan;
        var shape = selectedPattern.cells != null && selectedPattern.cells.Length > 0;
        if (scan || shape || selectedPattern.directions.Any(d => d.step > 1)) {
            ClearPreviewFog(at);
            var tint = Color.Lerp(scan ? FogState.fogScan : FogState.friendlySelected, FogState.fogNormal, 0.3f);
            foreach (var hex in PatternTargets(at, selectedPattern, false, out _)) {
                if (hex.gridRef == at) continue;
                hex.SetCustomFogColor(tint);
            }
        }
        if (scan) {
            arrowGroup.SetColor(FogState.fogScan * 2f);
        }
    }

    private void ClearPreviewFog(GridUnit keep) {
        foreach (var hex in TurnManager.instance.enemyTeam.teamBase.hexMap.hexes) {
            if (hex == keep || hex == selectedTarget) continue;
            hex.hexRenderer.RemoveFogOverrideColor();
        }
    }

    // a click on the enemy board locks the shot in there
    public void SelectTarget(GridUnit target) {
        var aimingState = attackState == AttackState.SelectShip || attackState == AttackState.SelectAttackPattern || attackState == AttackState.Confirm;
        if (!aimingState) return;

        if (target.hexRenderer.hexMap.teamBase.team.isPlayer) {
            UIKit.Show(errorText, true);
            errorText.text = "You can't select a friendly target";
            return;
        }

        // nothing picked fires the fleet's plain shot
        if (selectedOption == null) {
            selectedOption = fleetShot;
            selectedPattern = null;
        }

        selectedTarget = target;
        SetState(AttackState.Confirm);
        RemarkPattern();
        Preview(target);
        GenerateConfirmationInfo();
    }

    // back to aiming with the mouse, the ship and angle stay
    public void Unlock() {
        if (attackState != AttackState.Confirm) return;

        if (selectedTarget != null) selectedTarget.hexRenderer.RemoveFogOverrideColor();
        selectedTarget = null;
        aimedTarget = null;
        selector.ClearSelection();

        if (selectedShip == null) {
            selectedOption = null;
            selectedPattern = null;
            SetState(AttackState.SelectShip);
        }
        else {
            SetState(AttackState.SelectAttackPattern);
            GenerateAttackPatterns();
            RemarkPattern();
        }
        Preview(null);
    }

    private void RemarkPattern() {
        if (selectedOption == null) return;

        var index = selectedPattern != null ? System.Array.IndexOf(selectedOption.info.options, selectedPattern) : 0;
        var buttons = ChoiceButtons(patternsParent);
        for (var i = 0; i < buttons.Count; i++) {
            MarkChosen(buttons[i], i == index);
        }
    }

    public void GenerateConfirmationInfo() {
        if (attackState == AttackState.Confirm) {
            // dont show shots left if unlimited
            if (infoText == null) return;

            UIKit.Show(infoText, !selectedOption.info.unlimited);
            infoText.text = selectedOption.ammoLeft + " left";
        }
    }

    // every hex a pattern hits from a target, in order. peek stops at the first ship for patterns that do,
    // previews pass false so they never give a ship away
    public static List<HexRenderer> PatternTargets(GridUnit target, AttackPattern pattern, bool peek, out bool stopAtHit) {
        stopAtHit = false;
        var targets = new List<HexRenderer>();

        if (pattern == null) {
            targets.Add(target.hexRenderer);
            return targets;
        }

        if (pattern.cells != null && pattern.cells.Length > 0) {
            foreach (var cell in pattern.cells) {
                var unit = Offset(target, cell);
                if (unit != null && unit.hexRenderer.gameObject.activeSelf) targets.Add(unit.hexRenderer);
            }
            return targets.Distinct().ToList();
        }

        if (pattern.attackCenter && !pattern.doOffset) {
            targets.Add(target.hexRenderer);
        }

        foreach (AttackDirection direction in pattern.directions) {
            if (direction.stopAtHit) {
                stopAtHit = true;
            }

            var origin = target;
            bool reverse = direction.reverse;

            if (pattern.doOffset) {
                if (pattern.autoOptimize) {
                    var testOne = target.RangeInDirection(Convert(direction.rotation, reverse), true);
                    var testTwo = target.RangeInDirection(Convert(direction.rotation, !reverse), true);

                    if (testOne < testTwo) {
                        reverse = !reverse;
                    }
                }

                // go back to the edge of the board
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

            var step = Mathf.Max(1, direction.step);
            var n = 0;
            while (true) {
                for (var i = 0; i < step && origin != null; i++) {
                    origin = origin.GetNeighbor(Convert(direction.rotation, reverse));
                }
                if (origin == null) {
                    break;
                }

                targets.Add(origin.hexRenderer);

                var isHit = peek && (origin.shipSegment?.isAlive ?? false);
                if ((!direction.noRange && ++n >= direction.length) || (direction.stopAtHit && isHit)) {
                    break;
                }
            }
        }

        return targets.Distinct().ToList();
    }

    // the hex a shape cell lands on, null off the board (neighbour 0 is cube (1, -1, 0), neighbour 1 is (1, 0, -1))
    public static GridUnit Offset(GridUnit from, Vector2Int cell) {
        var c = from.coords;
        var cube = new Cube(c.q + cell.x + cell.y, c.r - cell.x, c.s - cell.y);
        return from.hexRenderer.hexMap.FromCoordinatesBrute(new CoordinateSystem(cube));
    }

    // a sweep shows whether anything is in the area: every open hex a miss, or a blue contact in the middle
    private bool Sweep(List<HexRenderer> area) {
        var contact = area.Any(h => h.gridRef.shipSegment != null && h.gridRef.shipSegment.isAlive);
        if (contact) {
            selectedTarget.hexRenderer.SetFogColorInstant(FogColor.Scan, false);
            ShotEffects.Contact(selectedTarget.hexRenderer);
            return true;
        }

        foreach (var hex in area) {
            // hexes already hit keep their hit
            if (hex.gridRef.shipSegment != null || (hex.fog != null && hex.fog.cleared)) continue;

            hex.SetFogColorInstant(FogColor.Miss, false);
            hex.ClearFog();
            ShotEffects.Shot(hex, false);
        }
        return false;
    }

    private void ClearChoices(VisualElement parent) {
        parent.Clear();
    }

    // a numbered choice (a weapon or an angle): its key, a label and a small note
    private Button AddChoice(VisualElement parent, int number, string label, string detail, bool ready, System.Action onClick) {
        var text = KeyHint.Key("Gameplay/Choice " + number) + " " + (ready ? label : "<s>" + label + "</s>");
        if (!string.IsNullOrEmpty(detail)) text += "  " + detail;

        var button = new Button { text = text, focusable = false };
        button.SetEnabled(ready);
        if (ready) UIKit.OnClick(button, onClick, ClickSound.Light);
        parent.Add(button);
        return button;
    }

    // the chosen angle stays lit
    private void MarkChosen(Button button, bool chosen) {
        UIKit.SetLit(button, chosen);
    }

    private List<Button> ChoiceButtons(VisualElement parent) {
        return parent.Children().OfType<Button>().ToList();
    }

    public void SkipTurn() {
        if (TurnManager.instance.currentTeam.isPlayer && !countdown.isPaused) {
            RandomAttack();
        }
    }

    public void RandomAttack() {
        RandomAttack(AttackState.SelectShip);
    }

    // fills in whatever the player hasn't picked yet and fires, for skipping and running out of time
    public void RandomAttack(AttackState startAt) {
        if (attackState == AttackState.None || attackState == AttackState.AttackOver) {
            return;
        }

        // nothing picked is the fleet's plain shot
        if (selectedOption == null) {
            selectedOption = fleetShot;
        }

        var patterns = selectedOption.info.options;
        if (selectedPattern == null && patterns.Length > 0) {
            selectedPattern = patterns[Random.Range(0, patterns.Length)];
        }

        if (selectedTarget == null) {
            var targets = TurnManager.instance.enemyTeam.teamBase.hexMap.hexes;
            var target = targets[Random.Range(0, targets.Count)];
            var valid = (target.shipSegment?.isAlive ?? true) && target.hexRenderer.gameObject.activeSelf;
            while (!valid) {
                target = targets[Random.Range(0, targets.Count)];
                valid = (target.shipSegment?.isAlive ?? true) && target.hexRenderer.gameObject.activeSelf;
            }
            selectedTarget = target;
        }

        attackState = AttackState.Confirm;
        ConfirmAttack();
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

            var targets = PatternTargets(selectedTarget, selectedPattern, true, out var stopAtHit);

            var hit = false;
            bool? destroyed = null;
            
            var sweep = selectedOption.info.isScan;
            var contact = sweep && Sweep(targets);

            for (int i = 0; i < targets.Count && !sweep; i++) {
                var target = targets[i];

                // the clouds and pegs show what happened, so the enemy cells keep their color
                if (target.gridRef.shipSegment == null) {
                    target.SetFogColorInstant(FogColor.Miss, false);
                    target.ClearFog();
                    ShotEffects.Shot(target, false);
                    OnAttack?.Invoke(TurnManager.instance.enemyTeam, false, target.coords.index, i == targets.Count - 1);
                }
                else if (target.gridRef.shipSegment.isAlive) {
                    target.SetFogColorInstant(FogColor.Hit, false);
                    target.EnableFlames();
                    ShotEffects.Shot(target, true);
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

            // the shot is away, so drop the locked in target
            Selector.instance.ClearSelection();

            if (!GameOver.instance.CheckIfGameOver()) {
                attackPanel.ResetData();
                var name = selectedShip != null ? selectedShip.shipModel.attackName : "FLEET";
                if (sweep) {
                    attackPanel.SetScanInfo(contact, name, selectedOption.ammoLeft);
                }
                else {
                    attackPanel.SetAttackInfo(hit, destroyed, name, selectedOption.info.unlimited ? 1000 : selectedOption.ammoLeft );
                }
                // only a hit shakes the camera
                if (hit) {
                    CameraManager.instance.Shake(true);
                }
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
        // your board shows misses with pegs and hits with the glowing hull, so no cell colors
        if (hex.gridRef.shipSegment == null) {
            ShotEffects.Shot(hex, false);
        }
        else if (hex.gridRef.shipSegment.isAlive) {
            destroyed = hex.gridRef.shipSegment.Destroy();
            ShotEffects.Shot(hex, true);
            hit = true;
        }

        if (GameModeInfo.instance.IsSingleplayer && finalAttack) {
            if (GameModeInfo.instance.IsBonus) {
                if (hit || attackPanel.persistingHit) {
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
        }

        GameOver.instance.CheckIfGameOver(TurnManager.instance.playerTeam, hit, hex.coords.index, finalAttack);

        attackPanel.SetAttackInfo_Save(hit, destroyed, null, null);
        
        if (finalAttack && !GameOver.instance.CheckIfGameOver()) {
            if (attackPanel.persistingHit) {
                CameraManager.instance.Shake(true);
            }
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
        if (bot == null) {
            bot = new BotAI(BotAI.SavedDifficulty, TurnManager.instance.playerTeam.teamBase.hexMap);
        }

        Debug.Log("Bot attack (" + bot.difficulty + ")");

        // var botShips = shipManager.enemyShips;
        // var botShip = botShips[Random.Range(0, botShips.Count)];
        // while (!botShip.hasAmmoLeft || !botShip.isAlive) {
        //     botShip = botShips[Random.Range(0, botShips.Count)];
        // }

        var shipLengths = shipManager.playerShips.Where(s => s.isAlive).Select(s => s.shipModel.length).ToList();
        var attacks = new List<Attack>();
        if (GameModeInfo.instance.IsAdvancedCombat) {
            // the bot doesn't sweep, it only fires
            attacks = shipManager.enemyShips.Where(s => s.isAlive).SelectMany(s => s.remainingAttacks).Where(a => !a.info.unlimited && !a.info.isScan).ToList();
        }

        var botTargets = bot.ChooseShot(shipLengths, attacks, out var attack);
        if (botTargets == null) return;

        if (attack != null) {
            attack.ammoLeft--;
            Debug.Log("Bot used " + attack.info.name);
        }

        var hits = botTargets.Select(t => t.shipSegment?.isAlive ?? false).ToList();
        
        for (int i = 0; i < botTargets.Count; i++) {
            GetAttackFromEnemy(TurnManager.instance.playerTeam.teamType, botTargets[i].coords.index, i == botTargets.Count - 1);
        }

        for (int i = 0; i < botTargets.Count; i++) {
            bot.Record(botTargets[i], hits[i]);
        }
    }

    public delegate void AttackEvent(Team against, bool hit, int hexIndex, bool finalAttack = false);

    public event AttackEvent OnAttack;

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
