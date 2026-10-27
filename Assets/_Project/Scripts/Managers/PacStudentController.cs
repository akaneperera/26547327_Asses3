using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    private enum Direction { None, Up, Left, Down, Right }

    [SerializeField] private float speed = 5f;
    [SerializeField] private LevelGenerator levelGenerator;
    
    [SerializeField] private AudioSource pelletMoveAudio; // plays audio whether pellet is there or not
    [SerializeField] private AudioSource emptyMoveAudio;  
    
    [SerializeField] private ParticleSystem dust;

    private Direction lastInput = Direction.None; 
    private Direction currentInput = Direction.None; 
    [SerializeField] private GameObject wallHitEffect;

    private int row, col; // the grid cell pacstudent is currently in
    private int targetRow, targetCol; // the current lerp
    private bool isLerping;
    private bool isMoving;

    private Vector3 startPos;
    private Vector3 endPos;
    private float startTime;
    private float duration;

    private Animator animator;
    private bool hasBumped;
    private GameObject[] teleporters;
    private Transform ignoredTeleporter;
    private Transform pendingTarget;

    private bool isDead;

    private void Start()
    {
        animator = GetComponent<Animator>();

        if (levelGenerator == null)
        {
            levelGenerator = FindAnyObjectByType<LevelGenerator>();
        }
        if (levelGenerator == null)
        {
            Debug.LogError("no LevelGenerator found.");
            enabled = false;
            return;
        }

        try
        {
            teleporters = GameObject.FindGameObjectsWithTag("Teleporter");
        }
        catch (UnityException)
        {
            Debug.LogWarning("create a 'Teleporter' tag and tag both teleporters with it.");
            teleporters = new GameObject[0];
        }

        row = 1; // start position
        col = 1;
        transform.position = levelGenerator.CellToWorld(row, col);

        if (animator != null)
        {
            animator.SetFloat("Horizontal", 1f);
            animator.SetFloat("Vertical", 0f);
        }

        isMoving = true;
        SetMoving(false);
    }

    private void Update()
    {
        if (isDead) return;
        ReadInput();

        if (isLerping)
        {
            UpdateLerp();
        }

        if (!isLerping)
        {
            ChooseNextMove();
        }
    }

    private void ReadInput()
    {
        if (Input.GetKeyDown(KeyCode.W)) { lastInput = Direction.Up; hasBumped = false; }
        if (Input.GetKeyDown(KeyCode.A)) { lastInput = Direction.Left; hasBumped = false; }
        if (Input.GetKeyDown(KeyCode.S)) { lastInput = Direction.Down; hasBumped = false; }
        if (Input.GetKeyDown(KeyCode.D)) { lastInput = Direction.Right; hasBumped = false; }
    }

    private void UpdateLerp()
    {
        // elapsed time / duration, capped at 1
        float t = Mathf.Clamp01((Time.time - startTime) / duration);
        transform.position = Vector3.Lerp(startPos, endPos, t);

        if (t >= 1f)
        {
            transform.position = endPos; // snap to grid
            row = targetRow;
            col = targetCol;
            isLerping = false;
            if (pendingTarget != null)
            {
                Teleport();
                TryStartLeg(currentInput);
            }
        }
    }

    private void ChooseNextMove()
    {
        if (TryStartLeg(lastInput))
        {
            currentInput = lastInput;
            return;
        }

        if (TryStartLeg(currentInput))
        {
            return;
        }

        // if both blocked
        SetMoving(false);
        TryBump();
    }

    private void TryBump()
    {
        if (hasBumped) return;

        // the direction we were travelling, or the key pressed if we never moved
        Direction d = currentInput != Direction.None ? currentInput : lastInput;
        if (d == Direction.None) return;

        GetDelta(d, out int dRow, out int dCol);
        Vector3 point = transform.position + new Vector3(dCol, -dRow, 0f) * 0.5f;

        if (wallHitEffect != null)
        {
            Instantiate(wallHitEffect, point, Quaternion.identity);
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayWallHitSFX();
        }
        hasBumped = true;
    }

    private bool TryStartLeg(Direction d)
    {
        if (d == Direction.None) return false;

        GetDelta(d, out int dRow, out int dCol);
        int nextRow = row + dRow;
        int nextCol = col + dCol;

        if (!levelGenerator.IsWalkable(nextRow, nextCol)) return false;

        targetRow = nextRow;
        targetCol = nextCol;

        startPos = levelGenerator.CellToWorld(row, col);
        endPos = levelGenerator.CellToWorld(nextRow, nextCol);
        duration = 1f / speed; // one tile per leg
        startTime = Time.time;
        isLerping = true;
        hasBumped = false;

        if (animator != null)
        {
            animator.SetFloat("Horizontal", dCol);
            animator.SetFloat("Vertical", -dRow); // up on screen = lower row
        }

        SetMoving(true);
        PlayMoveAudio(levelGenerator.HasPellet(nextRow, nextCol));
        return true;
    }

    private void Teleport()
    {
        levelGenerator.WorldToCell(pendingTarget.position, out row, out col);
        transform.position = levelGenerator.CellToWorld(row, col);

        ignoredTeleporter = pendingTarget; //arrive inside its trigger so ignore it
        pendingTarget = null;
    }

    private Transform GetOtherTeleporter(Transform entered)
    {
        foreach (GameObject t in teleporters)
        {
            if (t.transform != entered) return t.transform;
        }
        return null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Touched: " + other.name + " tag=" + other.tag);
        if (other.CompareTag("Ghost"))
        {
            HandleGhostTouch(other);
            return;
        }
 
        if (!other.CompareTag("Teleporter")) return;
        if (other.transform == ignoredTeleporter) return;
 
        // wait until current lerp reaches the tunnel cell
        pendingTarget = GetOtherTeleporter(other.transform);
    }
 
    private void HandleGhostTouch(Collider2D other)
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
 
        Ghostcontroller ghost = other.GetComponentInParent<Ghostcontroller>();
        if (ghost == null) return;
 
        switch (ghost.State)
        {
            case GhostState.Dead:
                break; //dead ghost can't hurt player or be eaten
            case GhostState.Normal:
                GameManager.Instance.PlayerHit();
                break;
            default: // scared or recovering
                GameManager.Instance.GhostEaten(ghost);
                break;
        }
    }

    private static void GetDelta(Direction d, out int dRow, out int dCol)
    {
        dRow = 0;
        dCol = 0;
        switch (d)
        {
            case Direction.Up: dRow = -1; break;
            case Direction.Down: dRow = 1; break;
            case Direction.Left: dCol = -1; break;
            case Direction.Right: dCol = 1; break;
        }
    }

    // methods called by gamemanager script
    public void PlayDeath()
{
    if (isDead) return;

    isDead = true;
    isLerping = false;
    pendingTarget = null;

    SetMoving(false);

    if (animator != null)
    {
        animator.speed = 1f;
        animator.SetFloat("Horizontal", 0f);
        animator.SetFloat("Vertical", 0f);
        animator.SetBool("Die", true);
    }
}

    public void ResetToStart()
    {
        isLerping = false;
        lastInput = Direction.None;
        currentInput = Direction.None;
        hasBumped = false;
        pendingTarget = null;
        ignoredTeleporter = null;
        isDead = false;
 
        row = 1;
        col = 1;
        transform.position = levelGenerator.CellToWorld(row, col);
 
        if (animator != null)
        {
            animator.SetBool("Die", false);
            animator.SetFloat("Horizontal", 1f); // facing right again
            animator.SetFloat("Vertical", 0f);
        }
 
        isMoving = true;//force the stopped state
        SetMoving(false);
    }

    // Animation, dust and audio 
    private void SetMoving(bool moving)
    {
        if (isMoving == moving) return;
        isMoving = moving;

        if (animator != null)
        {
            animator.speed = moving ? 1f : 0f;
        }

        if (dust != null)
        {
            ParticleSystem.EmissionModule emission = dust.emission;
            emission.enabled = moving;
        }

        if (!moving)
        {
            StopMoveAudio();
        }
    }

    private void PlayMoveAudio(bool nextHasPellet)
    {
        AudioSource wanted = nextHasPellet ? pelletMoveAudio : emptyMoveAudio;
        AudioSource other = nextHasPellet ? emptyMoveAudio : pelletMoveAudio;

        if (other != null && other.isPlaying) other.Stop();
        if (wanted != null && !wanted.isPlaying) wanted.Play(); // don't restart every tile
    }

    private void StopMoveAudio()
    {
        if (pelletMoveAudio != null) pelletMoveAudio.Stop();
        if (emptyMoveAudio != null) emptyMoveAudio.Stop();
    }
}