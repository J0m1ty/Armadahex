using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum BotDifficulty {
    Easy,
    Medium,
    Hard
}

public class BotAI {
    public BotDifficulty difficulty;

    private HexGrid grid;
    private Dictionary<GridUnit, GridUnit[]> neighbors;

    // what the bot knows about the player's board
    private HashSet<GridUnit> fired;
    private HashSet<GridUnit> hits;
    private HashSet<GridUnit> sunk;

    public static BotDifficulty SavedDifficulty => (BotDifficulty)PlayerPrefs.GetInt(Constants.BOT_DIFFICULTY_PREF_KEY, (int)BotDifficulty.Easy);

    public BotAI(BotDifficulty difficulty, HexGrid grid) {
        this.difficulty = difficulty;
        this.grid = grid;

        fired = new HashSet<GridUnit>();
        hits = new HashSet<GridUnit>();
        sunk = new HashSet<GridUnit>();

        neighbors = new Dictionary<GridUnit, GridUnit[]>();
        foreach (var hex in grid.hexes) {
            var n = new GridUnit[6];
            for (int i = 0; i < 6; i++) {
                n[i] = hex.GetNeighbor(i);
            }
            neighbors[hex] = n;
        }
    }

    private bool IsOpen(GridUnit hex) {
        return hex != null && hex.hexRenderer.gameObject.activeSelf && !fired.Contains(hex);
    }

    public List<GridUnit> ChooseShot(List<int> shipLengths, List<Attack> attacks, out Attack attack) {
        attack = null;

        var open = grid.hexes.Where(IsOpen).ToList();
        if (open.Count == 0) {
            return null;
        }

        switch (difficulty) {
            case BotDifficulty.Medium:
                return new List<GridUnit> { HuntTarget() ?? open[Random.Range(0, open.Count)] };
            case BotDifficulty.Hard:
                return BestShot(open, shipLengths, attacks, out attack);
            default:
                return new List<GridUnit> { open[Random.Range(0, open.Count)] };
        }
    }

    public void Record(GridUnit target, bool hit) {
        fired.Add(target);

        if (!hit) return;

        hits.Add(target);

        // once a ship sinks its hexes are no longer worth chasing
        var ship = target.shipSegment.parent;
        if (!ship.isAlive) {
            foreach (var hex in hits.Where(h => h.shipSegment.parent == ship).ToList()) {
                hits.Remove(hex);
                sunk.Add(hex);
            }
        }
    }

    // shoot around hits, following the line once two hits line up
    private GridUnit HuntTarget() {
        if (hits.Count == 0) return null;

        var inLine = new List<GridUnit>();
        var adjacent = new List<GridUnit>();

        foreach (var hit in hits) {
            for (int dir = 0; dir < 6; dir++) {
                var next = neighbors[hit][dir];
                if (IsOpen(next)) {
                    adjacent.Add(next);
                }

                var back = neighbors[hit][(dir + 3) % 6];
                if (back == null || !hits.Contains(back)) continue;

                // walk forward past the hits in this line
                var end = next;
                while (end != null && hits.Contains(end)) {
                    end = neighbors[end][dir];
                }
                if (IsOpen(end)) {
                    inLine.Add(end);
                }
            }
        }

        var options = inLine.Count > 0 ? inLine : adjacent;
        return options.Count > 0 ? options[Random.Range(0, options.Count)] : null;
    }

    // count every way the remaining ships could still fit
    private Dictionary<GridUnit, int> Weights(List<int> shipLengths) {
        var weights = new Dictionary<GridUnit, int>();
        var cells = new List<GridUnit>();

        foreach (var length in shipLengths) {
            foreach (var start in grid.hexes) {
                for (int dir = 0; dir < 3; dir++) {
                    cells.Clear();
                    var hitCount = 0;
                    var hex = start;

                    while (hex != null && cells.Count < length) {
                        if (!hex.hexRenderer.gameObject.activeSelf || sunk.Contains(hex)) break;
                        if (fired.Contains(hex) && !hits.Contains(hex)) break;

                        if (hits.Contains(hex)) hitCount++;
                        cells.Add(hex);
                        hex = neighbors[hex][dir];
                    }

                    if (cells.Count < length) continue;

                    // placements through known hits are far more likely
                    var weight = hitCount > 0 ? 100 * hitCount : 1;
                    foreach (var cell in cells) {
                        if (fired.Contains(cell)) continue;
                        weights.TryGetValue(cell, out var w);
                        weights[cell] = w + weight;
                    }
                }
            }
        }

        return weights;
    }

    // shoot the busiest hex, or use an advanced attack when it covers much more
    private List<GridUnit> BestShot(List<GridUnit> open, List<int> shipLengths, List<Attack> attacks, out Attack attack) {
        attack = null;

        var weights = Weights(shipLengths);
        if (weights.Count == 0) {
            return new List<GridUnit> { open[Random.Range(0, open.Count)] };
        }

        var best = weights.Values.Max();
        var bestHexes = weights.Where(p => p.Value == best).Select(p => p.Key).ToList();
        var shot = new List<GridUnit> { bestHexes[Random.Range(0, bestHexes.Count)] };

        var bestScore = best * 2;
        foreach (var a in attacks) {
            foreach (var pattern in a.info.options) {
                foreach (var center in grid.hexes) {
                    var cells = PatternCells(center, pattern);
                    if (cells == null) break;

                    var score = cells.Sum(c => weights.TryGetValue(c, out var w) ? w : 0);
                    if (score > bestScore) {
                        bestScore = score;
                        shot = cells;
                        attack = a;
                    }
                }
            }
        }

        return shot;
    }

    // hexes hit by a fixed shape pattern, null for ones that depend on what they hit
    public List<GridUnit> PatternCells(GridUnit center, AttackPattern pattern) {
        if (pattern.doOffset || pattern.directions.Any(d => d.noRange || d.stopAtHit)) return null;

        var cells = new List<GridUnit>();
        if (pattern.attackCenter) {
            cells.Add(center);
        }

        foreach (var direction in pattern.directions) {
            var hex = center;
            for (int i = 0; i < direction.length; i++) {
                hex = neighbors[hex][AttackUIManager.Convert(direction.rotation, direction.reverse)];
                if (hex == null) break;
                cells.Add(hex);
            }
        }

        return cells.Where(c => c.hexRenderer.gameObject.activeSelf).Distinct().ToList();
    }
}
