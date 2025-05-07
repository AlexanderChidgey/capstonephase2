using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadScanScene()
    {
        SceneManager.LoadScene("Scan");
    }
    public void LoadMapScene()
    {
        SceneManager.LoadScene("Homepage");
    }
    

    // public void LoadAboutScene()
    // {
    //     SceneManage.LoadScene("Scan");
    // }
}