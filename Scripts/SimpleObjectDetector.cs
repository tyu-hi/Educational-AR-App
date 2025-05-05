using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SimpleObjectDetector : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject infoCard;
    [SerializeField] private TextMeshProUGUI objectNameText;
    [SerializeField] private TextMeshProUGUI factText;
    [SerializeField] private Button scanButton;
    [SerializeField] private Button scanAgainButton;
    [SerializeField] private GameObject loadingIndicator;
    [SerializeField] private Text resultText;
    [SerializeField] private GameObject instructionText;

    [Header("API Components")]
    [SerializeField] private GoogleVisionAPI visionAPI; // object detector
    [SerializeField] private OpenRouterManager openRouterManager; // llm, using DeepSeek Free
    [SerializeField] private GoogleTTSManager ttsManager; // text-to-speech

    //private bool isGeneratingInfo = false;

    void Start()
    {
        // Hide info card and loading indicator initially
        if (infoCard != null)
            infoCard.SetActive(false);

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);

        // Add listeners to buttons
        if (scanButton != null)
            scanButton.onClick.AddListener(ScanObject);

        if (scanAgainButton != null)
            scanAgainButton.onClick.AddListener(ScanObject);

        // Find Vision API if not set
        if (visionAPI == null)
            visionAPI = FindObjectOfType<GoogleVisionAPI>();

        // Find OpenRouter Manager if not set
        if (openRouterManager == null)
            openRouterManager = FindObjectOfType<OpenRouterManager>();

        // Find TTS Manager if not set
        if (ttsManager == null)
            ttsManager = FindObjectOfType<GoogleTTSManager>();

        // Subscribe to object detection event
        if (visionAPI != null)
            visionAPI.OnObjectDetected += HandleObjectDetected;

        // Subscribe to OpenRouter info generation event
        if (openRouterManager != null)
            openRouterManager.OnInfoGenerated += HandleInfoGenerated;
    }

    void OnDestroy()
    {
        // Clean up listeners
        if (scanButton != null)
            scanButton.onClick.RemoveListener(ScanObject);

        if (scanAgainButton != null)
            scanAgainButton.onClick.RemoveListener(ScanObject);

        // Unsubscribe from events
        if (visionAPI != null)
            visionAPI.OnObjectDetected -= HandleObjectDetected;

        if (openRouterManager != null)
            openRouterManager.OnInfoGenerated -= HandleInfoGenerated;
    }

    public void ScanObject()
    {
        Debug.Log("Starting object detection...");

        // Hide the info card
        if (infoCard != null)
            infoCard.SetActive(false);

        // Hide instruction text when scanning
        if (instructionText != null)
            instructionText.SetActive(false);

        // Reset state
        //isGeneratingInfo = false;

        // Call the Vision API to capture screenshot
        if (visionAPI != null)
            visionAPI.CaptureScreenshot();
        else
            Debug.LogError("Vision API not found!");
    }

    private void HandleObjectDetected(string detectedObject)
    {
        Debug.Log($"Detected: {detectedObject}");

        // Keep the instruction text hidden when showing object info
        if (instructionText != null)
            instructionText.SetActive(false);

        // Update object name in UI
        if (objectNameText != null)
            objectNameText.text = CapitalizeFirstLetter(detectedObject);

        // Set temporary info text while we wait for OpenRouter
        if (factText != null)
            factText.text = "Generating interesting facts...";

        // Show loading indicator
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);

        // Show the info card
        if (infoCard != null)
            infoCard.SetActive(true);

        // Get info from OpenRouter
        // isGeneratingInfo = true;
        if (openRouterManager != null)
        {
            openRouterManager.GenerateObjectInfo(detectedObject);
        }
        else
        {
            Debug.LogError("OpenRouter Manager not found!");
            // Fallback to sample facts
            string facts = GetSampleFactsForObject(detectedObject);
            if (factText != null)
                factText.text = facts;

            // Speak the sample facts if TTS is available
            SpeakFactText(facts);
        }
    }

    private void HandleInfoGenerated(string generatedInfo)
    {
        // Update UI with the info from OpenRouter
        if (factText != null)
            factText.text = generatedInfo;

        // Hide loading indicator
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);

        //isGeneratingInfo = false;

        // Speak the generated info using TTS
        SpeakFactText(generatedInfo);
    }

    private void SpeakFactText(string textToSpeak)
    {
        // Use TTS Manager to convert the text to speech
        if (ttsManager != null)
        {
            ttsManager.SpeakText(textToSpeak);
        }
        else
        {
            Debug.LogWarning("TTS Manager not found - text will not be spoken");
        }
    }

    private string GetSampleFactsForObject(string objectName)
    {
        // Sample facts - this is a fallback if OpenRouter is not available
        string[] sampleObjects = { "tree", "book", "cat", "chair", "basketball" };
        string[] sampleFacts = {
            "Trees are incredible living organisms! Did you know they communicate underground through a network of fungi? They also clean our air by absorbing carbon dioxide.",
            "Books have been around for over 5,000 years! The first books were written on clay tablets in ancient Mesopotamia.",
            "Cats sleep for around 16 hours a day! Their whiskers help them determine if they can fit through tight spaces.",
            "Chairs have been used by humans for over 5,000 years. Ancient Egyptian chairs were often made from ebony and ivory.",
            "The first basketball was actually a soccer ball! The game was invented in 1891 by Dr. James Naismith."
        };

        // Match against our sample objects
        for (int i = 0; i < sampleObjects.Length; i++)
        {
            if (objectName.ToLower().Contains(sampleObjects[i]))
            {
                return sampleFacts[i];
            }
        }

        // Default response for unknown objects
        return $"This is a {objectName}! These are fascinating objects that have many interesting properties and uses.";
    }

    private string CapitalizeFirstLetter(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Split into words and capitalize each one
        string[] words = text.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (!string.IsNullOrEmpty(words[i]))
            {
                char[] letters = words[i].ToCharArray();
                letters[0] = char.ToUpper(letters[0]);
                words[i] = new string(letters);
            }
        }

        return string.Join(" ", words);
    }
}