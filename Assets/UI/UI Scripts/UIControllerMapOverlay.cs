// UIControllerMapOverlay.cs - Manages the technical information overlay within the map UI Toolkit document.
// Responsibilities:
//  - Wire up bottom nav/overlay buttons defined in mapOverlay.uxml
//  - Populate labels with data from DBLoader (substation details)
//  - React to detection matches / history selections to show the overlay
//
// NOTE: This controller assumes a DBLoader exists in the scene (or will be located via FindObjectOfType).

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Linq;
using System.Globalization;
// using UnityEngine.WSA;
using System.Text;

[RequireComponent(typeof(UIDocument))]
public class UIControllerMapOverlay : MonoBehaviour
{
    [Header("UXML Element Names")]
    [SerializeField] private string circleButtonName = "Circle_Btn";
    [SerializeField] private string backButtonName = "Back_Btn";
    [SerializeField] private string scrollViewName = "ScanDataScrollView";
    [SerializeField] private string resultsContainerName = "DetectionResultsContainer";
    [SerializeField] private string mapButtonName = "Map_Btn";

    [SerializeField] private DBLoader dbLoader;
    
    private UIDocument uiDocument;
    private VisualElement root;
    

    private VisualElement scrollView;
    private Button mapButton;
    private Button historyButton;
    private VisualElement resultsContainer;

    private Button circleButton;
    private Button backButton;

    private Button[] detectionButtons = new Button[3];
    private Label[] detectionNameLabels = new Label[3];
    private Label[] detectionIdLabels = new Label[3];

    private Action[] detectionHandlers = new Action[3];

    private List<ObjectDetectionHandler.MatchInfo> currentMatches;
    private bool isOverlayVisible = true;

    [Header("Technical data labels inside the technical scan overlay")]
    [SerializeField] private string overlayRootName = "ScanDataOverlay";
    [SerializeField] private string serialNumberLabel = "SerialNumber_Label";
    [SerializeField] private string modelNumberLabel = "ModelNumber_Label";
    [SerializeField] private string numberOfPhasesLabel = "NumberOfPhases_Label";
    [SerializeField] private string voltageLabel = "Voltage_Label";
    [SerializeField] private string lastServiceDateLabel = "LastServiceDate_Label";
    [SerializeField] private string nextServiceDateLabel = "NextServiceDate_Label";

    [SerializeField] private string addressLabel = "Address_Label";
    [SerializeField] private string latLabel = "Lat_Label";
    [SerializeField] private string lonLabel = "Lon_Label";

    [Header("Manager Scripts")]
    [SerializeField] private ToastManager toast;

    [SerializeField] private string exportButtonName = "ExportAsCSV_Btn";
    [SerializeField] private string copyAllButtonName = "CopyAllData_Btn";

    private VisualElement overlayRoot;
    private Label serialNumber;
    private Label modelNumber;
    private Label numberOfPhases;
    private Label voltage;
    private Label lastServiceDate;
    private Label nextServiceDate;
    private Label address;
    private Label lat;
    private Label lon;

    private Button serialNumberBtn;
    private Button modelNumberBtn;
    private Button numberOfPhasesBtn;
    private Button voltageBtn;
    private Button lastServiceDateBtn;
    private Button nextServiceDateBtn;
    private Button addressBtn;

    [Header("Scene Names")]
    [SerializeField] private string scannedObjectInfoSceneName = "ScannedObjectInfoScene";
    [SerializeField] private string historySceneName = "HistoryLogScene";

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument component is missing!");
        }
    }

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument reference is not assigned in UIController!");
            return;
        }

        root = uiDocument.rootVisualElement;

        circleButton = root.Q<Button>(circleButtonName);
        backButton = root.Q<Button>(backButtonName);
        scrollView = root.Q<VisualElement>(scrollViewName);
        mapButton = root.Q<Button>(mapButtonName);

        resultsContainer = root.Q<VisualElement>(resultsContainerName);

        overlayRoot = scrollView.Q<VisualElement>(overlayRootName);
        serialNumber = scrollView.Q<Label>(serialNumberLabel);
        modelNumber = scrollView.Q<Label>(modelNumberLabel);
        numberOfPhases = scrollView.Q<Label>(numberOfPhasesLabel);
        voltage = scrollView.Q<Label>(voltageLabel);
        lastServiceDate = scrollView.Q<Label>(lastServiceDateLabel);
        nextServiceDate = scrollView.Q<Label>(nextServiceDateLabel);

        address = scrollView.Q<Label>(addressLabel);
        lat = scrollView.Q<Label>(latLabel);
        lon = scrollView.Q<Label>(lonLabel);

        serialNumberBtn = root.Q<Button>("SerialNumber_Btn");
        modelNumberBtn = root.Q<Button>("ModelNumber_Btn");
        numberOfPhasesBtn = root.Q<Button>("NumberOfPhases_Btn");
        voltageBtn = root.Q<Button>("Voltage_Btn");
        lastServiceDateBtn = root.Q<Button>("LastServiceDate_Btn");
        nextServiceDateBtn = root.Q<Button>("NextServiceDate_Btn");
        addressBtn = root.Q<Button>("Address_Btn");

        HookCopy(serialNumberBtn, serialNumber, "Serial Number");
        HookCopy(modelNumberBtn, modelNumber, "Model Number");
        HookCopy(numberOfPhasesBtn, numberOfPhases, "Number of Phases");
        HookCopy(voltageBtn, voltage, "Voltage");
        HookCopy(lastServiceDateBtn, lastServiceDate, "Last Service Date");
        HookCopy(nextServiceDateBtn, nextServiceDate, "Next Service Date");
        HookCopy(addressBtn, address, "Address");

        Button exportBtn = root.Q<Button>(exportButtonName);
        Button copyAllBtn = root.Q<Button>(copyAllButtonName);

        if (circleButton != null)
        {
            circleButton.clicked += OnCircleButtonClicked;
            Debug.Log("Circle button registered");
        }
        else
        {
            Debug.LogWarning("Button '" + circleButtonName + "' not found in UI.");
        }
        if (mapButton != null)
        {
            mapButton.clicked += OnMapButtonClicked;
            Debug.Log("Map button registered");
        }
        else
        {
            Debug.LogWarning("Button '" + mapButtonName + "' not found in UI.");
        }
        if (exportBtn != null)
        {
            exportBtn.clicked += OnExportCsvClicked;
        }
        if (copyAllBtn != null)
        {
            copyAllBtn.clicked += OnCopyAllDataClicked;
        }


        if (backButton != null)
        {
            backButton.clicked += OnBackButtonClicked;
            Debug.Log("Back button registered");
        }
        else
        {
            Debug.LogWarning("Button '" + backButtonName + "' not found in UI.");
        }

        if (StoreSelectedScan.ShowOverlayNextScene && !string.IsNullOrEmpty(StoreSelectedScan.Id))
        {
            ShowOverlayForHistoryId(StoreSelectedScan.Id);
            StoreSelectedScan.ShowOverlayNextScene = false;
        }

        else
        {

        if (scrollView != null)
        {
            scrollView.style.display = DisplayStyle.None;
            Debug.Log("scrollView hidden by default.");
        }

        if (resultsContainer != null)
        {
            resultsContainer.style.display = DisplayStyle.None;
            Debug.Log("DetectionResultsContainer hidden by default.");
        }

        for (int i = 0; i < 3; i++)
        {
            detectionButtons[i] = root.Q<Button>($"Detection{i + 1}Button");

            if (detectionButtons[i] != null)
            {
                detectionNameLabels[i] = detectionButtons[i].Q<Label>($"Detection{i + 1}Label");
                detectionIdLabels[i] = detectionButtons[i].Q<Label>($"Detection{i + 1}Id");
                Debug.Log($"Detection button {i + 1} found: {detectionButtons[i].name}");
            }
            else {
                Debug.LogWarning($"Detection button {i + 1} not found in UXML.");
            }
        }
        }
    }

    private void OnDisable()
    {
        if (circleButton != null)
        {
            circleButton.clicked -= OnCircleButtonClicked;
        }
        if (backButton != null)
        {
            backButton.clicked -= OnBackButtonClicked;
        }
        if (mapButton != null)
        {
            mapButton.clicked -= OnMapButtonClicked;
        }

        for (int i = 0; i < detectionButtons.Length; i++)
        {
            if (detectionButtons[i] != null && detectionHandlers[i] != null)
            {
                detectionButtons[i].clicked -= detectionHandlers[i];
            }
        }

        UnhookCopy(serialNumberBtn);
        UnhookCopy(modelNumberBtn);
        UnhookCopy(numberOfPhasesBtn);
        UnhookCopy(voltageBtn);
        UnhookCopy(lastServiceDateBtn);
        UnhookCopy(nextServiceDateBtn);
        UnhookCopy(addressBtn);
    }

    private void OnCircleButtonClicked()
    {
        if (resultsContainer == null)
        {
            return;
        }

        bool isCurrentlyVisible = resultsContainer.style.display == DisplayStyle.Flex;

        if (isCurrentlyVisible)
        {
            resultsContainer.style.display = DisplayStyle.None;
            Debug.Log("DetectionResultsContainer hidden.");
        }
        else
        {
            resultsContainer.style.display = DisplayStyle.Flex;
            Debug.Log("DetectionResultsContainer shown.");
        }
    }
    private void OnMapButtonClicked()
    {
        string sceneName = "ZoomableMap"; 
        Debug.Log("Loading scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }

    private void OnBackButtonClicked()
    {
        string current = SceneManager.GetActiveScene().name;

        if (!string.IsNullOrEmpty(scannedObjectInfoSceneName) &&
            string.Equals(current, scannedObjectInfoSceneName))
        {
            if (!string.IsNullOrEmpty(historySceneName))
            {
                Debug.Log("Returning to History scene: " + historySceneName);
                SceneManager.LoadScene(historySceneName);
                return;
            }
            else
            {
                Debug.LogWarning("Back: historySceneName is not set.");
            }
        }

        HideOverlay();
    }

        public void ShowOverlay(string infoText = null)
    {
        Debug.Log(infoText);
        if (scrollView != null)
        {
            scrollView.style.display = DisplayStyle.Flex;
            isOverlayVisible = true;
            Debug.Log("Overlay shown");
        }
        else
        {
            Debug.LogError("scrollView is null. Cannot show overlay.");
        }
    }


    // public void ShowOverlay(string infoText)
    // {
    //     Debug.Log("[UIControllerMapOverlay] ShowOverlay called!");

    //     if (scrollView != null)
    //     {
    //         Debug.Log($"Before: scrollView display = {scrollView.resolvedStyle.display}");
    //         scrollView.style.display = DisplayStyle.Flex;
    //         Debug.Log($"After: scrollView display = {scrollView.resolvedStyle.display}");
    //         isOverlayVisible = true;


    //         //             private Label serialNumber;
    //         // private Label modelNumber;
    //         // private Label numberOfPhases;
    //         // private Label voltage;
    //         // private Label lastServiceDate;
    //         // private Label nextServiceDate;
    //         // private Label address;
    //         // private Label lat;
    //         // private Label lon;


    //         // if (serialNumber != null)
    //         // {
    //         // serialNumber.text = 
    //     }


    //         if (infoLabel != null)
    //         {
    //             infoLabel.text = infoText;
    //             Debug.Log($"[UIControllerMapOverlay] Updated label '{infoLabelName}' text: {infoLabel.text}");
    //         }
    //         else
    //         {
    //             Debug.LogWarning($"[UIControllerMapOverlay] Label '{infoLabelName}' was not found!");
    //         }
    //     }
    //     else
    //     {
    //         Debug.LogError("[UIControllerMapOverlay] scrollView is null in ShowOverlay!");
    //     }
    // }


    private void HideOverlay()
    {
        if (scrollView != null)
        {
            scrollView.style.display = DisplayStyle.None;
            isOverlayVisible = false;
            Debug.Log("Overlay hidden");
        }
        else
        {
            Debug.LogError("scrollView is null. Cannot hide overlay.");
        }
    }

    public void UpdateDetectionUI(List<ObjectDetectionHandler.MatchInfo> matches)
{
    currentMatches = matches ?? new List<ObjectDetectionHandler.MatchInfo>();
    Debug.Log("UpdateDetectionUI called with " + (matches?.Count ?? 0) + " matches.");

        for (int i = 0; i < detectionButtons.Length; i++)
    {
        if (detectionButtons[i] == null) continue;

        // Remove previous handler if it exists
        if (detectionHandlers[i] != null)
        {
            detectionButtons[i].clicked -= detectionHandlers[i];
        }

        if (i < currentMatches.Count)
        {
            detectionNameLabels[i].text = currentMatches[i].Name;
            detectionIdLabels[i].text = currentMatches[i].ID;
            detectionButtons[i].style.display = DisplayStyle.Flex;

            int index = i; // capture fixed index
            detectionHandlers[i] = () => OnDetectionButtonClicked(index);
            detectionButtons[i].clicked += detectionHandlers[i];
        }
        else
        {
            detectionNameLabels[i].text = "";
            detectionIdLabels[i].text = "";
            detectionButtons[i].style.display = DisplayStyle.None;

            detectionHandlers[i] = null;
        }
    }
}


    private void OnDetectionButtonClicked(int i)
    {
        if (resultsContainer != null)
        {
            resultsContainer.style.display = DisplayStyle.None;
            Debug.Log("Results container hidden.");
        }
        else
        {
            Debug.LogWarning("resultsContainer is null; cannot hide results.");
        }

        Debug.Log("OnDetectionButtonClicked invoked for index " + i.ToString());

        // Here, validate whether we can safely access currentMatches[index].
        bool canAccessMatch = false;
        int matchesCount = 0;

        if (currentMatches == null)
        {
            Debug.LogWarning("No detection matches available (currentMatches is null).");
        }
        else
        {
            matchesCount = currentMatches.Count;
            if (i < 0 || i >= matchesCount)
            {
                Debug.LogWarning(
                    "Invalid detection index: " + i.ToString() +
                    ". Valid range is 0.." + (matchesCount - 1).ToString() + "."
                );
            }
            else
            {
                canAccessMatch = true;
            }
        }

        if (canAccessMatch)
        {
            ObjectDetectionHandler.MatchInfo match = currentMatches[i];
            PopulateTechnicalPanel(match);
            StoreScanHistory(match);

            if (match == null)
            {
                Debug.LogWarning("Match at index " + i.ToString() + " is null.");
                return;
            }

            if (string.IsNullOrEmpty(match.Name))
            {
                Debug.LogWarning("Match.Name is null or empty.");
            }
            else
            {
                DetectionDataStore.SelectedName = match.Name;
            }

            if (string.IsNullOrEmpty(match.ID))
            {
                Debug.LogWarning("Match.ID is null or empty.");
                return;
            }

            DetectionDataStore.SelectedId = match.ID;
            StoreSelectedScan.Id = match.ID;
            StoreSelectedScan.Utc = DateTime.UtcNow;
            StoreSelectedScan.ShowOverlayNextScene = true;

            Debug.Log($"Loading ScannedObjectInfoScene for ID {match.ID}");
            SceneManager.LoadScene("ScannedObjectInfoScene");
        }
    }

    private Substation FindSubstationById(string id)
    {
        if (string.IsNullOrEmpty(id) || dbLoader == null) return null;

        List<Substation> list = dbLoader.GetSubstations(); 
        return list.FirstOrDefault(s =>
            string.Equals(s.SYSTEM_ID, id, StringComparison.OrdinalIgnoreCase));
    }

    private static void SetLabel(Label target, string value)
    {
        if (target == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(value))
        {
            target.text = "-";
        }
        else
        {
            target.text = value;
        }
    }

    private void PopulateTechnicalPanel(ObjectDetectionHandler.MatchInfo match)
    {
        if (overlayRoot == null)
        {
            Debug.LogWarning("ScanDataOverlay root not found. Did not populate technical panel.");
            return;
        }

        string id = match?.ID;

        Substation sub = FindSubstationById(id);

        SetLabel(serialNumber, sub?.SERIAL_NUMBER);
        SetLabel(modelNumber, sub?.MODEL_NUMBER);
        SetLabel(numberOfPhases, sub?.NUMBER_OF_PHASES);
        SetLabel(voltage, sub?.MAX_VOLT);
        SetLabel(lastServiceDate, sub?.LAST_SERVICE_DATE);
        SetLabel(nextServiceDate, sub?.NEXT_SERVICE_DATE);

        SetLabel(address, sub?.ADDRESS);
        SetLabel(lat, sub.LAT.ToString("F6", CultureInfo.InvariantCulture));
        SetLabel(lon, sub.LON.ToString("F6", CultureInfo.InvariantCulture));

    }

    private void StoreScanHistory(ObjectDetectionHandler.MatchInfo match)
    {
        string id = match?.ID;

        Substation sub = FindSubstationById(id);

        HistoryStoreScan.Add(
            id: match.ID,
            objectType: sub?.TR_TYPE,
            serialNumber: sub?.SERIAL_NUMBER,
            model: sub?.MODEL_NUMBER,
            voltage: sub?.MAX_VOLT
        );
    }

    private void HookCopy(Button btn, Label source, string friendlyName)
    {
        if (btn == null || source == null) return;

        Action handler = () =>
        {
            string text = string.IsNullOrEmpty(source.text) ? "-" : source.text;
            GUIUtility.systemCopyBuffer = text;
            if (toast != null) toast.Show($"{friendlyName} copied");
            else Debug.Log($"{friendlyName} copied: {text}");
        };

        btn.clicked += handler;
        btn.userData = handler;
    }

    private void UnhookCopy(Button btn)
    {
        if (btn?.userData is Action handler)
        {
            btn.clicked -= handler;
            btn.userData = null;
        }
    }

    private void OnExportCsvClicked()
    {
        DBLoader db = dbLoader != null ? dbLoader : FindObjectOfType<DBLoader>();

        GetScannedObjectInfo(db, out HistoryRecord record);

        string path = CsvExporter.ExportOneCsv(in record, db);

        ShowToast($"Exported CSV to:\n{path}");
    }

    private bool GetScannedObjectInfo(DBLoader db, out HistoryRecord record)
    {
        record = default;

        string id = DetectionDataStore.SelectedId;

        Substation sub = null;
        if (db != null)
        {
            List<Substation> list = db.GetSubstations();
            if (list != null)
            {
                sub = list.FirstOrDefault(s => string.Equals(s.SYSTEM_ID, id, StringComparison.OrdinalIgnoreCase));
            }
        }

        record = new HistoryRecord
        {
            Id = sub?.SYSTEM_ID ?? id ?? name ?? "unknown",
            ObjectType = sub?.TR_TYPE ?? "-",
            SerialNumber = sub?.SERIAL_NUMBER ?? "-",
            Model = sub?.MODEL_NUMBER ?? "-",
            Voltage = sub?.MAX_VOLT ?? "-",
            Utc = DateTime.UtcNow
        };

        return true;
    }

    private void ShowToast(string message)
    {
        ToastManager tm = FindObjectOfType<ToastManager>();
        if (tm != null) tm.Show(message);
        else Debug.Log($"{message}");
    }

    private void OnCopyAllDataClicked()
    {
        DBLoader db = dbLoader != null ? dbLoader : FindObjectOfType<DBLoader>();

        GetCurrentSubstation(db, out Substation sub, out string fallbackId, out string fallbackName);

        string payload = BuildCopyAllPayload(sub, fallbackId, fallbackName);
        GUIUtility.systemCopyBuffer = payload;
        ShowToast("Copied all data to clipboard.");
    }

    private bool GetCurrentSubstation(DBLoader db, out Substation sub, out string id, out string nameSel)
    {
        sub = null;

        id = DetectionDataStore.SelectedId;
        nameSel = DetectionDataStore.SelectedName;

        string idKey = id;

        if (db != null && !string.IsNullOrEmpty(idKey))
        {
            List<Substation> list = db.GetSubstations();
            if (list != null)
            {
                sub = list.FirstOrDefault(s => string.Equals(s.SYSTEM_ID, idKey, StringComparison.OrdinalIgnoreCase));
            }
        }

        return sub != null || !string.IsNullOrEmpty(id) || !string.IsNullOrEmpty(nameSel);
    }


    private string BuildCopyAllPayload(Substation sub, string fallbackId, string fallbackName)
    {
        string F(string s) => string.IsNullOrEmpty(s) ? "-" : s;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Exported (UTC): {DateTime.UtcNow:o}");
        sb.AppendLine();

        if (sub != null)
        {
            sb.AppendLine($"SYSTEM_ID: {F(sub.SYSTEM_ID)}");
            sb.AppendLine($"USER_REF_I: {F(sub.USER_REF_I)}");
            sb.AppendLine($"SITE_DESC: {F(sub.SITE_DESC)}");
            sb.AppendLine($"TR_TYPE: {F(sub.TR_TYPE)}");
            sb.AppendLine($"MAX_KVA: {F(sub.MAX_KVA)}");
            sb.AppendLine($"MAX_VOLT: {F(sub.MAX_VOLT)}");
            sb.AppendLine($"LON: {sub.LON}");
            sb.AppendLine($"LAT: {sub.LAT}");
            sb.AppendLine($"REFRESH_DT: {F(sub.REFRESH_DT)}");
            sb.AppendLine($"SERIAL_NUMBER: {F(sub.SERIAL_NUMBER)}");
            sb.AppendLine($"MODEL_NUMBER: {F(sub.MODEL_NUMBER)}");
            sb.AppendLine($"NUMBER_OF_PHASES: {F(sub.NUMBER_OF_PHASES)}");
            sb.AppendLine($"LAST_SERVICE_DATE: {F(sub.LAST_SERVICE_DATE)}");
            sb.AppendLine($"NEXT_SERVICE_DATE: {F(sub.NEXT_SERVICE_DATE)}");
            sb.AppendLine($"ADDRESS: {F(sub.ADDRESS)}");
        }
        else
        {
            ShowToast("Unable to copy technical data");
        }

        return sb.ToString();
    }

    public void ShowOverlayForHistoryId(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            ShowToast("Unable to show scan information.");
            return;
        }

        var sub = FindSubstationById(id);

        SetLabel(serialNumber, sub?.SERIAL_NUMBER);
        SetLabel(modelNumber, sub?.MODEL_NUMBER);
        SetLabel(numberOfPhases, sub?.NUMBER_OF_PHASES);
        SetLabel(voltage, sub?.MAX_VOLT);
        SetLabel(lastServiceDate, sub?.LAST_SERVICE_DATE);
        SetLabel(nextServiceDate, sub?.NEXT_SERVICE_DATE);
        SetLabel(address, sub?.ADDRESS);

        if (sub != null)
        {
            SetLabel(lat, sub.LAT.ToString("F6", CultureInfo.InvariantCulture));
            SetLabel(lon, sub.LON.ToString("F6", CultureInfo.InvariantCulture));
            DetectionDataStore.SelectedId = sub.SYSTEM_ID;
            DetectionDataStore.SelectedName = sub.SITE_DESC;
        }
        else
        {
            SetLabel(lat, "-");
            SetLabel(lon, "-");
            DetectionDataStore.SelectedId = id;
            DetectionDataStore.SelectedName = "-";
        }

        ShowOverlay();
    }

}




// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UIElements;

// [RequireComponent(typeof(UIDocument))]
// public class UIControllerMapOverlay : MonoBehaviour
// {
//     [Header("UXML Element Names")]
//     [SerializeField] private string backButtonName = "Back_Btn";
//     [SerializeField] private string scrollViewName = "ScanDataScrollView";
//     [SerializeField] private string infoLabelName = "serialNumberModified"; 

//     private UIDocument uiDocument;
//     private VisualElement root;

//     private VisualElement scrollView;
//     private Button backButton;
//     private Label infoLabel;

//     private bool isOverlayVisible = false;

//     private void Awake()
//     {
//         uiDocument = GetComponent<UIDocument>();
//         if (uiDocument == null)
//         {
//             Debug.LogError("[UIControllerMapOverlay] UIDocument component is missing!");
//         }
//     }

//     private void OnEnable()
//     {
//         if (uiDocument == null)
//         {
//             Debug.LogError("[UIControllerMapOverlay] No UIDocument assigned!");
//             return;
//         }

//         root = uiDocument.rootVisualElement;

//         // Query elements
//         backButton = root.Q<Button>(backButtonName);
//         scrollView = root.Q<VisualElement>(scrollViewName);
//         infoLabel = root.Q<Label>(infoLabelName);

//         // Debug which elements were found
//         Debug.Log($"[UIControllerMapOverlay] backButton = {(backButton != null ? "FOUND" : "NULL")}");
//         Debug.Log($"[UIControllerMapOverlay] scrollView = {(scrollView != null ? "FOUND" : "NULL")}");
//         Debug.Log($"[UIControllerMapOverlay] infoLabel = {(infoLabel != null ? "FOUND" : "NULL")}");

//         if (backButton != null)
//         {
//             backButton.clicked += OnBackButtonClicked;
//         }

//         if (scrollView != null)
//         {
//             scrollView.style.display = DisplayStyle.None; // hidden by default
//             Debug.Log("[UIControllerMapOverlay] scrollView hidden by default.");
//         }
//     }

//     private void OnDisable()
//     {
//         if (backButton != null)
//         {
//             backButton.clicked -= OnBackButtonClicked;
//         }
//     }

//     private void OnBackButtonClicked()
//     {
//         HideOverlay();
//     }

//     public void ShowOverlay(string infoText)
//     {
//         Debug.Log("[UIControllerMapOverlay] ShowOverlay called!");

//         if (scrollView != null)
//         {
//             Debug.Log($"Before: scrollView display = {scrollView.resolvedStyle.display}");
//             scrollView.style.display = DisplayStyle.Flex;
//             Debug.Log($"After: scrollView display = {scrollView.resolvedStyle.display}");
//             isOverlayVisible = true;

//             if (infoLabel != null)
//             {
//                 infoLabel.text = infoText;
//                 Debug.Log($"[UIControllerMapOverlay] Updated label '{infoLabelName}' text: {infoLabel.text}");
//             }
//             else
//             {
//                 Debug.LogWarning($"[UIControllerMapOverlay] Label '{infoLabelName}' was not found!");
//             }
//         }
//         else
//         {
//             Debug.LogError("[UIControllerMapOverlay] scrollView is null in ShowOverlay!");
//         }
//     }

//     public void HideOverlay()
//     {
//         if (scrollView != null)
//         {
//             scrollView.style.display = DisplayStyle.None;
//             isOverlayVisible = false;
//             Debug.Log("[UIControllerMapOverlay] Overlay hidden.");
//         }
//         else
//         {
//             Debug.LogError("[UIControllerMapOverlay] scrollView is null in HideOverlay!");
//         }
//     }
// }
