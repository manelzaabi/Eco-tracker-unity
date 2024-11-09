using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.Networking;
using System.Text;

[System.Serializable]
public class PlantIdentificationRequest
{
    public string[] images;
    public float latitude;
    public float longitude;
    public bool similar_images;
}

[System.Serializable]
public class PlantIdentificationResponse
{
    public string access_token;
    public Result result;
    public string status;
}

[System.Serializable]
public class Result
{
    public Classification classification;
}

[System.Serializable]
public class Classification
{
    public Suggestion[] suggestions;
}

[System.Serializable]
public class Suggestion
{
    public string name;
    public float probability;
}

public class CameraCapture : MonoBehaviour
{
    public RawImage cameraPreview;       // Display for camera feed
    public Button captureButton;         // Button to take the photo
    public Toggle plantDetectionToggle;  // Toggle for plant detection mode
    public Text resultText;              // Text to display plant detection results

    private WebCamTexture webCamTexture;
    private bool isPlantDetectionActive = false;
    private const string API_KEY = "1XE6wbqb4TWovk6KNLYd3dTAKzRin8KwDfuB7oinJNyroxCFpr"; // Replace with your actual API key
    private const string API_URL = "https://plant.id/api/v3/identification";

    void Start()
    {
        // Initialize the camera preview
        if (WebCamTexture.devices.Length > 0)
        {
            webCamTexture = new WebCamTexture();
            cameraPreview.texture = webCamTexture;
            webCamTexture.Play();
        }
        else
        {
            Debug.LogWarning("No camera detected");
        }

        // Link the capture function to the button
        captureButton.onClick.AddListener(CapturePhoto);

        // Link the toggle function
        plantDetectionToggle.onValueChanged.AddListener(OnPlantDetectionToggled);
    }

    private void OnPlantDetectionToggled(bool isOn)
    {
        isPlantDetectionActive = isOn;
        if (isOn)
        {
            resultText.text = "Plant Detection Mode: Active";
        }
        else
        {
            resultText.text = "";
        }
    }

    private void CapturePhoto()
    {
        // Create a Texture2D with the camera image
        Texture2D photo = new Texture2D(webCamTexture.width, webCamTexture.height);
        photo.SetPixels(webCamTexture.GetPixels());
        photo.Apply();

        // Convert to base64
        byte[] bytes = photo.EncodeToPNG();
        string base64Image = System.Convert.ToBase64String(bytes);

        // Save the image to a file (optional)
        string filePath = Path.Combine(Application.persistentDataPath, "photo.png");
        File.WriteAllBytes(filePath, bytes);
        Debug.Log("Photo saved at: " + filePath);

        // If plant detection is active, send to API
        if (isPlantDetectionActive)
        {
            StartCoroutine(IdentifyPlant(base64Image));
        }

        Destroy(photo);
    }

    private IEnumerator IdentifyPlant(string base64Image)
    {
        // Prepare the request data
        PlantIdentificationRequest requestData = new PlantIdentificationRequest
        {
            images = new string[] { "data:image/png;base64," + base64Image },
            latitude = 49.207f,  // Replace with actual GPS coordinates if needed
            longitude = 16.608f,
            similar_images = true
        };

        // Convert request data to JSON
        string jsonData = JsonUtility.ToJson(requestData);

        // Create the web request
        UnityWebRequest request = new UnityWebRequest(API_URL, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        // Set headers
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Api-Key", API_KEY);

        // Send the request
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Parse the response
            PlantIdentificationResponse response = JsonUtility.FromJson<PlantIdentificationResponse>(request.downloadHandler.text);

            if (response.result != null &&
                response.result.classification != null &&
                response.result.classification.suggestions != null &&
                response.result.classification.suggestions.Length > 0)
            {
                var topSuggestion = response.result.classification.suggestions[0];
                resultText.text = $"Detected Plant: {topSuggestion.name}\nConfidence: {(topSuggestion.probability * 100):F1}%";
            }
            else
            {
                resultText.text = "No plant detected";
            }
        }
        else
        {
            Debug.LogError($"Error: {request.error}");
            resultText.text = "Error identifying plant";
        }
    }

    private void OnDisable()
    {
        // Stop the camera when the object is disabled
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
        }
    }
}