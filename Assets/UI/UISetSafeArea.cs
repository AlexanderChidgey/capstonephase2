using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UISetSafeArea : MonoBehaviour
{
    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        var botPad = root.Q<VisualElement>("BotPadding");
        botPad.style.paddingBottom = Screen.safeArea.yMin;
    }
}
