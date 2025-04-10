using System;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldBuilder : MonoBehaviour
{
    public Tilemap tilemap; 
    public Tile groundTile;
    public Guid currentWorldId;


    public void BuildWorld(int height, int length)
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap is not assigned!");
            return;
        }

        tilemap.ClearAllTiles();

        int width = length;
        int maxHeight = height;

        Vector3 tilemapSize = new Vector3(width, maxHeight, 0);
        Vector3 tilemapCenterOffset = tilemapSize / 2f;

        tilemap.transform.position = new Vector3(-tilemapCenterOffset.x, -tilemapCenterOffset.y, 0);

        for (int x = 0; x < length; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), groundTile);
            }
        }

        Debug.Log("World built with size: " + height + "x" + length);
    }
}
