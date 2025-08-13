using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UIController : MonoBehaviour
{
    [Header("UXML Element Names")]
    [SerializeField] private string circleButtonName = "Circle_Btn";
    [SerializeField] private string backButtonName = "Back_Btn";
    [SerializeField] private string scrollViewName = "ScanDataScrollView";
    [SerializeField] private string resultsContainerName = "DetectionResultsContainer";

    private UIDocument uiDocument;
    private VisualElement root;

    private VisualElement scrollView;
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

        if (backButton != null)
        {
            backButton.clicked += OnBackButtonClicked;
            Debug.Log("Back button registered");
        }
        else
        {
            Debug.LogWarning("Button '" + backButtonName + "' not found in UI.");
        }

        if (resultsContainer != null)
        {
            resultsContainer.style.display = DisplayStyle.None;
            Debug.Log("DetectionResultsContainer hidden by default.");
        }

        for (int i = 0; i < 3; i++)
        {
            string buttonName = "Detection" + (i + 1).ToString() + "Button";
            Button detectionButton = root.Q<Button>(buttonName);

            if (detectionButton == null)
            {
                Debug.LogWarning("'" + buttonName + "' not found in UI.");
                continue;
            }

            detectionButtons[i] = detectionButton;

            string labelName = "Detection" + (i + 1).ToString() + "Label";
            detectionNameLabels[i] = detectionButton.Q<Label>(labelName);
            if (detectionNameLabels[i] == null)
            {
                Debug.LogWarning("Label '" + labelName + "' not found under " + buttonName + ".");
            }

            string idName = "Detection" + (i + 1).ToString() + "Id";
            detectionIdLabels[i] = detectionButton.Q<Label>(idName);
            if (detectionIdLabels[i] == null)
            {
                Debug.LogWarning("Label '" + idName + "' not found under " + buttonName + ".");
            }

            int index = i;
            detectionHandlers[i] = delegate
            {
                Debug.Log("Detection " + (index + 1).ToString() + " button clicked.");
                OnDetectionButtonClicked(index);
            };

            detectionButton.clicked += detectionHandlers[i];
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
        if (matches == null)
        {
            matches = new List<ObjectDetectionHandler.MatchInfo>();
        }

        currentMatches = matches;
        Debug.Log("UpdateDetectionUI called with " + currentMatches.Count.ToString() + " matches.");

        for (int i = 0; i < detectionButtons.Length; i++)
        {
            Button btn = detectionButtons[i];
            if (btn == null)
            {
                continue;
            }

            if (i < currentMatches.Count)
            {
                ObjectDetectionHandler.MatchInfo match = currentMatches[i];

                if (detectionNameLabels[i] != null)
                {
                    detectionNameLabels[i].text = match != null ? match.Name : "";
                }

                if (detectionIdLabels[i] != null)
                {
                    detectionIdLabels[i].text = match != null ? match.ID : "";
                }

                btn.style.display = DisplayStyle.Flex;
            }
            else
            {
                if (detectionNameLabels[i] != null)
                {
                    detectionNameLabels[i].text = "";
                }
                if (detectionIdLabels[i] != null)
                {
                    detectionIdLabels[i].text = "";
                }
                btn.style.display = DisplayStyle.None;
            }
        }

        if (resultsContainer != null)
        {
            resultsContainer.style.display = DisplayStyle.Flex;
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
