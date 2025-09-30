using System;
using UnityEngine;
using UnityEngine.UIElements;

public class HistoryLogController : MonoBehaviour
{
    [SerializeField] private string rootName = "HistoryRoot";
    [SerializeField] private string listName = "HistoryList";

    private ScrollView _scroll;

    void OnEnable()
    {
        UIDocument doc = GetComponent<UIDocument>();
        VisualElement root = doc.rootVisualElement;
        VisualElement historyRoot = root.Q<VisualElement>(rootName);
        VisualElement listContainer = historyRoot?.Q<VisualElement>(listName);
        _scroll = listContainer?.Q<ScrollView>() ?? root.Q<ScrollView>(listName);

        if (_scroll == null) { Debug.LogError("HistoryList ScrollView not found."); return; }
        Refresh();
    }

    public void Refresh()
    {
        VisualElement target = _scroll.contentContainer;
        target.Clear();

        System.Collections.Generic.IReadOnlyList<HistoryRecord> all = HistoryStoreScan.All;
        for (int i = all.Count - 1; i >= 0; i--)
        {
            HistoryRecord r = all[i];
            Button btn = BuildHistoryButton(r);
            target.Add(btn);
        }
    }

    private Button BuildHistoryButton(HistoryRecord r)
    {
        Button btn = new Button { name = $"History_{r.Id}" };
        btn.AddToClassList("data-button");
        btn.AddToClassList("history-item");
        btn.style.flexDirection = FlexDirection.Column;
        btn.style.alignItems = Align.Stretch;
        btn.style.justifyContent = Justify.FlexStart;

        // Object Type and Serial Number
        Label objectType = new Label($"{(string.IsNullOrEmpty(r.ObjectType) ? "Object" : r.ObjectType)} #{r.SerialNumber}");
        objectType.AddToClassList("history-item-primary-text");
        objectType.style.marginBottom = 2;
        objectType.style.unityTextAlign = TextAnchor.UpperLeft;
        objectType.style.alignSelf = Align.Stretch;

        // Scanned X ago
        Label scanned = new Label($"Scanned {RelativeAgo(r.Utc)}");
        scanned.AddToClassList("history-item-secondary-text");
        scanned.style.marginBottom = 6;
        scanned.style.unityTextAlign = TextAnchor.UpperLeft;
        scanned.style.alignSelf = Align.Stretch;

        // Model row
        VisualElement modelRow = new VisualElement();
        modelRow.style.flexDirection = FlexDirection.Row;
        modelRow.style.justifyContent = Justify.FlexStart;
        modelRow.style.alignItems = Align.Center;

        Label modelLabel = new Label("Model:");
        modelLabel.AddToClassList("history-item-secondary-text");
        modelLabel.style.unityTextAlign = TextAnchor.UpperLeft;

        Label modelValue = new Label(string.IsNullOrEmpty(r.Model) ? "-" : r.Model);
        modelValue.AddToClassList("history-item-secondary-text");
        modelValue.style.marginLeft = 6;
        modelValue.style.unityTextAlign = TextAnchor.UpperLeft;

        modelRow.Add(modelLabel);
        modelRow.Add(modelValue);

        // Voltage row
        VisualElement voltRow = new VisualElement();
        voltRow.style.flexDirection = FlexDirection.Row;
        voltRow.style.justifyContent = Justify.FlexStart;
        voltRow.style.alignItems = Align.Center;

        Label voltLabel = new Label("Voltage:");
        voltLabel.AddToClassList("history-item-secondary-text");
        voltLabel.style.unityTextAlign = TextAnchor.UpperLeft;

        Label voltValue = new Label(string.IsNullOrEmpty(r.Voltage) ? "-" : r.Voltage);
        voltValue.AddToClassList("history-item-secondary-text");
        voltValue.style.marginLeft = 6;
        voltValue.style.unityTextAlign = TextAnchor.UpperLeft;

        voltRow.Add(voltLabel);
        voltRow.Add(voltValue);

        btn.Add(objectType);
        btn.Add(scanned);
        btn.Add(modelRow);
        btn.Add(voltRow);

        btn.clicked += () => Debug.Log($"[HistoryUI] Clicked {r.Id}");

        return btn;
    }



    private static string RelativeAgo(DateTime utc)
    {
        if (utc == default) return "just now";
        TimeSpan ts = DateTime.UtcNow - utc;
        if (ts.TotalSeconds < 60) return $"{Mathf.Max(1, (int)ts.TotalSeconds)}s ago";
        if (ts.TotalMinutes < 60) return $"{(int)ts.TotalMinutes}m ago";
        if (ts.TotalHours < 24) return $"{(int)ts.TotalHours}h ago";
        return $"{(int)ts.TotalDays}d ago";
    }
}
