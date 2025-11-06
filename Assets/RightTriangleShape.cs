using UnityEngine;

public class RightTriangleShape : MonoBehaviour
{
    void Start()
    {
        CreateRightTriangle();
    }
    
    void CreateRightTriangle()
    {
        // Create mesh for right triangle
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        
        // Create triangle mesh (right angle at origin)
        Mesh mesh = new Mesh();
        
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0, 0, 0),      // Bottom-left (right angle here)
            new Vector3(1, 0, 0),      // Bottom-right
            new Vector3(0, 1, 0)       // Top-left
        };
        
        int[] triangles = new int[] { 0, 1, 2 };
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        meshFilter.mesh = mesh;
        
        // Set material
        meshRenderer.material = new Material(Shader.Find("Sprites/Default"));
    }
}