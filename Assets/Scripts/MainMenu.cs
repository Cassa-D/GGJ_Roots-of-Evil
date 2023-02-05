using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject OptionsMenu;
    
    public void StartGame()
    {
        SceneManager.LoadScene("Game_Level1");
    }

    public void Quit()
    {
        Application.Quit();
    }
    
    public void OpenOptions()
    {
        OptionsMenu.GetComponent<Animation>().Play("Fade In");
        OptionsMenu.transform.SetAsLastSibling();
        
        // Closes main menu
        GetComponent<Animation>().Play("MainMenuHide");
    }

    public void CloseOptions()
    {
        OptionsMenu.GetComponent<Animation>().Play("Fade out");
        
        // Shows main menu
        GetComponent<Animation>().Play("MainMenuShow");
    }
}
