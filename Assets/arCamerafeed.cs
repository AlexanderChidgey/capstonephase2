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
    private bool objectDetected = false;

    private Worker worker;
    private Tensor<float> inputTensor;
    private Texture2D inputTexture;
    private IEnumerator executionSchedule;
    private bool executionStarted = false;

    private const int imageWidth = 640;
    private const int imageHeight = 640;
    private const float confThreshold = 0.70f;
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
        if (executionStarted || objectDetected)
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
                if (objectDetected) break;


                float latitude = Input.location.lastData.latitude;
                float longitude = Input.location.lastData.longitude;
                float heading = Input.compass.trueHeading;

                detectionHandler?.HandleDetection(det.classId, latitude, longitude, heading);

                Debug.Log($"Detected: {det.classId} | Lat: {latitude}, Lon: {longitude} | Heading: {heading}° | Score: {det.score} | Box: [{det.x}, {det.y}, {det.w}, {det.h}]");

                objectDetected = true;

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
