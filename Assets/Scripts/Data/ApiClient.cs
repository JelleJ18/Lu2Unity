using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

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

    public GameObject worldItemPrefab;
    public Transform worldListContainer;

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
        Debug.Log("Register aangeroepen!");
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
        Debug.Log("Login aangeroepen!");
        var request = new PostLoginRequestDto()
        {
            email = emailInput.text,
            password = passwordInput.text
        };

        var jsondata = JsonUtility.ToJson(request);
        Debug.Log("Verzonden login JSON: " + jsondata);

        var response = await PerformApiCall("https://avansict2220486.azurewebsites.net/account/login", "POST", jsondata);
        Debug.Log("Ontvangen API response: " + response);

        if (!string.IsNullOrEmpty(response))
        {
            var responseDto = JsonUtility.FromJson<PostLoginResponseDto>(response);

            if (responseDto != null)
            {
                token = responseDto.accessToken;
                _username = emailInput.text;
                Debug.Log("Token ontvangen: " + token);
                Debug.Log("Username opgeslagen: " + userName);

                var worlds = await GetWorlds();
                if (worlds != null)
                {
                    DisplayWorlds(worlds);
                }
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
            UserName = _username
        };

        var jsonData = JsonUtility.ToJson(request);
        Debug.Log("Sending request to API: " + jsonData + " with token: " + token);

        string response = await PerformApiCall("https://avansict2220486.azurewebsites.net/api/environment2d", "POST", jsonData, token);


        Debug.Log("response = " + response);

        if (!string.IsNullOrEmpty(response))
        {
            WorldDTO worldData = JsonUtility.FromJson<WorldDTO>(jsonData);
            return worldData;
        }

        return null;

    }

    public async Task<List<WorldDTO>> GetWorlds()
    {
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("Token not available");
            return null;
        }

        string apiUrl = "https://avansict2220486.azurewebsites.net/api/environment2d/userworlds?UserName=" + _username; // Zorg ervoor dat je het juiste endpoint gebruikt

        string response = await PerformApiCall(apiUrl, "GET", token: token);

        if (!string.IsNullOrEmpty(response))
        {
            WorldDTOListWrapper worldWrapper = JsonUtility.FromJson<WorldDTOListWrapper>("{\"worlds\":" + response + "}");

            // Haal de werelden uit de wrapper
            List<WorldDTO> worlds = new List<WorldDTO>(worldWrapper.worlds);
            return worlds;
        }

        return null;
    }

    public void DisplayWorlds(List<WorldDTO> worlds)
    {
        // Eerst: opruimen
        foreach (Transform child in worldListContainer)
        {
            Destroy(child.gameObject);
        }

        // Dan: vullen
        foreach (var world in worlds)
        {
            GameObject worldItem = Instantiate(worldItemPrefab, worldListContainer);
            TMP_Text nameText = worldItem.GetComponentInChildren<TMP_Text>();
            nameText.text = world.Name;

            // Optional: als je er een button aan hebt hangen
            Button button = worldItem.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => {
                    Debug.Log("Klikte op wereld: " + world.Name);
                    // Hier kun je bijv. een functie aanroepen om de wereld te laden
                });
            }
        }
    }


    private async Task<string> PerformApiCall(string apiUrl, string httpMethod, string jsonData = null, string token = null)
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

            // Verwijder de Authorization header voor login-aanroepen
            if (!string.IsNullOrEmpty(token) && apiUrl.Contains("environment2d"))
            {
                request.SetRequestHeader("Authorization", "Bearer " + token);
            }

            errorText.color = Color.white;
            errorText.text = "Logging in!";

            await request.SendWebRequest();

            if (request.responseCode == 401)
            {
                Debug.LogError("Incorrect password or email!");
                errorText.color = Color.red;
                errorText.text = "Incorrect email or password!";
            }
            else if (request.responseCode == 400)
            {
                Debug.LogError("Bad Request: " + request.downloadHandler.text);
                errorText.color = Color.red;
                errorText.text = "Bad Request - Check your input fields!";
            }
            else
            {
                Debug.Log("Request sent successfully");
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                isLoggedIn = true;
                loginPanel.SetActive(false);
                buildManager.enabled = true;
                cameraMovement.enabled = true;
                buildPanel.SetActive(true);
                Debug.Log("API Success: " + request.downloadHandler.text);
                return request.downloadHandler.text;
            }
            else
            {
                isLoggedIn = false;
                Debug.LogError("API Error: " + request.error);
                return null;
            }
        }
    }


}
