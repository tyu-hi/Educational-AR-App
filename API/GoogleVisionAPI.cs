using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text;
using TMPro;

public class GoogleVisionAPI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RawImage previewImage; // used to preview screenshot
    [SerializeField] private GameObject loadingIndicator;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private GameObject screenshotConfirmPanel;
    [SerializeField] private RawImage screenshotPreview;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private GameObject instructionText; 
    [SerializeField] private Button scanButton; 

    [Header("API Settings")]
    [SerializeField] private string apiKey = "your api key";

    // Event for when object is detected
    public delegate void ObjectDetectedHandler(string objectName);
    public event ObjectDetectedHandler OnObjectDetected;

    private Texture2D capturedScreenshot;

    private void Start()
    {
        // Set up UI listeners
        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmScreenshot);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelScreenshot);

        // Make sure the confirmation panel is hidden
        if (screenshotConfirmPanel != null)
            screenshotConfirmPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        // Clean up listeners
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(ConfirmScreenshot);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(CancelScreenshot);
    }

    // Call this from the ScanButton
    public void CaptureScreenshot()
    {
        // Hide instruction text and scan button before taking screenshot
        if (instructionText != null)
            instructionText.SetActive(false);

        if (scanButton != null)
            scanButton.gameObject.SetActive(false);

        StartCoroutine(CaptureScreenshotCoroutine());
    }

    private IEnumerator CaptureScreenshotCoroutine()
    {
        // Hide UI elements that shouldn't be in the screenshot
        bool wasLoadingActive = false;
        if (loadingIndicator != null)
        {
            wasLoadingActive = loadingIndicator.activeSelf;
            loadingIndicator.SetActive(false);
        }
        // Hide instruction text
        bool wasInstructionTextActive = false;
        if (instructionText != null)
        {
            wasInstructionTextActive = instructionText.activeSelf;
            instructionText.SetActive(false);
        }

        // Wait until the end of the frame
        yield return new WaitForEndOfFrame();

        // Create a texture the size of the screen
        capturedScreenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);

        // Read screen pixels into the texture
        capturedScreenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        capturedScreenshot.Apply();

        // Restore UI elements
        if (loadingIndicator != null && wasLoadingActive)
            loadingIndicator.SetActive(true);

        // Show the preview
        if (screenshotPreview != null)
            screenshotPreview.texture = capturedScreenshot;

        if (screenshotConfirmPanel != null)
            screenshotConfirmPanel.SetActive(true);
    }

    // Called when the user confirms the screenshot
    public void ConfirmScreenshot()
    {
        if (screenshotConfirmPanel != null)
            screenshotConfirmPanel.SetActive(false);

        // Show loading
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);

        if (resultText != null)
            resultText.text = "Processing image...";

        // Process the captured screenshot
        ProcessScreenshot(capturedScreenshot);
    }

    // Called when the user cancels the screenshot
    public void CancelScreenshot()
    {
        // Hide the confirmation panel
        if (screenshotConfirmPanel != null)
            screenshotConfirmPanel.SetActive(false);

        // Show instruction text and scan button again
        if (instructionText != null)
            instructionText.SetActive(true);

        if (scanButton != null)
            scanButton.gameObject.SetActive(true);

        // Clean up the texture
        if (capturedScreenshot != null)
            Destroy(capturedScreenshot);
    }

    private void ProcessScreenshot(Texture2D screenshot)
    {
        if (screenshot == null)
        {
            Debug.LogError("No screenshot to process");
            return;
        }

        // Show preview of the screenshot we're analyzing
        if (previewImage != null)
            previewImage.texture = screenshot;

        // Convert to JPG and base64 encode
        byte[] jpgData = screenshot.EncodeToJPG(75); // 75% quality
        string base64Image = Convert.ToBase64String(jpgData);

        // Send to Google Vision API
        StartCoroutine(DetectObjectWithVisionAPI(base64Image));
    }

    // Vision API code, updated to use screenshot
    private IEnumerator DetectObjectWithVisionAPI(string base64Image)
    {
        // Base URL for the Vision API
        string url = $"https://vision.googleapis.com/v1/images:annotate?key={apiKey}";

        // Create the request body
        VisionRequest requestBody = new VisionRequest
        {
            requests = new List<AnnotateImageRequest>
            {
                new AnnotateImageRequest
                {
                    image = new Image
                    {
                        content = base64Image
                    },
                    features = new List<Feature>
                    {
                        new Feature
                        {
                            type = "OBJECT_LOCALIZATION",
                            maxResults = 5
                        },
                        new Feature
                        {
                            type = "LABEL_DETECTION",
                            maxResults = 5
                        }
                    }
                }
            }
        };

        // Convert to JSON
        string jsonBody = JsonConvert.SerializeObject(requestBody);

        Debug.Log("Sending request to Vision API...");

        // Create a web request
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // Send the request
            yield return request.SendWebRequest();

            // Hide loading indicator
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);

            // After receiving the response:
            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;

                // Log the FULL response to console
                Debug.Log("Full Vision API Response: " + response);

                // Parse the response
                VisionResponse visionResponse = JsonConvert.DeserializeObject<VisionResponse>(response);

                // Get the detected object name (use your existing code for this)
                string detectedObject = ExtractObjectName(visionResponse);

                if (!string.IsNullOrEmpty(detectedObject))
                {
                    Debug.Log($"Object detected: {detectedObject}");

                    // Update UI
                    if (resultText != null)
                        resultText.text = $"Detected: {detectedObject}";

                    // Notify listeners
                    OnObjectDetected?.Invoke(detectedObject);
                }
                else
                {
                    Debug.Log("No objects detected");
                    if (resultText != null)
                        resultText.text = "No objects detected";
                }
            }
            else
            {
                Debug.LogError($"Vision API Error: {request.error}");
                Debug.LogError($"Response: {request.downloadHandler.text}");

                if (resultText != null)
                    resultText.text = "Error: " + request.error;
            }
        }

        // Clean up the texture
        Destroy(capturedScreenshot);
    }

    // Helper method to extract object name from response
    private string ExtractObjectName(VisionResponse visionResponse)
    {
        // parsing the response
        if (visionResponse?.responses != null && visionResponse.responses.Count > 0)
        {
            // Check for localized object annotations
            if (visionResponse.responses[0].localizedObjectAnnotations != null &&
                visionResponse.responses[0].localizedObjectAnnotations.Count > 0)
            {
                return visionResponse.responses[0].localizedObjectAnnotations[0].name.ToLower();
            }

            // Check for label annotations as fallback
            if (visionResponse.responses[0].labelAnnotations != null &&
                visionResponse.responses[0].labelAnnotations.Count > 0)
            {
                return visionResponse.responses[0].labelAnnotations[0].description.ToLower();
            }
        }

        return null;
    }
}

// Classes for API requests and responses:
[Serializable]
public class VisionRequest
{
    public List<AnnotateImageRequest> requests;
}

[Serializable]
public class AnnotateImageRequest
{
    public Image image;
    public List<Feature> features;
}

[Serializable]
public class Image
{
    public string content;
}

[Serializable]
public class Feature
{
    public string type;
    public int maxResults;
}

[Serializable]
public class VisionResponse
{
    public List<AnnotateImageResponse> responses;
}

[Serializable]
public class EntityAnnotation
{
    public string description;
    public float score;
}

[Serializable]
public class LocalizedObjectAnnotation
{
    public string name;
    public float score;
}


// more detailed
[Serializable]
public class AnnotateImageResponse
{
    public List<EntityAnnotation> labelAnnotations;
    public List<LocalizedObjectAnnotation> localizedObjectAnnotations;
    public List<EntityAnnotation> textAnnotations;
    public List<FaceAnnotation> faceAnnotations;
    public ImageProperties imagePropertiesAnnotation;
    // can add more if i want
}

[Serializable]
public class FaceAnnotation
{
    public BoundingPoly boundingPoly;
    public float detectionConfidence;
    public List<Landmark> landmarks;
    // can add more if i want
}

[Serializable]
public class Landmark
{
    public string type;
    public Position position;
}

[Serializable]
public class Position
{
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class BoundingPoly
{
    public List<Vertex> vertices;
}

[Serializable]
public class Vertex
{
    public float x;
    public float y;
}

[Serializable]
public class ImageProperties
{
    public DominantColors dominantColors;
}

[Serializable]
public class DominantColors
{
    public List<ColorInfo> colors;
}

[Serializable]
public class ColorInfo
{
    public Color color;
    public float score;
    public float pixelFraction;
}

[Serializable]
public class Color
{
    public float red;
    public float green;
    public float blue;
    public float alpha;
}