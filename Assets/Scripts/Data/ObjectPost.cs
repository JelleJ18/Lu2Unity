using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectPost : MonoBehaviour
{
    public static ObjectPost Instance { get; private set; }
    public List<GameObject> placedObjects = new List<GameObject>();
    [SerializeField] private List<PrefabMapping> prefabMappings;
    private Dictionary<string, GameObject> prefabMapping;

    [System.Serializable]
    public class PrefabMapping
    {
        public string prefabId;
        public GameObject prefab;
    }

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
        DontDestroyOnLoad(this);

        if (prefabMappings == null || prefabMappings.Count == 0)
        {
            Debug.LogError("Prefab mappings are empty or null!");
        }
        else
        {
            prefabMapping = prefabMappings.ToDictionary(p => p.prefabId, p => p.prefab);
            Debug.Log("Prefab mappings loaded successfully.");
        }
    }

    public void SaveAllObjects(Guid environmentId)
    {
        List<ObjectDTO> objectDtos = new List<ObjectDTO>();

        var uniqueObjects = placedObjects
        .GroupBy(o => new { Name = o.name, Pos = o.transform.position })
        .Select(g => g.First())
        .ToList();

        foreach (GameObject obj in placedObjects)
        {
            ObjectData data = obj.GetComponent<ObjectData>();

            ObjectDTO dto = new ObjectDTO
            {
                Id = Guid.NewGuid(),
                PrefabId = obj.name.Replace("(Clone)", "").Trim(),
                PositionX = obj.transform.position.x,
                PositionY = obj.transform.position.y,
                ScaleX = obj.transform.localScale.x,
                ScaleY = obj.transform.localScale.y,
                RotationZ = obj.transform.eulerAngles.z,
                EnvironmentId = environmentId
            };

            objectDtos.Add(dto);
        }

        ApiClient.Instance.SaveObjectsForWorld(objectDtos);
    }

    public void SpawnObjectFromDto(ObjectDTO dto)
    {
        if (prefabMapping == null)
        {
            Debug.LogError("Prefab mapping is null!");
            return;
        }

        if (!prefabMapping.ContainsKey(dto.PrefabId))
        {
            Debug.LogWarning($"PrefabId '{dto.PrefabId}' not found in the mapping.");
            return;
        }

        GameObject prefab = prefabMapping[dto.PrefabId];
        if (prefab == null)
        {
            Debug.LogWarning($"Prefab for '{dto.PrefabId}' is null.");
            return;
        }

        Vector2 position = new Vector2(dto.PositionX, dto.PositionY);
        GameObject instance = Instantiate(prefab, position, Quaternion.Euler(0, 0, dto.RotationZ));

        instance.transform.localScale = new Vector3(dto.ScaleX, dto.ScaleY, 1f);

        placedObjects.Add(instance);
    }

    public void ClearWorldFromObjects()
    {
        foreach (GameObject obj in placedObjects)
        {
            Destroy(obj);
        }
        placedObjects.Clear();
    }


}
