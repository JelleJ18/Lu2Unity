using System;
using UnityEngine;

[System.Serializable]
public class ObjectDTO
{
    public Guid Id;
    public string PrefabId;
    public float PositionX;
    public float PositionY;
    public float ScaleX;
    public float ScaleY;
    public float RotationZ;
    public Guid EnvironmentId;  // Zorg ervoor dat je de juiste ID hebt om te koppelen aan een environment
}


