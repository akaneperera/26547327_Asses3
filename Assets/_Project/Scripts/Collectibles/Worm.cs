using UnityEngine;

public class Worm : MonoBehaviour
{
    public int score = 100;

    private void OnTriggerEnter2D(Collider2D other) // same logic as pellet
    {
        if (other.CompareTag("PacStudent"))
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayFoodSFX();
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(score);
            }
            Destroy(gameObject);
        }
    }
}