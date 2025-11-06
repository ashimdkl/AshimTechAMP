using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CustomLevelData
{
    public string levelName;
    public List<ShapeData> shapes;
}

[System.Serializable]
public class ShapeData
{
    public string shapeType;        // "Triangle", "RightTriangle", "Square"
    public float colorR;            // Red component (0-1)
    public float colorG;            // Green component (0-1)
    public float colorB;            // Blue component (0-1)
    public float colorA;            // Alpha component (0-1)
    public float positionX;         // World X position
    public float positionY;         // World Y position
    public float rotationZ;         // Z-axis rotation in degrees
    public bool isStartingPiece;    // True if colored (non-white)
}