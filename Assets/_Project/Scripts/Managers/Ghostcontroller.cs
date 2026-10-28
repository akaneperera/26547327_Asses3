using UnityEngine;

public enum GhostState { Normal, Scared, Recovering, Dead }

public class Ghostcontroller : MonoBehaviour
{
    private Vector3 startPosition;
    private Animator animator;
    private GhostState state = GhostState.Normal;

    public GhostState State { get { return state; } }

    private void Awake()
    {
        startPosition = transform.position;
        animator = GetComponent<Animator>();
    }

    public void SetState(GhostState s)
    {
        state = s;
        if (animator != null) animator.SetInteger("State", (int)s);
    }

    // 0 Right, 1 Left, 2 Up, 3 Down
    public void SetDirection(int d)
    {
        if (animator != null) animator.SetInteger("Direction", d);
    }

    public void ResetToStart()
    {
        transform.position = startPosition;
        SetState(GhostState.Normal);
        SetDirection(0);
    }
}