// DBLoader.cs - Handles loading substation metadata from local cache or Firebase RTDB and exposes helper utilities.
// Written for the Energy QLD project to ensure consistent data availability across scenes.
//
// Core responsibilities:
//  - Ensure a Firebase app exists and load cached substation JSON (or fallback to Firebase when missing)
//  - Persist the fetched records to persistentDataPath for offline use and geo conversion
//  - Expose events/utilities so UI and gameplay systems can inspect the loaded data easily
//
// NOTE: The script logs aggressively to help diagnose data issues in production builds.

using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using System.IO;
using Newtonsoft.Json;
using Firebase;
using System.Linq;
using Firebase.Database;
using Firebase.Extensions;

[System.Serializable]
public class Substation
{
    // Core identity / classification
    public string SYSTEM_ID;        // Unique numeric/string identifier
    public string USER_REF_I;       // Human readable reference
    public string SITE_DESC;        // Friendly site description
    public string TR_TYPE;          // Type of asset (e.g. PILLAR)

    // Electrical properties
    public string MAX_KVA;
    public string MAX_VOLT;
    public double LON;
    public double LAT;
    public string REFRESH_DT;

    // Detailed technical fields used across the UI
    public string SERIAL_NUMBER;
    public string MODEL_NUMBER;
    public string NUMBER_OF_PHASES;
    public string LAST_SERVICE_DATE;
    public string NEXT_SERVICE_DATE;
    public string ADDRESS;
}

public class DBLoader : MonoBehaviour
{
    [Header("UI Output (optional)")]
    public TextMeshProUGUI displayText;

    [Header("Defaults")]
    public string systemIdToLoad = "0";        // ID used when calling DisplaySubstationInfo manually
    public bool IsLoaded { get; private set; } = false;

    private List<Substation> substations;

    [Header("Geo Conversion")]
    [SerializeField] private convertToGeo geoConverter; // Optional component that consumes the freshly loaded records

    public event Action<List<Substation>> OnSubstationsLoaded;

    void Awake()
    {
        // Kick off dependency check and begin local/Firebase load chain.
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result != DependencyStatus.Available)
            {
                Debug.LogError("Firebase dependencies not available: " + task.Result);
                // We still try to serve cached data so the app can limp along offline.
                LoadSubstationsFromJson();
                return;
            }

            // Initialise default Firebase app on-demand. Avoid duplicate creation guard.
            if (FirebaseApp.DefaultInstance == null)
            {
                var options = new AppOptions
                {
                    ApiKey = "AIzaSyBzQG8CuZM34Ktj36w4-bY8IFmWTQsyDk",
                    AppId = "1:153954704089:android:809d3034934f79951a7cc8",
                    ProjectId = "energyqld-915da",
                    DatabaseUrl = new Uri("https://energyqld-915da-default-rtdb.firebaseio.com/")
                };
                FirebaseApp.Create(options);
            }

            // Prefer cached data, fall back to realtime DB automatically.
            LoadSubstationsFromJson();
        });
    }

    // ---------- Debug / Inspection Helpers ----------

    private void LogPersistentPath()
    {
        Debug.Log($"[DBLoader] persistentDataPath = {Application.persistentDataPath}");
    }

    [ContextMenu("Dump Local JSON (raw)")]
    public void DumpLocalJson()
    {
        try
        {
            string path = Path.Combine(Application.persistentDataPath, "DistSubstation.json");
            LogPersistentPath();
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[DBLoader] No JSON at {path}");
                return;
            }

            string json = File.ReadAllText(path);
            int previewLen = Mathf.Min(json.Length, 4000);
            Debug.Log($"[DBLoader] DistSubstation.json ({json.Length} chars). Preview:\n{json.Substring(0, previewLen)}");
            if (json.Length > previewLen) Debug.Log("[DBLoader] ...truncated preview...");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DBLoader] DumpLocalJson failed: {ex.Message}");
        }
    }

    public void PrintSubstationsSample(int count = 5)
    {
        if (substations == null || substations.Count == 0)
        {
            Debug.LogWarning("[DBLoader] Substations list is empty (load not finished or no data).");
            return;
        }

        int n = Mathf.Min(count, substations.Count);
        var sample = substations.Take(n).ToList();
        string pretty = JsonConvert.SerializeObject(sample, Formatting.Indented);
        Debug.Log($"[DBLoader] Showing first {n}/{substations.Count} substations:\n{pretty}");
    }

    public void PrintAvailableFields()
    {
        if (substations == null || substations.Count == 0) return;
        var s = substations[0];
        Debug.Log(
            "[DBLoader] Fields example:\n" +
            $"SYSTEM_ID={s.SYSTEM_ID}, USER_REF_I={s.USER_REF_I}, SITE_DESC={s.SITE_DESC}, TR_TYPE={s.TR_TYPE}, " +
            $"MAX_KVA={s.MAX_KVA}, MAX_VOLT={s.MAX_VOLT}, LAT={s.LAT}, LON={s.LON}, REFRESH_DT={s.REFRESH_DT}, " +
            $"SERIAL_NUMBER={s.SERIAL_NUMBER}, MODEL_NUMBER={s.MODEL_NUMBER}, NUMBER_OF_PHASES={s.NUMBER_OF_PHASES}, " +
            $"LAST_SERVICE_DATE={s.LAST_SERVICE_DATE}, NEXT_SERVICE_DATE={s.NEXT_SERVICE_DATE}, ADDRESS={s.ADDRESS}"
        );
    }

    // ---------- Save / Load ----------

    private void SaveSubstationsToJson()
    {
        if (substations == null || substations.Count == 0) return;

        string json = JsonConvert.SerializeObject(substations, Formatting.Indented);
        string path = Path.Combine(Application.persistentDataPath, "DistSubstation.json");
        File.WriteAllText(path, json);

        if (geoConverter != null)
        {
            geoConverter.Convert(substations);
        }

        Debug.Log($"[DBLoader] Saved {substations.Count} substations to JSON at: {path}");
        Debug.Log($"[DBLoader] Save preview:\n{json.Substring(0, Math.Min(json.Length, 1000))}");
    }

    private void LoadSubstationsFromJson()
    {
        string path = Path.Combine(Application.persistentDataPath, "DistSubstation.json");
        LogPersistentPath();

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            substations = JsonConvert.DeserializeObject<List<Substation>>(json) ?? new List<Substation>();
            IsLoaded = true;
            Debug.Log($"[DBLoader] Loaded {substations.Count} substations from local JSON");
            PrintSubstationsSample(3);
            PrintAvailableFields();
            OnSubstationsLoaded?.Invoke(substations);
        }
        else
        {
            Debug.Log("[DBLoader] No local JSON found, will fetch from Firebase");
            LoadDatabaseFromFirebase();
        }
    }

    private void LoadDatabaseFromFirebase()
    {
        Debug.Log("[DBLoader] Loading substations from Firebase...");

        var database = FirebaseDatabase.DefaultInstance;

        database.RootReference.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("[DBLoader] Firebase data load error: " + task.Exception);
                substations = new List<Substation>();
                IsLoaded = true;
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                string json = snapshot.GetRawJsonValue();

                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        substations = JsonConvert.DeserializeObject<List<Substation>>(json) ?? new List<Substation>();
                        Debug.Log($"[DBLoader] Successfully loaded {substations.Count} substations from Firebase");
                        IsLoaded = true;

                        // Sample logs help verify remote data integrity.
                        PrintSubstationsSample(3);
                        PrintAvailableFields();

                        SaveSubstationsToJson();
                        OnSubstationsLoaded?.Invoke(substations);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[DBLoader] Failed to parse Firebase JSON: " + e.Message);
                        substations = new List<Substation>();
                        IsLoaded = true;
                    }
                }
                else
                {
                    Debug.LogWarning("[DBLoader] Firebase returned empty data.");
                    substations = new List<Substation>();
                    IsLoaded = true;
                }
            }
        });
    }

    // ---------- UI Helpers ----------

    public List<Substation> GetSubstations()
    {
        return substations ?? new List<Substation>();
    }

    public void LoadSubstationInfoFromButton()
    {
        DisplaySubstationInfo(systemIdToLoad);
    }

    public void ShowFirstRecord()
    {
        if (substations != null && substations.Count > 0)
            DisplaySubstationInfo(substations[0].SYSTEM_ID);
        else if (displayText != null)
            displayText.text = "No records loaded.";
    }

    public void DisplaySubstationInfo(string systemId)
    {
        if (substations == null)
        {
            if (displayText != null) displayText.text = "Data not yet loaded.";
            return;
        }

        Substation result = substations.Find(s => s.SYSTEM_ID == systemId);

        if (result != null)
        {
            if (displayText != null)
            {
                displayText.text =
                    $"System ID: {result.SYSTEM_ID}\n" +
                    $"User Ref: {result.USER_REF_I}\n" +
                    $"Site: {result.SITE_DESC}\n" +
                    $"Type: {result.TR_TYPE}, KVA: {result.MAX_KVA}, Volt: {result.MAX_VOLT}\n" +
                    $"Location: ({result.LAT}, {result.LON})\n" +
                    $"Updated: {result.REFRESH_DT}";
            }
        }
        else
        {
            if (displayText != null) displayText.text = $"System ID '{systemId}' not found.";
        }
    }
}
