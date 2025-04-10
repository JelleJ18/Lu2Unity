using System;
using System.Collections.Generic;
using UnityEngine;
using static ApiClient;

public class ObjectPlacementManager : MonoBehaviour
{
    public static ObjectPlacementManager Instance;

    private List<GameObject> placedObjects = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void AddObjectToWorld(GameObject obj)
    {
        if (!placedObjects.Contains(obj))
        {
            placedObjects.Add(obj);
        }
    }

    public async void SaveObjectsInWorld(Guid worldId)
    {
        List<ObjectDTO> objectDTOs = new List<ObjectDTO>();

        // Zet objecten om naar DTO's voor opslag
        foreach (var obj in placedObjects)
        {
            ObjectDTO dto = new ObjectDTO
            {
                Id = Guid.NewGuid(),
                PrefabId = obj.name, // Of een andere identificatie van het object
                PositionX = obj.transform.position.x,
                PositionY = obj.transform.position.y,
                RotationZ = obj.transform.eulerAngles.z,
                ScaleX = obj.transform.localScale.x,
                ScaleY = obj.transform.localScale.y
            };

            objectDTOs.Add(dto);
        }

        // Maak een objectDTO-lijst aan om op te slaan
        Object2DListWrapper wrapper = new Object2DListWrapper { objects = objectDTOs };

        string json = JsonUtility.ToJson(wrapper);
        string url = $"https://avansict2220486.azurewebsites.net/api/object2d/{worldId}";

        // Stuur de objecten naar de API om ze op te slaan
        await ApiClient.Instance.PerformApiCall(url, "POST", json, ApiClient.Instance.GetToken());
    }
}
