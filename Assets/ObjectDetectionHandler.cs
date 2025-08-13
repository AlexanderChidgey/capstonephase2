using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using System.Text.RegularExpressions;

public class ObjectDetectionHandler : MonoBehaviour
{
    public GameObject Cube;
    public TMP_Text detectionText;
    public TMP_Text[] matchTexts;
    public TMP_Text[] matchIds;
    public GameObject[] matchInfoPanel;
    // public TMP_Text match1Id;
    // public TMP_Text match2Name;
    // public TMP_Text match2Id;

    private DistanceMatching distanceMatching;

    void Start()
    {
    if (matchInfoPanel != null)
    {
            foreach (var panel in matchInfoPanel)
            {
                panel.SetActive(false);
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

        if (Cube != null)
        {
            Cube.SetActive(false); // Hide the Cube initially
        }
        else
        {
            Debug.LogWarning("Cube is not assigned in ObjectDetectionHandler.");
        }

        // Get the text objects for the matches
        // matchTexts = new TMP_Text[] { match1Text, match2Text, match3Text };
        // matchIds = new TMP_Text[] { match1Id, match2Id, match3Id };

        
        // Get the DistanceMatching component
        distanceMatching = GetComponent<DistanceMatching>();
        if (distanceMatching == null)
        {
            distanceMatching = gameObject.AddComponent<DistanceMatching>();
            Debug.Log("Added DistanceMatching component");
        }
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

    public void HandleDetection(int classId, float latitude, float longitude, float heading)
    {
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
            nearbySubstations = distanceMatching.FindNearbySubstations(latitude, longitude, heading, objectType);
            Debug.Log("Objects Nearby Length: " + nearbySubstations.Length);
        }

        Debug.Log("Objects Nearby data: " + nearbySubstations);


        // Update detection text

        if (detectionText == null || matchTexts == null)
        {
            Debug.LogError("Cannot update text - TextMeshPro Text component is null!");
        }
        else if (nearbySubstations.Length <= 10)
        {
            Debug.LogWarning("Nearby substations data is too short or empty, skipping UI update.");
        }
        else
        {
            

            string fullText = $"Detected: {objectType}\n" +
                              $"Lat: {latitude:F6}, Lon: {longitude:F6}\n" +
                              $"Heading: {heading:F6}°\n" +
                              $"Substations:\n{nearbySubstations}";

            Debug.Log($"Setting detectionText: {fullText.Length} chars\n{fullText}");

            var matches = ExtractMatches(nearbySubstations);

            for (int i = 0; i < matchTexts.Length; i++)
            {
                if (i < matches.Count)
                {
                    matchTexts[i].SetText($"{matches[i].Name}");
                    matchIds[i].SetText($"{matches[i].ID}");
                    matchTexts[i].gameObject.SetActive(true);
                    matchIds[i].gameObject.SetActive(true);
                    matchInfoPanel[i].SetActive(true);
                }
                else
                {
                    matchTexts[i].gameObject.SetActive(false);
                    matchIds[i].gameObject.SetActive(false);
                    matchInfoPanel[i].SetActive(false);
                }
            }


            



            detectionText.SetText(fullText);
            detectionText.ForceMeshUpdate();
            // LayoutRebuilder.ForceRebuildLayoutImmediate(detectionText.rectTransform);
        }

    }
}


// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;

// public class ObjectDetectionHandler : MonoBehaviour
// {
//     public GameObject canvas; // Optional: Reference to a canvas to hold the text
//     private GameObject detectionTextGO; // Reference to the dynamically created text GameObject

//     // private CaptureAndRunYOLO captureAndRunYOLOScript; // Reference to the existing YOLO script
//     CharacterController controller;


//     void Start()
//     {
//         controller = 
//         // Get reference to the CaptureAndRunYOLO script in the scene
//         // captureAndRunYOLOScript = FindObjectOfType<GameObject>();
        
//         // Ensure canvas is available if you want it as a parent
//         if (canvas != null)
//         {
//             canvas.SetActive(false); // Hide canvas initially
//         }

//         if (captureAndRunYOLOScript == null)
//         {
//             Debug.LogError("CaptureAndRunYOLO script not found in the scene.");
//         }
//     }

//     // This method will handle the detection logic and dynamically create text
//     public void HandleDetection(int classId)
//     {
//         // Create a new GameObject with a Text component
//         detectionTextGO = new GameObject("DetectionText");
        
//         // Set the parent of the text GameObject to the canvas (optional)
//         if (canvas != null)
//         {
//             detectionTextGO.transform.SetParent(canvas.transform, false);
//         }

//         // Add the Text component to the new GameObject
//         Text detectionText = detectionTextGO.AddComponent<Text>();

//         // Set font and style (optional but recommended)
//         detectionText.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); // Default font
//         detectionText.fontSize = 24;  // Set font size
//         detectionText.color = Color.white;  // Set text color

//         // Set position (optional, can be adjusted based on the canvas settings)
//         RectTransform rectTransform = detectionText.GetComponent<RectTransform>();
//         rectTransform.localPosition = new Vector3(0, 0, 0); // Position in the center (can be adjusted)

//         // Set the text based on the detection result
//         if (classId == 0) // Power Pole detected
//         {
//             detectionText.text = "Found Power Pole!";
//         }
//         else if (classId == 1) // Pillar Box detected
//         {
//             detectionText.text = "Found Pillar Box!";
//         }
//         else
//         {
//             detectionText.text = "Object Detected!";
//         }

//         // Optionally, you can add logic to destroy the text after a certain time
//         // Destroy(detectionTextGO, 3f); // Destroy the text object after 3 seconds
//     }
// }
