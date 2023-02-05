using System;
using System.Collections;
using System.Collections.Generic;
using TarodevController;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;
    public enum GameState { Game, Lose };
    
    private GameState _currentState;

    [SerializeField] private GameObject defeatScreen;
    [SerializeField] private GameObject pauseMenu;
    
    [SerializeField] private PlayerInput playerInput;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (playerInput.FrameInput.PauseDown)
        {
            Pause();
        }
    }

    private void Pause()
    {
        pauseMenu.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0;
    }
    
    public void Resume()
    {
        pauseMenu.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1;
    }

    public void SetGameState(GameState gameState)
    {
        if (_currentState == GameState.Game)
        {
            _currentState = gameState;
        }
        
        switch (_currentState)
        {
            case GameState.Lose:
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                defeatScreen.SetActive(true);
                defeatScreen.GetComponent<AudioSource>().Play();
                Time.timeScale = 0f;
                break;
            case GameState.Game:
            default:
                break;
        }
    }

    public void Delete()
    {
        Destroy(gameObject);
    }
}
