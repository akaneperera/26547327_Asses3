using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameState { Countdown, Playing, Dying, GameOver }
public enum GhostState { Normal, Scared, Recovering }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("HUD")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text gameTimerText;
    [SerializeField] private TMP_Text scaredTimerText;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private Image blockingImage;
    [SerializeField] private GameObject[] lifeIcons;
    [SerializeField] private Button exitButton;

    [Header("Characters")]
    [SerializeField] private PacStudentController pacStudent;
    [SerializeField] private GameObject[] ghosts;

    [Header("Rules")]
    [SerializeField] private int startingLives = 3;
    [SerializeField] private int ghostScore = 300;
    [SerializeField] private float scaredDuration = 10f;
    [SerializeField] private float recoverTime = 3f;
    [SerializeField] private float deathDuration = 2f;
    [SerializeField] private float gameOverDelay = 3f;
    [SerializeField] private string startSceneName = "StartScene";

    // Shared state: other scripts only read these
    public GameState State { get; private set; } = GameState.Countdown;
    public GhostState CurrentGhostState { get; private set; } = GhostState.Normal;
    public int Score { get { return score; } }
    public int Lives { get { return lives; } }
    public int PelletsRemaining { get { return pelletsRemaining; } }
    public float GameTime { get { return gameTime; } }
    public float ScaredTimeLeft { get { return scaredTimeLeft; } }
    public int GhostsDead { get { return ghostsDead; } }

    private int score;
    private int lives;
    private int pelletsRemaining;
    private float gameTime;
    private float scaredTimeLeft;
    private int ghostsDead;

    private Vector3[] ghostStartPositions;
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
        ghostStartPositions = new Vector3[ghosts.Length];
        for (int i = 0; i < ghosts.Length; i++)
        {
            if (ghosts[i] != null) ghostStartPositions[i] = ghosts[i].transform.position;
        }

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
        if (Input.GetKeyDown(KeyCode.Space)) AddScore(10); // TEMP TEST
        UpdateHud();
    }

    // Public methods other scripts call
    public void AddScore(int points)
    {
        score += points;
    }

    public void PelletEaten()
    {
        pelletsRemaining--;
        CheckAllPelletsEaten();
    }

    public void PowerPillEaten()
    {
        pelletsRemaining--;
        scaredTimeLeft = scaredDuration;
        SetGhostState(GhostState.Scared);
        CheckAllPelletsEaten();
    }

    public void GhostEaten()
    {
        AddScore(ghostScore);
        ghostsDead++;
        UpdateMusic();
    }

    // Call when a dead ghost is back in the ghost house
    public void GhostRevived()
    {
        ghostsDead = Mathf.Max(0, ghostsDead - 1);
        UpdateMusic();
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

    private void UpdateScared()
    {
        if (CurrentGhostState == GhostState.Normal) return;

        scaredTimeLeft -= Time.deltaTime;

        if (scaredTimeLeft <= 0f)
        {
            scaredTimeLeft = 0f;
            SetGhostState(GhostState.Normal);
        }
        else if (scaredTimeLeft <= recoverTime && CurrentGhostState == GhostState.Scared)
        {
            SetGhostState(GhostState.Recovering);
        }
    }

    private void SetGhostState(GhostState newState)
    {
        CurrentGhostState = newState;
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

        // scared timer: empty when it isn't running
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

        // 1. overlay and (empty) countdown text
        if (blockingImage != null) blockingImage.gameObject.SetActive(true);
        if (countdownText != null)
        {
            countdownText.text = "";
            countdownText.gameObject.SetActive(true);
        }

        // intro music plays while the countdown runs
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(AudioManager.Instance.introMusic, false);
        }

        yield return null;
        pelletsRemaining = FindObjectsByType<Pellet>().Length;

        for (int i = 3; i >= 1; i--)
        {
            if (countdownText != null) countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        if (countdownText != null) countdownText.text = "GO!";
        yield return new WaitForSeconds(1f);

        //hide everything then start playing
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
        if (pacStudent != null) pacStudent.PlayDeath();
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

    private void ResetCharacters()
    {
        if (pacStudent != null) pacStudent.ResetToStart();

        for (int i = 0; i < ghosts.Length; i++)
        {
            if (ghosts[i] != null) ghosts[i].transform.position = ghostStartPositions[i];
        }

        ghostsDead = 0;
        scaredTimeLeft = 0f;
        SetGhostState(GhostState.Normal);
    }

    private IEnumerator GameOverRoutine()
    {
        State = GameState.GameOver;
        if (exitButton != null) exitButton.interactable = false;
        if (blockingImage != null) blockingImage.gameObject.SetActive(true);
        if (gameOverText != null) gameOverText.gameObject.SetActive(true);

        //save the high score and best time with PlayerPrefs here

        yield return new WaitForSeconds(gameOverDelay);
        SceneManager.LoadScene(startSceneName);
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