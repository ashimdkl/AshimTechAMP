using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TriangleAngleExplorer : MonoBehaviour
{
    [Header("Triangle Settings")]
    public Transform vertexA;
    public Transform vertexB;
    public Transform vertexC;
    public LineRenderer triangleLines;
    
    [Header("UI Elements")]
    public TextMeshProUGUI angleAText;
    public TextMeshProUGUI angleBText;
    public TextMeshProUGUI angleCText;
    public TextMeshProUGUI totalAngleText;
    public TextMeshProUGUI instructionText;
    
    [Header("Visual Settings")]
    public Color[] angleColors = new Color[6];
    public GameObject angleArcPrefab;
    
    // Private variables
    private Camera mainCamera;
    private Vector3[] triangleVertices = new Vector3[3];
    private float[] currentAngles = new float[3];
    private List<GameObject> angleArcs = new List<GameObject>();
    private bool isDragging = false;
    private int dragVertexIndex = -1;
    private Vector3 dragOffset;
    
    // Constraints
    private float minVertexDistance = 1f;
    private Bounds cameraBounds;
    
    void Start()
    {
        mainCamera = Camera.main;
        SetupCameraBounds();
        SetupInitialTriangle();
        SetupAngleColors();
        UpdateTriangle();
        
        instructionText.text = "Triangle angles always add to 180°. Drag any vertex to explore different triangles!";
    }
    
    void SetupCameraBounds()
    {
        float cameraHeight = 2f * mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        cameraBounds = new Bounds(Vector3.zero, new Vector3(cameraWidth - 2f, cameraHeight - 2f, 0));
    }
    
    void SetupInitialTriangle()
    {
        // Set initial positions for an equilateral-ish triangle
        vertexA.position = new Vector3(0f, 2f, 0f);
        vertexB.position = new Vector3(-2f, -1f, 0f);
        vertexC.position = new Vector3(2f, -1f, 0f);
    }
    
    void SetupAngleColors()
    {
        // Define color ranges for different angle sizes
        angleColors[0] = Color.yellow;        // 0-35°
        angleColors[1] = new Color(1f, 0.5f, 0f); // 36-65° (orange)
        angleColors[2] = Color.red;           // 66-95°
        angleColors[3] = new Color(0.5f, 0f, 1f); // 96-125° (purple)
        angleColors[4] = Color.blue;          // 126-155°
        angleColors[5] = Color.green;         // 156-180°
    }
    
    void Update()
    {
        HandleInput();
        UpdateTriangle();
    }
    
    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartDrag();
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            ContinueDrag();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            EndDrag();
        }
    }
    
    void StartDrag()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        
        // Check which vertex is closest to mouse
        float closestDistance = float.MaxValue;
        int closestIndex = -1;
        
        Transform[] vertices = { vertexA, vertexB, vertexC };
        
        for (int i = 0; i < vertices.Length; i++)
        {
            float distance = Vector3.Distance(mouseWorldPos, vertices[i].position);
            if (distance < 0.5f && distance < closestDistance) // Within 0.5 units
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }
        
        if (closestIndex != -1)
        {
            isDragging = true;
            dragVertexIndex = closestIndex;
            dragOffset = vertices[closestIndex].position - mouseWorldPos;
        }
    }
    
    void ContinueDrag()
    {
        if (!isDragging) return;
        
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        
        Vector3 newPosition = mouseWorldPos + dragOffset;
        
        // Constrain to camera bounds
        newPosition.x = Mathf.Clamp(newPosition.x, cameraBounds.min.x, cameraBounds.max.x);
        newPosition.y = Mathf.Clamp(newPosition.y, cameraBounds.min.y, cameraBounds.max.y);
        
        // Apply minimum distance constraint
        Transform[] vertices = { vertexA, vertexB, vertexC };
        for (int i = 0; i < vertices.Length; i++)
        {
            if (i == dragVertexIndex) continue;
            
            float distance = Vector3.Distance(newPosition, vertices[i].position);
            if (distance < minVertexDistance)
            {
                Vector3 direction = (newPosition - vertices[i].position).normalized;
                newPosition = vertices[i].position + direction * minVertexDistance;
            }
        }
        
        vertices[dragVertexIndex].position = newPosition;
    }
    
    void EndDrag()
    {
        isDragging = false;
        dragVertexIndex = -1;
    }
    
    void UpdateTriangle()
    {
        // Update vertex positions
        triangleVertices[0] = vertexA.position;
        triangleVertices[1] = vertexB.position;
        triangleVertices[2] = vertexC.position;
        
        // Update line renderer
        UpdateTriangleLines();
        
        // Calculate angles
        CalculateAngles();
        
        // Update UI
        UpdateAngleDisplay();
        
        // Update visual angle arcs (we'll implement this later)
        UpdateAngleArcs();
    }
    
    void UpdateTriangleLines()
    {
        triangleLines.positionCount = 4;
        triangleLines.SetPosition(0, triangleVertices[0]);
        triangleLines.SetPosition(1, triangleVertices[1]);
        triangleLines.SetPosition(2, triangleVertices[2]);
        triangleLines.SetPosition(3, triangleVertices[0]); // Close the triangle
    }
    
    void CalculateAngles()
    {
        // Calculate angle at vertex A (between AB and AC)
        currentAngles[0] = CalculateAngle(triangleVertices[0], triangleVertices[1], triangleVertices[2]);
        
        // Calculate angle at vertex B (between BA and BC)
        currentAngles[1] = CalculateAngle(triangleVertices[1], triangleVertices[0], triangleVertices[2]);
        
        // Calculate angle at vertex C (between CA and CB)
        currentAngles[2] = CalculateAngle(triangleVertices[2], triangleVertices[0], triangleVertices[1]);
    }
    
    float CalculateAngle(Vector3 vertex, Vector3 point1, Vector3 point2)
    {
        Vector3 vector1 = (point1 - vertex).normalized;
        Vector3 vector2 = (point2 - vertex).normalized;
        
        float dot = Vector3.Dot(vector1, vector2);
        dot = Mathf.Clamp(dot, -1f, 1f); // Prevent floating point errors
        
        float angleInRadians = Mathf.Acos(dot);
        float angleInDegrees = angleInRadians * Mathf.Rad2Deg;
        
        return angleInDegrees;
    }
    
    void UpdateAngleDisplay()
    {
        angleAText.text = $"A: {Mathf.Round(currentAngles[0])}°";
        angleBText.text = $"B: {Mathf.Round(currentAngles[1])}°";
        angleCText.text = $"C: {Mathf.Round(currentAngles[2])}°";
        
        float total = currentAngles[0] + currentAngles[1] + currentAngles[2];
        totalAngleText.text = $"Total: {Mathf.Round(total)}°";
        
        // Update colors based on angle size
        angleAText.color = GetAngleColor(currentAngles[0]);
        angleBText.color = GetAngleColor(currentAngles[1]);
        angleCText.color = GetAngleColor(currentAngles[2]);
    }
    
    Color GetAngleColor(float angle)
    {
        int roundedAngle = Mathf.RoundToInt(angle);
        
        if (roundedAngle <= 35) return angleColors[0];
        if (roundedAngle <= 65) return angleColors[1];
        if (roundedAngle <= 95) return angleColors[2];
        if (roundedAngle <= 125) return angleColors[3];
        if (roundedAngle <= 155) return angleColors[4];
        return angleColors[5];
    }
    
    void UpdateAngleArcs()
    {
        // Clear existing arcs
        foreach (GameObject arc in angleArcs)
        {
            if (arc != null) Destroy(arc);
        }
        angleArcs.Clear();
        
        // Create new arcs (simplified for now)
        CreateAngleArc(0, triangleVertices[0], triangleVertices[1], triangleVertices[2], currentAngles[0]);
        CreateAngleArc(1, triangleVertices[1], triangleVertices[0], triangleVertices[2], currentAngles[1]);
        CreateAngleArc(2, triangleVertices[2], triangleVertices[0], triangleVertices[1], currentAngles[2]);
    }
    
    void CreateAngleArc(int angleIndex, Vector3 vertex, Vector3 point1, Vector3 point2, float angle)
    {
        if (angleArcPrefab == null || angle < 2f) return;
        
        GameObject arc = Instantiate(angleArcPrefab, vertex, Quaternion.identity);
        angleArcs.Add(arc);
        
        // Set arc color
        Renderer arcRenderer = arc.GetComponent<Renderer>();
        if (arcRenderer != null)
        {
            arcRenderer.material.color = GetAngleColor(angle);
        }
        
        // Scale arc based on triangle size (simple approximation)
        float scale = Mathf.Min(Vector3.Distance(vertex, point1), Vector3.Distance(vertex, point2)) * 0.3f;
        arc.transform.localScale = Vector3.one * scale;
    }
}