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
    public GameObject[] matchInfoPanel;
    private DistanceMatching distanceMatching;
    private UIController uiController;

    void Start()
    {
        var uiDocumentGO = GameObject.Find("UIDocument");
        if (uiDocumentGO != null)
        {
            uiController = uiDocumentGO.GetComponent<UIController>();
            if (uiController == null)
                Debug.LogWarning("UIController component not found on UIDocument GameObject.");
        }
        else
        {
            Debug.LogWarning("UIDocument GameObject not found.");
        }

        if (matchInfoPanel != null)
        {
            foreach (var panel in matchInfoPanel)
            {
                if (panel != null)
                {
                    panel.SetActive(false);
                }
                else
                {
                    Debug.LogWarning("One of the matchInfoPanel entries is null!");
                }
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
            }else
            {
                Debug.LogWarning("UIController not assigned.");
            }

            // Also update your detectionText if you want:
            // if (detectionText != null)
            // {
            //     string fullText = $"Detected: {objectType}\n" +
            //                     $"Lat: {latitude:F6}, Lon: {longitude:F6}\n" +
            //                     $"Heading: {heading:F6}°\n" +
            //                     $"Substations:\n{nearbySubstations}";
            //     detectionText.SetText(fullText);
            //     detectionText.ForceMeshUpdate();
            // }


            // for (int i = 0; i < matchTexts.Length; i++)
            // {
            //     if (i < showCount)
            //     {
            //         matchTexts[i].SetText($"{matches[i].Name}");
            //         matchIds[i].SetText($"{matches[i].ID}");
            //         matchTexts[i].gameObject.SetActive(true);
            //         matchIds[i].gameObject.SetActive(true);
            //         matchInfoPanel[i].SetActive(true);

            //         // Add a click listener to the panel/gameobject to navigate to InfoData
            //         var index = i; // local copy for closure
            //         var buttonComponent = matchInfoPanel[i].GetComponent<UnityEngine.UI.Button>();
            //         if (buttonComponent != null)
            //         {
            //             buttonComponent.onClick.RemoveAllListeners(); // Remove existing listeners to avoid duplicates
            //             buttonComponent.onClick.AddListener(() =>
            //             {
            //                 // Save data to static store
            //                 DetectionDataStore.SelectedName = matches[index].Name;
            //                 DetectionDataStore.SelectedId = matches[index].ID;
            //                 DetectionDataStore.Latitude = latitude;
            //                 DetectionDataStore.Longitude = longitude;
            //                 DetectionDataStore.Heading = heading;

            //                 // Load InfoData scene
            //                 SceneManager.LoadScene("InfoData");
            //             });
            //         }
            //         else
            //         {
            //             Debug.LogWarning($"No Button component found on matchInfoPanel[{i}]");
            //         }
            //     }
            //     else
            //     {
            //         matchTexts[i].gameObject.SetActive(false);
            //         matchIds[i].gameObject.SetActive(false);
            //         matchInfoPanel[i].SetActive(false);
            //     }
            // }








            // detectionText.SetText(fullText);
            // detectionText.ForceMeshUpdate();
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
