// MapOverlayController.cs - Singleton responsible for instantiating the map overlay UXML and wiring nav buttons.
// Responsibilities:
//  - Clone the ScanDataOverlay visual tree at runtime and keep it alive across scene loads
//  - Manage bottom-nav button bindings so they always navigate to the correct scenes
//  - Provide helper methods to show/hide the overlay and ensure the nav stays interactive
//
// NOTE: This controller is referenced by the Mapbox map scripts, so it persists via DontDestroyOnLoad.

using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class MapOverlayController : MonoBehaviour
{
    public static MapOverlayController Instance { get; private set; }

    [Header("Setup")]
    [SerializeField] private PanelSettings panelSettings;
    [SerializeField] private VisualTreeAsset overlayAsset;

    [Header("Bottom Nav Button Names")]
    [SerializeField] private string scanButtonName = "Scan_Btn";
    [SerializeField] private string mapButtonName = "Map_Btn";
    [SerializeField] private string historyButtonName = "History_Btn";
    [SerializeField] private string aboutButtonName = "About_Btn";

    private UIDocument _doc;
    private VisualElement _overlay;  
    
    private ScrollView _scrollView;
    private Button _scanButton;
    private Button _mapButton;
    private Button _historyButton;
    private Button _aboutButton;
    
    
    
    // Popup Stuff "Labels"
    private Label _objectType; // Type of the object i.e. Power Pole/Pillar Box
    private Label _idNumber; // Unique Identifier object in table
    private Label _serialNumberModified; // Number on the object
    private Label _maxKVA; // Maximum KVA of the object
    private Label _maxVolt; // Maximum Volt of the object
    private Label _updated; // Updated date of the object
    private Label _location; // Site of the object
    private Label _lastUpdated; // Last Updated date of the object in the database

    public bool IsOpen { get; private set; } = false;


    private Button _backBtn;                  // "Back_Btn"
    private Coroutine _initRoutine;

    private void Awake()
    {
        Debug.Log("[MapOverlay] Awake started");
        
        if (Instance != null && Instance != this) 
        { 
            Debug.Log("[MapOverlay] Destroying duplicate instance");
            Destroy(gameObject); 
            return; 
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[MapOverlay] Instance set");

        // Ensure a UIDocument exists
        _doc = GetComponent<UIDocument>();
        if (_doc == null)
        {
            Debug.Log("[MapOverlay] No UIDocument found, adding one");
            _doc = gameObject.AddComponent<UIDocument>();
        }
        Debug.Log("[MapOverlay] UIDocument ready");

        // Ensure panel settings are assigned
        if (_doc.panelSettings == null && panelSettings != null)
        {
            _doc.panelSettings = panelSettings;
            Debug.Log("[MapOverlay] Applied fallback PanelSettings");
        }

        if (_doc.panelSettings == null)
        {
            Debug.LogError("[MapOverlay] UIDocument has no PanelSettings assigned.");
        }
    }

    private void OnEnable()
    {

        if (_doc == null)
        {
            Debug.LogError("[MapOverlay] UIDocument not available on enable");
            return;
        }

        if (_initRoutine == null)
        {
            _initRoutine = StartCoroutine(EnsureInitialized());
        }

        Hide();
    }

    private void OnDisable()
    {
        if (_initRoutine != null)
        {
            StopCoroutine(_initRoutine);
            _initRoutine = null;
        }

        if (_backBtn != null)
        {
            _backBtn.clicked -= Hide;
        }
    }

    private IEnumerator EnsureInitialized()
    {
        Debug.Log("[MapOverlay] Waiting for panel to initialise");

        // Wait until rootVisualElement is available
        while (_doc.rootVisualElement == null)
        {
            yield return null;
        }

        BuildOverlay();
        _initRoutine = null;
    }

    private void BuildOverlay()
    {
        var root = _doc.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("[MapOverlay] Cannot build overlay — rootVisualElement is null");
            return;
        }
        root.Clear();

        var vta = ResolveOverlayAsset();
        if (vta == null)
        {
            Debug.LogError("[MapOverlay] No VisualTreeAsset could be resolved for the overlay");
            return;
        }

        var tree = vta.CloneTree();
        tree.name = "ScanDataOverlayRoot";
        root.Add(tree);

        _overlay = tree.Q<VisualElement>("ScanDataOverlay") ?? tree;
        _scrollView = _overlay.Q<ScrollView>("ScanDataScrollView");

        _scanButton = root.Q<Button>(scanButtonName);
        _mapButton = root.Q<Button>(mapButtonName);
        _historyButton = root.Q<Button>(historyButtonName);
        _aboutButton = root.Q<Button>(aboutButtonName);

        WireNavButtons();

        _objectType = tree.Q<Label>("ObjectType_Label");
        _idNumber = tree.Q<Label>("idNumberModified");
        _serialNumberModified = tree.Q<Label>("serialNumberModified");
        _maxKVA = tree.Q<Label>("numberOfPhasesModified");
        _maxVolt = tree.Q<Label>("voltageModified");
        _lastUpdated = tree.Q<Label>("lastUpdatedModified");


        // _lastServicedData = tree.Q<Label>("lastServicedDateModified");




        var newBackBtn = tree.Q<Button>("Back_Btn");

        if (_backBtn != null && _backBtn != newBackBtn)
        {
            _backBtn.clicked -= Hide;
        }

        _backBtn = newBackBtn;

        // Debug.Log($"[MapOverlay] Overlay built. Overlay: {_overlay != null}, Label: {_infoLabel != null}, BackBtn: {_backBtn != null}");

        if (_backBtn != null)
        {
            _backBtn.clicked += Hide;
        }

        if (_overlay != null)
        {
            _overlay.style.display = DisplayStyle.None;
        }

        if (_scrollView != null)
        {
            _scrollView.style.display = DisplayStyle.None;
        }

        Debug.Log("[MapOverlay] Overlay initialised and hidden");
    }

    private VisualTreeAsset ResolveOverlayAsset()
    {
        if (overlayAsset != null)
        {
            return overlayAsset;
        }

        if (_doc.visualTreeAsset != null)
        {
            return _doc.visualTreeAsset;
        }

        var loaded = Resources.Load<VisualTreeAsset>("UI/ScanDataOverlay");
        if (loaded == null)
        {
            Debug.LogError("[MapOverlay] Could not locate Resources/UI/ScanDataOverlay.uxml");
        }
        else
        {
            Debug.Log("[MapOverlay] Loaded overlay from Resources/UI/ScanDataOverlay");
        }

        return loaded;
    }

    public void Show(Substation substation)
    {
        if (IsOpen)
        {
            return;
        }
        if (_overlay == null) { Debug.LogWarning("[Overlay] No overlay visual element."); return; }

        if (_objectType != null)
            _objectType.text = string.IsNullOrEmpty(substation.TR_TYPE) ? "Unknown Type" : substation.TR_TYPE;

        if (_idNumber != null)
            _idNumber.text = string.IsNullOrEmpty(substation.USER_REF_I) ? "—" : substation.USER_REF_I;

        if (_serialNumberModified != null)
            _serialNumberModified.text = string.IsNullOrEmpty(substation.SYSTEM_ID) ? "—" : substation.SYSTEM_ID;

        if (_maxKVA != null)
            _maxKVA.text = string.IsNullOrEmpty(substation.MAX_KVA) ? "—" : substation.MAX_KVA;

        if (_maxVolt != null)
            _maxVolt.text = string.IsNullOrEmpty(substation.MAX_VOLT) ? "—" : substation.MAX_VOLT;

        if (_lastUpdated != null)
            _lastUpdated.text = string.IsNullOrEmpty(substation.REFRESH_DT) ? "—" : substation.REFRESH_DT;

        // if (_site != null)
        //     _site.text = string.IsNullOrEmpty(substation.SITE_DESC) ? "—" : substation.SITE_DESC;


        _overlay.style.display = DisplayStyle.Flex;
        IsOpen = true;
    }

    public void Hide()
    {
        if (_overlay != null)
        {
            _overlay.style.display = DisplayStyle.None;
        }

        if (_scrollView != null)
        {
            _scrollView.style.display = DisplayStyle.None;
        }

        Debug.Log("[MapOverlay] Overlay hidden");
        IsOpen = false;
    }

    private void WireNavButtons()
    {
        UnwireNavButtons();

        HookButton(_scanButton, OnScanButtonClicked, "Scan");
        HookButton(_mapButton, OnMapButtonClicked, "Map");
        HookButton(_historyButton, OnHistoryButtonClicked, "History");
        HookButton(_aboutButton, OnAboutButtonClicked, "About");
    }

    private void UnwireNavButtons()
    {
        UnhookButton(_scanButton, OnScanButtonClicked);
        UnhookButton(_mapButton, OnMapButtonClicked);
        UnhookButton(_historyButton, OnHistoryButtonClicked);
        UnhookButton(_aboutButton, OnAboutButtonClicked);
    }

    private static void HookButton(Button button, System.Action action, string label)
    {
        if (button == null)
        {
            Debug.LogWarning($"[MapOverlay] {label} button not found.");
            return;
        }

        button.clicked += action;
        Debug.Log($"[MapOverlay] {label} button wired.");
    }

    private static void UnhookButton(Button button, System.Action action)
    {
        if (button != null)
        {
            button.clicked -= action;
        }
    }

    private void OnScanButtonClicked()
    {
        LoadScene("MainScene");
    }

    private void OnMapButtonClicked()
    {
        LoadScene("ZoomableMap");
    }

    private void OnHistoryButtonClicked()
    {
        LoadScene("HistoryLogScene");
    }

    private void OnAboutButtonClicked()
    {
        LoadScene("ScannedObjectInfoScene");
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[MapOverlay] Scene name is empty.");
            return;
        }

        Debug.Log($"[MapOverlay] Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    private void HandleNavButton(string sceneName)
    {
        Hide();
        LoadScene(sceneName);
    }
}

