using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// a copy of one side's board at the end of a match, light enough to carry to the game over screen and draw there
public class BoardSnapshot
{
    public class Cell {
        // the hex's place on the board, flat, in board units
        public Vector2 position;
        public bool land;
        // -1 for open water, otherwise which ship sits here
        public int ship = -1;
        public bool shipSunk;
        public bool hit;
        public bool miss;
    }

    public bool isPlayer;
    public float hexSize;
    public List<Cell> cells = new List<Cell>();
    public int shipCount;

    public int hits => cells.Count(c => c.hit);
    public int misses => cells.Count(c => c.miss);
    public int shots => hits + misses;
    public int shipsSunk => cells.Where(c => c.ship >= 0 && c.shipSunk).Select(c => c.ship).Distinct().Count();
    public int shipsLeft => shipCount - shipsSunk;

    public static BoardSnapshot Take(HexGrid grid, bool isPlayer) {
        var snapshot = new BoardSnapshot { isPlayer = isPlayer, hexSize = grid.size };
        var ships = new Dictionary<Ship, int>();

        foreach (var unit in grid.hexes) {
            var hex = unit.hexRenderer;
            var local = grid.transform.InverseTransformPoint(hex.transform.position);
            var cell = new Cell {
                position = new Vector2(local.x, local.z),
                land = unit.preventShips && !hex.gameObject.activeSelf
            };

            var segment = unit.shipSegment;
            if (segment != null && segment.parent != null) {
                if (!ships.TryGetValue(segment.parent, out var id)) {
                    id = ships.Count;
                    ships[segment.parent] = id;
                }
                cell.ship = id;
                cell.shipSunk = !segment.parent.isAlive;
            }

            cell.hit = unit.attacked && cell.ship >= 0;
            cell.miss = unit.attacked && cell.ship < 0;

            snapshot.cells.Add(cell);
        }

        snapshot.shipCount = ships.Count;
        return snapshot;
    }
}
