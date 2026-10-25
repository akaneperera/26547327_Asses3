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

    private int row, col; // the grid cell pacstudent is currently in
    private int targetRow, targetCol; // the current lerp
    private bool isLerping;
    private bool isMoving;

    private Vector3 startPos;
    private Vector3 endPos;
    private float startTime;
    private float duration;

    private Animator animator;

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
        if (Input.GetKeyDown(KeyCode.W)) lastInput = Direction.Up;
        if (Input.GetKeyDown(KeyCode.A)) lastInput = Direction.Left;
        if (Input.GetKeyDown(KeyCode.S)) lastInput = Direction.Down;
        if (Input.GetKeyDown(KeyCode.D)) lastInput = Direction.Right;
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

        if (animator != null)
        {
            animator.SetFloat("Horizontal", dCol);
            animator.SetFloat("Vertical", -dRow); // up on screen = lower row
        }

        SetMoving(true);
        PlayMoveAudio(levelGenerator.HasPellet(nextRow, nextCol));
        return true;
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
        isLerping = false;
        SetMoving(false); 
 
        if (animator != null)
        {
            animator.speed = 1f;
            animator.SetBool("Die", true);
        }
    }

    public void ResetToStart()
    {
        isLerping = false;
        lastInput = Direction.None;
        currentInput = Direction.None;
 
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