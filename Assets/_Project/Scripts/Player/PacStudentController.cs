using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    [SerializeField] private float speed = 3f; // tiles per second

    private readonly Vector3[] corners =
    {
        new Vector3(1f, -1f, 0f),
        new Vector3(6f, -1f, 0f),
        new Vector3(6f, -5f, 0f),
        new Vector3(1f, -5f, 0f)
    };

    private int targetIndex;      // corner we are currently heading toward
    private Vector3 startPos;
    private Vector3 endPos;
    private float startTime;
    private float duration;

    private Animator animator;
    private AudioSource audioSource;

    private void Start()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        transform.position = corners[0];
        targetIndex = 1;
        StartLeg();

        if (audioSource != null && !audioSource.isPlaying) //loop the audio
        {
            audioSource.Play();
        }
    }

    private void Update()
    {
        // go through the current leg which is capped at 1
        float t = Mathf.Clamp01((Time.time - startTime) / duration);

        transform.position = Vector3.Lerp(startPos, endPos, t);

        if (t >= 1f)
        {
            transform.position = endPos; // snap to the corner
            targetIndex = (targetIndex + 1) % corners.Length; // wrap 3 to 0
            StartLeg(); // instant turn
        }
    }

    private void StartLeg()
    {
        startPos = transform.position;
        endPos = corners[targetIndex];

        duration = Vector3.Distance(startPos, endPos) / speed;
        startTime = Time.time;

        Vector3 dir = (endPos - startPos).normalized;
        if (animator != null)
        {
            animator.SetFloat("Horizontal", dir.x);
            animator.SetFloat("Vertical", dir.y);
        }
    }
}