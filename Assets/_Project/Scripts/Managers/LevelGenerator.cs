using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] private GameObject outsideCornerPrefab; // 1
    [SerializeField] private GameObject outsideWallPrefab;   // 2
    [SerializeField] private GameObject insideCornerPrefab;  // 3
    [SerializeField] private GameObject insideWallPrefab;    // 4
    [SerializeField] private GameObject tJunctionPrefab;     // 7
    [SerializeField] private GameObject ghostExitPrefab;     // 8
    [SerializeField] private GameObject standardPelletPrefab; // 5
    [SerializeField] private GameObject powerPelletPrefab;    // 6
    [SerializeField] private string manualLevelName = "Level01";
    [SerializeField] private float cameraPadding = 1f; // extra tiles around the level

    private readonly int[,] levelMap =
    {
        {1,2,2,2,2,2,2,2,2,2,2,2,2,7},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,4},
        {2,6,4,0,0,4,5,4,0,0,0,4,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,3},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,5},
        {2,5,3,4,4,3,5,3,3,5,3,4,4,4},
        {2,5,3,4,4,3,5,4,4,5,3,4,4,3},
        {2,5,5,5,5,5,5,4,4,5,5,5,5,4},
        {1,2,2,2,2,1,5,4,3,4,4,3,0,4},
        {0,0,0,0,0,2,5,4,3,4,4,3,0,3},
        {0,0,0,0,0,2,5,4,4,0,0,0,0,0},
        {0,0,0,0,0,2,5,4,4,0,3,4,4,8},
        {2,2,2,2,2,1,5,3,3,0,4,0,0,0},
        {0,0,0,0,0,0,5,0,0,0,4,0,0,0},
    };
    private const int Up = 0, Left = 1, Down = 2, Right = 3;
    private static readonly int[] dRow = { -1, 0, 1, 0 };
    private static readonly int[] dCol = { 0, -1, 0, 1 };

    private const int OutsideCornerDefault = (1 << Left) | (1 << Up);
    private const int InsideCornerDefault = (1 << Down) | (1 << Right);
    
    //default orientation
    private const int DefaultBandSide = Down;  
    private const int DefaultTMissingSide = Down;
    
    private int fullRows, fullCols; //fully completed row and col
    private int[,] map; // fully mirrored level
    private bool[,] horizontal;
    private int[,] missing;// missing side of every T-junction 

    private void Awake()
    {
        GameObject manual = GameObject.Find(manualLevelName);
        if (manual != null)
        {
            Destroy(manual);
        }

        BuildMap();
        FixStraights();
        FixTJunctions();
        SpawnLevel();
        FitCamera();
    }

    private void BuildMap()
    {
        int rows = levelMap.GetLength(0);
        int cols = levelMap.GetLength(1);

        fullRows = 2 * rows - 1; // skips the duplicated last row
        fullCols = 2 * cols;

        map = new int[fullRows, fullCols];
        horizontal = new bool[fullRows, fullCols];
        missing = new int[fullRows, fullCols];

        for (int r = 0; r < fullRows; r++)
        {
            int srcR = r < rows ? r : 2 * rows - 2 - r;
            for (int c = 0; c < fullCols; c++)
            {
                int srcC = c < cols ? c : 2 * cols - 1 - c;
                map[r, c] = levelMap[srcR, srcC];
            }
        }
    }

    private int TileAt(int r, int c)
    {
        if (r < 0 || r >= fullRows || c < 0 || c >= fullCols) return -1; // out of bounds = not a wall
        return map[r, c];
    }

    private int TileAtSide(int r, int c, int side)
    {
        return TileAt(r + dRow[side], c + dCol[side]);
    }

    private static int Opposite(int side) { return (side + 2) % 4; }

    private static bool IsWall(int t) { return t == 1 || t == 2 || t == 3 || t == 4 || t == 7 || t == 8; }
    private static bool IsStraight(int t) { return t == 2 || t == 4 || t == 8; }
    private static bool IsCorner(int t) { return t == 1 || t == 3; }
    private static bool IsPelletTile(int t) { return t == 5 || t == 6; }

    private static int RotateMask(int mask, int steps)
    {
        int result = 0;
        for (int s = 0; s < 4; s++)
        {
            if ((mask & (1 << s)) != 0)
            {
                result |= 1 << ((s + steps) % 4);
            }
        }
        return result;
    }

    private static int BitCount(int v)
    {
        int n = 0;
        while (v != 0) { n += v & 1; v >>= 1; }
        return n;
    }

    // quarter turns until the default connections match
    private static int StepsFor(int defaultMask, int wanted)
    {
        for (int k = 0; k < 4; k++)
        {
            if (RotateMask(defaultMask, k) == wanted) return k;
        }

        //fall back to the rotation with the most overlap.
        int best = 0, bestScore = -1;
        for (int k = 0; k < 4; k++)
        {
            int score = BitCount(RotateMask(defaultMask, k) & wanted);
            if (score > bestScore) { bestScore = score; best = k; }
        }
        return best;
    }

    // does  straight piece at (r, c) run along the direction of side?
    private bool StraightRunsAlong(int r, int c, int side)
    {
        bool sideIsHorizontal = (side == Left || side == Right);
        return horizontal[r, c] == sideIsHorizontal;
    }

    private void FixStraights()
    {
        for (int r = 0; r < fullRows; r++)
        {
            for (int c = 0; c < fullCols; c++)
            {
                if (!IsStraight(map[r, c])) continue;

                bool up = IsWall(TileAtSide(r, c, Up));
                bool down = IsWall(TileAtSide(r, c, Down));
                bool left = IsWall(TileAtSide(r, c, Left));
                bool right = IsWall(TileAtSide(r, c, Right));

                if (left && right) horizontal[r, c] = true;
                else if (up && down) horizontal[r, c] = false;
                else if (left || right) horizontal[r, c] = true;   // dead end
                else if (up || down) horizontal[r, c] = false;
                else horizontal[r, c] = true;
            }
        }
    }

    private void FixTJunctions()
    {
        for (int r = 0; r < fullRows; r++)
        {
            for (int c = 0; c < fullCols; c++)
            {
                if (map[r, c] != 7) continue;

                bool[] connected = new bool[4];
                for (int s = 0; s < 4; s++)
                {
                    int t = TileAtSide(r, c, s);
                    if (IsStraight(t))
                    {
                        connected[s] = StraightRunsAlong(r + dRow[s], c + dCol[s], s);
                    }
                    else
                    {
                        connected[s] = IsCorner(t) || t == 7;
                    }
                }

                List<int> open = new List<int>();
                for (int s = 0; s < 4; s++)
                {
                    if (!connected[s]) open.Add(s);
                }

                int m = DefaultTMissingSide;
                if (open.Count == 1)
                {
                    m = open[0];
                }
                else if (open.Count > 1)
                {
                    m = open[0];
                    foreach (int s in open)
                    {
                        if (connected[Opposite(s)]) { m = s; break; }
                    }
                }
                missing[r, c] = m;
            }
        }
    }

    private bool ConnectsToward(int r, int c, int side)
    {
        int nr = r + dRow[side];
        int nc = c + dCol[side];
        int t = TileAt(nr, nc);

        if (IsStraight(t)) return StraightRunsAlong(nr, nc, side);
        if (t == 7) return missing[nr, nc] != Opposite(side); // T's side facing us isn't its missing one
        return false;
    }

    private int CornerSteps(int r, int c, int defaultMask)
    {
        int mask = 0, count = 0;
        for (int s = 0; s < 4; s++)
        {
            if (ConnectsToward(r, c, s)) { mask |= 1 << s; count++; }
        }

        if (count < 2)
        {
            List<int> cand = new List<int>();
            for (int s = 0; s < 4; s++)
            {
                if ((mask & (1 << s)) == 0 && IsCorner(TileAtSide(r, c, s))) cand.Add(s);
            }

            if (count == 1)
            {
                int have = 0;
                for (int s = 0; s < 4; s++) { if ((mask & (1 << s)) != 0) have = s; }

                foreach (int s in cand)
                {
                    if (s != Opposite(have)) { mask |= 1 << s; break; } // must be perpendicular
                }
            }
            else
            {
                for (int s = 0; s < 4; s++)
                {
                    int next = (s + 1) % 4;
                    if (cand.Contains(s) && cand.Contains(next))
                    {
                        mask |= (1 << s) | (1 << next);
                        break;
                    }
                }
            }
        }

        return StepsFor(defaultMask, mask);
    }

    private int OutsideWallSteps(int r, int c)
    {
        bool h = horizontal[r, c];
        int sideA = h ? Up : Left;
        int sideB = h ? Down : Right;
        int tA = TileAtSide(r, c, sideA);
        int tB = TileAtSide(r, c, sideB);

        int band;
        if (tA == -1) band = sideA;              // out of bounds: band goes there
        else if (tB == -1) band = sideB;
        else if (IsPelletTile(tA)) band = sideB;   // pellet on one side: band on the other
        else if (IsPelletTile(tB)) band = sideA;
        else
        {
            // face away from the centre of the level
            float centre = h ? (fullRows - 1) / 2f : (fullCols - 1) / 2f;
            float pos = h ? r : c;
            band = pos < centre ? sideA : sideB;
        }

        return (band - DefaultBandSide + 4) % 4;
    }
    private void SpawnLevel()
    {
        Transform parent = new GameObject("GeneratedLevel").transform;

        for (int r = 0; r < fullRows; r++)
        {
            for (int c = 0; c < fullCols; c++)
            {
                int type = map[r, c];
                if (type == 0) continue; // empty: place nothing

                GameObject prefab = null;
                int steps = 0;

                switch (type)
                {
                    case 1:
                        prefab = outsideCornerPrefab;
                        steps = CornerSteps(r, c, OutsideCornerDefault);
                        break;
                    case 2:
                        prefab = outsideWallPrefab;
                        steps = OutsideWallSteps(r, c);
                        break;
                    case 3:
                        prefab = insideCornerPrefab;
                        steps = CornerSteps(r, c, InsideCornerDefault);
                        break;
                    case 4:
                        prefab = insideWallPrefab;
                        steps = horizontal[r, c] ? 0 : 1;
                        break;
                    case 5:
                        prefab = standardPelletPrefab;
                        break;
                    case 6:
                        prefab = powerPelletPrefab;
                        break;
                    case 7:
                        prefab = tJunctionPrefab;
                        steps = (missing[r, c] - DefaultTMissingSide + 4) % 4;
                        break;
                    case 8:
                        prefab = ghostExitPrefab;
                        steps = horizontal[r, c] ? 0 : 1;
                        break;
                }

                if (prefab == null)
                {
                    Debug.LogWarning("no prefab assigned for tile type " + type);
                    continue;
                }

                Vector3 pos = new Vector3(c, -r, 0f);
                Quaternion rot = Quaternion.Euler(0f, 0f, 90f * steps);
                Instantiate(prefab, pos, rot, parent);
            }
        }
    }

    private void FitCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        cam.orthographic = true;
        cam.transform.position = new Vector3((fullCols - 1) / 2f, -(fullRows - 1) / 2f, -10f);

        float halfHeight = fullRows / 2f + cameraPadding;
        float halfWidth = fullCols / 2f + cameraPadding;
        cam.orthographicSize = Mathf.Max(halfHeight, halfWidth / cam.aspect);
    }

    //queries for other scripts

    public bool IsWalkable(int row, int col)
    {
        int t = TileAt(row, col);
        return t == 0 || t == 5 || t == 6;
    }

    public bool HasPellet(int row, int col)
    {
        return IsPelletTile(TileAt(row, col));
    }

    public Vector3 CellToWorld(int row, int col)
    {
        return new Vector3(col, -row, 0f);
    }

    public void WorldToCell(Vector3 world, out int row, out int col)
    {
        col = Mathf.RoundToInt(world.x);
        row = Mathf.RoundToInt(-world.y);
    }
}