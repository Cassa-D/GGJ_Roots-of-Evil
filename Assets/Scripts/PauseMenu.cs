using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public void Continue()
    {
        GameStateManager.Instance.Resume();
    }
    
    public void Quit()
    {
        Application.Quit();
    }
    
    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
        GameStateManager.Instance.Delete();
    }
}
