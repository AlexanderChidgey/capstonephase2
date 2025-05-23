using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;

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
    public string systemIdToLoad = "2692672"; // <- Set in Inspector

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
            string json = File.ReadAllText(path);
            substations = JsonHelper.FromJson<Substation>(json);
            Debug.Log($"Successfully loaded {substations.Count} substations");
        }
        else
        {
            Debug.LogError("DistSubstations.json not found.");
            substations = new List<Substation>();
        }
    }

    // Public method to access the substations list
    public List<Substation> GetSubstations()
    {
        if (substations == null)
        {
            LoadDatabase();
        }
        return substations;
    }

    // This method can be called from the Button!
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

public static class JsonHelper
{
    [System.Serializable]
    private class Wrapper<T>
    {
        public List<T> Items;
    }

    public static List<T> FromJson<T>(string json)
    {
        string wrappedJson = "{\"Items\":" + json + "}";
        return JsonUtility.FromJson<Wrapper<T>>(wrappedJson).Items;
    }
}
