using UnityEngine;

public class Pellet : MonoBehaviour
{
    public int score = 10;
    public bool isPellet = false;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PacStudent")) // if it's the pacstudent
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPelletSFX(); // play pellet sfx
            }
            // then add score
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(score);
            }
            Destroy(gameObject);
        }
    }

   void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
