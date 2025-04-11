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

    private void Awake()
    {
        buildManager.enabled = false;
        cameraMovement.enabled = false;
        buildPanel.SetActive(false);

        //Instance aanmaken zodat ik apimanager overal kan aanroepen.
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

    //Functionaliteit voor het registreren,, er word een post gestuurd naar de api met de email en password text uit de inputfields en dit word er vervolgens als nieuwe gebruiker in de database gezet.
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

    //Functionaliteit voor het inloggen, er word een post gestuurd naar de api met de email en password text uit de inputfields en er word gecheckt voor een match. Vervolgens word de token opgeslagen en kan die verder gebruikt worden.
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

    //Functionaliteit voor de knop om een wereld te maken, deze roept de CreateWorld aan en vervolgend word de wereld lijst bijgewerkt door DisplayWorlds().
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

    //Functionaliteit voor het maken van een wereld, er word gebruik gemaakt van een post naar de api en de worlddto voor de wereld word aangemaakt.
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

    //Functionaliteit voor het ophalen van de werelden van de gebruiker, er word een get uitgevoerd met de username en er word gekeken naar welke werelden aan welke user gekoppelt zit.
    public async Task<List<WorldDTO>> GetWorlds()
    {
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("Token not available");
            return null;
        }

        string apiUrl = "https://avansict2220486.azurewebsites.net/api/environment2d/userworlds?UserName=" + _username; 

        string response = await PerformApiCall(apiUrl, "GET", token: token);

        Debug.Log("API Response: " + response);

        if (!string.IsNullOrEmpty(response))
        {
            try
            {
                List<WorldDTO> worlds = JsonConvert.DeserializeObject<List<WorldDTO>>(response);
                return worlds;
            }
            catch (Exception e)
            {
                Debug.LogError("Error deserializing response: " + e.Message);
            }
        }

        return null;
    }

    //Functionaliteit voor het weergeven van de werelden in de lijst wanneer er is ingelogd, vervolgens word er een onclick event toegevoegd aan de button voor het laden en verwijderen van die speciefieke wereld.
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


            Button[] buttons = worldItem.GetComponentsInChildren<Button>();
            if (buttons.Length >= 2)
            {
                var capturedWorld = world;

                buttons[0].onClick.AddListener(() => {
                    LoadWorld(capturedWorld);
                });

                buttons[1].onClick.AddListener(() => {
                    DeleteWorld(capturedWorld);
                });
            }
        }
    }

    //Functionaliteit voor het inladen van de werelde en zijn objecten, tilemap word gebouwd en objecten worden ge-instantiate.
    public async void LoadWorld(WorldDTO world)
    {
        Debug.Log("Wereld aan het laden: " + world.name);

        worldBuilder.currentWorldId = world.id;

        worldBuilder.BuildWorld(world.maxHeight, world.maxLength);
        await LoadObjectsForWorld(world.id);
        worldMenu.SetActive(false);
        Debug.Log("Huidige wereld ID na het bouwen van de wereld: " + worldBuilder.currentWorldId);  
    }

    //Functionaliteit voor het verwijderen van een environment, roept een delete aan via de api.
    public async void DeleteWorld(WorldDTO world)
    {
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("Geen token beschikbaar voor het verwijderen.");
            return;
        }

        string url = $"https://avansict2220486.azurewebsites.net/api/environment2d/{world.id}";
        string response = await PerformApiCall(url, "DELETE", token: token);

        if (response != null)
        {
            Debug.Log("Wereld succesvol verwijderd: " + world.name);

            var worlds = await GetWorlds();
            if (worlds != null && worlds.Count > 0)
            {
                DisplayWorlds(worlds);
            }
            else
            {
                DisplayWorlds(new List<WorldDTO>());
            }
        }
        else
        {
            Debug.LogError("Fout bij verwijderen van wereld: " + world.name + response);
        }
    }

    //Wanneer er op de save button word geklikt word de save functionaliteit aangeroepen en alle objecten opgeslagen.
    public void OnSaveButtonClicked()
    {
        if (ObjectPost.Instance != null)
        {
            ObjectPost.Instance.SaveAllObjects(worldBuilder.currentWorldId);
        }
        else
        {
            Debug.LogError("ObjectPost is null. Kan objecten niet opslaan.");
        }
    }

    //Wanneer er op de home button word geklikt worden objecten uit de wereld gehaald.
    public void OnHomeButtonClicked()
    {

        if (ObjectPost.Instance != null)
        {
            ObjectPost.Instance.ClearWorldFromObjects();
        }
        else
        {
            Debug.LogError("ObjectPost is null. Kan objecten niet opslaan.");
        }
    }

    public async void SaveObjectsForWorld(List<ObjectDTO> objectsToSave)
    {
        if (worldBuilder.currentWorldId == Guid.Empty)
        {
            Debug.LogError("De wereld GUID is leeg. Kan geen objecten opslaan.");
            return;
        }

        Debug.Log("Wereld GUID: " + worldBuilder.currentWorldId);

        foreach (var objectToSave in objectsToSave)
        {
            objectToSave.EnvironmentId = worldBuilder.currentWorldId;
        }

        foreach (var objectToSave in objectsToSave)
        {
            string jsonData = JsonConvert.SerializeObject(objectToSave);
            string url = $"https://avansict2220486.azurewebsites.net/api/object2d"; 
            string token = ApiClient.Instance.GetToken();

            Debug.Log("JSON data: " + jsonData);

            string response = await ApiClient.Instance.PerformApiCall(url, "POST", jsonData, token);

            if (!string.IsNullOrEmpty(response))
            {
                Debug.Log("Object succesvol opgeslagen: " + response);
            }
            else
            {
                Debug.LogError("Geen reactie ontvangen bij opslaan.");
            }
        }
    }

    private async Task LoadObjectsForWorld(Guid environmentId)
    {
        string url = $"https://avansict2220486.azurewebsites.net/api/object2d/environment/{environmentId}";
        string token = ApiClient.Instance.GetToken();

        string response = await ApiClient.Instance.PerformApiCall(url, "GET", null, token);

        if (!string.IsNullOrEmpty(response))
        {
            ObjectDTO[] objectDtos = JsonConvert.DeserializeObject<ObjectDTO[]>(response);
            foreach (ObjectDTO dto in objectDtos)
            {
                ObjectPost.Instance.SpawnObjectFromDto(dto);
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

            if (!string.IsNullOrEmpty(token))
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
                errorText.text = "Bad Request check input fields!";
            }
            else
            {
                Debug.Log("Request successfull");
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                loginPanel.SetActive(false);
                buildManager.enabled = true;
                cameraMovement.enabled = true;
                buildPanel.SetActive(true);
                return request.downloadHandler.text;
            }
            else
            {
                Debug.LogError("API Error: " + request.error);
                return null;
            }
        }
    }
}
