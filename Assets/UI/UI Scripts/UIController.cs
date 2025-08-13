using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class UIController : MonoBehaviour
{
    private VisualElement root;
    private UIDocument uiDocument;
    private ToggleScanDataOverlayVisibility toggleOverlay;


    private Button[] detectionButtons = new Button[3];
    private Label[] detectionNameLabels = new Label[3];
    private Label[] detectionIdLabels = new Label[3];

    private List<ObjectDetectionHandler.MatchInfo> currentMatches;
    void Awake()
    {
        toggleOverlay = GetComponent<ToggleScanDataOverlayVisibility>();
        if (toggleOverlay == null)
        {
            Debug.LogWarning("ToggleScanDataOverlayVisibility component not found on this GameObject!");
        }
        if (uiDocument == null)
        uiDocument = GetComponent<UIDocument>();

    }

    void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument reference is not assigned in UIController!");
            return;
        }

        root = uiDocument.rootVisualElement;

        for (int i = 0; i < 3; i++)
        {
            detectionButtons[i] = root.Q<Button>($"Detection{i + 1}Button");
            if (detectionButtons[i] != null)
            {
                detectionNameLabels[i] = detectionButtons[i].Q<Label>($"Detection{i + 1}Label");
                detectionIdLabels[i] = detectionButtons[i].Q<Label>($"Detection{i + 1}Id");
            }
        }
    }

    public void UpdateDetectionUI(List<ObjectDetectionHandler.MatchInfo> matches)
    {
        currentMatches = matches;

        for (int i = 0; i < detectionButtons.Length; i++)
        {
            if (detectionButtons[i] == null) continue;

            detectionButtons[i].clicked -= () => OnDetectionButtonClicked(i);  // Remove any previous listeners

            if (i < matches.Count)
            {
                detectionNameLabels[i].text = matches[i].Name;
                detectionIdLabels[i].text = matches[i].ID;
                detectionButtons[i].style.display = DisplayStyle.Flex;

                int index = i;
                detectionButtons[i].clicked += () => OnDetectionButtonClicked(index);

            }
            else
            {
                detectionNameLabels[i].text = "";
                detectionIdLabels[i].text = "";
                detectionButtons[i].style.display = DisplayStyle.None;
            }
        }
    }

    private void OnDetectionButtonClicked(int index)
    {
        for (int i = 0; i < detectionButtons.Length; i++)
        {
            if (detectionButtons[i] != null)
            {
                detectionButtons[i].style.display = DisplayStyle.None;
                Debug.Log($"Removed button {i}");
            }
        }
        if (currentMatches == null || index < 0 || index >= currentMatches.Count) return;

        var match = currentMatches[index];
        DetectionDataStore.SelectedName = match.Name;
        DetectionDataStore.SelectedId = match.ID;

        if (toggleOverlay != null)
            toggleOverlay.ShowOverlay();
        else
            Debug.LogWarning("ToggleScanDataOverlayVisibility reference not assigned!");
    }
}
