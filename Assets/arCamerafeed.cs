using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using Unity.Sentis;
using Unity.Collections.LowLevel.Unsafe;

public class CaptureAndRunYOLO : MonoBehaviour
{
    public ARCameraManager cameraManager;
    public ObjectDetectionHandler detectionHandler; // Reference to the ObjectDetectionHandler script
    public ModelAsset modelAsset;
    public RawImage displayImage;
    public int framesToExecute = 30;

    private Worker worker;
    private Tensor<float> inputTensor;
    private Texture2D inputTexture;
    private IEnumerator executionSchedule;
    private bool executionStarted = false;

    private const int imageWidth = 640;
    private const int imageHeight = 640;
    private const float confThreshold = 0.75f;
    private const float iouThreshold = 0.45f;

    IEnumerator Start()
    {
        Application.targetFrameRate = 60;


        Input.location.Start();
        Input.compass.enabled = true;

        // Wait until service initializes
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (Input.location.status != LocationServiceStatus.Running)
        {
            Debug.LogError("Unable to start location services.");
            yield break;
        }

        var model = ModelLoader.Load(modelAsset);
        worker = new Worker(model, BackendType.GPUCompute);
        inputTensor = new Tensor<float>(new TensorShape(1, 3, imageHeight, imageWidth));
        inputTexture = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false);

        cameraManager.frameReceived += OnCameraFrameReceived;

        Debug.Log("YOLO model and camera ready.");
    }

    unsafe void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        if (executionStarted)
            return;

        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            return;

        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(imageWidth, imageHeight),
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.MirrorY
        };

        var rawTextureData = inputTexture.GetRawTextureData<byte>();
        try
        {
            image.Convert(conversionParams, new IntPtr(rawTextureData.GetUnsafePtr()), rawTextureData.Length);
        }
        finally
        {
            image.Dispose();
        }

        inputTexture.Apply();

        TextureConverter.ToTensor(inputTexture, inputTensor, new TextureTransform());
        executionSchedule = worker.ScheduleIterable(inputTensor);
        executionStarted = true;

        if (displayImage != null)
            displayImage.texture = inputTexture;
    }

    void Update()
    {
        if (!executionStarted) return;

        int steps = framesToExecute;
        while (steps-- > 0 && executionSchedule.MoveNext()) { }

        if (executionSchedule.Current == null)
        {
            using var output = (worker.PeekOutput() as Tensor<float>).ReadbackAndClone();
            if (output == null)
            {
                Debug.LogWarning("Output tensor is null.");
                executionStarted = false;
                return;
            }

            List<Detection> detections = new();
            Console.WriteLine(output.shape);

            for (int i = 0; i < 8400; i++)
            {

                float cx = output[0, 0, i];
                float cy = output[0, 1, i];
                float w = output[0, 2, i];
                float h = output[0, 3, i];
                float x1 = cx - w / 2f;
                float y1 = cy - h / 2f;

                int maxClass = -1;
                float maxScore = 0f;
                for (int j = 4; j < 6; j++)
                {
                    float score = output[0, j, i];
                    if (score > maxScore)
                    {
                        maxScore = score;
                        maxClass = j - 4;
                    }
                }

                float finalScore = maxScore;
                if (finalScore >= confThreshold)
                {
                    detections.Add(new Detection
                    {
                        x = x1,
                        y = y1,
                        w = w,
                        h = h,
                        score = finalScore,
                        classId = maxClass
                    });
                }
            }

            var finalDetections = ApplyNMS(detections, iouThreshold);

            foreach (var det in finalDetections)
            {
                detectionHandler?.HandleDetection(det.classId);


                float latitude = Input.location.lastData.latitude;
                float longitude = Input.location.lastData.longitude;
                float heading = Input.compass.trueHeading;


                Debug.Log($"Detected: {det.classId} | Lat: {latitude}, Lon: {longitude} | Heading: {heading}° | Score: {det.score} | Box: [{det.x}, {det.y}, {det.w}, {det.h}]");

                // Debug.Log($"Detection: Class {det.classId}, Score {det.score}, Box: [{det.x}, {det.y}, {det.w}, {det.h}]");
            }

            output.Dispose();
            executionStarted = false;
        }
    }

    List<Detection> ApplyNMS(List<Detection> detections, float iouThreshold)
    {
        List<Detection> result = new();
        detections.Sort((a, b) => b.score.CompareTo(a.score));

        while (detections.Count > 0)
        {
            var best = detections[0];
            result.Add(best);
            detections.RemoveAt(0);

            detections.RemoveAll(det => IoU(best, det) > iouThreshold);
        }

        return result;
    }

    float IoU(Detection a, Detection b)
    {
        float x1 = Mathf.Max(a.x, b.x);
        float y1 = Mathf.Max(a.y, b.y);
        float x2 = Mathf.Min(a.x + a.w, b.x + b.w);
        float y2 = Mathf.Min(a.y + a.h, b.y + b.h);

        float interArea = Mathf.Max(0, x2 - x1) * Mathf.Max(0, y2 - y1);
        float unionArea = a.w * a.h + b.w * b.h - interArea;
        return interArea / unionArea;
    }

    void OnDisable()
    {
        worker?.Dispose();
        inputTensor?.Dispose();
        cameraManager.frameReceived -= OnCameraFrameReceived;
    }

    class Detection
    {
        public float x, y, w, h, score;
        public int classId;
    }
}

// using System.Collections;
// using UnityEngine;
// using UnityEngine.UI;
// using UnityEngine.XR.ARFoundation;
// using UnityEngine.XR.ARSubsystems;
// using Unity.Collections;
// using Unity.Sentis;
// using FF = Unity.Sentis.Functional;

// public class CaptureAndRunYOLO : MonoBehaviour
// {
//     public ARCameraManager cameraManager;
//     public ModelAsset modelAsset;
//     public RawImage displayImage;
//     public int framesToExecute = 2;

//     private Worker worker;
//     private Tensor<float> inputTensor;
//     private Texture2D capturedTexture;
//     private IEnumerator executionSchedule;
//     private bool executionStarted = false;

//     private const int imageWidth = 640;
//     private const int imageHeight = 640;
//     Tensor<float> centersToCorners;

//     void Start()
//     {
//         Application.targetFrameRate = 60;
//         var model = ModelLoader.Load(modelAsset);
//         worker = new Worker(model, BackendType.CPU); // or CPU if needed
//         inputTensor = new Tensor<float>(new TensorShape(1, 3, imageHeight, imageWidth));
//         Debug.Log("YOLO model loaded.");
//     }

//     void Update()
//     {
//         if (!executionStarted)
//         {
//             if (TryCaptureFrame(out capturedTexture))
//             {
//                 TextureConverter.ToTensor(capturedTexture, inputTensor, new TextureTransform());
//                 executionSchedule = worker.ScheduleIterable(inputTensor);
//                 executionStarted = true;

//                 if (displayImage != null)
//                     displayImage.texture = capturedTexture;
//             }
//         }
//         else
//         {
//             int steps = framesToExecute;
//             while (steps-- > 0 && executionSchedule.MoveNext()) { }

//             if (executionSchedule.Current == null) // Inference done
//             {
//                 using var output = (worker.PeekOutput() as Tensor<float>).ReadbackAndClone();
//                 if (output == null)
//                 {
//                     Debug.LogWarning("Output tensor is null.");
//                     executionStarted = false;
//                     return;
//                 }
//                 float sigmoid(float x) => 1.0f / (1.0f + Mathf.Exp(-x));

//                 Debug.Log($"YOLO inference complete. Output shape: {output.shape}");

//                 for (int i = 0; i < 8400; i++)
//                 {

//                     float cx = output[0, 0, i];
//                     float cy = output[0, 1, i];
//                     float w = output[0, 2, i];
//                     float h = output[0, 3, i];
//                     float x1 = cx - w / 2f;
//                     float y1 = cy - h / 2f;
//                     float x2 = cx + w / 2f;
//                     float y2 = cy + h / 2f;


//                     float maxScore = 0f;
//                     int maxClass = -1;
//                     for (int j = 5; j < 7; j++) {
//                         float score = (output[0, j, i]);
//                         if (score > maxScore) {
//                             maxScore = score;
//                             maxClass = j - 5;
//                         }
//                     }

//                     if ((maxScore > 0) && (maxScore < 1)){
//                         if (maxClass == 0){
//                             Debug.Log($"Max score: {maxScore}, Max Class: Pillar Box");
//                         }else{
//                             Debug.Log($"Max score: {maxScore}, Max Class: Power Pole");
//                         }
//                     }


                    




//                     // float class0 = (output[0, 5, i]);
//                     // float class1 = (output[0, 6, i]);
//                     // float conf = (output[0,4,i]);

//                     // if (class0 > 0.3f || class1 > 0.3f)
//                     // {
//                     //     Debug.Log($"Box {i}:");
//                     //     Debug.Log($"  Bounding Box {i}: {output[0, 0, i]}");
//                     //     Debug.Log($"  Bounding Box {i}: {output[0, 1, i]}");
//                     //     Debug.Log($"  Bounding Box {i}: {output[0, 2, i]}");
//                     //     Debug.Log($"  Bounding Box {i}: {output[0, 3, i]}");


//                     //     Debug.Log($"  Confidence of box {i}: {output[0, 4, i]} : {conf}");

//                     //     Debug.Log($"  Class 1: {output[0, 5, i]} : {class0}");
//                     //     Debug.Log($"  : {output[0, 6, i]} : {class1}");

//                     //     // for (int j = 0; j < 8; j++)
//                     //     // {
//                     //     //     if (j < 4)
//                     //     //         Debug.Log($"  Bounding Box {j}: {output[0, j, i]}");
//                     //     //     else if (j == 4)
//                     //     //         Debug.Log($"  Bounding Box {j}: {output[0, j, i]}");
//                     //     //     else
//                     //     //         Debug.Log($"  Bounding Box {j}: {output[0, j, i]}");
//                     //     // }
//                     // }
//                 }

//                 output.Dispose();

//                 executionStarted = false;  // Immediately allow the next frame to be processed
//                 capturedTexture = null;
//             }
//         }
//     }


//     bool TryCaptureFrame(out Texture2D texture)
//     {
//         texture = null;
//         if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
//             return false;

//         var conversionParams = new XRCpuImage.ConversionParams
//         {
//             inputRect = new RectInt(0, 0, image.width, image.height),
//             outputDimensions = new Vector2Int(imageWidth, imageHeight),
//             outputFormat = TextureFormat.RGBA32,
//             transformation = XRCpuImage.Transformation.MirrorY
//         };

//         var rawData = new NativeArray<byte>(imageWidth * imageHeight * 4, Allocator.Temp);
//         image.Convert(conversionParams, rawData);

//         texture = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false);
//         texture.LoadRawTextureData(rawData);
//         texture.Apply();

//         rawData.Dispose();
//         image.Dispose();

//         return true;
//     }

//     void OnDisable()
//     {
//         worker?.Dispose();
//         inputTensor?.Dispose();
//     }
// }

// using UnityEngine;
// using System.Collections;
// using UnityEngine.UI;
// using UnityEngine.XR.ARFoundation;
// using UnityEngine.XR.ARSubsystems;
// using Unity.Collections;
// using Unity.Sentis;
// using FF = Unity.Sentis.Functional;

// public class CaptureAndRunYOLO : MonoBehaviour
// {
//     public ARCameraManager cameraManager;
//     public ModelAsset modelAsset;
//     public RawImage displayImage;

//     private Worker worker;
//     private Tensor<float> inputTensor;
//     private bool hasCaptured = false;

//     private const int imageWidth = 640;
//     private const int imageHeight = 640;
//     public int framesToExectute = 2;


//     // COCO labels
//     string[] cocoLabels = new string[] {
//         "person", "bicycle", "car", "motorcycle", "airplane", "bus", "train", "truck", "boat", "traffic light",
//         "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat", "dog", "horse", "sheep", "cow",
//         "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee",
//         "skis", "snowboard", "sports ball", "kite", "baseball bat", "baseball glove", "skateboard", "surfboard",
//         "tennis racket", "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple",
//         "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "couch",
//         "potted plant", "bed", "dining table", "toilet", "tv", "laptop", "mouse", "remote", "keyboard", "cell phone",
//         "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase", "scissors", "teddy bear",
//         "hair drier", "toothbrush"
//     };

//     void Start()
//     {
//         Application.targetFrameRate = 30;

//         var model = ModelLoader.Load(modelAsset);
//         worker = new Worker(model, BackendType.CPU);
//         inputTensor = new Tensor<float>(new TensorShape(1, 3, imageHeight, imageWidth));

//         Debug.Log("YOLO model loaded.");
//     }

//     bool executingStarted = false;
//     IEnumerator executionSchedule;

//     void Update()
//     {


//         if (!executingStarted){

//         }
//         if (hasCaptured) return;

//         if (cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
//         {
//             var conversionParams = new XRCpuImage.ConversionParams
//             {
//                 inputRect = new RectInt(0, 0, image.width, image.height),
//                 outputDimensions = new Vector2Int(imageWidth, imageHeight),
//                 outputFormat = TextureFormat.RGBA32,
//                 transformation = XRCpuImage.Transformation.MirrorY
//             };

//             var texture = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false);
//             var rawData = new NativeArray<byte>(imageWidth * imageHeight * 4, Allocator.Temp);

//             image.Convert(conversionParams, rawData);
//             texture.LoadRawTextureData(rawData);
//             texture.Apply();

//             rawData.Dispose();
//             image.Dispose();

//             if (displayImage != null)
//                 displayImage.texture = texture;

//             Debug.Log("Converted image to Texture2D.");

//             // --- YOLO PIPELINE START ---
//             TextureConverter.ToTensor(texture, inputTensor, new TextureTransform());
//             worker.Schedule(inputTensor);

//             using var output = (worker.PeekOutput() as Tensor<float>).ReadbackAndClone();

//             Debug.Log($"YOLO inference complete. Output shape: {output.shape}");

//             // Example: Print first 10 values from output for debugging
//             for (int i = 0; i < 84; i++) 
//             {
//                 float value = output[0, i, i];  // Example of extracting value from output tensor
//                 Debug.Log($"Feature[{i}] = {value}");
//             }

//             output.Dispose();
//             // --- YOLO PIPELINE END ---

//             hasCaptured = true;
//         }
//     }

//     void OnDisable()
//     {
//         worker?.Dispose();
//         inputTensor?.Dispose();
//     }
// }


// using UnityEngine;
// using UnityEngine.UI;
// using UnityEngine.XR.ARFoundation;
// using UnityEngine.XR.ARSubsystems;
// using Unity.Collections;

// public class ARCameraFeedToRawImage : MonoBehaviour
// {
//     public ARCameraManager cameraManager;
//     public RawImage rawImage;

//     private Texture2D cameraTexture;

//     void Update()
//     {
//         if (cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
//         {
//             var conversionParams = new XRCpuImage.ConversionParams
//             {
//                 inputRect = new RectInt(0, 0, image.width, image.height),
//                 outputDimensions = new Vector2Int(image.width, image.height),
//                 outputFormat = TextureFormat.RGBA32,
//                 transformation = XRCpuImage.Transformation.MirrorX
//             };

//             int dataSize = image.GetConvertedDataSize(conversionParams);
//             var buffer = new NativeArray<byte>(dataSize, Allocator.Temp);
//             image.Convert(conversionParams, buffer);

//             if (cameraTexture == null || cameraTexture.width != image.width || cameraTexture.height != image.height)
//             {
//                 cameraTexture = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
//             }

//             cameraTexture.LoadRawTextureData(buffer);
//             cameraTexture.Apply();

//             rawImage.texture = cameraTexture;

//             buffer.Dispose();
//             image.Dispose();
//         }
//     }
// }
