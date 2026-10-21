using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public AudioSource musicSource;
    public AudioSource sfxSource;

    //music clips
    public AudioClip introMusic;
    public AudioClip menuMusic;
    public AudioClip enemyNormalMusic;
    public AudioClip enemyScaredMusic;
    public AudioClip enemyDeadMusic;

    // sfx clips
    public AudioClip moveSfx;
    public AudioClip pelletSfx;
    public AudioClip eatEnemySfx;
    public AudioClip foodSfx;
    public AudioClip wallHitSfx;
    public AudioClip deathSfx;

    private Coroutine introRoutine; // so the intro can be cancelled or restarted

    private void Awake() // makes sure there's only 1 audiomanager and also it can surive scene changes. other scripts can call it
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // duplicate only appears when the level scene is loaded again.
            Instance.IntroToEnemy();
            Destroy(gameObject);
        }
    }

    // methods to play sfx
    public void PlayMoveSFX() {sfxSource.PlayOneShot(moveSfx);}
    public void PlayPelletSFX() {sfxSource.PlayOneShot(pelletSfx);}
    public void PlayEatEnemySFX() {sfxSource.PlayOneShot(eatEnemySfx);}
    public void PlayFoodSFX() {sfxSource.PlayOneShot(foodSfx);}
    public void PlayWallHitSFX() {sfxSource.PlayOneShot(wallHitSfx);}
    public void PlayDeathSFX() {sfxSource.PlayOneShot(deathSfx);}

    public void PlayMusic(AudioClip clip, bool loop = true) // don't restart same track
    {
        if (clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void StopMusic()
    {
        // cancel a running intro
        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }
        musicSource.Stop();
    }

    public void IntroToEnemy() // uses coroutine so that method can pause itself and wait for a condition (intro music still playing)
    {
        if (introRoutine != null) StopCoroutine(introRoutine);
        introRoutine = StartCoroutine(Intro());
    }

    private System.Collections.IEnumerator Intro()
    {
        musicSource.clip = introMusic; // start with intro music
        musicSource.loop = false;
        musicSource.Play();

        //then wait for music to end or 3 seconds
        float  timer = 0f;
        while (timer < 3f && musicSource.isPlaying)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        PlayMusic(enemyNormalMusic, true);
        introRoutine = null;
    }

    void Start()
    {
        if (Instance != this) return; // a duplicate is about to be destroyed
        IntroToEnemy();
    }
    void Update()
    {

    }
}