using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;

public class OpenRouterManager : MonoBehaviour
{
    [Header("API Settings")]
    [SerializeField] private string apiKey = "your api key";
    [SerializeField] private string modelName = "deepseek/deepseek-chat-v3-0324:free"; // can choose different models from openrouter by copying its name
    [SerializeField] private string apiUrl = "https://openrouter.ai/api/v1/chat/completions";

    [Header("Request Settings")]
    [SerializeField] private float temperature = 0.7f;
    [SerializeField] private int maxTokens = 300;
    //[SerializeField] private string appName = "AR Object Detection App"; // my app name for open router, not really needed

    // Event for when API responds with information
    public delegate void InfoGeneratedHandler(string generatedInfo);
    public event InfoGeneratedHandler OnInfoGenerated;

    // Method to generate information about an object
    public void GenerateObjectInfo(string objectName)
    {
        Debug.Log($"Generating info for: {objectName} using OpenRouter");
        StartCoroutine(GenerateInfoCoroutine(objectName));
    }

    private IEnumerator GenerateInfoCoroutine(string objectName)
    {
        // My specific prompt that instructs the format I want, can change if needed.
        string prompt = $"Identify this object as '{objectName}' and provide exactly 2 fun and interesting facts about it. " +
                       $"Format your response like this: 'This is a {objectName}. [Fact 1] [Fact 2]' " +
                       $"Do not add any introductions, conclusions, or other commentary.";

        // Create the API request body
        OpenRouterRequest requestBody = new OpenRouterRequest
        {
            model = modelName,
            messages = new OpenRouterMessage[]
            {
                new OpenRouterMessage
                {
                    role = "system",
                    content = "You are a concise information provider for an AR application. Respond with exactly the format requested, " +
                              "without any additional text, introductions, or commentary. Be direct and informative."
                },
                new OpenRouterMessage
                {
                    role = "user",
                    content = prompt
                }
            },
            temperature = temperature,
            max_tokens = maxTokens
        };

        // Convert to JSON
        string jsonBody = JsonConvert.SerializeObject(requestBody);
        Debug.Log("Sending request to OpenRouter API...");

        // Create the web request
        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
;
            // Send the request
            yield return request.SendWebRequest();

            // Process the response
            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                Debug.Log("OpenRouter API Response: " + response);

                // Parse the response
                OpenRouterResponse openRouterResponse = JsonConvert.DeserializeObject<OpenRouterResponse>(response);

                if (openRouterResponse != null && openRouterResponse.choices != null && openRouterResponse.choices.Length > 0)
                {
                    string generatedText = openRouterResponse.choices[0].message.content;
                    Debug.Log("Generated info: " + generatedText);

                    // Notify listeners
                    OnInfoGenerated?.Invoke(generatedText);
                }
                else
                {
                    Debug.LogError("Failed to parse OpenRouter response");
                    OnInfoGenerated?.Invoke("Sorry, I couldn't generate information about this object.");
                }
            }
            else
            {
                Debug.LogError($"OpenRouter API Error: {request.error}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
                OnInfoGenerated?.Invoke("Sorry, there was an error communicating with the AI assistant.");
            }
        }
    }
}

// Classes for API requests and responses
[Serializable]
public class OpenRouterRequest
{
    public string model;
    public OpenRouterMessage[] messages;
    public float temperature;
    public int max_tokens;
}

[Serializable]
public class OpenRouterMessage
{
    public string role;
    public string content;
}

[Serializable]
public class OpenRouterResponse
{
    public string id;
    public string @object;
    public long created;
    public string model;
    public OpenRouterChoice[] choices;
    public OpenRouterUsage usage;
}

[Serializable]
public class OpenRouterChoice
{
    public OpenRouterMessage message;
    public string finish_reason;
    public int index;
}

[Serializable]
public class OpenRouterUsage
{
    public int prompt_tokens;
    public int completion_tokens;
    public int total_tokens;
}