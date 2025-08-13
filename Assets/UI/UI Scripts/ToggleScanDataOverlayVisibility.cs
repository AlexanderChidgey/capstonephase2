using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class ToggleScanDataOverlayVisibility : MonoBehaviour
{
<<<<<<< HEAD
    [SerializeField] private string circleButtonName = "Circle_Btn";
    [SerializeField] private string backButtonName = "Back_Btn";
    [SerializeField] private string scrollViewName = "ScanDataScrollView";
=======
    [Header("UI Element Names")]
    public string circleButtonName = "Circle_Btn";
    public string backButtonName = "Back_Btn";
    public string scrollViewName = "ScanDataScrollView";
>>>>>>> cbffa882d9965374648fddf0709ca2be865a35a5

    private VisualElement scrollView;
    private bool isVisible = true;

<<<<<<< HEAD
    private VisualElement resultsContainer;
    private Button circleButton;
    private Button backButton;

    private void OnEnable()
=======
    void OnEnable()
>>>>>>> cbffa882d9965374648fddf0709ca2be865a35a5
    {
        UIDocument uiDocumentComponent = GetComponent<UIDocument>();
        VisualElement root = uiDocumentComponent.rootVisualElement;

<<<<<<< HEAD
        circleButton = root.Q<Button>(circleButtonName);
        backButton = root.Q<Button>(backButtonName);
=======
        // Get UI elements
        var circleButton = root.Q<Button>(circleButtonName);
        var backButton = root.Q<Button>(backButtonName);
>>>>>>> cbffa882d9965374648fddf0709ca2be865a35a5
        scrollView = root.Q<VisualElement>(scrollViewName);
        resultsContainer = root.Q<VisualElement>("DetectionResultsContainer");

        // Circle button click
        if (circleButton != null)
        {
            circleButton.clicked += OnCircleButtonClicked;
            Debug.Log("Circle button registered");
        }
        else
        {
<<<<<<< HEAD
            Debug.LogWarning("Button '" + circleButtonName + "' not found in UI.");
=======
            Debug.LogWarning($"Button '{circleButtonName}' not found.");
>>>>>>> cbffa882d9965374648fddf0709ca2be865a35a5
        }

        // Back button click
        if (backButton != null)
<<<<<<< HEAD
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
=======
>>>>>>> cbffa882d9965374648fddf0709ca2be865a35a5
        {
            backButton.clicked += HideOverlay;
            Debug.Log("Back button registered");
        }
        else
        {
            Debug.LogWarning($"Button '{backButtonName}' not found.");
        }
    }

<<<<<<< HEAD
    private void HideOverlay()
    {
        if (scrollView != null)
        {
            scrollView.style.display = DisplayStyle.None;
            isVisible = false;
            Debug.Log("Overlay hidden");
        }
=======
    public void ToggleVisibility()
    {
        isVisible = !isVisible;

        if (isVisible)
            ShowOverlay();
        else
            HideOverlay();

        Debug.Log($"Overlay visibility toggled: {(isVisible ? "Shown" : "Hidden")}");
    }

    public void ShowOverlay()
    {
        if (scrollView == null) return;
        scrollView.style.display = DisplayStyle.Flex;
        isVisible = true;
        Debug.Log("Overlay shown");
    }

    public void HideOverlay()
    {
        if (scrollView == null) return;
        scrollView.style.display = DisplayStyle.None;
        isVisible = false;
        Debug.Log("Overlay hidden");
>>>>>>> cbffa882d9965374648fddf0709ca2be865a35a5
    }
}
