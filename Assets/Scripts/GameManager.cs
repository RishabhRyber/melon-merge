using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("Game State")]
    public bool isGamePaused = false;
    public bool isGamePlaying = false;
    
    [Header("UI References")]
    public GameObject pauseMenuUI; // Assign your pause menu UI in inspector
    public GameObject gameUI; // Assign your main game UI in inspector
    
    [Header("Fruit Settings")]
    public Dictionary<string, string> nextFruitMapping;
    
    // Input System reference
    private InputSystem_Actions inputActions;
    
    // Events for other scripts to subscribe to
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;
    public static event Action OnGameStarted;
    public static event Action OnGameStopped;
    
    // Singleton pattern for easy access
    public static GameManager Instance { get; private set; }
    
    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        // Initialize input actions
        inputActions = new InputSystem_Actions();
    }
    
    void OnEnable()
    {
        inputActions.Enable();
        
        // Subscribe to pause input (you'll need to add this to your Input Actions)
        // For now, we'll use Keyboard.current as an alternative
        inputActions.UI.Cancel.performed += OnPauseInputPerformed;
    }
    
    void OnDisable()
    {
        inputActions.UI.Cancel.performed -= OnPauseInputPerformed;
        inputActions.Disable();
    }
    
    void OnDestroy()
    {
        inputActions?.Dispose();
        
        // Clean up events to prevent memory leaks
        OnGamePaused = null;
        OnGameResumed = null;
        OnGameStarted = null;
        OnGameStopped = null;
    }
    
    void Start()
    {
        // Initialize game state
        InitializeGame();
    }
    
    void Update()
    {
        // Alternative method using Keyboard.current from new Input System
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }
    
    // Input callback method
    private void OnPauseInputPerformed(InputAction.CallbackContext context)
    {
        TogglePause();
    }
    
    private void InitializeGame()
    {
        // Set initial game state
        isGamePlaying = false;
        isGamePaused = false;
        
        // Hide pause menu initially
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);
            
        // Show game UI
        if (gameUI != null)
            gameUI.SetActive(true);
    }
    
    #region Game State Management
    
    public void StartGame()
    {
        isGamePlaying = true;
        isGamePaused = false;
        Time.timeScale = 1f; // Normal time
        
        // Hide pause menu
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);
            
        // Show game UI
        if (gameUI != null)
            gameUI.SetActive(true);
        
        // Notify other scripts
        OnGameStarted?.Invoke();
        
        Debug.Log("Game Started!");
    }
    
    public void PauseGame()
    {
        if (!isGamePlaying) return; // Can't pause if not playing
        
        isGamePaused = true;
        Time.timeScale = 0f; // Freeze time
        
        // Show pause menu
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(true);
        
        // Notify other scripts
        OnGamePaused?.Invoke();
        
        Debug.Log("Game Paused!");
    }
    
    public void ResumeGame()
    {
        if (!isGamePaused) return; // Can't resume if not paused
        
        isGamePaused = false;
        Time.timeScale = 1f; // Resume normal time
        
        // Hide pause menu
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);
        
        // Notify other scripts
        OnGameResumed?.Invoke();
        
        Debug.Log("Game Resumed!");
    }
    
    public void StopGame()
    {
        isGamePlaying = false;
        isGamePaused = false;
        Time.timeScale = 1f; // Reset time scale
        
        // Hide all UI elements
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);
        if (gameUI != null)
            gameUI.SetActive(false);
        
        // Notify other scripts
        OnGameStopped?.Invoke();
        
        Debug.Log("Game Stopped!");
    }
    
    public void RestartGame()
    {
        StopGame();
        
        // Add any restart logic here (reset score, clear fruits, etc.)
        
        StartGame();
        Debug.Log("Game Restarted!");
    }
    
    #endregion
    
    #region Utility Methods
    
    public bool IsGameActive()
    {
        return isGamePlaying && !isGamePaused;
    }
    
    public void TogglePause()
    {
        if (isGamePaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }
    
    #endregion
}
