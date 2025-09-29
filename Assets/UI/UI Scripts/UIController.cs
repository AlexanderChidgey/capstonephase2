using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UIController : MonoBehaviour
{
    [Header("UXML Element Names")]
    [SerializeField] private string circleButtonName = "Circle_Btn";
    [SerializeField] private string backButtonName = "Back_Btn";
    [SerializeField] private string scrollViewName = "ScanDataScrollView";
    [SerializeField] private string resultsContainerName = "DetectionResultsContainer";
    [SerializeField] private string mapButtonName = "Map_Btn";

    private UIDocument uiDocument;
    private VisualElement root;
    

    private VisualElement scrollView;
    private Button mapButton;
    private VisualElement resultsContainer;

    private Button circleButton;
    private Button backButton;

    private Button[] detectionButtons = new Button[3];
    private Label[] detectionNameLabels = new Label[3];
    private Label[] detectionIdLabels = new Label[3];

    private Action[] detectionHandlers = new Action[3];

    private List<ObjectDetectionHandler.MatchInfo> currentMatches;
    private bool isOverlayVisible = true;

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

        circleButton = root.Q<Button>(circleButtonName);
        backButton = root.Q<Button>(backButtonName);
        scrollView = root.Q<VisualElement>(scrollViewName);
        mapButton = root.Q<Button>(mapButtonName);

        resultsContainer = root.Q<VisualElement>(resultsContainerName);

        if (circleButton != null)
        {
            circleButton.clicked += OnCircleButtonClicked;
            Debug.Log("Circle button registered");
        }
        else
        {
            Debug.LogWarning("Button '" + circleButtonName + "' not found in UI.");
        }
        if (mapButton != null)
        {
            mapButton.clicked += OnMapButtonClicked;
            Debug.Log("Map button registered");
        }
        else
        {
            Debug.LogWarning("Button '" + mapButtonName + "' not found in UI.");
        }



        if (backButton != null)
        {
            backButton.clicked += OnBackButtonClicked;
            Debug.Log("Back button registered");
        }
        else
        {
            Debug.LogWarning("Button '" + backButtonName + "' not found in UI.");
        }

        if (scrollView != null)
        {
            scrollView.style.display = DisplayStyle.None;
            Debug.Log("scrollView hidden by default.");
        }

        if (resultsContainer != null)
        {
            resultsContainer.style.display = DisplayStyle.None;
            Debug.Log("DetectionResultsContainer hidden by default.");
        }

        for (int i = 0; i < 3; i++)
        {
            detectionButtons[i] = root.Q<Button>($"Detection{i + 1}Button");

            if (detectionButtons[i] != null)
            {
                detectionNameLabels[i] = detectionButtons[i].Q<Label>($"Detection{i + 1}Label");
                detectionIdLabels[i] = detectionButtons[i].Q<Label>($"Detection{i + 1}Id");
                Debug.Log($"Detection button {i + 1} found: {detectionButtons[i].name}");
            }
            else {
                Debug.LogWarning($"Detection button {i + 1} not found in UXML.");
            }
        }
    }

    private void OnDisable()
    {
        if (circleButton != null)
        {
            circleButton.clicked -= OnCircleButtonClicked;
        }
        if (backButton != null)
        {
            backButton.clicked -= OnBackButtonClicked;
        }
        if (mapButton != null)
        {
            mapButton.clicked -= OnMapButtonClicked;
        }

        for (int i = 0; i < detectionButtons.Length; i++)
        {
            if (detectionButtons[i] != null && detectionHandlers[i] != null)
            {
                detectionButtons[i].clicked -= detectionHandlers[i];
            }
        }
    }

    private void OnCircleButtonClicked()
    {
        if (resultsContainer == null)
        {
            return;
        }

        bool isCurrentlyVisible = resultsContainer.style.display == DisplayStyle.Flex;

        if (isCurrentlyVisible)
        {
            resultsContainer.style.display = DisplayStyle.None;
            Debug.Log("DetectionResultsContainer hidden.");
        }
        else
        {
            resultsContainer.style.display = DisplayStyle.Flex;
            Debug.Log("DetectionResultsContainer shown.");
        }
    }
    private void OnMapButtonClicked()
    {
        string sceneName = "ZoomableMap"; 
        Debug.Log("Loading scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }


    private void OnBackButtonClicked()
    {
        HideOverlay();
    }

    public void ShowOverlay()
    {
        if (scrollView != null)
        {
            scrollView.style.display = DisplayStyle.Flex;
            isOverlayVisible = true;
            Debug.Log("Overlay shown");
        }
        else
        {
            Debug.LogError("scrollView is null. Cannot show overlay.");
        }
    }

    private void HideOverlay()
    {
        if (scrollView != null)
        {
            scrollView.style.display = DisplayStyle.None;
            isOverlayVisible = false;
            Debug.Log("Overlay hidden");
        }
        else
        {
            Debug.LogError("scrollView is null. Cannot hide overlay.");
        }
    }

    public void UpdateDetectionUI(List<ObjectDetectionHandler.MatchInfo> matches)
{
    currentMatches = matches ?? new List<ObjectDetectionHandler.MatchInfo>();
    Debug.Log("UpdateDetectionUI called with " + (matches?.Count ?? 0) + " matches.");

        for (int i = 0; i < detectionButtons.Length; i++)
    {
        if (detectionButtons[i] == null) continue;

        // Remove previous handler if it exists
        if (detectionHandlers[i] != null)
        {
            detectionButtons[i].clicked -= detectionHandlers[i];
        }

        if (i < currentMatches.Count)
        {
            detectionNameLabels[i].text = currentMatches[i].Name;
            detectionIdLabels[i].text = currentMatches[i].ID;
            detectionButtons[i].style.display = DisplayStyle.Flex;

            int index = i; // capture fixed index
            detectionHandlers[i] = () => OnDetectionButtonClicked(index);
            detectionButtons[i].clicked += detectionHandlers[i];
        }
        else
        {
            detectionNameLabels[i].text = "";
            detectionIdLabels[i].text = "";
            detectionButtons[i].style.display = DisplayStyle.None;

            detectionHandlers[i] = null;
        }
    }
}


    private void OnDetectionButtonClicked(int i)
    {
        if (resultsContainer != null)
        {
            resultsContainer.style.display = DisplayStyle.None;
            Debug.Log("Results container hidden.");
        }
        else
        {
            Debug.LogWarning("resultsContainer is null; cannot hide results.");
        }

        Debug.Log("OnDetectionButtonClicked invoked for index " + i.ToString());

        // Here, validate whether we can safely access currentMatches[index].
        bool canAccessMatch = false;
        int matchesCount = 0;

        if (currentMatches == null)
        {
            Debug.LogWarning("No detection matches available (currentMatches is null).");
        }
        else
        {
            matchesCount = currentMatches.Count;
            if (i < 0 || i >= matchesCount)
            {
                Debug.LogWarning(
                    "Invalid detection index: " + i.ToString() +
                    ". Valid range is 0.." + (matchesCount - 1).ToString() + "."
                );
            }
            else
            {
                canAccessMatch = true;
            }
        }

        if (canAccessMatch)
        {
            ObjectDetectionHandler.MatchInfo match = currentMatches[i];
            if (match == null)
            {
                Debug.LogWarning("Match at index " + i.ToString() + " is null.");
            }
            else
            {
                if (string.IsNullOrEmpty(match.Name))
                {
                    Debug.LogWarning("Match.Name is null or empty.");
                }
                else
                {
                    DetectionDataStore.SelectedName = match.Name;
                }

                if (string.IsNullOrEmpty(match.ID))
                {
                    Debug.LogWarning("Match.ID is null or empty.");
                }
                else
                {
                    DetectionDataStore.SelectedId = match.ID;
                }
            }
        }
        //for now, overlay of electrical object data will always be visible (even if data is invalid or missing)!! 
        ShowOverlay();
    }

}



