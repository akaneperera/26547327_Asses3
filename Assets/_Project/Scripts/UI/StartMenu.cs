using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenu : MonoBehaviour
{
    [SerializeField] private Button level1Button;
    [SerializeField] private Button level2Button; // not wired yet (Level 2 is innovation-band only)
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
            Debug.LogWarning("Level 1 button is not assigned.");
        }

        if (menuMusic != null && !menuMusic.isPlaying)
        {
            menuMusic.Play();
        }
    }

    private void LoadLevel1()
    {
        SceneManager.LoadScene(level1SceneName);
    }
}