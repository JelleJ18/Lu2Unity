using UnityEngine;

public class ObjectData : MonoBehaviour
{
    public ObjectType objectType;
    public bool isPlaced = false;

    private void OnMouseDown()
    {
        if (isPlaced)
        {
            // Verwijder object als het al geplaatst is
            Destroy(gameObject);
        }
    }
}

