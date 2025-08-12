using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadScanScene()
    {
        SceneManager.LoadScene("Scan");
    }
    public void LoadHomeScreen()
    {
        SceneManager.LoadScene("Homepage");
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