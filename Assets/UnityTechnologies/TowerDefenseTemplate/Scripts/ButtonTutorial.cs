using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonTutorial : MonoBehaviour
{
    public void TutorialMenu()
    {
        SceneManager.LoadScene("Tutorial");
    }
    public void MainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

}
