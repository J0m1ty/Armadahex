using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CoordinateSystemBase {
    public virtual int index { get; set; } = 0;
    public virtual int layer { get; set; } = 0;
    public virtual int position { get; set; } = 0;
    public virtual int q { get; set; } = 0;
    public virtual int r { get; set; } = 0;
    public virtual int s { get; set; } = 0;
}

public class Spiral : CoordinateSystemBase  {
    public override int index { get; set; }

    public Spiral(int index) {
        this.index = index;

        if (index < 0) {
            throw new System.ArgumentException("index must be greater than or equal to 0");
        }
    }

    public static Spiral FromPolar(Polar polar) {
        int layer = polar.layer;
        int position = polar.position;
        int index = 3 * layer * (layer - 1) + 1 + position;

        return new Spiral(index);
    }

    public static Spiral FromCube(Cube cube) {
        return FromPolar(Polar.FromCube(cube));
    }
}

public class Polar : CoordinateSystemBase {
    public override int layer { get; set; }
    public override int position { get; set; }

    public Polar(int layer, int position) {
        this.layer = layer;
        this.position = position;
    }

    public static Polar FromSpiral(Spiral spiral) {
        if (spiral.index == 0) return new Polar(0, 0);
        
        int index = spiral.index;
        int layer = (int) Mathf.Floor((3 + Mathf.Sqrt(12 * index - 3)) / 6);
        int position = index - 3 * layer * (layer - 1) - 1;

        return new Polar(layer, position);
    }
    
    public static Polar FromCube(Cube cube) {
        int x = cube.q;
        int y = cube.r;
        int z = cube.s;

        int layer = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y), Mathf.Abs(z));

        int p = 0;
        if (x >= 0 && y >= 0) { p = x; }
        else if (y < 0 && z < 0) { p = layer - y; }
        else if (x >= 0 && z >= 0) { p = 2 * layer + z; }
        else if (x < 0 && y < 0) { p = 3 * layer - x; }
        else if (y >= 0 && z >= 0) { p = 4 * layer + y; }
        else if (x < 0 && z < 0) { p = 5 * layer - z; }

        return new Polar(layer, Polar.Mod(((3 * (layer - 1)) + 2) - p, layer * 6));
    }

    public static int Mod(int x, int y) {
        if (y == 0) { return 0; }
        return ((x % y) + y) % y;
    }
}

public class Cube : CoordinateSystemBase {
    public override int q { get; set; }
    public override int r { get; set; }
    public override int s { get; set; }

    public class Frac {
        public float q;
        public float r;
        public float s;

        public Frac(float q, float r, float s) {
            this.q = q;
            this.r = r;
            this.s = s;
        }
    }

    public Cube(int q, int r, int s) {
        this.q = q;
        this.r = r;
        this.s = s;

        if (q + r + s != 0) {
            throw new System.ArgumentException("q + r + s must be 0");
        }
    }

    public static Cube FromSpiral(Spiral spiral) {
        if (spiral.index == 0) return new Cube(0, 0, 0);
        
        return FromPolar(Polar.FromSpiral(spiral));
    }

    public static Cube FromPolar(Polar polar) {
        int layer = polar.layer;
        int position = polar.position;
        
        int k = Polar.Mod((int) Mathf.Floor(position / layer), 6);
        int j = Polar.Mod(position, layer);

        int x = 0, y = 0, z = 0;

        switch (k) {
            case 0:
                x = j;
                y = layer - j;
                z = -layer;
                break;
            case 1:
                x = layer;
                y = -j;
                z = j - layer;
                break;
            case 2:
                x = layer - j;
                y = -layer;
                z = j;
                break;
            case 3:
                x = -j;
                y = j - layer;
                z = layer;
                break;
            case 4:
                x = -layer;
                y = j;
                z = layer - j;
                break;
            case 5:
                x = j - layer;
                y = layer;
                z = -j;
                break;
        }

        return new Cube(x, y, z);
    }

    public static Cube Round(Frac frac) {
        int q = Mathf.RoundToInt(frac.q);
        int r = Mathf.RoundToInt(frac.r);
        int s = Mathf.RoundToInt(frac.s);

        float qd = Mathf.Abs(q - frac.q);
        float rd = Mathf.Abs(r - frac.r);
        float sd = Mathf.Abs(s - frac.s);
        
        if (qd > rd && qd > sd) {
            q = -r - s;
        } else if (rd > sd) {
            r = -q - s;
        } else {
            s = -q - r;
        }
        
        return new Cube(q, r, s);
    }
}

public class CoordinateSystem : CoordinateSystemBase {
    private Spiral spiral;
    private Polar polar;
    private Cube cube;

    public CoordinateSystem(Spiral spiral) {
        this.spiral = spiral;
        this.polar = Polar.FromSpiral(spiral);
        this.cube = Cube.FromSpiral(spiral);
    }

    public CoordinateSystem(Polar polar) {
        this.spiral = Spiral.FromPolar(polar);
        this.polar = polar;
        this.cube = Cube.FromPolar(polar);
    }

    public CoordinateSystem(Cube cube) {
        this.spiral = Spiral.FromCube(cube);
        this.polar = Polar.FromCube(cube);
        this.cube = cube;
    }

    public override int index { get { return spiral.index; } }
    public override int layer { get { return polar.layer; } }
    public override int position { get { return polar.position; } }
    public override int q { get { return cube.q; } }
    public override int r { get { return cube.r; } }
    public override int s { get { return cube.s; } }

    public bool Equals(CoordinateSystem other) {
        return this.index == other.index;
    }
}