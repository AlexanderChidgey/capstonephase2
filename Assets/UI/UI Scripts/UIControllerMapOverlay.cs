using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UIControllerMapOverlay : MonoBehaviour
{
    [Header("UXML Element Names")]
    [SerializeField] private string backButtonName = "Back_Btn";
    [SerializeField] private string scrollViewName = "ScanDataScrollView";
    [SerializeField] private string infoLabelName = "serialNumberModified"; 

    private UIDocument uiDocument;
    private VisualElement root;

    private VisualElement scrollView;
    private Button backButton;
    private Label infoLabel;

    private bool isOverlayVisible = false;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("[UIControllerMapOverlay] UIDocument component is missing!");
        }
    }

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[UIControllerMapOverlay] No UIDocument assigned!");
            return;
        }

        root = uiDocument.rootVisualElement;

        // Query elements
        backButton = root.Q<Button>(backButtonName);
        scrollView = root.Q<VisualElement>(scrollViewName);
        infoLabel = root.Q<Label>(infoLabelName);

        // Debug which elements were found
        Debug.Log($"[UIControllerMapOverlay] backButton = {(backButton != null ? "FOUND" : "NULL")}");
        Debug.Log($"[UIControllerMapOverlay] scrollView = {(scrollView != null ? "FOUND" : "NULL")}");
        Debug.Log($"[UIControllerMapOverlay] infoLabel = {(infoLabel != null ? "FOUND" : "NULL")}");

        if (backButton != null)
        {
            backButton.clicked += OnBackButtonClicked;
        }

        if (scrollView != null)
        {
            scrollView.style.display = DisplayStyle.None; // hidden by default
            Debug.Log("[UIControllerMapOverlay] scrollView hidden by default.");
        }
    }

    private void OnDisable()
    {
        if (backButton != null)
        {
            backButton.clicked -= OnBackButtonClicked;
        }
    }

    private void OnBackButtonClicked()
    {
        HideOverlay();
    }

    public void ShowOverlay(string infoText)
    {
        Debug.Log("[UIControllerMapOverlay] ShowOverlay called!");

        if (scrollView != null)
        {
            Debug.Log($"Before: scrollView display = {scrollView.resolvedStyle.display}");
            scrollView.style.display = DisplayStyle.Flex;
            Debug.Log($"After: scrollView display = {scrollView.resolvedStyle.display}");
            isOverlayVisible = true;

            if (infoLabel != null)
            {
                infoLabel.text = infoText;
                Debug.Log($"[UIControllerMapOverlay] Updated label '{infoLabelName}' text: {infoLabel.text}");
            }
            else
            {
                Debug.LogWarning($"[UIControllerMapOverlay] Label '{infoLabelName}' was not found!");
            }
        }
        else
        {
            Debug.LogError("[UIControllerMapOverlay] scrollView is null in ShowOverlay!");
        }
    }

    public void HideOverlay()
    {
        if (scrollView != null)
        {
            scrollView.style.display = DisplayStyle.None;
            isOverlayVisible = false;
            Debug.Log("[UIControllerMapOverlay] Overlay hidden.");
        }
        else
        {
            Debug.LogError("[UIControllerMapOverlay] scrollView is null in HideOverlay!");
        }
    }
}
