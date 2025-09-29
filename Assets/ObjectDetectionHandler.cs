using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Linq;



public static class DetectionDataStore
{
    public static string SelectedName;
    public static string SelectedId;
    public static float Latitude;
    public static float Longitude;
    public static float Heading;
}

public class ObjectDetectionHandler : MonoBehaviour
{
    public GameObject Cube;
    public TMP_Text detectionText;
    public TMP_Text[] matchTexts;
    public TMP_Text[] matchIds;
    private VisualElement[] matchInfoPanel;
    private DistanceMatching distanceMatching;
    private UIController uiController;
    public DBLoader dbLoader;
    private List<Substation> cachedSubstations;
    private List<Substation> substations;


    void Start()
    {
        var uiDocumentGO = GameObject.Find("UIDocument");
        if (uiDocumentGO != null)
        {
            uiController = uiDocumentGO.GetComponent<UIController>();
            if (uiController == null)
                Debug.LogWarning("UIController component not found on UIDocument GameObject.");
        }
        var uiDoc = uiDocumentGO.GetComponent<UIDocument>();

        if (uiDoc != null)
        {
            var root = uiDoc.rootVisualElement;

            matchInfoPanel = new VisualElement[]
            {
                root.Q<VisualElement>("MatchInfoPanel1"),
                root.Q<VisualElement>("MatchInfoPanel2"),
                root.Q<VisualElement>("MatchInfoPanel3")
            };

            foreach (var panel in matchInfoPanel)
            {
                if (panel != null)
                    panel.style.display = DisplayStyle.None;
                else
                    Debug.LogWarning("One of the matchInfoPanel entries is null!");
            }
        }

        if (matchTexts != null)
        {
            foreach (var text in matchTexts)
            {
                if (text != null)

                    text.gameObject.SetActive(false);
            }
        }

        if (matchIds != null)
        {
            foreach (var text in matchIds)
            {
                if (text != null)
                    text.gameObject.SetActive(false);
            }
        }
        // Get the DistanceMatching component
        distanceMatching = GetComponent<DistanceMatching>();
        if (distanceMatching == null)
        {
            distanceMatching = gameObject.AddComponent<DistanceMatching>();
            Debug.Log("Added DistanceMatching component");
        }

        if (dbLoader != null)
        {
            substations = dbLoader.GetSubstations();
            cachedSubstations = substations;         

            Debug.Log($"Loaded {substations.Count} substation data successfully");
        }
        else
        {
            Debug.Log($"ERROR: Substations is null!");
        }
        #if UNITY_EDITOR
            // Mock coordinates (Brisbane CBD) for testing in the simulator
            float latitude = -27.4698f;
            float longitude = 153.0251f;
            float heading = 90f;
            int classId = 0;
        
            HandleDetection(classId, latitude, longitude, heading);
            Debug.Log($"Class ID: {classId}, Latitude: {latitude}, Longitude: {longitude}, Heading: {heading}");
                
        #endif
    }
    public class MatchInfo
    {
        public string Name;
        public string ID;
    }


    public static List<MatchInfo> ExtractMatches(string input)
    {
        var results = new List<MatchInfo>();
        string[] lines = input.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        MatchInfo current = null;

        foreach (var line in lines)
        {
            if (line.StartsWith("Match"))
            {
                current = new MatchInfo();
                // Get the name after the colon
                int colonIndex = line.IndexOf(':');
                if (colonIndex >= 0)
                {
                    current.Name = line.Substring(colonIndex + 1).Trim();
                }
                results.Add(current);
            }
            else if (current != null && line.Trim().Contains("ID:"))
            {
                var parts = line.Trim().Split(',');

                if (parts.Length >= 2)
                {
                    Match idMatch = Regex.Match(parts[1], @"ID:(\d+)");
                    if (idMatch.Success)
                    {
                        current.ID = idMatch.Groups[1].Value;
                    }
                }
            }
        }

        return results;
    }
    public void SetSubstations(List<Substation> loadedSubstations)
    {
        cachedSubstations = loadedSubstations;
        substations = loadedSubstations;
        Debug.Log($"ObjectDetectionHandler received {substations.Count} substations.");
    }

    public void HandleDetection(int classId, float latitude, float longitude, float heading)
    {
        if (cachedSubstations == null)
        {
            Debug.Log("Data not loaded Yet");
            return;
        }

        if (substations == null || substations.Count == 0)
        {
            Debug.Log("Not substations Loaded Yet");
            return;
        }


        Debug.Log($"Detection: {classId},{latitude},{longitude},{heading}");

        string objectType = "";

        if (classId == 0)
        {
            objectType = "PILLAR";
        }
        else if (classId == 1)
        {
            objectType = "POLE";
        }
        else
        {
            objectType = "C&I";
        }

        // Get nearby substations using DistanceMatching
        string nearbySubstations = "";
        if (distanceMatching != null)
        {
            nearbySubstations = distanceMatching.FindNearbySubstations(latitude, longitude, heading, objectType, substations);
            Debug.Log("Objects Nearby Length: " + nearbySubstations.Length);
        }

        Debug.Log("Objects Nearby data: " + nearbySubstations);


        // Update detection text

        if (matchTexts == null)
        {
            Debug.LogError("Cannot update text - TextMeshPro Text component is null!");
        }
        else if (nearbySubstations.Length <= 10)
        {
            Debug.LogWarning("Nearby substations data is too short or empty, skipping UI update.");
        }
        else
        {
            var matches = ExtractMatches(nearbySubstations);
            var showCount = 3;
            if (uiController != null)
            {
                uiController.UpdateDetectionUI(matches.Take(3).ToList());
            }
            else
            {
                Debug.LogWarning("UIController not assigned.");
            }
        }

    }
}
