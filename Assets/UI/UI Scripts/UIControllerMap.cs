// UIControllerMap.cs - Lightweight controller for the bottom navigation buttons on the Map scene
// (when not using the MapOverlayController singleton). It simply binds buttons to scene loads.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UIControllerMap : MonoBehaviour
{
    [Header("UXML Element Names")]
    [SerializeField] private string mapButtonName = "Map_Btn";
    [SerializeField] private string scanButtonName = "Scan_Btn";
    [SerializeField] private string historyButtonName = "History_Btn";

    private UIDocument uiDocument;
    private VisualElement root;

    private Button mapButton;
    private Button scanButton;
    private Button historyButton;
    private VisualElement resultsContainer;

    private Button circleButton;
    private Button backButton;


    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument component is missing!");
        }
    }

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument reference is not assigned in UIController!");
            return;
        }

        root = uiDocument.rootVisualElement;

        BindButtons();
    }

    private void OnDisable()
    {
        UnbindButtons();
    }
    private void OnMapButtonClicked()
    {
        string sceneName = "ZoomableMap";
        Debug.Log("Loading scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }

    private void OnScanButtonClicked()
    {
        string sceneName = "MainScene";
        
        SceneManager.LoadScene(sceneName);
    }
    private void OnHistoryButtonClicked()
    {
        string sceneName = "HistoryLogScene";
        Debug.Log("Loading scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }

    private void BindButtons()
    {
        UnbindButtons();

        if (root == null)
        {
            return;
        }

        mapButton = root.Q<Button>(mapButtonName);
        scanButton = root.Q<Button>(scanButtonName);
        historyButton = root.Q<Button>(historyButtonName);

        if (mapButton != null)
        {
            mapButton.clicked += OnMapButtonClicked;
            Debug.Log("Map button registered");
        }
        else
        {
            Debug.LogWarning("Button '" + mapButtonName + "' not found in UI.");
        }
        if (scanButton != null)
        {
            scanButton.clicked += OnScanButtonClicked;
            Debug.Log("Scan button registered");
        }
        else
        {
            Debug.LogWarning("Button '" + scanButtonName + "' not found in UI.");
        }
        if (historyButton != null)
        {
            historyButton.clicked += OnHistoryButtonClicked;
            Debug.Log("Map button registered");
        }
        else
        {
            Debug.LogWarning("Button '" + historyButtonName + "' not found in UI.");
        }
    }

    private void UnbindButtons()
    {
        if (mapButton != null)
        {
            mapButton.clicked -= OnMapButtonClicked;
            mapButton = null;
        }
        if (scanButton != null)
        {
            scanButton.clicked -= OnScanButtonClicked;
            scanButton = null;
        }
        if (historyButton != null)
        {
            historyButton.clicked -= OnHistoryButtonClicked;
            historyButton = null;
        }
    }

    public void RefreshButtons()
    {
        root = uiDocument != null ? uiDocument.rootVisualElement : null;
        BindButtons();
    }
}

