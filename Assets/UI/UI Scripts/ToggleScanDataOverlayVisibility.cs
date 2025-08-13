using System;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class ToggleScanDataOverlayVisibility : MonoBehaviour
{
    public string circleButtonName = "Circle_Btn"; 
    public string backButtonName = "Back_Btn";
    public string scrollViewName = "ScanDataScrollView";

    private VisualElement scrollView;
    private bool isVisible = true;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        var circleButton = root.Q<VisualElement>(circleButtonName);
        var backButton = root.Q<VisualElement>(backButtonName);
        scrollView = root.Q<VisualElement>(scrollViewName);

        if (circleButton != null)
        {
            circleButton.RegisterCallback<ClickEvent>(_ => ToggleVisibility());
            Console.WriteLine("Circle button registered");
        }

        if (backButton != null)
        {
            backButton.RegisterCallback<ClickEvent>(_ => HideOverlay());
            Console.WriteLine("Back button registered");
        }
    }

    void ToggleVisibility()
    {
        isVisible = !isVisible;

        if (isVisible)
        {
            ShowOverlay();
        }
        else
        {
            HideOverlay();
        }

        Console.WriteLine("Toggle visibility");
    }

    void ShowOverlay()
    {
        scrollView.style.display = DisplayStyle.Flex;
        Console.WriteLine("Overlay shown");
    }

    void HideOverlay()
    {
        scrollView.style.display = DisplayStyle.None;
        Console.WriteLine("Overlay hidden");
        isVisible = false;
    }
}
