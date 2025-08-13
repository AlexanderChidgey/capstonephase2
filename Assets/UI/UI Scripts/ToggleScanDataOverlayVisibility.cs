using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class ToggleScanDataOverlayVisibility : MonoBehaviour
{
    [SerializeField] private string circleButtonName = "Circle_Btn";
    [SerializeField] private string backButtonName = "Back_Btn";
    [SerializeField] private string scrollViewName = "ScanDataScrollView";

    private VisualElement scrollView;
    private bool isVisible = true;

    private VisualElement resultsContainer;
    private Button circleButton;
    private Button backButton;

    private void OnEnable()
    {
        UIDocument uiDocumentComponent = GetComponent<UIDocument>();
        VisualElement root = uiDocumentComponent.rootVisualElement;

        circleButton = root.Q<Button>(circleButtonName);
        backButton = root.Q<Button>(backButtonName);
        scrollView = root.Q<VisualElement>(scrollViewName);
        resultsContainer = root.Q<VisualElement>("DetectionResultsContainer");

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
    }

    private void OnCircleButtonClicked()
    {
        //ToggleVisibility();

        if (resultsContainer != null)
        {
            bool isCurrentlyVisible = resultsContainer.style.display == DisplayStyle.Flex;

            if (isCurrentlyVisible == true)
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
            isVisible = true;
            Debug.Log("Overlay shown");
        }
    }

    private void HideOverlay()
    {
        if (scrollView != null)
        {
            scrollView.style.display = DisplayStyle.None;
            isVisible = false;
            Debug.Log("Overlay hidden");
        }
    }
}
