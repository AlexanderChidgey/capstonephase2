using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UISetSafeArea : MonoBehaviour
{
    [SerializeField] private string bottomBarName = "BottomNavBarCanvas";

    private int baseHeight = 250;

    private UIDocument _doc;
    private VisualElement _root;
    private VisualElement _bottomBar;

    private Rect _lastSafe;
    private Vector2Int _lastScreen;

    private void OnEnable()
    {
        _doc = GetComponent<UIDocument>();
        _root = _doc.rootVisualElement;

        _bottomBar = _root.Q<VisualElement>(bottomBarName);
        if (_bottomBar == null)
        {
            Debug.LogWarning("SafeAreaBottomBar: element '" + bottomBarName + "' not found.");
            return;
        }

        // Re-apply when layout or screen changes (rotation, resize, etc.)
        _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

        Apply();
    }

    private void OnDisable()
    {
        _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        Rect safe = Screen.safeArea;
        Vector2Int screenNow = new Vector2Int(Screen.width, Screen.height);
        if (safe != _lastSafe || screenNow != _lastScreen)
            Apply();
    }

    private void Apply()
    {
        _lastSafe   = Screen.safeArea;
        _lastScreen = new Vector2Int(Screen.width, Screen.height);

        float bottomInset = Mathf.Max(0f, Screen.height - (_lastSafe.y + _lastSafe.height));

        _bottomBar.style.height = baseHeight + bottomInset;

        _bottomBar.style.paddingBottom = bottomInset;
    }
}
