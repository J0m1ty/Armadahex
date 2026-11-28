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
    [Header("UI")]
    [SerializeField]
    private UIDocument document;

    private Ship selectedShip;

    private GridUnit selectedTarget;

    // a row per ship down the left, always in fleet order; the picked one opens into weapons and angles
    private VisualElement fleetParent;
    private List<FleetKey> fleetKeys = new List<FleetKey>();

    private Attack selectedOption;
    private AttackPattern selectedPattern;
    [SerializeField]
    private ArrowGroup arrowGroup;

    // the picked ship's numbered weapon rows
    private List<Button> weaponButtons = new List<Button>();

    // fire stays at the bottom of your side and only lights up once a hex is locked
    public Button confirmButton { get; private set; }

    // what esc and fire do in the current state
    private System.Action backAction;
    private System.Action confirmAction;

    [Header("Game Stats")]
    public int shotsFired;
    public int shotsHit;
    public int advancedShotsFired;

    private BotAI bot;

    // ships always sit in this order, so a number key always means the same ship
    private static readonly ShipType[] fleetOrder = { ShipType.Carrier, ShipType.Battleship, ShipType.Submarine, ShipType.Cruiser, ShipType.Destroyer };

    // the hex the mouse is over while aiming, previewed until a click locks it in
    private GridUnit aimedTarget;

    // the enemy's shots in one volley add up to one line in the feed
    private bool volleyHit;
    private string volleyHitShip;
    private string volleySunk;

    // the plain shot when no ship is picked, the fleet's rather than any one ship's
    private Attack fleetShot = new Attack {
        info = new AttackInfo { name = "Single Shot", unlimited = true, options = new AttackPattern[0], isAllowed = true },
        ammoLeft = -1
    };
    [Header("Other")]
    public audioScript gameAudio;

    void Awake() {
        selector = GetComponent<Selector>();

        shotsFired = 0;
        shotsHit = 0;
        advancedShotsFired = 0;

        var root = document.rootVisualElement;
        fleetParent = root.Q("fleet");
        confirmButton = root.Q<Button>("fire");

        UIKit.OnClick(confirmButton, Fire, ClickSound.Heavy);
    }

    // enter
    public void Fire() {
        confirmAction?.Invoke();
    }

    // esc steps back if there's anything to step back from
    public bool StepBack() {
        if (backAction == null) return false;
        backAction();
        return true;
    }

    public void SetState(AttackState state) {
        attackState = state;

        // a button clicked earlier would otherwise take the enter key too
        UIKit.Blur(document);

        backAction = null;
        confirmAction = null;
        confirmButton.SetEnabled(false);
        if (state != AttackState.SelectAttackPattern && state != AttackState.Confirm) {
            arrowGroup.gameObject.SetActive(false);
        }

        var aiming = state == AttackState.SelectShip || state == AttackState.SelectAttackPattern || state == AttackState.Confirm;
        if (!aiming) {
            selector.ClearSelection();
        }

        if (state != AttackState.None) {
            selector.allowSelectingGrids = false;
            selector.allowSelectingShips = false;
        }

        switch (state) {
            case AttackState.SelectShip:
                // nothing picked: the fleet's plain shot follows the mouse, or pick a ship
                selector.allowSelectingGrids = true;
                if (GameModeInfo.instance.IsAdvancedCombat) {
                    GenerateShipCards();
                }
                break;
            case AttackState.SelectAttackOption:
                // a ship with more than one special picks which first
                selector.allowSelectingGrids = true;
                backAction = Back;
                break;
            case AttackState.SelectAttackPattern:
                selector.allowSelectingGrids = true;
                backAction = Back;
                break;
            case AttackState.Confirm:
                selector.allowSelectingGrids = true;
                confirmButton.SetEnabled(true);
                confirmAction = ConfirmAttack;
                backAction = Back;
                break;
        }

        UIKit.Show(fleetParent, GameModeInfo.instance.IsAdvancedCombat);
        MarkFleet();
    }

    public static string ShortName(ShipType type) {
        switch (type) {
            case ShipType.Carrier: return "CARRIER";
            case ShipType.Submarine: return "SUB";
            default: return type.ToString().ToUpper();
        }
    }

    // the picked ship's row stays lit with its weapons open, and the numbers only show while they pick ships
    private void MarkFleet() {
        var fleet = FleetInOrder();
        for (var i = 0; i < fleetKeys.Count && i < fleet.Count; i++) {
            if (fleetKeys[i] == null) continue;
            fleetKeys[i].ShowNumber(attackState == AttackState.SelectShip);
            if (fleet[i] == selectedShip) FillSub(fleetKeys[i].sub);
            else fleetKeys[i].sub.Clear();
            fleetKeys[i].SetSelected(fleet[i] == selectedShip);
        }
    }

    private List<Ship> FleetInOrder() {
        return InFleetOrder(shipManager.playerShips);
    }

    public static List<Ship> InFleetOrder(List<Ship> ships) {
        var fleet = new List<Ship>();
        foreach (var type in fleetOrder) {
            var ship = ships.Find(s => s.shipModel.type == type);
            if (ship != null) fleet.Add(ship);
        }
        // anything without a slot goes on the end
        foreach (var ship in ships) {
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
        if (!ship.isAlive) return;

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
                if (index < weaponButtons.Count && weaponButtons[index].enabledSelf) UIKit.Click(weaponButtons[index]);
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
        fleetParent.Clear();
        fleetKeys.Clear();

        var fleet = FleetInOrder();
        for (var i = 0; i < fleet.Count; i++) {
            var ship = fleet[i];
            var key = new FleetKey(fleetParent);
            key.Set(i + 1, ship.shipModel.silhouette, ShortName(ship.shipModel.type), ship.isAlive);

            // clicking another ship swaps to it, clicking the picked one puts it back
            UIKit.OnClick(key.button, () => {
                var swap = attackState != AttackState.SelectShip;
                var same = ship == selectedShip;
                if (swap) BackToFleet();
                if (!same) PickShip(ship);
            });
            fleetKeys.Add(key);
        }
    }

    // under the picked ship: its weapons with the ammo beside each, and the picked weapon's angles under it
    private void FillSub(VisualElement sub) {
        sub.Clear();
        weaponButtons.Clear();
        if (selectedShip == null) return;

        var number = 1;
        foreach (var attack in Specials(selectedShip)) {
            var chosen = attack;
            var ready = Ready(attack);
            var lit = attack == selectedOption;

            var row = new Button { focusable = false };
            row.AddToClassList("weapon-row");
            UIKit.SetLit(row, lit);
            var key = new KeyLabel { format = "{0}", action = "Gameplay/Choice " + number++ };
            key.AddToClassList("key-chip");
            var name = new Label(attack.info.name.ToUpper()) { pickingMode = PickingMode.Ignore };
            name.AddToClassList("weapon-row__name");
            var ammo = new Label(ready ? "×" + attack.ammoLeft : "") { pickingMode = PickingMode.Ignore };
            ammo.AddToClassList("weapon-row__ammo");
            row.Add(key);
            row.Add(name);
            row.Add(ammo);
            row.SetEnabled(ready);
            if (ready) UIKit.OnClick(row, () => {
                if (attackState == AttackState.Confirm) Unlock();
                ChooseAttack(chosen);
            }, ClickSound.Light);
            sub.Add(row);
            weaponButtons.Add(row);

            if (lit && attack.info.options.Length > 1) {
                var angles = new VisualElement { pickingMode = PickingMode.Ignore };
                angles.AddToClassList("angles");
                for (var i = 0; i < attack.info.options.Length; i++) {
                    var index = i;
                    var pattern = attack.info.options[i];
                    var on = pattern == selectedPattern;
                    var angle = new Button { focusable = false };
                    angle.AddToClassList("angle");
                    var shape = new PatternShape();
                    shape.Set(pattern, on ? new Color(0.93f, 0.95f, 0.98f) : new Color(0.59f, 0.67f, 0.77f));
                    var chip = new KeyLabel { format = "{0}", action = "Gameplay/Choice " + (i + 1) };
                    chip.AddToClassList("key-chip");
                    angle.Add(shape);
                    angle.Add(chip);
                    UIKit.SetLit(angle, on);
                    UIKit.OnClick(angle, () => SetPattern(index), ClickSound.Light);
                    angles.Add(angle);
                }
                sub.Add(angles);
            }
        }
    }

    // a weapon goes to its angles, or straight to aiming when it only has the one shape
    private void ChooseAttack(Attack attack) {
        selectedOption = attack;
        selectedPattern = null;
        aimedTarget = null;
        SetState(AttackState.SelectAttackPattern);
        SetPattern(0);
    }

    // an angle only changes the aim, nothing fires until a hex is locked in and confirmed
    public void SetPattern(int index) {
        if (selectedOption == null) return;

        var patterns = selectedOption.info.options;
        selectedPattern = index >= 0 && index < patterns.Length ? patterns[index] : null;
        if (attackState == AttackState.Confirm && Wasted(selectedTarget, selectedOption, selectedPattern)) Unlock();

        MarkFleet();
        Preview(selectedTarget ?? aimedTarget);
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

        // your own board isn't a target
        if (target.hexRenderer.hexMap.teamBase.team.isPlayer) return;

        // nothing picked fires the fleet's plain shot
        if (Wasted(target, selectedOption ?? fleetShot, selectedOption != null ? selectedPattern : null)) return;
        if (selectedOption == null) {
            selectedOption = fleetShot;
            selectedPattern = null;
        }

        selectedTarget = target;
        SetState(AttackState.Confirm);
        Preview(target);
    }

    // a shot that would only land on hexes already shot is wasted, so it can't be locked in; patterns can still overlap old shots
    private static bool Wasted(GridUnit target, Attack option, AttackPattern pattern) {
        return !option.info.isScan && PatternTargets(target, pattern, false, out _).All(h => h.gridRef.attacked);
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
        }
        Preview(null);
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
            // a hit keeps its color, a miss turns into the contact
            if (!selectedTarget.attacked || selectedTarget.shipSegment == null) selectedTarget.hexRenderer.SetFogColorInstant(FogColor.Scan, false);
            ShotEffects.Contact(selectedTarget.hexRenderer);
            return true;
        }

        foreach (var hex in area) {
            // hexes already hit keep their hit
            if (hex.gridRef.shipSegment != null || (hex.fog != null && hex.fog.cleared)) continue;

            // a clear sweep shows these as misses, so they count as shot
            hex.gridRef.attacked = true;
            hex.SetFogColorInstant(FogColor.Miss, false);
            hex.ClearFog();
            ShotEffects.Shot(hex, false);
        }
        return false;
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
            // only hexes that haven't been shot yet, misses included
            var targets = TurnManager.instance.enemyTeam.teamBase.hexMap.hexes.FindAll(x => !x.attacked && x.hexRenderer.gameObject.activeSelf);
            selectedTarget = targets[Random.Range(0, targets.Count)];
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
            string sunk = null;
            
            var sweep = selectedOption.info.isScan;
            var contact = sweep && Sweep(targets);

            for (int i = 0; i < targets.Count && !sweep; i++) {
                var target = targets[i];

                // the clouds and pegs show what happened, so the enemy cells keep their color
                target.gridRef.attacked = true;
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
                    var struck = target.gridRef.shipSegment.parent;
                    destroyed = target.gridRef.shipSegment.Destroy();
                    if (destroyed == true) sunk = ShortName(struck.shipModel.type);
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
                var weapon = selectedOption.info.unlimited ? "SHOT" : selectedOption.info.name;
                if (sweep) {
                    MatchHud.instance.AddShot(true, contact ? MatchHud.ShotKind.Contact : MatchHud.ShotKind.Clear, weapon);
                }
                else {
                    MatchHud.instance.AddShot(true, sunk != null ? MatchHud.ShotKind.Sunk : (hit ? MatchHud.ShotKind.Hit : MatchHud.ShotKind.Miss), sunk ?? weapon);
                }
                // only a hit shakes the camera
                if (hit) {
                    CameraManager.instance.Shake(true);
                }
                AudioManager.instance?.PlayActionSound(ActionType.Explosion);
                AudioManager.instance?.PlayHitSound(hit, 3f);

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
        hex.gridRef.attacked = true;
        // your board shows misses with pegs and hits with the glowing hull, so no cell colors
        if (hex.gridRef.shipSegment == null) {
            ShotEffects.Shot(hex, false);
        }
        else if (hex.gridRef.shipSegment.isAlive) {
            var struck = hex.gridRef.shipSegment.parent;
            destroyed = hex.gridRef.shipSegment.Destroy();
            ShotEffects.Shot(hex, true);
            hit = true;
            volleyHit = true;
            volleyHitShip = ShortName(struck.shipModel.type);
            if (destroyed == true) volleySunk = volleyHitShip;
        }

        if (GameModeInfo.instance.IsSingleplayer && finalAttack) {
            if (GameModeInfo.instance.IsBonus) {
                if (volleyHit) {
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

        if (finalAttack && !GameOver.instance.CheckIfGameOver()) {
            if (volleyHit) {
                CameraManager.instance.Shake(true);
            }
            AudioManager.instance?.PlayActionSound(ActionType.Explosion);
            AudioManager.instance?.PlayHitSound(volleyHit, 3f);
            var kind = volleySunk != null ? MatchHud.ShotKind.Sunk : (volleyHit ? MatchHud.ShotKind.Hit : MatchHud.ShotKind.Miss);
            MatchHud.instance.AddShot(false, kind, volleySunk ?? volleyHitShip ?? "SHOT");
            
            // wait until next turn or continue turn to restart or resume the countdown
            countdown.isPaused = true;
        }

        // the volley is over
        if (finalAttack) {
            volleyHit = false;
            volleyHitShip = null;
            volleySunk = null;
        }
    }
    
    public void RandomBotAttack() {
        if (bot == null) {
            bot = new BotAI(BotAI.MatchDifficulty, TurnManager.instance.playerTeam.teamBase.hexMap);
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
