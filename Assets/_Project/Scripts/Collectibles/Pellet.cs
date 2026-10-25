using UnityEngine;

public class Pellet : MonoBehaviour
{
    public bool isPowerFood = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PacStudent"))
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPelletSFX();
            }

            //GameManager adds the score
            if (GameManager.Instance != null)
            {
                if (isPowerFood)
                {
                    GameManager.Instance.PowerPillEaten(); //ghosts become scared
                }
                else
                {
                    GameManager.Instance.PelletEaten();
                }
            }
            Destroy(gameObject);
        }
    }
}