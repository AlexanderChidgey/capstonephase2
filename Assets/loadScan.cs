using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadScanScene()
    {
        SceneManager.LoadScene("MainScene");
    }
    public void LoadHomeScreen()
    {
        SceneManager.LoadScene("ZoomableMap");
    }

    public void LoadMapScene()
    {
        SceneManager.LoadScene("ZoomableMap");
    }
    

    // public void LoadAboutScene()
    // {
    //     SceneManage.LoadScene("Scan");
    // }
}