using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public class ToastManager : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Timing")]
    [SerializeField] private float holdSeconds = 1.6f;
    [SerializeField] private float fadeInSeconds = 0.16f;
    [SerializeField] private float fadeOutSeconds = 0.22f;

    [Header("UI Styling and Layout")]
    [SerializeField] private int fontSize = 16;
    [SerializeField] private float minWidthPx = 260f;
    [SerializeField] private float maxWidthPercent = 85f;
    [SerializeField] private float hPad = 18f;
    [SerializeField] private float vPad = 12f;
    [SerializeField] private float cornerRadius = 14f;
    [SerializeField] private float itemGap = 8f;
    [SerializeField] private int maxVisible = 4;

    private float verticalOffset = 0f;

    private VisualElement root;
    private VisualElement overlay;
    private VisualElement stack;

    private readonly List<VisualElement> activeToasts = new List<VisualElement>();

    void OnEnable()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) uiDocument = FindObjectOfType<UIDocument>();
        }

        if (uiDocument == null)
        {
            Debug.LogWarning("[ToastManager] No UIDocument found—toast will not render.");
            return;
        }

        root = uiDocument.rootVisualElement;
        EnsureUi();
    }

    public void Show(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || overlay == null || stack == null) return;

        var (item, label) = BuildToastItem(message);

        if (activeToasts.Count >= Mathf.Max(1, maxVisible))
        {
            var oldest = activeToasts[0];
            StartCoroutine(FadeOutAndRemove(oldest));
        }

        stack.Add(item);
        activeToasts.Add(item);

        StartCoroutine(FadeInHoldFadeOut(item));
    }

    // ---- UI Construction ---

    private void EnsureUi()
    {
        if (overlay != null) return;

        overlay = new VisualElement
        {
            name = "ToastOverlay",
            pickingMode = PickingMode.Ignore
        };
        overlay.style.position = Position.Absolute;
        overlay.style.left = 0; overlay.style.right = 0; overlay.style.top = 0; overlay.style.bottom = 0;
        overlay.style.display = DisplayStyle.Flex;
        overlay.style.opacity = 1f;
        overlay.style.flexDirection = FlexDirection.Row;
        overlay.style.alignItems = Align.Center;
        overlay.style.justifyContent = Justify.Center;

        if (Mathf.Abs(verticalOffset) > 0.01f)
            overlay.style.translate = new Translate(0, verticalOffset);

        stack = new VisualElement
        {
            name = "ToastStack",
            pickingMode = PickingMode.Ignore
        };
        stack.style.flexDirection = FlexDirection.Column;
        stack.style.alignItems = Align.Center;
        stack.style.justifyContent = Justify.Center;

        overlay.Add(stack);
        root.Add(overlay);
    }

    private (VisualElement item, Label label) BuildToastItem(string message)
    {
        var item = new VisualElement
        {
            name = "ToastItem",
            pickingMode = PickingMode.Ignore
        };

        item.style.opacity = 0f;
        item.style.scale = new Scale(new Vector3(0.98f, 0.98f, 1f));

        // The text bubble visual:
        item.style.backgroundColor = new Color(0f, 0f, 0f, 0.82f);
        item.style.borderTopLeftRadius = cornerRadius;
        item.style.borderTopRightRadius = cornerRadius;
        item.style.borderBottomLeftRadius = cornerRadius;
        item.style.borderBottomRightRadius = cornerRadius;
        item.style.paddingLeft = hPad;
        item.style.paddingRight = hPad;
        item.style.paddingTop = vPad;
        item.style.paddingBottom = vPad;

        item.style.minWidth = minWidthPx;
        item.style.maxWidth = Length.Percent(Mathf.Clamp(maxWidthPercent, 40f, 100f));

        var label = new Label(message)
        {
            name = "ToastLabel"
        };
        label.style.color = Color.white;
        label.style.fontSize = fontSize;
        label.style.unityFontStyleAndWeight = FontStyle.Normal;
        label.style.whiteSpace = WhiteSpace.Normal;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;

        item.style.alignItems = Align.Center;
        item.style.justifyContent = Justify.Center;

        item.Add(label);
        return (item, label);
    }

    // ---- animation ---

    private IEnumerator FadeInHoldFadeOut(VisualElement item)
    {
        // Fade/scale in
        yield return Animate(item, 0f, 1f, 0.98f, 1f, fadeInSeconds);

        // Hold
        float t = 0f;
        while (t < holdSeconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // Fade out & remove
        yield return FadeOutAndRemove(item);
    }

    private IEnumerator FadeOutAndRemove(VisualElement item)
    {
        if (item == null) yield break;

        if (!stack.Contains(item)) yield break;

        yield return Animate(item, item.resolvedStyle.opacity, 0f, 1f, 0.98f, fadeOutSeconds);

        if (stack.Contains(item)) stack.Remove(item);
        activeToasts.Remove(item);
    }

    private IEnumerator Animate(VisualElement ve, float fromOpacity, float toOpacity, float fromScale, float toScale, float duration)
    {
        if (ve == null) yield break;

        if (duration <= 0.0001f)
        {
            ve.style.opacity = toOpacity;
            ve.style.scale = new Scale(new Vector3(toScale, toScale, 1f));
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = p * p * (3f - 2f * p);

            ve.style.opacity = Mathf.Lerp(fromOpacity, toOpacity, eased);
            float s = Mathf.Lerp(fromScale, toScale, eased);
            ve.style.scale = new Scale(new Vector3(s, s, 1f));

            yield return null;
        }

        ve.style.opacity = toOpacity;
        ve.style.scale = new Scale(new Vector3(toScale, toScale, 1f));
    }
}
