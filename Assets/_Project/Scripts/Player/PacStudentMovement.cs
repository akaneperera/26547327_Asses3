using UnityEngine;

public class PacStudentMovement : MonoBehaviour
{
    // external variables
    public float moveSpeed = 5f;
    public LayerMask wallLayer;
    public float collisionCheckDistance = 0.5f;

    // internal variables
    private Vector2 currentDirection;
    private Vector2 inputDirection;
    private Rigidbody2D rb; // needs to be kinematic
    private Animator animator;
    


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = true; // allows rigidbody2d to trigger collision events with other rigidbody2d types
        }
        else
        {
            Debug.LogError("PacStudent needs rigidbody2d set to Kinematic");
        }
    }

    void Update()
    {
        GetInput();

        Move();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            animator.SetTrigger("Die");
        }
    }

    private void GetInput()
    {

        inputDirection = Vector2.zero;

        
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            inputDirection = Vector2.up;
        }
        else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            inputDirection = Vector2.left;
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            inputDirection = Vector2.down;
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            inputDirection = Vector2.right;
        }

        
    }

    private void Move()
    {
        if (CanMove(inputDirection))
        {
            currentDirection = inputDirection; 
        }
        if (CanMove(currentDirection))
        {
            transform.Translate(currentDirection * moveSpeed * Time.deltaTime); // movement is not dependent on frame rate
        }
        else
        {
            currentDirection = Vector2.zero;
        }

        float currentH = animator.GetFloat("Horizontal");
        float currentV = animator.GetFloat("Vertical");

        if (currentDirection.x != currentH)
        {
            animator.SetFloat("Horizontal", currentDirection.x);
            
        }
        if (currentDirection.y != currentV)
        {
            animator.SetFloat("Vertical", currentDirection.y);
        }

        
    }

    private bool CanMove(Vector2 direction)
    {
        if (direction == Vector2.zero) return false;

        float radius = GetComponent<CircleCollider2D>().radius * transform.localScale.x; // need to figure out the width of pacstudent to find its 2 edges

        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        Vector2 leftOffset = perpendicular * (radius * 0.8f);
        Vector2 rightOffset = -perpendicular * (radius * 0.8f);

        // i tried using a single raycast from the players centre to check player collisions but if you hit a wall partially you just phase through it so im gonna use 3 raycasts (left, right, centre)
        RaycastHit2D hitCentre = Physics2D.Raycast(transform.position, direction, collisionCheckDistance, wallLayer);
        if (hitCentre.collider != null) return false;

        RaycastHit2D hitLeft = Physics2D.Raycast((Vector2)transform.position + leftOffset, direction, collisionCheckDistance, wallLayer);
        if (hitLeft.collider != null) return false;

        RaycastHit2D hitRight = Physics2D.Raycast((Vector2)transform.position + rightOffset, direction, collisionCheckDistance, wallLayer);
        if (hitRight.collider != null) return false;

        return true;
    }
}
