using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance; // same logic as audiomanger
    public int currentScore = 0;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        Debug.Log("Score" + currentScore);
    }
    public void ResetScore()
    {
        currentScore = 0;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
