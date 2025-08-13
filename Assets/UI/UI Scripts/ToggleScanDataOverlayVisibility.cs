// ChatGPT cleaned Up
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

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        var circleButton = root.Q<Button>(circleButtonName);
        var backButton = root.Q<Button>(backButtonName);
        scrollView = root.Q<VisualElement>(scrollViewName);

        if (circleButton != null)
        {
            circleButton.clicked += ToggleVisibility;
            Debug.Log("Circle button registered");
        }
        else
        {
            Debug.LogWarning($"Button '{circleButtonName}' not found in UI.");
        }

        if (backButton != null)
            {
                backButton.clicked += HideOverlay;
                Debug.Log("Back button registered");
            }
            else
            {
                Debug.LogWarning($"Button '{backButtonName}' not found in UI.");
            }
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



    private void ToggleVisibility()
    {
        isVisible = !isVisible;
        scrollView.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        Debug.Log($"Overlay visibility toggled: {(isVisible ? "Shown" : "Hidden")}");
    }

    private void HideOverlay()
    {
        scrollView.style.display = DisplayStyle.None;
        isVisible = false;
        Debug.Log("Overlay hidden");
    }
}
