using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Newtonsoft.Json;

public class ApiClient : MonoBehaviour
{
    public static ApiClient Instance { get; private set; }
    private static string token;
    private static string _username;

    public string GetToken()
    {
        return token;
    }

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
    public GameObject worldMenu;
    public TMP_InputField worldNameInput;
    public TMP_InputField worldHeightInput;
    public TMP_InputField worldWidthInput;

    public WorldBuilder worldBuilder;

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


    public async void OnCreateWorldButtonClicked()
    {
        string naam = worldNameInput.text; 
        int maxHeight = int.Parse(worldHeightInput.text);
        int maxWidth = int.Parse(worldWidthInput.text);

        WorldDTO worldCreated = await CreateWorld(naam, maxHeight, maxWidth);
        
        if(worldCreated != null)
        {
            List<WorldDTO> worlds = await GetWorlds();
            if(worlds != null)
            {
                DisplayWorlds(worlds);
            }
        }
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
            WorldDTO worldData = JsonUtility.FromJson<WorldDTO>(response);
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

        // Log de response om te controleren wat je van de API ontvangt
        Debug.Log("API Response: " + response);

        if (!string.IsNullOrEmpty(response))
        {
            try
            {
                List<WorldDTO> worlds = JsonConvert.DeserializeObject<List<WorldDTO>>(response);
                Debug.Log("Werelden geladen: " + worlds.Count);
                return worlds;
            }
            catch (Exception e)
            {
                Debug.LogError("Error deserializing response: " + e.Message);
            }
        }

        return null;
    }


    public void DisplayWorlds(List<WorldDTO> worlds)
    {
        foreach (Transform child in worldListContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var world in worlds)
        {
            GameObject worldItem = Instantiate(worldItemPrefab, worldListContainer);
            TMP_Text nameText = worldItem.GetComponentInChildren<TMP_Text>();
            nameText.text = world.name;
            Debug.Log("World naam: " + world.name + " | ID: " + world.id);


            Button button = worldItem.GetComponentInChildren<Button>();
            if (button != null)
            {
                var capturedWorld = world; 
                button.onClick.AddListener(() => {
                    LoadWorld(capturedWorld);
                });
            }
        }
    }

    public void LoadWorld(WorldDTO world)
    {
        Debug.Log("WorldBuilder reference: " + worldBuilder);
        Debug.Log("Wereld aan het laden: " + world.name);
        Debug.Log("World ID: " + world.id);  // Log de wereld ID

        worldBuilder.currentWorldId = world.id;
        Debug.Log("Loaded world ID: " + worldBuilder.currentWorldId);  // Log de wereld ID na toewijzing

        worldBuilder.BuildWorld(world.maxHeight, world.maxLength);
        worldMenu.SetActive(false);
        Debug.Log("Huidige wereld ID na het bouwen van de wereld: " + worldBuilder.currentWorldId);  // Log na bouwen
    }


    public async void SaveObjectsForWorld(List<ObjectDTO> objectsToSave)
    {
        if (worldBuilder.currentWorldId == Guid.Empty)
        {
            Debug.LogError("De wereld GUID is leeg. Kan geen objecten opslaan.");
            return;
        }

        Debug.Log("Wereld GUID: " + worldBuilder.currentWorldId);

        // Voeg het juiste EnvironmentId toe aan elk object
        foreach (var objectToSave in objectsToSave)
        {
            objectToSave.EnvironmentId = worldBuilder.currentWorldId;
        }

        // Zet de objecten om naar JSON
        foreach (var objectToSave in objectsToSave)
        {
            string jsonData = JsonConvert.SerializeObject(objectToSave);
            string url = $"https://avansict2220486.azurewebsites.net/api/object2d"; // Verander dit naar jouw API URL
            string token = ApiClient.Instance.GetToken();

            Debug.Log("Token: " + token);
            Debug.Log("Object Id" + objectToSave.Id);
            Debug.Log("Env Id: " + objectToSave.EnvironmentId);
            Debug.Log("JSON data die wordt verzonden: " + jsonData);

            // Voer de API-aanroep uit
            string response = await ApiClient.Instance.PerformApiCall(url, "POST", jsonData, token);

            if (!string.IsNullOrEmpty(response))
            {
                Debug.Log("Object succesvol opgeslagen: " + response);
            }
            else
            {
                Debug.LogError("Fout bij opslaan van object: Geen geldige reactie ontvangen.");
            }
        }
    }

    [System.Serializable]
    public class Object2DListWrapper
    {
        public List<ObjectDTO> objects;
    }


    public async Task<string> PerformApiCall(string apiUrl, string httpMethod, string jsonData = null, string token = null)
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
