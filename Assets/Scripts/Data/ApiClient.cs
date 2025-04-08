using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient : MonoBehaviour
{
    public static ApiClient Instance { get; private set; }
    private string token;
    private static string _username;

    private string userName
    {
        get { return _username; }
        set { _username = value; }
    }

    public string GetUserName()
    {
        return userName;
    }

    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_Text errorText;
    public GameObject loginPanel;
    public GameObject buildPanel;
    public BuildManager buildManager;
    public CameraMovement cameraMovement;

    bool isLoggedIn;

    private void Awake()
    {
        buildManager.enabled = false;
        cameraMovement.enabled = false;
        buildPanel.SetActive(false);
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        DontDestroyOnLoad(this);
    }


    public async void Register()
    {
        var request = new PostRegisterRequestDto()
        {
            email = emailInput.text,
            password = passwordInput.text
        };

        var jsondata = JsonUtility.ToJson(request);
        Debug.Log(jsondata);

        await PerformApiCall("https://avansict2220486.azurewebsites.net/account/register", "Post",jsondata);
    }

    public async void Login()
    {
        var request = new PostLoginRequestDto()
        {
            email = emailInput.text,
            password = passwordInput.text
        };

        var jsondata = JsonUtility.ToJson(request);
        Debug.Log("Verzonden login JSON: " + jsondata);

        var response = await PerformApiCall("https://avansict2220486.azurewebsites.net/account/login", "Post", jsondata);
        Debug.Log("Ontvangen API response: " + response);

        if (!string.IsNullOrEmpty(response))
        {
            var responseDto = JsonUtility.FromJson<PostLoginResponseDto>(response);

            if (responseDto != null)
            {
                token = responseDto.accessToken;
                Debug.Log("Token ontvangen: " + token);
            }
            else
            {
                Debug.LogError("Fout: Kon de response niet omzetten naar PostLoginResponseDto!");
            }
        }
        else
        {
            Debug.LogError("Fout: Geen response ontvangen van API!");
        }
    }

    public void OnCreateWorldButtonClicked()
    {
        string naam = "Mijn Wereld"; // Dit kun je aanpassen naar invoervelden in de UI
        int maxHeight = 100; // Dit kun je aanpassen naar invoervelden in de UI
        int maxWidth = 100;

        CreateWorld(naam, maxHeight, maxWidth);
    }

    public async Task<WorldDTO> CreateWorld(string name, int height, int length)
    {

        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("Token not availible");
            return null;
        }

        var request = new CreateWorldRequestDTO()
        {
            Name = name,
            MaxHeight = height,
            MaxLength = length,
            UserName = "TestUser"
        };

        var jsonData = JsonUtility.ToJson(request);
        Debug.Log("Sending request to API: " + jsonData + " with token: " + token);

        string response = await PerformApiCall("https://avansict2220486.azurewebsites.net/api/environment2d", "POST", jsonData, token);


        Debug.Log("De response = " + jsonData);

        if (!string.IsNullOrEmpty(jsonData))
        {
            WorldDTO worldData = JsonUtility.FromJson<WorldDTO>(jsonData);
            return worldData;
        }

        return null;

    }

    private async Task<string> PerformApiCall(string apiUrl, string httpMethod, string jsonData = null, string token = null)     //jsonData is niet nodig bij het ophalen van data, vandaar = null (optioneel)
    {
        using (UnityWebRequest request = new UnityWebRequest(apiUrl, httpMethod))
        {
            if (!string.IsNullOrEmpty(jsonData))
            {
                byte[] jsonToSend = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(token) && apiUrl.Contains("environment2d"))
            {
                request.SetRequestHeader("Authorization", "Bearer " + token);
            }
            errorText.color = Color.white;
            errorText.text = "Logging in!";

            await request.SendWebRequest();

            if (request.responseCode == 401)
            {
                Debug.Log("Incorrect password or email!");
                errorText.color = Color.red;
                errorText.text = "Incorrect email or password!";
            }
            else if(request.responseCode == 400)
            {
                Debug.Log("Account already exists!");
                errorText.color = Color.red;
                errorText.text = "Account already exists!";
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                isLoggedIn = true;
                loginPanel.SetActive(false);
                buildManager.enabled = true;
                cameraMovement.enabled = true;
                buildPanel.SetActive(true);
                Debug.Log("API succes: " + request.downloadHandler.text);
                return request.downloadHandler.text;
            }
            else
            {
                isLoggedIn = false;
                Debug.LogError("API error: " + request.error);
                return null;
            }
        }

    }

}
