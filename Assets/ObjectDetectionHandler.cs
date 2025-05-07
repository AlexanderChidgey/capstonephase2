using UnityEngine;

public class ObjectDetectionHandler : MonoBehaviour
{
    public GameObject Cube;

    void Start(){
        if (Cube != null){
            Cube.SetActive(false); // Hide the Cube initially
        }else{
            Debug.LogWarning("Cube is not assigned in ObjectDetectionHandler.");
        }
    }

    public void HandleDetection(int classId){
        
        // if (Cube == null) return;

        Debug.Log($"Got Here: {classId}");

        Cube.SetActive(true); // Show the Cube when something is detected

        Renderer renderer = Cube.GetComponent<Renderer>();

        if (renderer != null)
        {
            if (classId == 0) // Power Pole
            {
                renderer.material.color = Color.red;
            }
            else if (classId == 1) // Pillar Box
            {
                renderer.material.color = Color.blue;
            }
            else
            {
                renderer.material.color = Color.white;
            }
        }

        // Optional: move the Cube or perform other actions
        // Cube.transform.position = new Vector3(0, 0.5f, 2f); // Move to visible location in front of camera
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
