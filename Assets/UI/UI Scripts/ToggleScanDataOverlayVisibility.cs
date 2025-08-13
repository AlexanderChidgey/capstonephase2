using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class ToggleScanDataOverlayVisibility : MonoBehaviour
{
    [Header("UI Element Names")]
    public string circleButtonName = "Circle_Btn";
    public string backButtonName = "Back_Btn";
    public string scrollViewName = "ScanDataScrollView";

    private VisualElement scrollView;
    private bool isVisible = true;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Get UI elements
        var circleButton = root.Q<Button>(circleButtonName);
        var backButton = root.Q<Button>(backButtonName);
        scrollView = root.Q<VisualElement>(scrollViewName);

        // Circle button click
        if (circleButton != null)
        {
            circleButton.clicked += ToggleVisibility;
            Debug.Log("Circle button registered");
        }
        else
        {
            Debug.LogWarning($"Button '{circleButtonName}' not found.");
        }

        // Back button click
        if (backButton != null)
        {
            backButton.clicked += HideOverlay;
            Debug.Log("Back button registered");
        }
        else
        {
            Debug.LogWarning($"Button '{backButtonName}' not found.");
        }
    }

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
    }
}
