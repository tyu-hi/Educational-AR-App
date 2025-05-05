using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;

public class GoogleTTSManager : MonoBehaviour
{
    [Header("API Settings")]
    [SerializeField] private string apiKey = "your api key";
    [SerializeField] private string apiUrl = "https://texttospeech.googleapis.com/v1/text:synthesize";

    [Header("Voice Settings")]
    [SerializeField] private string languageCode = "en-US";
    [SerializeField] private string voiceName = "en-US-Wavenet-D"; // this is the Male voice
    [SerializeField] private bool useFemaleVoice = false; // set checkbox to true for female voice in Unity

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;

    private void Awake()
    {
        // Create audio source if one isn't assigned
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    // Converts text to speech and plays it through the audio source
    public void SpeakText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("No text to speak");
            return;
        }

        // If using female voice
        if (useFemaleVoice)
        {
            voiceName = "en-US-Wavenet-F"; // female voice
        }

        Debug.Log($"Converting to speech: {text}");
        StartCoroutine(SynthesizeSpeechCoroutine(text));
    }

    private IEnumerator SynthesizeSpeechCoroutine(string text)
    {
        // Prepare the request body
        TTSRequest requestBody = new TTSRequest
        {
            input = new TTSInput { text = text },
            voice = new TTSVoice
            {
                languageCode = languageCode,
                name = voiceName,
                ssmlGender = useFemaleVoice ? "FEMALE" : "MALE"
            },
            audioConfig = new TTSAudioConfig { audioEncoding = "MP3" }
        };

        // Convert to JSON
        string jsonBody = JsonConvert.SerializeObject(requestBody);

        // Create the web request with API key in URL
        string urlWithKey = $"{apiUrl}?key={apiKey}";
        using (UnityWebRequest request = new UnityWebRequest(urlWithKey, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // Send the request
            yield return request.SendWebRequest();

            // Check for errors
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"TTS API Error: {request.error}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
                yield break;
            }

            // Parse the response
            string response = request.downloadHandler.text;
            TTSResponse ttsResponse = JsonConvert.DeserializeObject<TTSResponse>(response);

            if (ttsResponse != null && !string.IsNullOrEmpty(ttsResponse.audioContent))
            {
                // Convert the base64 audio content to bytes
                byte[] audioBytes = Convert.FromBase64String(ttsResponse.audioContent);

                // Create an audio clip from the bytes
                yield return CreateAndPlayAudioClip(audioBytes);
            }
            else
            {
                Debug.LogError("Failed to get audio content from TTS API");
            }
        }
    }

    private IEnumerator CreateAndPlayAudioClip(byte[] audioBytes)
    {
        // Create a temporary WAV file path
        string tempFilePath = $"{Application.temporaryCachePath}/tts_audio.mp3";

        // Write the audio bytes to the file
        System.IO.File.WriteAllBytes(tempFilePath, audioBytes);

        // Use UnityWebRequest to load the audio clip
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempFilePath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to load audio clip: {www.error}");
                yield break;
            }

            // Get the audio clip
            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);

            // Play the audio
            if (clip != null)
            {
                audioSource.clip = clip;
                audioSource.Play();

                Debug.Log("Playing TTS audio");
            }
            else
            {
                Debug.LogError("Failed to create audio clip");
            }
        }

        // Clean up the temporary file
        yield return new WaitForSeconds(1.0f);
        try
        {
            System.IO.File.Delete(tempFilePath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to delete temporary audio file: {e.Message}");
        }
    }
}

// Classes for API requests and responses
[Serializable]
public class TTSRequest
{
    public TTSInput input;
    public TTSVoice voice;
    public TTSAudioConfig audioConfig;
}

[Serializable]
public class TTSInput
{
    public string text;
}

[Serializable]
public class TTSVoice
{
    public string languageCode;
    public string name;
    public string ssmlGender;
}

[Serializable]
public class TTSAudioConfig
{
    public string audioEncoding;
}

[Serializable]
public class TTSResponse
{
    public string audioContent;
}