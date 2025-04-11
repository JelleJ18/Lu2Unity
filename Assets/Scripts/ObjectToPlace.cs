using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ObjectToPlace : MonoBehaviour
{
    public GameObject objectToPlace;
    private bool isDragging;
    private GameObject instantiatedObject;
    private Tilemap groundMap;
    private AudioSource placeSfx;

    private void Awake()
    {
        groundMap = GameObject.FindGameObjectWithTag("GroundMap").GetComponent<Tilemap>();
        placeSfx = GetComponent<AudioSource>();
    }

    public void PlaceObject()
    {
        if(!isDragging )
        {
            instantiatedObject = Instantiate(objectToPlace);
            isDragging = true;
        }
    }

    private void Update()
    {
        if (isDragging)
        {
            if (instantiatedObject != null)
            {
                if (instantiatedObject.GetComponent<ObjectData>().objectType == ObjectType.Interior)
                {
                    instantiatedObject.transform.position = GetMousePos();
                }
                else if (instantiatedObject.GetComponent<ObjectData>().objectType == ObjectType.Decorations)
                {
                    instantiatedObject.transform.position = GetMousePos();
                }
                else if (instantiatedObject.GetComponent<ObjectData>().objectType == ObjectType.Ground)
                {
                    instantiatedObject.transform.position = GetTilePos();
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                isDragging = false;
                instantiatedObject.GetComponent<ObjectData>().isPlaced = true;
                ObjectPost.Instance.placedObjects.Add(instantiatedObject);
                

                placeSfx.pitch = 0.7f;
                placeSfx.Play();
            }

            else if (Input.GetMouseButtonUp(1))
            {
                isDragging = false;
                Destroy(instantiatedObject);
            }
        }
    }


    public Vector3 GetMousePos()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float mouseposX = Mathf.RoundToInt(mousePos.x);
        float mousePosY = Mathf.RoundToInt(mousePos.y);
        mousePos = new Vector3(mouseposX, mousePosY, 0);
        return mousePos;
    }

    public Vector3 GetTilePos()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int tileCoordinate = groundMap.WorldToCell(mouseWorldPos);
        return tileCoordinate;
    }
}
