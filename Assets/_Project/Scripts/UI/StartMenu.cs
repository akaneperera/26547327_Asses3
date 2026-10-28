using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenu : MonoBehaviour
{
    [SerializeField] private Button level1Button;
    [SerializeField] private Button level2Button;
    [SerializeField] private TMP_Text level1ScoreText;
    [SerializeField] private TMP_Text level2ScoreText;

    // Level 2 stay at their defaults
    private const string Level2HighScoreKey = "Level2HighScore";
    private const string Level2BestTimeKey = "Level2BestTime";
    [SerializeField] private string level1SceneName = "ManualLevel";

    [SerializeField] private AudioSource menuMusic; // looping menu music, Play On Awake off

    private void Start()
    {
        if (level1Button != null)
        {
            level1Button.onClick.AddListener(LoadLevel1);
        }
        else
        {
            Debug.LogWarning("Level 1 button is not assigned");
        }

        // Same keys GameManager saves to
        if (level1ScoreText != null)
        {
            level1ScoreText.text = BuildScoreLine(GameManager.HighScoreKey, GameManager.BestTimeKey);
        }
        if (level2ScoreText != null)
        {
            level2ScoreText.text = BuildScoreLine(Level2HighScoreKey, Level2BestTimeKey);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }

        if (menuMusic != null && !menuMusic.isPlaying)
        {
            menuMusic.Play();
        }
    }

    private string BuildScoreLine(string scoreKey, string timeKey)
    {
        int score = PlayerPrefs.GetInt(scoreKey, 0);
        float time = PlayerPrefs.GetFloat(timeKey, float.MaxValue);

        string timeText = time >= float.MaxValue ? "00:00:00" : GameManager.FormatTime(time); //nothing has been saved yet

        return "Best: " + score + " Time: " + timeText;
    }

    private void LoadLevel1()
    {
        SceneManager.LoadScene(level1SceneName);
    }
}