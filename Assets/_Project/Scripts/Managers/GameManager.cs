using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameState { Countdown, Playing, Dying, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text gameTimerText;
    [SerializeField] private TMP_Text scaredTimerText;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private Image blockingImage;
    [SerializeField] private GameObject[] lifeIcons;
    [SerializeField] private Button exitButton;

    [SerializeField] private PacStudentController pacStudent;
    [SerializeField] private Ghostcontroller[] ghosts;
    [SerializeField] private GameObject deathEffect;

    [SerializeField] private int startingLives = 3;
    [SerializeField] private int pelletScore = 10;
    [SerializeField] private int powerPillScore = 50;
    [SerializeField] private int ghostScore = 300;
    [SerializeField] private float ghostDeadDuration = 3f;
    [SerializeField] private float scaredDuration = 10f;
    [SerializeField] private float recoverTime = 3f;
    [SerializeField] private float deathDuration = 4f;
    [SerializeField] private float gameOverDelay = 3f;
    [SerializeField] private string startSceneName = "StartScene";

    //other scripts only read these
    public GameState State { get; private set; } = GameState.Countdown;
    public GhostState CurrentGhostState { get { return GetGlobalGhostState(); } }
    public int Score { get { return score; } }
    public int Lives { get { return lives; } }
    public int PelletsRemaining { get { return pelletsRemaining; } }
    public float GameTime { get { return gameTime; } }
    public float ScaredTimeLeft { get { return scaredTimeLeft; } }
    public int GhostsDead { get { return ghostsDead; } }

    public const string HighScoreKey = "Level1HighScore";
    public const string BestTimeKey = "Level1BestTime";

    private int score;
    private int lives;
    private int pelletsRemaining;
    private float gameTime;
    private float scaredTimeLeft;
    private int ghostsDead;

    private GhostState appliedGhostState = GhostState.Normal; //last state of the ghosts
    private readonly Dictionary<Ghostcontroller, Coroutine> deadTimers = new Dictionary<Ghostcontroller, Coroutine>();
    private Collider2D pacStudentCollider;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        lives = startingLives;
        if (pacStudent != null) pacStudentCollider = pacStudent.GetComponent<Collider2D>();

        if (gameOverText != null) gameOverText.gameObject.SetActive(false);
        if (exitButton != null) exitButton.onClick.AddListener(ExitToStart);

        StartCoroutine(CountdownRoutine());
    }

    private void Update()
    {
        if (State == GameState.Playing)
        {
            gameTime += Time.deltaTime;
            UpdateScared();
        }
        UpdateHud();
    }

    // Public methods other scripts call
    public void AddScore(int points)
    {
        score += points;
    }

    public void PelletEaten()
    {
        AddScore(pelletScore);
        pelletsRemaining--;
        CheckAllPelletsEaten();
    }

    public void PowerPillEaten()
    {
        AddScore(powerPillScore);
        pelletsRemaining--;

        scaredTimeLeft = scaredDuration; // eating another pill restarts the timer
        ApplyGhostState(GhostState.Scared);

        CheckAllPelletsEaten();
    }

    public void GhostEaten(Ghostcontroller ghost)
    {
        if (ghost == null || ghost.State == GhostState.Dead) return;
 
        ghost.SetState(GhostState.Dead);
        AddScore(ghostScore);
        ghostsDead++;
 
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEatEnemySFX();
        UpdateMusic();
 
        StartDeadTimer(ghost);
    }

    public void GhostEaten()
    {
        AddScore(ghostScore);
        ghostsDead++;
        UpdateMusic();
    }

    public void GhostRevived()
    {
        ghostsDead = Mathf.Max(0, ghostsDead - 1);
        UpdateMusic();
    }

    private void StartDeadTimer(Ghostcontroller ghost)
    {
        Coroutine existing;
        if (deadTimers.TryGetValue(ghost, out existing) && existing != null) StopCoroutine(existing);
        deadTimers[ghost] = StartCoroutine(GhostDeadTimer(ghost));
    }
 
    private IEnumerator GhostDeadTimer(Ghostcontroller ghost)
    {
        yield return new WaitForSeconds(ghostDeadDuration);
 
        deadTimers.Remove(ghost);
 
        // Only revive if it is still dead (a reset may already have brought it back)
        if (ghost != null && ghost.State == GhostState.Dead)
        {
            // back to Scared/Recovering if the timer is still running, otherwise Normal
            ghost.SetState(GetGlobalGhostState());
            GhostRevived();
        }
    }

    public void PlayerHit()
    {
        if (State != GameState.Playing) return;

        State = GameState.Dying;
        lives--;

        StartCoroutine(DeathRoutine());
    }

    private void CheckAllPelletsEaten()
    {
        if (pelletsRemaining <= 0 && State == GameState.Playing)
        {
            StartCoroutine(GameOverRoutine());
        }
    }

    public GhostState GetGlobalGhostState()
    {
        if (scaredTimeLeft <= 0f) return GhostState.Normal;
        if (scaredTimeLeft <= recoverTime) return GhostState.Recovering;
        return GhostState.Scared;
    }

    private void UpdateScared()
    {
        if (scaredTimeLeft <= 0f) return;

        scaredTimeLeft = Mathf.Max(0f, scaredTimeLeft - Time.deltaTime);

        GhostState now = GetGlobalGhostState();
        if (now != appliedGhostState)
        {
            ApplyGhostState(now);
        }
    }

    private void ApplyGhostState(GhostState newState)
    {
        appliedGhostState = newState; //every ghost that is not dead, call ghost.SetState(newState)
        foreach (Ghostcontroller ghost in ghosts)
        {
            if (ghost != null && ghost.State != GhostState.Dead) ghost.SetState(newState);
        }
        UpdateMusic();
    }

    private void UpdateMusic()
    {
        AudioManager am = AudioManager.Instance;
        if (am == null) return;

        if (ghostsDead > 0) am.PlayMusic(am.enemyDeadMusic);
        else if (CurrentGhostState != GhostState.Normal) am.PlayMusic(am.enemyScaredMusic);
        else am.PlayMusic(am.enemyNormalMusic);
    }

    private void UpdateHud()
    {
        if (scoreText != null) scoreText.text = score.ToString("D6");
        if (gameTimerText != null) gameTimerText.text = FormatTime(gameTime);

        // one icon per remaining life
        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] != null) lifeIcons[i].SetActive(i < lives);
        }

        if (scaredTimerText != null)
        {
            scaredTimerText.text = scaredTimeLeft > 0f
                ? Mathf.CeilToInt(scaredTimeLeft).ToString()
                : "";
        }
    }

    public static string FormatTime(float t)
    {
        int minutes = (int)(t / 60f);
        int seconds = (int)(t % 60f);
        int centiseconds = (int)((t * 100f) % 100f);
        return minutes.ToString("D2") + ":" + seconds.ToString("D2") + ":" + centiseconds.ToString("D2");
    }

    private IEnumerator CountdownRoutine()
    {
        State = GameState.Countdown;
        SetPacStudentCollider(false);

        // empty countdown text and play music
        if (blockingImage != null) blockingImage.gameObject.SetActive(true);
        if (countdownText != null)
        {
            countdownText.text = "";
            countdownText.gameObject.SetActive(true);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(AudioManager.Instance.introMusic, false);
        }
        yield return null;
        pelletsRemaining = FindObjectsByType<Pellet>().Length;

        //321
        for (int i = 3; i >= 1; i--)
        {
            if (countdownText != null) countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        if (countdownText != null) countdownText.text = "GO!";
        yield return new WaitForSeconds(1f);

        //hide everything
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (blockingImage != null) blockingImage.gameObject.SetActive(false);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(AudioManager.Instance.enemyNormalMusic, true);
        }

        State = GameState.Playing;
        SetPacStudentCollider(true); // creates the Enter event for anything already touching it
    }

    private IEnumerator DeathRoutine()
    {
        SetPacStudentCollider(false);
        if (pacStudent != null)
        {
            pacStudent.PlayDeath();
            if (deathEffect != null) Instantiate(deathEffect, pacStudent.transform.position, Quaternion.identity);
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlayDeathSFX();

        yield return new WaitForSeconds(deathDuration);

        if (lives <= 0)
        {
            yield return StartCoroutine(GameOverRoutine());
            yield break;
        }

        ResetCharacters();
        State = GameState.Playing;
        SetPacStudentCollider(true);
    }

    private void ResetCharacters() //respawn now cancels all pending dead timers
    {
        if (pacStudent != null) pacStudent.ResetToStart();
 
        foreach (KeyValuePair<Ghostcontroller, Coroutine> timer in deadTimers)
        {
            if (timer.Value != null) StopCoroutine(timer.Value);
        }
        deadTimers.Clear();
 
        foreach (Ghostcontroller ghost in ghosts)
        {
            if (ghost != null) ghost.ResetToStart();
        }
 
        ghostsDead = 0;
        scaredTimeLeft = 0f;
        ApplyGhostState(GhostState.Normal);
    }

    private IEnumerator GameOverRoutine()
    {
        if (State == GameState.GameOver) yield break;
 
        // check for Playing state
        State = GameState.GameOver;
 
        if (exitButton != null) exitButton.interactable = false; // blocking image stops clicks 
        if (blockingImage != null) blockingImage.gameObject.SetActive(true);
        if (gameOverText != null) gameOverText.gameObject.SetActive(true);
 
        SaveHighScore();
 
        yield return new WaitForSeconds(gameOverDelay);
        SceneManager.LoadScene(startSceneName);
    }

        private void SaveHighScore()
    {
        int storedScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        float storedTime = PlayerPrefs.GetFloat(BestTimeKey, float.MaxValue);

        bool higherScore = score > storedScore;
        bool sameScoreFaster = score == storedScore && gameTime < storedTime;

        if (higherScore || sameScoreFaster)
        {
            PlayerPrefs.SetInt(HighScoreKey, score);
            PlayerPrefs.SetFloat(BestTimeKey, gameTime);
            PlayerPrefs.Save();
        }
    }

    private void SetPacStudentCollider(bool on)
    {
        if (pacStudentCollider != null) pacStudentCollider.enabled = on;
    }

    private void ExitToStart()
    {
        SceneManager.LoadScene(startSceneName);
    }
}