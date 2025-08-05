using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using System.IO;
using Newtonsoft.Json; // ← make sure Newtonsoft.Json is imported!

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
    public string systemIdToLoad = "2692672";

    private List<Substation> substations;

    void Awake()
    {
        LoadDatabase();
    }

    private void LoadDatabase()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "DistSubstations.json");

        if (File.Exists(path))
        {
            Debug.Log($"Reading from path: {path}");
            Debug.Log($"File size: {new FileInfo(path).Length} bytes");

            try
            {
                using (StreamReader file = File.OpenText(path))
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    substations = serializer.Deserialize<List<Substation>>(reader);
                }
                Debug.Log($"Successfully loaded {substations.Count} substations");

                // Optional: Verify by writing to a file
                File.WriteAllText(Path.Combine(Application.persistentDataPath, "load_log.txt"),
                    $"Loaded {substations.Count} items");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load JSON: {e.Message}");
                substations = new List<Substation>();
            }
        }
        else
        {
            Debug.LogError("DistSubstations.json not found.");
            substations = new List<Substation>();
        }
    }

    public List<Substation> GetSubstations()
    {
        if (substations == null)
        {
            LoadDatabase();
        }
        return substations;
    }

    public void LoadSubstationInfoFromButton()
    {
        DisplaySubstationInfo(systemIdToLoad);
    }

    public void DisplaySubstationInfo(string systemId)
    {
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
