using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject levelCreatorPanel;
    public GameObject userCreatedLevelsPanel;

    [Header("Managers")]
    public LevelCreatorManager levelCreatorManager;
    public UserCreatedLevelsManager userCreatedLevelsManager;

    public bool isLevel1Active = false;

    void Start()
    {
        Debug.Log("GameManager: Initializing...");

        // Find managers if not assigned
        if (levelCreatorManager == null)
        {
            levelCreatorManager = FindObjectOfType<LevelCreatorManager>();
        }

        if (userCreatedLevelsManager == null)
        {
            userCreatedLevelsManager = FindObjectOfType<UserCreatedLevelsManager>();
        }

        // Show main menu at start
        ShowMainMenu();

        Debug.Log("GameManager: Ready");
    }

    public void ShowMainMenu()
    {
        Debug.Log("Showing main menu...");

        // Hide all other panels
        if (levelCreatorPanel != null)
        {
            levelCreatorPanel.SetActive(false);
        }

        if (userCreatedLevelsPanel != null)
        {
            userCreatedLevelsPanel.SetActive(false);
        }

        // Show main menu
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        isLevel1Active = false;

        Debug.Log("Main menu displayed");
    }

    public void StartLevelCreator()
    {
        Debug.Log("Starting Level Creator...");

        // Hide main menu
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        // Show level creator panel
        if (levelCreatorPanel != null)
        {
            levelCreatorPanel.SetActive(true);

            // Initialize level creator
            if (levelCreatorManager != null)
            {
                levelCreatorManager.InitializeLevelCreator();
            }
            else
            {
                Debug.LogError("LevelCreatorManager not found!");
            }
        }
        else
        {
            Debug.LogError("Level Creator Panel not assigned!");
        }

        Debug.Log("Level Creator started");
    }

    public void ShowUserCreatedLevels()
    {
        Debug.Log("Opening User Created Levels menu...");

        // Hide main menu
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        // Show user created levels panel
        if (userCreatedLevelsPanel != null)
        {
            userCreatedLevelsPanel.SetActive(true);

            // Initialize and load levels
            if (userCreatedLevelsManager != null)
            {
                userCreatedLevelsManager.InitializeUserLevelsMenu();
            }
            else
            {
                Debug.LogError("UserCreatedLevelsManager not found!");
            }
        }
        else
        {
            Debug.LogError("User Created Levels Panel not assigned!");
        }

        Debug.Log("User Created Levels menu opened");
    }

    public void StartLevel1()
    {
        Debug.Log("Starting Level 1...");
        isLevel1Active = true;

        // Hide main menu
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        // Add your Level 1 logic here
        Debug.Log("Level 1 started");
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}