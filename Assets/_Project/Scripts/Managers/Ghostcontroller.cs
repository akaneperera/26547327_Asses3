using System.Collections.Generic;
using UnityEngine;

public enum GhostState { Normal, Scared, Recovering, Dead }
public class Ghostcontroller : MonoBehaviour
{
    private enum Heading { None = -1, Right = 0, Left = 1, Up = 2, Down = 3 }
    
    [SerializeField, Range(1, 4)] private int ghostNumber = 1; // must match the number on this ghost's label
    [SerializeField] private LevelGenerator levelGenerator;
    [SerializeField] private Transform pacStudent;

    // Grid position and the last direction moved
    private int row, col;
    private Heading heading = Heading.None;

    //lerp
    private Vector3 startPos;
    private Vector3 endPos;
    private float startTime;
    private float duration;
    private bool isLerping;

    private bool insideHouse = true;
    private readonly List<Vector2Int> leavingHouse = new List<Vector2Int>(); // x = row, y = column
    private int leaveIndex;
    private Heading exitHeading = Heading.Up;

    private Vector3 startPosition;
    private int startRow, startCol;
    private Animator animator;
    private PacStudentController player;

    private GhostState state = GhostState.Normal;

    public GhostState State { get { return state; } }

    [SerializeField] private float normalSpeedFactor = 0.9f;
    [SerializeField] private float scaredSpeedFactor = 0.5f; 
    public int GhostNumber { get { return ghostNumber; } }

    private static readonly Heading[] AllHeadings = { Heading.Right, Heading.Left, Heading.Up, Heading.Down };
    public int Row { get { return row; } }
    public int Col { get { return col; } }
    private int targetRow, targetCol;

    public bool InsideHouse { get { return insideHouse; } }


    private void OnValidate()
    {
        ghostNumber = Mathf.Clamp(ghostNumber, 1, 4);
    }

    private void Awake()
    {
        startPosition = transform.position;
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (levelGenerator == null)
        {
            levelGenerator = FindAnyObjectByType<LevelGenerator>();
        }
        if (pacStudent == null)
        {
            player = FindAnyObjectByType<PacStudentController>();
            if (player != null) pacStudent = player.transform;
        }
        else
        {
            player = pacStudent.GetComponent<PacStudentController>();
        }
        if (levelGenerator == null)
        {
            Debug.LogError("no LevelGenerator found.");
            enabled = false;
            return;
        }
 
        // work out the start cells
        levelGenerator.WorldToCell(startPosition, out startRow, out startCol);
        startPosition = levelGenerator.CellToWorld(startRow, startCol);
        transform.position = startPosition;
 
        row = startRow;
        col = startCol;

        BuildLeavingPath();
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
 
        // dead ghosts' own movement
        if (state == GhostState.Dead)
        {
            UpdateDead();
            return;
        }
 
        if (isLerping)
        {
            UpdateLerp();
        }
 
        if (!isLerping)
        {
            if (insideHouse) ChooseHouseMove();
            else ChooseNextMove();
        }
    }

    private void UpdateLerp()
    {
        // elapsed time / duration
        float t = Mathf.Clamp01((Time.time - startTime) / duration);
        transform.position = Vector3.Lerp(startPos, endPos, t);
 
        if (t >= 1f)
        {
            transform.position = endPos;
            row = targetRow;
            col = targetCol;
            isLerping = false;
        }
    }

    private void StartLeg(Heading h) //sets the target cell
    {
        GetDelta(h, out int dRow, out int dCol);
        targetRow = row + dRow;
        targetCol = col + dCol;
 
        startPos = levelGenerator.CellToWorld(row, col);
        endPos = levelGenerator.CellToWorld(targetRow, targetCol);
        duration = 1f / CurrentSpeed();
        startTime = Time.time;
        isLerping = true;
 
        heading = h;
        SetDirection((int)h);
    }

    private float CurrentSpeed() // 90% speed of player
    {
        float playerSpeed = player != null ? player.Speed : 5f;
        float normal = playerSpeed * normalSpeedFactor;
        return state == GhostState.Normal ? normal : normal * scaredSpeedFactor;
    }

    private bool CanEnter(int r, int c)
    {
        return levelGenerator.IsGhostWalkable(r, c, insideHouse);
    }

    private void ChooseNextMove()
    {
        List<Heading> options = new List<Heading>();
        foreach (Heading h in AllHeadings)
        {
            if (heading != Heading.None && h == Opposite(heading)) continue; // no backstep
            GetDelta(h, out int dRow, out int dCol);
            if (CanEnter(row + dRow, col + dCol)) options.Add(h);
        }
 
        if (options.Count > 0)
        {
            StartLeg(options[Random.Range(0, options.Count)]);
            return;
        }
 
        if (heading != Heading.None)
        {
            Heading back = Opposite(heading);
            GetDelta(back, out int bRow, out int bCol);
            if (CanEnter(row + bRow, col + bCol)) StartLeg(back);
        }
    }

    // path out of the house
    private void BuildLeavingPath()
    {
        leavingHouse.Clear();
        leaveIndex = 0;

        List<Vector2Int> exits = levelGenerator.GetExitWallCells();
        if (exits.Count == 0)
        {
            insideHouse = false;
            return;
        }

        bool top = ghostNumber == 1 || ghostNumber == 3;
        exitHeading = top ? Heading.Up : Heading.Down;

        int exitRow = top ? int.MaxValue : int.MinValue;
        foreach (Vector2Int e in exits)
        {
            exitRow = top ? Mathf.Min(exitRow, e.x) : Mathf.Max(exitRow, e.x);
        }

        int exitCol = startCol;
        int bestDistance = int.MaxValue;
        foreach (Vector2Int e in exits)
        {
            if (e.x != exitRow) continue;
            int distance = Mathf.Abs(e.y - startCol);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                exitCol = e.y;
            }
        }

        int stepCol = exitCol > startCol ? 1 : -1;
        for (int c = startCol; c != exitCol; c += stepCol)
        {
            leavingHouse.Add(new Vector2Int(startRow, c + stepCol));
        }

        int stepRow = top ? -1 : 1;
        int outsideRow = exitRow + stepRow;
        for (int r = startRow + stepRow; r != outsideRow + stepRow; r += stepRow)
        {
            leavingHouse.Add(new Vector2Int(r, exitCol));
        }
    }

    private void ChooseHouseMove()
    {
        if (leaveIndex < leavingHouse.Count)
        {
            Vector2Int next = leavingHouse[leaveIndex];
            leaveIndex++;
            StartLeg(HeadingFromDelta(next.x - row, next.y - col));
            return;
        }

        insideHouse = false;
        heading = exitHeading;
        ChooseNextMove();
    }

    private static Heading HeadingFromDelta(int dRow, int dCol)
    {
        if (dCol > 0) return Heading.Right;
        if (dCol < 0) return Heading.Left;
        if (dRow < 0) return Heading.Up;
        if (dRow > 0) return Heading.Down;
        return Heading.None;
    }

    //move back to the spawn area
    private void UpdateDead()
    {
    }
 
    private static Heading Opposite(Heading h)
    {
        switch (h)
        {
            case Heading.Right: return Heading.Left;
            case Heading.Left: return Heading.Right;
            case Heading.Up: return Heading.Down;
            case Heading.Down: return Heading.Up;
            default: return Heading.None;
        }
    }
 
    private static void GetDelta(Heading h, out int dRow, out int dCol)
    {
        dRow = 0;
        dCol = 0;
        switch (h)
        {
            case Heading.Right: dCol = 1; break;
            case Heading.Left: dCol = -1; break;
            case Heading.Up: dRow = -1; break;
            case Heading.Down: dRow = 1; break;
        }
    }

    public void SetState(GhostState s)
    {
        state = s;
        if (animator != null) animator.SetInteger("State", (int)s);
    }

    public void SetDirection(int d)
    {
        if (animator != null) animator.SetInteger("Direction", d);
    }

    public void ResetToStart()
    {
        transform.position = startPosition;
        row = startRow;
        col = startCol;
        heading = Heading.None;
        isLerping = false;
        insideHouse = true;
        leavingHouse.Clear();
        if (levelGenerator != null) BuildLeavingPath();
 
        SetState(GhostState.Normal);
        SetDirection(0);
    }
}