using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HUDExit : MonoBehaviour
{
    [SerializeField] private Button exitButton;
    [SerializeField] private string startSceneName = "StartScene";

    private void Start()
    {
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitToStart);
        }
        else
        {
            Debug.LogWarning("HUDExit: Exit button is not assigned.");
        }
    }

    private void ExitToStart()
    {
        SceneManager.LoadScene(startSceneName);
    }
}