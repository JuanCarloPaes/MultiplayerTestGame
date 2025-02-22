using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public void PlayGameButton(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void ExitButton()
    {
        Application.Quit();
    }

    public void ShowPopUp(GameObject go)
    {
        go.SetActive(true);
    }

    public void HidePopUp(GameObject go)
    {
        go.SetActive(false);
    }

    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
