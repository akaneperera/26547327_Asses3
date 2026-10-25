using UnityEngine;

public class Pellet : MonoBehaviour
{
    public int score = 10;
    public bool isPowerFood = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PacStudent")) // if it's the pacstudent
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPelletSFX(); // play pellet sfx
            }

            // then add score and tell the game manager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(score);

                if (isPowerFood)
                {
                    GameManager.Instance.PowerPillEaten(); // ghosts become scared
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