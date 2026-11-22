using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// metallic hex columns snapped to the board's hex lattice, generated from a synced seed
public class HexPillarTerrain : MonoBehaviour {
    [Header("Materials")]
    [SerializeField] private Material metalMaterial;
    [SerializeField] private Material trimMaterial;
    [SerializeField] private float topRimWidth = 0.18f;

    [Header("Board")]
    [SerializeField] private int clearRadius = 4;
    [SerializeField] private int maxBlockedHexes = 40;
    [SerializeField] private float maxBoardHeight = 9f;
    [SerializeField] private float boardPillarScale = 0.55f;

    [Header("Basin")]
    [SerializeField] private int wallRise = 5;
    [SerializeField] private int wallWidth = 5;
    [SerializeField] private float wallHeight = 160f;
    [SerializeField] private int tailLength = 22;
    [SerializeField] private float tailSoftness = 4f;
    [SerializeField] private float rangeNoiseScale = 0.004f;
    // how many hexes the ranges take to step down to a channel, rather than a sheer cut
    [SerializeField] private float channelSlope = 4f;

    [Header("Pillars")]
    [SerializeField] private float pillarInset = 1.5f;
    [SerializeField] private float heightStep = 3f;
    // tops this close to the water get washed over by the waves, so they're lifted clear or sunk
    [SerializeField] private float waterClearance = 1.5f;
    [SerializeField] private float textureScale = 0.015f;
    [SerializeField] private float seabedDepth = 40f;

    private static readonly Vector2Int[] directions = {
        new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1),
        new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1)
    };

    private float size;
    private List<Vector3> vertices;
    private List<Vector3> normals;
    private List<Vector2> uvs;
    private List<Vector2> glowData;
    private List<int> metalTris;
    private List<int> trimTris;

    public void Generate(TeamBase teamBase, int seed) {
        var rng = new System.Random(seed);
        var grid = teamBase.hexMap;
        var center = grid.transform.position;
        var boardRadius = grid.gridRadius - 1;
        size = grid.size;

        transform.position = new Vector3(center.x, 0f, center.z);

        // board hexes keyed by lattice cell, in grid order so every client walks them the same way
        var board = new Dictionary<Vector2Int, GridUnit>();
        var edgeCells = new List<Vector2Int>();
        foreach (var hex in grid.hexes) {
            var cell = ToCell(hex.hexRenderer.transform.position - center);
            board[cell] = hex;
            if (Distance(cell) >= boardRadius - 1) edgeCells.Add(cell);
        }

        var blocked = new HashSet<Vector2Int>();
        var pillars = new Dictionary<Vector2Int, float>();
        var clearZones = new List<(Vector2Int cell, int radius)>();

        // a basin wall around the board that hides the open sea, then a long gentle tail down into the depths
        // with one ring of open water between the board and the wall so ships don't clip into it
        var wallStart = boardRadius + 2;
        var wallEnd = wallStart + wallRise + wallWidth;
        var outerRadius = wallEnd + tailLength;

        // eat bays into the board edge, carrying on out through the ranges
        var bays = 3;
        for (var i = 0; i < bays; i++) {
            var bay = edgeCells[rng.Next(edgeCells.Count)];
            var radius = rng.Next(1, 3);
            var channelRadius = 1;
            // the bay keeps going out to open sea as a channel that wanders and swells like a river cut
            var pos = CellToLocal(bay);
            var heading = Mathf.Atan2(pos.z, pos.x);
            var wander = rng.Next(0, 10000) * 0.13f;
            var reach = outerRadius * size * Mathf.Sqrt(3f);
            for (var j = 0; pos.magnitude < reach; j++) {
                var bend = Mathf.PerlinNoise(wander + j * 0.18f, 0.5f) * 2f - 1f;
                heading += bend * 0.35f;

                // keep it heading outward so it can't curl back into the basin
                var outward = Mathf.Atan2(pos.z, pos.x);
                heading = outward + Mathf.Clamp(Mathf.DeltaAngle(outward * Mathf.Rad2Deg, heading * Mathf.Rad2Deg) * Mathf.Deg2Rad, -0.9f, 0.9f);

                var swell = Mathf.PerlinNoise(wander + 50f, j * 0.22f) * 2.5f;
                clearZones.Add((ToCell(pos), channelRadius + Mathf.RoundToInt(swell * 0.4f + Mathf.Min(j * 0.04f, 0.6f))));

                pos += new Vector3(Mathf.Cos(heading), 0f, Mathf.Sin(heading)) * size * 1.6f;
            }

            foreach (var cell in board.Keys) {
                if (blocked.Count >= maxBlockedHexes) break;
                if (Distance(cell - bay) <= radius && Distance(cell) > clearRadius) blocked.Add(cell);
            }
        }

        // low pillar clusters on the edge of the board so the middle stays readable
        var clusters = rng.Next(2, 5);
        for (var i = 0; i < clusters; i++) {
            var start = edgeCells[rng.Next(edgeCells.Count)];
            var cells = new List<Vector2Int> { start };
            foreach (var dir in directions) {
                if (rng.NextDouble() < 0.5) cells.Add(start + dir);
            }

            foreach (var cell in cells) {
                if (blocked.Count >= maxBlockedHexes) break;
                if (!board.ContainsKey(cell) || blocked.Contains(cell) || Distance(cell) <= clearRadius + 1) continue;

                blocked.Add(cell);
                var t = (Distance(cell) - clearRadius) / (float)(boardRadius - clearRadius);
                pillars[cell] = Step(2f + t * (maxBoardHeight - 2f) + rng.Next(0, 2) * heightStep);
            }
        }

        foreach (var cell in blocked) {
            var hex = board[cell];
            hex.preventShips = true;
            hex.hexRenderer.gameObject.SetActive(false);
        }

        // only build on our half of the sea so neighbouring bases don't overlap
        var others = new List<Vector3>();
        foreach (var other in FindObjectsByType<TeamBase>(FindObjectsSortMode.None)) {
            if (other != teamBase) others.Add(other.hexMap.transform.position - center);
        }

        var offset = new Vector2(rng.Next(0, 10000) * 0.37f, rng.Next(0, 10000) * 0.37f);
        for (var q = -outerRadius; q <= outerRadius; q++) {
            for (var r = -outerRadius; r <= outerRadius; r++) {
                var cell = new Vector2Int(q, r);
                var dist = Distance(cell);
                var roll = rng.NextDouble();
                var jitter = rng.Next(0, 3);
                if (dist < wallStart || dist > outerRadius) continue;
                // channel floors stay open, and the ranges slope down to them
                var channelEdge = int.MaxValue;
                foreach (var zone in clearZones) {
                    channelEdge = Mathf.Min(channelEdge, Distance(cell - zone.cell) - zone.radius);
                }
                if (channelEdge <= 0) continue;
                var bank = Mathf.SmoothStep(0.15f, 1f, Mathf.Clamp01(channelEdge / channelSlope));

                var pos = CellToLocal(cell);
                if (others.Exists(o => Vector3.Distance(pos, o) < pos.magnitude + size)) continue;

                var noise = 1f - Mathf.Abs(Mathf.PerlinNoise(pos.x * rangeNoiseScale + offset.x, pos.z * rangeNoiseScale + offset.y) * 2f - 1f);
                var tail = Mathf.Max(0f, dist - wallEnd) / tailSoftness;
                if (noise < Mathf.Lerp(0.15f, 0.6f, tail / (tailLength / tailSoftness)) && roll > 0.05) continue;

                // 1 / (1 + x^2) falloff that settles just above the seabed
                var rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(wallStart - 1, wallStart + wallRise, dist));
                var depth = seabedDepth - 6f;
                var height = (wallHeight * rise + depth) / (1f + tail * tail) - depth;
                if (dist < wallEnd) height = wallHeight * rise;

                pillars[cell] = Step((height * (0.45f + noise * 0.75f) + jitter * heightStep) * bank);
            }
        }

        // a few spots out in the ranges the trim glow breathes out from
        var sources = new Vector4[3];
        for (var i = 0; i < sources.Length; i++) {
            var angle = (float)rng.NextDouble() * Mathf.PI * 2f;
            var distance = (boardRadius + 2 + rng.Next(0, 6)) * size * Mathf.Sqrt(3f);
            sources[i] = new Vector4(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, (float)rng.NextDouble(), 0f);
        }

        BuildMesh(pillars, blocked, teamBase, sources);
        BuildSeabed(outerRadius, others);

        Physics.SyncTransforms();
    }

    private void BuildMesh(Dictionary<Vector2Int, float> pillars, HashSet<Vector2Int> onBoard, TeamBase teamBase, Vector4[] sources) {
        vertices = new List<Vector3>();
        normals = new List<Vector3>();
        uvs = new List<Vector2>();
        glowData = new List<Vector2>();
        metalTris = new List<int>();
        trimTris = new List<int>();

        var color = TeamColor.GetSideColor(teamBase.team);

        // the trim shader pulses outward from the sources by each pillar's position
        foreach (var pillar in pillars) {
            var start = vertices.Count;
            var center = CellToLocal(pillar.Key);
            // thinner posts on the board itself, so ships on the next hex over don't clip into them
            var radius = onBoard.Contains(pillar.Key) ? size * boardPillarScale : size - pillarInset;
            AddPillar(center, pillar.Value, radius);

            // foam around everything that breaks the surface
            if (pillar.Value > 0f) ShoreFoam.Add(transform.position + center, radius, color);

            var data = new Vector2(center.x, center.z);
            for (var i = start; i < vertices.Count; i++) glowData.Add(data);
        }

        var mesh = new Mesh();
        mesh.name = "Hex Pillars";
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetUVs(1, glowData);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(metalTris, 0);
        mesh.SetTriangles(trimTris, 1);
        mesh.RecalculateBounds();

        var obj = new GameObject("Pillars", typeof(MeshFilter), typeof(MeshRenderer));
        obj.layer = gameObject.layer;
        obj.transform.SetParent(transform, false);
        obj.GetComponent<MeshFilter>().sharedMesh = mesh;

        var meshRenderer = obj.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = new[] { metalMaterial, trimMaterial };

        var block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", color);
        block.SetVectorArray("_PulseSources", sources);
        meshRenderer.SetPropertyBlock(block, 1);
    }

    private void BuildSeabed(int outerRadius, List<Vector3> others) {
        var extent = (outerRadius + 2) * size * Mathf.Sqrt(3f);
        var min = new Vector2(-extent, -extent);
        var max = new Vector2(extent, extent);
        foreach (var other in others) {
            if (Mathf.Abs(other.x) > Mathf.Abs(other.z)) {
                if (other.x > 0f) max.x = Mathf.Min(max.x, other.x / 2f);
                else min.x = Mathf.Max(min.x, other.x / 2f);
            }
            else {
                if (other.z > 0f) max.y = Mathf.Min(max.y, other.z / 2f);
                else min.y = Mathf.Max(min.y, other.z / 2f);
            }
        }

        var obj = new GameObject("Seabed", typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider));
        obj.layer = gameObject.layer;
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = Vector3.down * seabedDepth;

        var mesh = new Mesh();
        mesh.name = "Seabed";
        mesh.vertices = new[] {
            new Vector3(min.x, 0f, min.y), new Vector3(min.x, 0f, max.y),
            new Vector3(max.x, 0f, max.y), new Vector3(max.x, 0f, min.y)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        obj.GetComponent<MeshFilter>().sharedMesh = mesh;
        obj.GetComponent<MeshRenderer>().sharedMaterial = metalMaterial;

        var box = obj.GetComponent<BoxCollider>();
        box.size = new Vector3(max.x - min.x, 1f, max.y - min.y);
        box.center = new Vector3((min.x + max.x) / 2f, -0.5f, (min.y + max.y) / 2f);
    }

    private void AddPillar(Vector3 center, float top, float radius) {
        var bottom = -seabedDepth;

        AddRing(center, radius, bottom, top, metalTris);

        // metal cap with a glowing rim so the side color shows from the top camera
        var inner = radius * (1f - topRimWidth);
        var c = vertices.Count;
        AddTop(center, Vector3.zero, top);
        for (var k = 0; k < 6; k++) AddTop(center, Corner(k, inner), top);
        for (var k = 0; k < 6; k++) {
            metalTris.Add(c);
            metalTris.Add(c + 1 + (k + 1) % 6);
            metalTris.Add(c + 1 + k);
        }

        for (var k = 0; k < 6; k++) {
            var i = vertices.Count;
            AddTop(center, Corner(k, inner), top);
            AddTop(center, Corner(k, radius), top);
            AddTop(center, Corner(k + 1, radius), top);
            AddTop(center, Corner(k + 1, inner), top);

            trimTris.Add(i);
            trimTris.Add(i + 2);
            trimTris.Add(i + 1);
            trimTris.Add(i);
            trimTris.Add(i + 3);
            trimTris.Add(i + 2);
        }
    }

    private void AddTop(Vector3 center, Vector3 corner, float top) {
        var point = center + corner + Vector3.up * top;
        vertices.Add(point);
        normals.Add(Vector3.up);
        uvs.Add(new Vector2(point.x, point.z) * textureScale);
    }

    private void AddRing(Vector3 center, float radius, float y0, float y1, List<int> tris) {
        for (var k = 0; k < 6; k++) {
            var a = center + Corner(k, radius);
            var b = center + Corner(k + 1, radius);
            var normal = Quaternion.Euler(0f, -(30f + 60f * k), 0f) * Vector3.right;

            var i = vertices.Count;
            vertices.Add(a + Vector3.up * y0);
            vertices.Add(a + Vector3.up * y1);
            vertices.Add(b + Vector3.up * y1);
            vertices.Add(b + Vector3.up * y0);
            for (var n = 0; n < 4; n++) normals.Add(normal);
            uvs.Add(new Vector2(0f, y0) * textureScale);
            uvs.Add(new Vector2(0f, y1) * textureScale);
            uvs.Add(new Vector2(radius, y1) * textureScale);
            uvs.Add(new Vector2(radius, y0) * textureScale);

            tris.Add(i);
            tris.Add(i + 1);
            tris.Add(i + 2);
            tris.Add(i);
            tris.Add(i + 2);
            tris.Add(i + 3);
        }
    }

    // flat topped, corners at 0, 60, 120... degrees like the board hexes
    private Vector3 Corner(int k, float radius) {
        var angle = k * Mathf.PI / 3f;
        return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
    }

    private Vector3 CellToLocal(Vector2Int cell) {
        return new Vector3(size * 1.5f * cell.x, 0f, size * Mathf.Sqrt(3f) * (cell.y + cell.x / 2f));
    }

    private Vector2Int ToCell(Vector3 local) {
        var q = (2f / 3f * local.x) / size;
        var r = (-1f / 3f * local.x + Mathf.Sqrt(3f) / 3f * local.z) / size;
        var s = -q - r;

        var rq = Mathf.Round(q);
        var rr = Mathf.Round(r);
        var rs = Mathf.Round(s);

        var dq = Mathf.Abs(rq - q);
        var dr = Mathf.Abs(rr - r);
        var ds = Mathf.Abs(rs - s);

        if (dq > dr && dq > ds) rq = -rr - rs;
        else if (dr > ds) rr = -rq - rs;

        return new Vector2Int((int)rq, (int)rr);
    }

    private static int Distance(Vector2Int cell) {
        return (Mathf.Abs(cell.x) + Mathf.Abs(cell.y) + Mathf.Abs(cell.x + cell.y)) / 2;
    }

    private float Step(float height) {
        var stepped = Mathf.Round(height / heightStep) * heightStep;
        if (Mathf.Abs(stepped) < waterClearance) stepped = height >= 0f ? waterClearance : -waterClearance;
        return stepped;
    }
}
