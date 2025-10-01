using System;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System.Text;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class HistoryLogController : MonoBehaviour
{
    [SerializeField] private string rootName = "HistoryRoot";
    [SerializeField] private string listName = "HistoryList";
    [SerializeField] private ToastManager toast;

    private ScrollView _scroll;

    void OnEnable()
    {
        UIDocument doc = GetComponent<UIDocument>();
        VisualElement root = doc.rootVisualElement;
        VisualElement historyRoot = root.Q<VisualElement>(rootName);
        VisualElement listContainer = historyRoot?.Q<VisualElement>(listName);
        _scroll = listContainer?.Q<ScrollView>() ?? root.Q<ScrollView>(listName);

        if (_scroll == null) {
            Debug.LogError("HistoryList ScrollView not found."); 
            return; 
        }
        _scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        _scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        Refresh();
    }

    public void Refresh()
    {
        VisualElement target = _scroll.contentContainer;
        target.Clear();

        IReadOnlyList<HistoryRecord> all = HistoryStoreScan.All;
        for (int i = all.Count - 1; i >= 0; i--)
        {
            HistoryRecord r = all[i];
            VisualElement card = BuildHistoryItemCard(r);
            target.Add(card);
        }
    }


    private VisualElement BuildHistoryItemCard(HistoryRecord r)
    {
        VisualElement card = new VisualElement();
        card.AddToClassList("history-wrap");
        card.style.flexDirection = FlexDirection.Column;
        card.style.marginBottom = 8;

        Button mainBtn = BuildHistoryButton(r);

        VisualElement actionRow = new VisualElement();
        actionRow.style.flexDirection = FlexDirection.Row;
        actionRow.style.justifyContent = Justify.FlexEnd;
        actionRow.style.alignItems = Align.Center;
        actionRow.style.marginTop = 6;

        DBLoader db = FindObjectOfType<DBLoader>();

        Button exportBtn = BuildExportCsvButton(() =>
        {
            // EXPORT SCAN INFO TO CSV:
            string path = CsvExporter.ExportOneCsv(r, db);
            NotifyExport(path);
        });

        Button deleteBtn = BuildDeleteButton(() =>
        {
            // DELETE SCAN:
            DeleteHistoryRecord(r);
        });

        actionRow.Add(exportBtn);
        actionRow.Add(deleteBtn);

        deleteBtn.RegisterCallback<PointerDownEvent>(e => e.StopImmediatePropagation());
        deleteBtn.RegisterCallback<ClickEvent>(e => e.StopPropagation());

        exportBtn.RegisterCallback<PointerDownEvent>(e => e.StopImmediatePropagation());
        exportBtn.RegisterCallback<ClickEvent>(e => e.StopPropagation());
        
        card.Add(mainBtn);
        card.Add(actionRow);

        return card;
    }


    private Button BuildHistoryButton(HistoryRecord r)
    {
        Button btn = new Button();
        btn.name = $"History_{r.Id}";
        btn.AddToClassList("data-button");
        btn.AddToClassList("history-item");
        btn.style.flexDirection = FlexDirection.Column;
        btn.style.alignItems = Align.Stretch;
        btn.style.justifyContent = Justify.FlexStart;

        // Object Type and Serial Number
        Label objectType = new Label((string.IsNullOrEmpty(r.ObjectType) ? "Object" : r.ObjectType) + " #" + r.SerialNumber);
        objectType.AddToClassList("history-item-primary-text");
        objectType.style.marginBottom = 2;
        objectType.style.unityTextAlign = TextAnchor.UpperLeft;
        objectType.style.alignSelf = Align.Stretch;

        // Scanned X ago
        Label scanned = new Label("Scanned " + RelativeAgo(r.Utc));
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

        btn.clicked += () =>
        {
            Debug.Log("[HistoryUI] Clicked " + r.Id);

            StoreSelectedScan.Id = r.Id;
            StoreSelectedScan.Utc = r.Utc;
            StoreSelectedScan.ShowOverlayNextScene = true;

            SceneManager.LoadScene("ScannedObjectInfoScene");
        };

        return btn;
    }

    private Button BuildExportCsvButton(System.Action onClicked)
    {
        Button exportBtn = new Button();
        exportBtn.name = "ExportAsCSV_Btn";
        exportBtn.AddToClassList("secondary-button");
        exportBtn.AddToClassList("history-secondary-button");
        exportBtn.style.flexDirection = FlexDirection.Row;
        exportBtn.style.alignItems = Align.Center;
        exportBtn.style.justifyContent = Justify.Center;
        exportBtn.style.paddingLeft = 12;
        exportBtn.style.paddingRight = 12;

        Label label = new Label("Export as CSV");
        label.name = "ExportAsCSV_Label";
        label.style.unityTextAlign = TextAnchor.MiddleCenter;

        VisualElement icon = new VisualElement();
        icon.name = "CSV_Icon";
        icon.AddToClassList("button-icon");

        Sprite sprite = Resources.Load<Sprite>("Images/Icons/CSV_Icon");
        if (sprite != null)
        {
            icon.style.backgroundImage = new StyleBackground(sprite);
        }
        else
        {
            Debug.LogWarning("CSV icon not found at Resources/Images/Icons/CSV_Icon");
        }

        exportBtn.Add(icon);
        exportBtn.Add(label);

        if (onClicked != null)
        {
            exportBtn.clicked += () => onClicked();
        }

        return exportBtn;
    }

    private Button BuildDeleteButton(System.Action onClicked)
    {
        Button btn = new Button();
        btn.name = "DeleteHistory_Btn";
        btn.AddToClassList("danger-button");
        btn.AddToClassList("secondary-button");
        btn.AddToClassList("history-secondary-button");
        btn.style.flexDirection = FlexDirection.Row;
        btn.style.alignItems = Align.Center;
        btn.style.justifyContent = Justify.Center;
        btn.style.paddingLeft = 12;
        btn.style.paddingRight = 12;

        VisualElement icon = new VisualElement();
        icon.name = "CSV_Icon";
        icon.AddToClassList("button-icon");

        Sprite sprite = Resources.Load<Sprite>("Images/Icons/Trash_Icon");
        if (sprite != null)
        {
            icon.style.backgroundImage = new StyleBackground(sprite);
        }
        else
        {
            Debug.LogWarning("Trash icon not found at Resources/Images/Icons/Trash_Icon");
        }

        Label label = new Label("Delete");
        label.name = "Delete_Label";
        label.style.unityTextAlign = TextAnchor.MiddleCenter;

        btn.Add(icon);
        btn.Add(label);

        if (onClicked != null)
            btn.clicked += () => onClicked();
            
        return btn;
    }

    private void DeleteHistoryRecord(HistoryRecord r)
    {
        bool removed = false;

        removed = HistoryStoreScan.RemoveById(r.Id);

        if (removed)
        {
            ShowToast("Deleted from history");
            Refresh();
        }
        else
        {
            ShowToast("Could not delete this item");
        }
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
    private void ShowToast(string message)
    {
        ToastManager tm = FindObjectOfType<ToastManager>();
        if (tm != null) tm.Show(message);
        else Debug.Log($"{message}");
    }

    private void NotifyExport(string path)
    {
        ShowToast($"Exported CSV to:\n{path}\n");
    }
}
