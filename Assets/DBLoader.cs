using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using TMPro;
using Newtonsoft.Json;
using Firebase;
using System.Linq;
using Firebase.Database;
using Firebase.Extensions;

[System.Serializable]
public class Substation
{
    public string SYSTEM_ID;
    public string USER_REF_I;
    public string SITE_DESC;
    public string TR_TYPE;
    public string MAX_KVA;
    public string MAX_VOLT;
    public double LON;
    public double LAT;
    public string REFRESH_DT;
}


public class DBLoader : MonoBehaviour
{
    public TextMeshProUGUI displayText;
    public string systemIdToLoad = "0";
    public bool IsLoaded { get; private set; } = false;

    private List<Substation> substations;
    [SerializeField] private convertToGeo geoConverter;


    private void SaveSubstationsToJson()
    {
        if (substations == null || substations.Count == 0) return;

        string json = JsonConvert.SerializeObject(substations, Formatting.Indented);
        File.WriteAllText(Path.Combine(Application.persistentDataPath, "substations.json"), json);
        geoConverter.Convert(substations);

        Debug.Log($"Saved {substations.Count} substations to JSON.");

    }
    private void LoadSubstationsFromJson()
    {
        string path = Path.Combine(Application.persistentDataPath, "substations.json");
        // if (!File.Exists(path)) return false;
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            substations = JsonConvert.DeserializeObject<List<Substation>>(json);
            IsLoaded = true;
            Debug.Log($"Loaded {substations.Count} substations from local JSON");
            OnSubstationsLoaded?.Invoke(substations);
        }
        else
        {
            Debug.Log("No local JSON found, will fetch from Firebase");
            LoadDatabaseFromFirebase();
        }
    }


    void Awake()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result != DependencyStatus.Available)
            {
                Debug.LogError("Firebase dependencies not available: " + task.Result);
                return;
            }

            // ✅ Initialize default Firebase app
            AppOptions options = new AppOptions
            {
                ApiKey = "AIzaSyBzQG8CuZM34Ktj36w4-bY8IFmWTQsyDk",
                AppId = "1:153954704089:android:809d3034934f79951a7cc8",
                ProjectId = "energyqld-915da",
                DatabaseUrl = new Uri("https://energyqld-915da-default-rtdb.firebaseio.com/")
            };

            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(options);
            }

            // LoadDatabaseFromFirebase();
            LoadSubstationsFromJson();
        });
    }

    public event Action<List<Substation>> OnSubstationsLoaded;

    private void LoadDatabaseFromFirebase()
    {
        Debug.Log("Loading substations from Firebase...");

        var database = FirebaseDatabase.DefaultInstance;

        database.RootReference.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Firebase data load error: " + task.Exception);
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
                        substations = JsonConvert.DeserializeObject<List<Substation>>(json);
                        Debug.Log($"Successfully loaded {substations.Count} substations from Firebase");
                        IsLoaded = true;
                        SaveSubstationsToJson();
                        // Notify any listeners that data is ready
                        OnSubstationsLoaded?.Invoke(substations);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Failed to parse Firebase JSON: " + e.Message);
                        substations = new List<Substation>();
                    }
                }
                else
                {
                    Debug.LogWarning("Firebase returned empty data.");
                    substations = new List<Substation>();
                }
            }
        });
    }
    public List<Substation> GetSubstations()
    {
        return substations ?? new List<Substation>();
    }

    public void LoadSubstationInfoFromButton()
    {
        DisplaySubstationInfo(systemIdToLoad);
    }

    public void DisplaySubstationInfo(string systemId)
    {
        if (substations == null)
        {
            displayText.text = "Data not yet loaded from Firebase.";
            return;
        }

        Substation result = substations.Find(s => s.SYSTEM_ID == systemId);

        if (result != null)
        {
            displayText.text =
                $"System ID: {result.SYSTEM_ID}\n" +
                $"User Ref: {result.USER_REF_I}\n" +
                $"Site: {result.SITE_DESC}\n" +
                $"Type: {result.TR_TYPE}, KVA: {result.MAX_KVA}, Volt: {result.MAX_VOLT}\n" +
                $"Location: ({result.LAT}, {result.LON})\n" +
                $"Updated: {result.REFRESH_DT}";
        }
        else
        {
            displayText.text = $"System ID '{systemId}' not found.";
        }
    }
}
