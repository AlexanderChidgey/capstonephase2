using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    public void GoToSceneOverlayScan()
    {
        SceneManager.LoadScene("ScanSceneOverlay");
    }

    public void GoToSceneScan()
    {
        SceneManager.LoadScene("ScanScene");
    }
}
