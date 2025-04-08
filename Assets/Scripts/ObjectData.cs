using UnityEngine;

public class ObjectData : MonoBehaviour
{
    public ObjectType objectType;
    public bool isPlaced = false;

    private void OnMouseDown()
    {
        if (isPlaced)
        {
            Destroy(gameObject);
        }
    }
}
