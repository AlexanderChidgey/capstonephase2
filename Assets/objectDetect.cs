// You said:
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using FF = Unity.Sentis.Functional;
// using Unity.Sentis.Functional;

public class ARYOLODetector : MonoBehaviour
{
    [Tooltip("Drag a YOLO model .onnx file here")]
    public ModelAsset modelAsset;
    [Tooltip("Drag the classes.txt here")]
    public TextAsset classesAsset;

    [Tooltip("Create a Raw Image in the scene and link it here")]
    public RawImage displayImage;

    [Tooltip("Drag a border box texture here")]
    public Texture2D borderTexture;

    [Tooltip("Select an appropriate font for the labels")]
    public Font font;

    public ARCameraManager cameraManager;  // AR Camera Manager

    private Worker worker;
    private string[] labels;
    private Tensor<float> inputTensor;
    private RenderTexture targetRT;
    private List<GameObject> boxPool = new List<GameObject>();
    // private string[] labels;

    // Image size for the model
    private const int imageWidth = 640;
    private const int imageHeight = 640;

    [Tooltip("Intersection over union threshold used for non-maximum suppression")]
    [SerializeField, Range(0, 1)] float iouThreshold = 0.5f;

    [Tooltip("Confidence score threshold used for non-maximum suppression")]
    [SerializeField, Range(0, 1)] float scoreThreshold = 0.5f;

    Tensor<float> centersToCorners;

    private const float detectionThreshold = 0.2f; // Confidence threshold for detection

    void Start()
    {
        // Debugging: Start loading the model
        Debug.Log("Loading YOLO model...");

        labels = classesAsset.text.Split('\n');
        
        // Load YOLO model
        var model = ModelLoader.Load(modelAsset);
        worker = new Worker(model, BackendType.GPUCompute);

        // Setup render texture and input tensor for YOLO
        targetRT = new RenderTexture(imageWidth, imageHeight, 0);
        inputTensor = new Tensor<float>(new TensorShape(1, 3, imageHeight, imageWidth));

        // Load labels (you should add a way to load them, for example, from a text file)
        labels = new string[80]; // Replace with the correct labels if needed

        // Initialize the centers-to-corners tensor
        centersToCorners = new Tensor<float>(new TensorShape(4, 4),
        new float[]
        {
            1, 0, 1, 0,
            0, 1, 0, 1,
            -0.5f, 0, 0.5f, 0,
            0, -0.5f, 0, 0.5f
        });

        // Debugging: Successfully loaded model
        Debug.Log("Model loaded successfully.");
    }

    void OnDisable()
    {
        Debug.Log("Disposing worker and tensors...");

        worker?.Dispose();
        inputTensor?.Dispose();
        centersToCorners?.Dispose();

        Debug.Log("Worker and tensors disposed.");
    }

    void Update()
    {
        // Debugging: Checking if the camera is acquiring the latest image
        if (cameraManager.TryAcquireLatestCpuImage(out var cpuImage))
        {
            // Debugging: Successfully acquired AR camera image
            Debug.Log("Successfully acquired AR camera image.");
            
            // Convert AR camera image to a texture for YOLO
            var conversionParams = new XRCpuImage.ConversionParams
            {
                inputRect = new RectInt(0, 0, cpuImage.width, cpuImage.height),
                outputDimensions = new Vector2Int(imageWidth, imageHeight),
                outputFormat = TextureFormat.RGBA32,
                transformation = XRCpuImage.Transformation.None // Do not mirror or flip
            };

            var rawTexture = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false);
            var rawData = new NativeArray<byte>(conversionParams.outputDimensions.x * conversionParams.outputDimensions.y * 4, Allocator.Temp);
            cpuImage.Convert(conversionParams, rawData);
            rawTexture.LoadRawTextureData(rawData);
            rawTexture.Apply();
            rawData.Dispose();
            cpuImage.Dispose();

            // Display on the UI for debugging purposes
            displayImage.texture = rawTexture;

            if (rawTexture == null)
            {
                Debug.LogError("rawTexture is null. Check the camera image conversion process.");
                return;
            }

            // Debugging: Successfully converted image to tensor
            Debug.Log("Converted image to tensor for YOLO input.");

            // Preprocess the image to a tensor format for YOLO input
            TextureConverter.ToTensor(rawTexture, inputTensor, new TextureTransform());

            // Run YOLO inference
            Debug.Log("Running YOLO inference...");
            worker.Schedule(inputTensor);

            // Get the output (bounding box predictions)
            using var output = (worker.PeekOutput() as Tensor<float>).ReadbackAndClone();
            // for (var i = 0; i< output.length; i++){
            //     Debug.Log(output[0][i]);
            // }
            Debug.Log($"Inference complete. Output tensor shape: {output.shape}");

            // Process output and draw bounding boxes
            ProcessOutput(output);

            // Dispose of the output tensor after usage
            output.Dispose();
        }
        else
        {
            Debug.LogWarning("Failed to acquire AR camera image.");
        }
    }

    private void ProcessOutput(Tensor<float> output)
    {
        // Debugging: Checking output shape and contents
        Debug.Log($"Processing output with shape: {output.shape}");

        float displayWidth = displayImage.rectTransform.rect.width;
        float displayHeight = displayImage.rectTransform.rect.height;

        float scaleX = displayWidth / imageWidth;
        float scaleY = displayHeight / imageHeight;

        int boxesFound = output.shape[0]; // Number of boxes found
        Debug.Log($"Boxes found: {boxesFound}");

        // Loop over the boxes found in the output
        for (int i = 0; i < Mathf.Min(boxesFound, 10); i++) // Limit to 10 boxes for example
        {
            // Extract bounding box coordinates and confidence
            float centerX = output[i, 0] * scaleX - displayWidth / 2;
            float centerY = output[i, 1] * scaleY - displayHeight / 2;
            float width = output[i, 2] * scaleX;
            float height = output[i, 3] * scaleY;
            float confidence = output[i, 4]; // Confidence score

            // Debugging: Print the bounding box and confidence values
            Debug.Log($"Box {i}: CenterX = {centerX}, CenterY = {centerY}, Width = {width}, Height = {height}, Confidence = {confidence}");

            // Apply confidence threshold to filter out low-confidence boxes
            if (confidence > detectionThreshold)
            {
                // Optionally, apply non-maximum suppression here if needed
                // For simplicity, we're skipping this step in this basic example

                // Convert the bounding box coordinates to UI coordinates (optional)
                Debug.Log($"Drawing box {i} with confidence: {confidence}");
                DrawBoundingBox(centerX, centerY, width, height, confidence);
            }
            else
            {
                Debug.Log($"Skipping box {i} with low confidence: {confidence}");
            }
        }
    }

    private void DrawBoundingBox(float centerX, float centerY, float width, float height, float confidence)
    {
        // Debugging: Drawing bounding box details
        Debug.Log($"Drawing bounding box at ({centerX}, {centerY}) with size ({width}, {height})");

        // Create a bounding box on the screen (you can use UI elements or 3D objects for this)
        GameObject box = new GameObject("BoundingBox");
        box.transform.SetParent(displayImage.transform);

        RectTransform rt = box.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);
        rt.localPosition = new Vector3(centerX, centerY, 0);

        Image img = box.AddComponent<Image>();
        img.color = Color.yellow;

        // Optionally, display the confidence score as a label
        var label = new GameObject("Label");
        label.transform.SetParent(box.transform);

        Text text = label.AddComponent<Text>();
        text.text = $"{confidence * 100:0.0}%";
        text.color = Color.white;
        text.font = font;
        text.fontSize = 14;

        RectTransform labelRT = label.GetComponent<RectTransform>();
        labelRT.localPosition = new Vector3(0, height / 2 + 10, 0); // Position label above box
    }
}
// using System.Collections.Generic;
// using Unity.Sentis;
// using UnityEngine;
// using UnityEngine.UI;
// using UnityEngine.XR.ARFoundation;
// using UnityEngine.XR.ARSubsystems;
// using Unity.Collections;
// using FF = Unity.Sentis.Functional;

// public class ARYOLODetector : MonoBehaviour
// {
//     [Tooltip("Drag a YOLO model .onnx file here")]
//     public ModelAsset modelAsset;
//     [Tooltip("Drag the classes.txt here")]
//     public TextAsset classesAsset;

//     [Tooltip("Create a Raw Image in the scene and link it here")]
//     public RawImage displayImage;

//     [Tooltip("Drag a border box texture here")]
//     public Texture2D borderTexture;

//     [Tooltip("Select an appropriate font for the labels")]
//     public Font font;

//     public ARCameraManager cameraManager;  // AR Camera Manager

//     private Worker worker;
//     private string[] labels;
//     private Tensor<float> inputTensor;
//     private RenderTexture targetRT;
//     private List<GameObject> boxPool = new List<GameObject>();

//     // Image size for the model
//     private const int imageWidth = 640;
//     private const int imageHeight = 640;

//     [Tooltip("Intersection over union threshold used for non-maximum suppression")]
//     [SerializeField, Range(0, 1)] float iouThreshold = 0.5f;

//     [Tooltip("Confidence score threshold used for non-maximum suppression")]
//     [SerializeField, Range(0, 1)] float scoreThreshold = 0.5f;

//     Tensor<float> centersToCorners;

//     private const float detectionThreshold = 0.2f; // Confidence threshold for detection

//     void Start()
//     {
//         // Debugging: Start loading the model
//         Debug.Log("Loading YOLO model...");

//         labels = classesAsset.text.Split('\n');
        
//         // Load YOLO model
//         var model = ModelLoader.Load(modelAsset);
//         worker = new Worker(model, BackendType.GPUCompute);

//         // Setup render texture and input tensor for YOLO
//         targetRT = new RenderTexture(imageWidth, imageHeight, 0);
//         inputTensor = new Tensor<float>(new TensorShape(1, 3, imageHeight, imageWidth));

//         // Initialize the centers-to-corners tensor
//         centersToCorners = new Tensor<float>(new TensorShape(4, 4),
//         new float[]
//         {
//             1, 0, 1, 0,
//             0, 1, 0, 1,
//             -0.5f, 0, 0.5f, 0,
//             0, -0.5f, 0, 0.5f
//         });

//         // Debugging: Successfully loaded model
//         Debug.Log("Model loaded successfully.");
//     }

//     void OnDisable()
//     {
//         Debug.Log("Disposing worker and tensors...");

//         worker?.Dispose();
//         inputTensor?.Dispose();
//         centersToCorners?.Dispose();

//         Debug.Log("Worker and tensors disposed.");
//     }

//     void Update()
//     {
//         // Debugging: Checking if the camera is acquiring the latest image
//         if (cameraManager.TryAcquireLatestCpuImage(out var cpuImage))
//         {
//             // Debugging: Successfully acquired AR camera image
//             Debug.Log("Successfully acquired AR camera image.");
            
//             // Convert AR camera image to a texture for YOLO
//             var conversionParams = new XRCpuImage.ConversionParams
//             {
//                 inputRect = new RectInt(0, 0, cpuImage.width, cpuImage.height),
//                 outputDimensions = new Vector2Int(imageWidth, imageHeight),
//                 outputFormat = TextureFormat.RGBA32,
//                 transformation = XRCpuImage.Transformation.None // Do not mirror or flip
//             };

//             var rawTexture = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false);
//             var rawData = new NativeArray<byte>(conversionParams.outputDimensions.x * conversionParams.outputDimensions.y * 4, Allocator.Temp);
//             cpuImage.Convert(conversionParams, rawData);
//             rawTexture.LoadRawTextureData(rawData);
//             rawTexture.Apply();
//             rawData.Dispose();
//             cpuImage.Dispose();

//             // Display on the UI for debugging purposes
//             displayImage.texture = rawTexture;

//             if (rawTexture == null)
//             {
//                 Debug.LogError("rawTexture is null. Check the camera image conversion process.");
//                 return;
//             }

//             // Debugging: Successfully converted image to tensor
//             Debug.Log("Converted image to tensor for YOLO input.");

//             // Preprocess the image to a tensor format for YOLO input
//             TextureConverter.ToTensor(rawTexture, inputTensor, new TextureTransform());

//             // Run YOLO inference
//             Debug.Log("Running YOLO inference...");
//             worker.Schedule(inputTensor);

//             // Get the output (bounding box predictions)
//             using var output = (worker.PeekOutput("output_0") as Tensor<float>).ReadbackAndClone();
//             Debug.Log($"Inference complete. Output tensor shape: {output.shape}");

//             // Process output and draw bounding boxes
//             ProcessOutput(output);

//             // Dispose of the output tensor after usage
//             output.Dispose();
//         }
//         else
//         {
//             Debug.LogWarning("Failed to acquire AR camera image.");
//         }
//     }

//     private void ProcessOutput(Tensor<float> output)
//     {
//         // Debugging: Checking output shape and contents
//         Debug.Log($"Processing output with shape: {output.shape}");

//         float displayWidth = displayImage.rectTransform.rect.width;
//         float displayHeight = displayImage.rectTransform.rect.height;

//         float scaleX = displayWidth / imageWidth;
//         float scaleY = displayHeight / imageHeight;

//         int boxesFound = output.shape[0]; // Number of boxes found
//         Debug.Log($"Boxes found: {boxesFound}");

//         // Extract box coordinates and class scores
//         var boxCoords = output[0, 0..4, ..].Transpose(0, 1);  // shape=(8400,4)
//         var allScores = output[0, 4.., ..];  // shape=(80,8400)
//         var scores = FF.ReduceMax(allScores, 0);  // shape=(8400)
//         var classIDs = FF.ArgMax(allScores, 0);  // shape=(8400)

//         // Apply the centers-to-corners transformation
//         var boxCorners = FF.MatMul(boxCoords, FF.Constant(centersToCorners));  // shape=(8400,4)

//         // Apply Non-Maximum Suppression
//         var indices = FF.NMS(boxCorners, scores, iouThreshold, scoreThreshold);  // shape=(N)
        
//         // Select final bounding box coordinates and class IDs after NMS
//         var coords = FF.IndexSelect(boxCoords, 0, indices);  // shape=(N,4)
//         var labelIDsFiltered = FF.IndexSelect(classIDs, 0, indices);  // shape=(N)

//         // Debugging: Log selected box and label info
//         for (int i = 0; i < coords.shape[0]; i++)
//         {
//             Debug.Log($"Box {i}: {coords[i]}, Label ID: {labelIDsFiltered[i]}, Score: {scores[i]}");
//         }

//         // Draw the bounding boxes
//         for (int n = 0; n < Mathf.Min(coords.shape[0], 10); n++)  // Limit to 10 boxes for example
//         {
//             var box = new BoundingBox
//             {
//                 centerX = coords[n, 0] * scaleX - displayWidth / 2,
//                 centerY = coords[n, 1] * scaleY - displayHeight / 2,
//                 width = (coords[n, 2] - coords[n, 0]) * scaleX,
//                 height = (coords[n, 3] - coords[n, 1]) * scaleY,
//                 label = labels[labelIDsFiltered[n]],
//             };
//             DrawBoundingBox(box, n, displayHeight * 0.05f);
//         }
//     }

//     private void DrawBoundingBox(BoundingBox box, int index, float labelYOffset)
//     {
//         // Create a bounding box on the screen
//         GameObject boxObj = new GameObject($"Box_{index}");
//         boxObj.transform.SetParent(displayImage.transform);

//         RectTransform rt = boxObj.AddComponent<RectTransform>();
//         rt.sizeDelta = new Vector2(box.width, box.height);
//         rt.localPosition = new Vector3(box.centerX, box.centerY, 0);

//         Image img = boxObj.AddComponent<Image>();
//         img.color = Color.yellow;

//         // Optionally, display the confidence score as a label
//         var label = new GameObject("Label");
//         label.transform.SetParent(boxObj.transform);

//         Text text = label.AddComponent<Text>();
//         text.text = $"{box.label}";
//         text.color = Color.white;
//         text.font = font;
//         text.fontSize = 14;

//         RectTransform labelRT = label.GetComponent<RectTransform>();
//         labelRT.localPosition = new Vector3(0, labelYOffset, 0); // Position label above box
//     }
// }

// // Add your own BoundingBox class if needed, it would be used to hold the bounding box data
// public class BoundingBox
// {
//     public float centerX;
//     public float centerY;
//     public float width;
//     public float height;
//     public string label;
// }



// Next test
// using UnityEngine;
// using UnityEngine.XR.ARFoundation;
// using UnityEngine.XR.ARSubsystems;
// using Unity.Sentis;
// using UnityEngine.UI;
// using Unity.Collections;

// public class ARYOLODetector : MonoBehaviour
// {
//     [Tooltip("Drag a YOLO model .onnx file here")]
//     public ModelAsset modelAsset;

//     [Tooltip("Create a Raw Image in the scene and link it here")]
//     public RawImage displayImage;

//     private Worker worker;
//     private Tensor<float> inputTensor;
//     private RenderTexture targetRT;

//     private const int imageWidth = 640;
//     private const int imageHeight = 640;

//     public ARCameraManager cameraManager;  // AR Camera Manager

//     private const float detectionThreshold = 0.2f; // Confidence threshold for detection

//     void Start()
//     {
//         // Load YOLO model
//         var model = ModelLoader.Load(modelAsset);
//         worker = new Worker(model, BackendType.GPUCompute);

//         // Setup render texture and input tensor for YOLO
//         targetRT = new RenderTexture(imageWidth, imageHeight, 0);
//         inputTensor = new Tensor<float>(new TensorShape(1, 3, imageHeight, imageWidth));
//     }

//     void OnDisable()
//     {
//         worker?.Dispose();
//         inputTensor?.Dispose();
//     }

//     void Update()
//     {
//         // Capture AR camera frame and perform inference
//         if (cameraManager.TryAcquireLatestCpuImage(out var cpuImage))
//         {
//             // Convert AR camera image to a texture for YOLO
//             var conversionParams = new XRCpuImage.ConversionParams
//             {
//                 inputRect = new RectInt(0, 0, cpuImage.width, cpuImage.height),
//                 outputDimensions = new Vector2Int(imageWidth, imageHeight),
//                 outputFormat = TextureFormat.RGBA32,
//                 transformation = XRCpuImage.Transformation.None // Do not mirror or flip
//             };

//             var rawTexture = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false);
//             var rawData = new NativeArray<byte>(conversionParams.outputDimensions.x * conversionParams.outputDimensions.y * 4, Allocator.Temp);
//             cpuImage.Convert(conversionParams, rawData);
//             rawTexture.LoadRawTextureData(rawData);
//             rawTexture.Apply();
//             rawData.Dispose();
//             cpuImage.Dispose();

//             // Display on the UI for debugging purposes
//             displayImage.texture = rawTexture;

//             if (rawTexture == null)
//             {
//                 Debug.LogError("rawTexture is null. Check the camera image conversion process.");
//                 return;
//             }

//             // Preprocess the image to a tensor format for YOLO input
//             TextureConverter.ToTensor(rawTexture, inputTensor, new TextureTransform());

//             // Run YOLO inference
//             worker.Schedule(inputTensor);

//             // Get the output (bounding box predictions)
//             using var output = (worker.PeekOutput("output_0") as Tensor<float>).ReadbackAndClone();
//             // using var labelIDs = (worker.PeekOutput("output_1") as Tensor<int>).ReadbackAndClone();
//             // Debug.LogError(labelIDs);

//             // Process output and draw bounding boxes
//             ProcessOutput(output);

//             // Dispose of the output tensor after usage
//             output.Dispose();
//         }
//     }

//     private void ProcessOutput(Tensor<float> output)
//     {
//         // Process the output to get bounding boxes and labels
//         // Example: Assuming output contains bounding box coordinates, class scores, and confidence
//         float displayWidth = displayImage.rectTransform.rect.width;
//         float displayHeight = displayImage.rectTransform.rect.height;

//         float scaleX = displayWidth / imageWidth;
//         float scaleY = displayHeight / imageHeight;


//         int boxesFound = output.shape[0]; // Number of boxes found
//         Debug.LogError($"Output tensor shape: {output.shape}");
//         Debug.LogError($"found: {boxesFound} items");
//         for (int i = 0; i < Mathf.Min(boxesFound, 10); i++) // Limit to 10 boxes for example
//         {
//             float centerX = output[i, 0] * scaleX - displayWidth / 2;
//             float centerY = output[i, 1] * scaleY - displayHeight / 2;
//             float width = output[i, 2] * scaleX;
//             float height = output[i, 3] * scaleY;

            
//             // float centerX = output[i, 0]; // Extract center X
//             // float centerY = output[i, 1]; // Extract center Y
//             // float width = output[i, 2];   // Extract width
//             // float height = output[i, 3];  // Extract height
//             float confidence = output[i, 4]; // Confidence score
//             Debug.LogError($"Box {i}: centerX = {centerX}, centerY = {centerY}, width = {width}, height = {height}, confidence = {confidence}");

//             // Apply confidence threshold to filter out low-confidence boxes
//             if (confidence > detectionThreshold)
//             {
//                 // Convert the bounding box coordinates to UI coordinates (optional)
//                 DrawBoundingBox(centerX, centerY, width, height);
//             }
//         }
//     }

//     private void DrawBoundingBox(float centerX, float centerY, float width, float height)
//     {
//         // Create a bounding box on the screen (you can use UI elements or 3D objects for this)
//         GameObject box = new GameObject("BoundingBox");
//         box.transform.SetParent(displayImage.transform);

//         RectTransform rt = box.AddComponent<RectTransform>();
//         rt.sizeDelta = new Vector2(width, height);
//         rt.localPosition = new Vector3(centerX, centerY, 0);

//         Image img = box.AddComponent<Image>();
//         img.color = Color.yellow;
//     }
// }

// Test 2
// using UnityEngine;
// using UnityEngine.XR.ARFoundation;
// using UnityEngine.XR.ARSubsystems;
// using Unity.Sentis;
// using UnityEngine.UI;
// using Unity.Collections;

// public class ARYOLODetector : MonoBehaviour
// {
//     [Tooltip("Drag a YOLO model .onnx file here")]
//     public ModelAsset modelAsset;

//     [Tooltip("Create a Raw Image in the scene and link it here")]
//     public RawImage displayImage;

//     private Worker worker;
//     private Tensor<float> inputTensor;
//     private RenderTexture targetRT;

//     private const int imageWidth = 640;
//     private const int imageHeight = 640;

//     public ARCameraManager cameraManager;  // AR Camera Manager

//     void Start()
//     {
//         // Load YOLO model
//         var model = ModelLoader.Load(modelAsset);
//         worker = new Worker(model, BackendType.GPUCompute);

//         // Setup render texture and input tensor for YOLO
//         targetRT = new RenderTexture(imageWidth, imageHeight, 0);
//         inputTensor = new Tensor<float>(new TensorShape(1, 3, imageHeight, imageWidth));
//     }

//     void OnDisable()
//     {
//         worker?.Dispose();
//         inputTensor?.Dispose();
//     }

//     void Update()
//     {
//         // Capture AR camera frame and perform inference
//         if (cameraManager.TryAcquireLatestCpuImage(out var cpuImage))
//         {
//             // Convert AR camera image to a texture for YOLO
//             var conversionParams = new XRCpuImage.ConversionParams
//             {
//                 inputRect = new RectInt(0, 0, cpuImage.width, cpuImage.height),
//                 outputDimensions = new Vector2Int(imageWidth, imageHeight),
//                 outputFormat = TextureFormat.RGBA32,
//                 transformation = XRCpuImage.Transformation.MirrorX // Flip horizontally (optional)
//             };

//             var rawTexture = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false);
//             var rawData = new NativeArray<byte>(conversionParams.outputDimensions.x * conversionParams.outputDimensions.y * 4, Allocator.Temp);
//             cpuImage.Convert(conversionParams, rawData);
//             rawTexture.LoadRawTextureData(rawData);
//             rawTexture.Apply();
//             rawData.Dispose();
//             cpuImage.Dispose();

//             // Display on the UI for debugging purposes
//             displayImage.texture = rawTexture;

//             if (rawTexture == null)
//             {
//                 Debug.LogError("rawTexture is null. Check the camera image conversion process.");
//                 return;
//             }
//             Debug.LogError("Success Photo?");

//             // Preprocess the image to a tensor format for YOLO input
//             TextureConverter.ToTensor(rawTexture, inputTensor, new TextureTransform());

//             // Run YOLO inference
//             worker.Schedule(inputTensor);

//             // Get the output (bounding box predictions)
//             using var output = (worker.PeekOutput() as Tensor<float>).ReadbackAndClone();

//             // Process output and draw bounding boxes
//             ProcessOutput(output);

//             // Dispose of the output tensor after usage
//             output.Dispose();
//         }
//     }

//     private void ProcessOutput(Tensor<float> output)
//     {
//         // Process the output to get bounding boxes and labels
//         // Example: Assuming output contains bounding box coordinates and confidence scores
//         // You may need to adjust this based on your model's output format

//         int boxesFound = output.shape[0]; // Number of boxes found
//         for (int i = 0; i < Mathf.Min(boxesFound, 10); i++) // Limit to 10 boxes for example
//         {
//             float centerX = output[i, 0]; // Extract center X
//             float centerY = output[i, 1]; // Extract center Y
//             float width = output[i, 2];   // Extract width
//             float height = output[i, 3];  // Extract height

//             // Use these values to draw bounding boxes or labels
//             // For simplicity, you can display a text label for each detected object
//             DrawBoundingBox(centerX, centerY, width, height);
//         }
//     }

//     private void DrawBoundingBox(float centerX, float centerY, float width, float height)
//     {
//         // Create a bounding box on the screen (you can use UI elements or 3D objects for this)
//         GameObject box = new GameObject("BoundingBox");
//         box.transform.SetParent(displayImage.transform);

//         RectTransform rt = box.AddComponent<RectTransform>();
//         rt.sizeDelta = new Vector2(width, height);
//         rt.localPosition = new Vector3(centerX, centerY, 0);

//         Image img = box.AddComponent<Image>();
//         img.color = Color.yellow;
//     }
// }


// using UnityEngine;
// using UnityEngine.XR.ARFoundation;
// using Unity.Sentis;
// using UnityEngine.UI;
// using Unity.Collections;

// public class ARYOLODetector : MonoBehaviour
// {
//     [Tooltip("Drag a YOLO model .onnx file here")]
//     public ModelAsset modelAsset;

//     [Tooltip("Create a Raw Image in the scene and link it here")]
//     public RawImage displayImage;

//     private Worker worker;
//     private Tensor<float> inputTensor;
//     private RenderTexture targetRT;

//     private const int imageWidth = 640;
//     private const int imageHeight = 640;

//     public ARCameraManager cameraManager;  // AR Camera Manager

//     void Start()
//     {
//         // Load YOLO model
//         var model = ModelLoader.Load(modelAsset);
//         worker = new Worker(model, BackendType.GPUCompute);
//         // worker = WorkerFactory.CreateWorker(BackendType.GPUCompute, model);

//         // Setup render texture and input tensor for YOLO
//         targetRT = new RenderTexture(imageWidth, imageHeight, 0);
//         using Tensor<float> inputTensor = new Tensor<float>(new TensorShape(1, 3, imageHeight, imageWidth));
//         // inputTensor = new Tensor<float>(1, 3, imageHeight, imageWidth);
//     }

//     void OnDisable()
//     {
//         worker?.Dispose();
//         inputTensor?.Dispose();
//     }

//     void Update()
//     {
//         // Capture AR camera frame and perform inference
//         if (cameraManager.TryAcquireLatestCpuImage(out var cpuImage))
//         {
//             // Convert AR camera image to a texture for YOLO
//             var conversionParams = new XRCpuImage.ConversionParams
//             {
//                 inputRect = new RectInt(0, 0, cpuImage.width, cpuImage.height),
//                 outputDimensions = new Vector2Int(imageWidth, imageHeight),
//                 outputFormat = TextureFormat.RGBA32,
//                 transformation = XRCpuImage.Transformation.MirrorX
//             };

//             var rawTexture = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false);
//             var rawData = new NativeArray<byte>(conversionParams.outputDimensions.x * conversionParams.outputDimensions.y * 4, Allocator.Temp);
//             cpuImage.Convert(conversionParams, rawData);
//             rawTexture.LoadRawTextureData(rawData);
//             rawTexture.Apply();
//             rawData.Dispose();
//             cpuImage.Dispose();

//             // Display on the UI
//             displayImage.texture = rawTexture;

//             // Preprocess the image to a tensor format for YOLO input
//             TextureConverter.ToTensor(rawTexture, inputTensor);

//             // Run YOLO inference
//             worker.Execute(inputTensor);

//             // Get the output (bounding box predictions)
//             var output = worker.PeekOutput() as Tensor<float>;

//             // TODO: Add code to process output and draw bounding boxes on the UI

//             output.Dispose();
//         }
//     }
// }



// using UnityEngine;
// using UnityEngine.XR.ARFoundation;
// using Unity.Sentis;
// using Unity.Collections;
// using UnityEngine.UI;

// public class ARYOLODetector : MonoBehaviour
// {
//     [Tooltip("Drag a YOLO model .onnx file here")]
//     public ModelAsset modelAsset;


//     public ARCameraManager cameraManager;

//     [Tooltip("Drag the classes.txt here")]
//     public TextAsset classesAsset;

//     [Tooltip("Create a Raw Image in the scene and link it here")]
//     public RawImage displayImage;

//     [Tooltip("Drag a border box texture here")]
//     public Texture2D borderTexture;

//     [Tooltip("Select an appropriate font for the labels")]
//     public Font font;
//     const BackendType backend = BackendType.GPUCompute;

//     private Transform displayLocation;
//     private Worker worker;
//     private string[] labels;
//     private RenderTexture targetRT;
//     private Sprite borderSprite;



//     // public static Model Load(ModelAsset modelAsset)


//     // public ModelAsset yoloModelAsset;
//     // public RawImage debugDisplay;

//     // private Worker engine;
//     // private Tensor<float> inputTensor;
//     // private RenderTexture resizedTexture;

//     Tensor<float> centersToCorners;

//     private const int inputWidth = 640;
//     private const int inputHeight = 640;

//     void Start()
//     {
//         // Load YOLO model
//         var model = ModelLoader.Load(yoloModelAsset);
//         worker = WorkerFactory.CreateWorker(BackendType.GPUCompute, model);

//         // Setup input
//         inputTensor = new Tensor<float>(1, 3, inputHeight, inputWidth);
//         resizedTexture = new RenderTexture(inputWidth, inputHeight, 0);
//     }

//     void OnDisable()
//     {
//         worker?.Dispose();
//         inputTensor?.Dispose();
//     }

//     void Update()
//     {
//         if (cameraManager.TryAcquireLatestCpuImage(out var cpuImage))
//         {
//             // Convert camera image to texture
//             var conversionParams = new XRCpuImage.ConversionParams
//             {
//                 inputRect = new RectInt(0, 0, cpuImage.width, cpuImage.height),
//                 outputDimensions = new Vector2Int(inputWidth, inputHeight),
//                 outputFormat = TextureFormat.RGBA32,
//                 transformation = XRCpuImage.Transformation.MirrorX
//             };

//             var rawTexture = new Texture2D(inputWidth, inputHeight, TextureFormat.RGBA32, false);
//             var rawData = new NativeArray<byte>(conversionParams.outputDimensions.x * conversionParams.outputDimensions.y * 4, Allocator.Temp);
//             cpuImage.Convert(conversionParams, rawData);
//             rawTexture.LoadRawTextureData(rawData);
//             rawTexture.Apply();
//             rawData.Dispose();
//             cpuImage.Dispose();

//             // Display on UI for debug
//             debugDisplay.texture = rawTexture;

//             // Preprocess
//             TextureConverter.ToTensor(rawTexture, inputTensor);

//             // Run model
//             worker.Execute(inputTensor);

//             // Get output
//             var output = worker.PeekOutput() as Tensor<float>;

//             // TODO: Parse detections here (draw bounding boxes)
//             output.Dispose();
//         }
//     }
// }
