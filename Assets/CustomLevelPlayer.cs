using UnityEngine;
using System.Collections.Generic;

public class CustomLevelPlayer : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject trianglePrefab;
    public GameObject rightTrianglePrefab;
    public GameObject squarePrefab;

    [Header("References")]
    public Camera mainCamera;

    // parent for all spawned shapes so localPositions match your creator grid
    private Transform spawnRoot;

    // Game state
    private readonly List<GameObject> allShapes = new List<GameObject>();
    private GameObject currentShape;
    private readonly Dictionary<GameObject, bool> visitedShapes = new Dictionary<GameObject, bool>();
    private readonly Dictionary<GameObject, int>  heatMap       = new Dictionary<GameObject, int>();
    private bool isPlaying = false;

    // Heat map colors
    private readonly Color[] heatColors = new Color[]
    {
        new Color(1f, 1f, 1f, 1f),
        new Color(1f, 0.9f, 0.7f, 1f),
        new Color(1f, 0.8f, 0.4f, 1f),
        new Color(1f, 0.6f, 0.2f, 1f),
        new Color(1f, 0.4f, 0.1f, 1f),
        new Color(1f, 0.2f, 0f, 1f)
    };

    public void InitializeRuntime(Transform root)
    {
        spawnRoot = root;
        // Ensure camera
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.orthographic = true;
            mainCamera.transform.position = new Vector3(0f, 0f, -10f);
            mainCamera.orthographicSize = 5.5f; // tune to your grid height
        }
    }

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    public void LoadAndStartLevel(CustomLevelData levelData)
    {
        Debug.Log("Loading level: " + levelData.levelName);

        ClearLevel();

        // If creator is inactive, grab its prefabs (includeInactive = true)
        if (trianglePrefab == null || rightTrianglePrefab == null || squarePrefab == null)
        {
            LevelCreatorManager creator = FindObjectOfType<LevelCreatorManager>(true);
            if (creator != null)
            {
                if (trianglePrefab == null)      trianglePrefab      = creator.trianglePrefab;
                if (rightTrianglePrefab == null) rightTrianglePrefab = creator.rightTrianglePrefab;
                if (squarePrefab == null)        squarePrefab        = creator.squarePrefab;
            }
        }

        int spawned = SpawnShapes(levelData);
        Debug.Log($"Spawn complete. Spawned: {spawned}");

        FindStartingPosition(levelData);
        InitializeHeatMap();

        isPlaying = true;
        Debug.Log("Level loaded! " + allShapes.Count + " shapes spawned");
    }

    int SpawnShapes(CustomLevelData levelData)
    {
        if (spawnRoot == null)
        {
            // safety net
            GameObject fallbackRoot = new GameObject("SpawnRoot");
            fallbackRoot.transform.SetParent(transform, false);
            spawnRoot = fallbackRoot.transform;
        }

        int count = 0;

        if (levelData.shapes == null || levelData.shapes.Count == 0)
            return 0;

        foreach (ShapeData s in levelData.shapes)
        {
            GameObject prefab = GetPrefabForShapeType(s.shapeType);
            if (prefab == null)
            {
                Debug.LogWarning($"No prefab for shapeType '{s.shapeType}'. Skipping.");
                continue;
            }

            // Positions saved from creator are already in grid coordinates.
            // Use localPosition so it matches your grid parent.
            Vector3 localPos = new Vector3(s.positionX, s.positionY, 0f);
            Quaternion rot = Quaternion.Euler(0, 0, s.rotationZ);

            GameObject shape = Instantiate(prefab, spawnRoot);
            shape.transform.localPosition = localPos;
            shape.transform.localRotation = rot;
            shape.name = s.shapeType + "_" + allShapes.Count;

            var sr = shape.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(s.colorR, s.colorG, s.colorB, s.colorA);
                sr.sortingOrder = 10;
            }
            if (shape.GetComponent<Collider2D>() == null)
                shape.AddComponent<PolygonCollider2D>();

            allShapes.Add(shape);
            count++;
        }

        return count;
    }

    GameObject GetPrefabForShapeType(string shapeType)
    {
        // Normalize a few possible spellings
        string key = (shapeType ?? "").Trim().ToLowerInvariant();
        switch (key)
        {
            case "triangle":
                return trianglePrefab;
            case "righttriangle":
            case "right triangle":
            case "right_triangle":
                return rightTrianglePrefab;
            case "square":
                return squarePrefab;
            default:
                return null;
        }
    }

    void FindStartingPosition(CustomLevelData levelData)
    {
        // Prefer first non-white shape from the JSON order
        foreach (var go in allShapes)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            Color c = sr.color;
            bool isWhite = Mathf.Approximately(c.r, 1f) && Mathf.Approximately(c.g, 1f) && Mathf.Approximately(c.b, 1f);
            if (!isWhite) { currentShape = go; return; }
        }
        if (allShapes.Count > 0) currentShape = allShapes[0];
    }

    void InitializeHeatMap()
    {
        foreach (var g in allShapes)
        {
            visitedShapes[g] = false;
            heatMap[g] = 0;
        }

        if (currentShape != null) VisitShape(currentShape);
        Debug.Log($"Heat map initialized for {allShapes.Count} shapes");
    }

    void Update()
    {
        if (!isPlaying || currentShape == null) return;

        Vector2 dir = Vector2.zero;
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) dir = Vector2.up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) dir = Vector2.down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) dir = Vector2.left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) dir = Vector2.right;

        if (dir != Vector2.zero) TryFlipToAdjacentShape(dir);
    }

    void TryFlipToAdjacentShape(Vector2 direction)
    {
        GameObject adj = FindAdjacentShape(currentShape, direction);
        if (adj != null)
        {
            currentShape = adj;
            VisitShape(currentShape);
            CheckWinCondition();
        }
    }

    GameObject FindAdjacentShape(GameObject from, Vector2 direction)
    {
        foreach (var other in allShapes)
        {
            if (other == from) continue;
            if (AreShapesAdjacent(from, other, direction)) return other;
        }
        return null;
    }

    bool AreShapesAdjacent(GameObject a, GameObject b, Vector2 direction)
    {
        // Use local grid coordinates
        Vector3 p1 = a.transform.localPosition;
        Vector3 p2 = b.transform.localPosition;
        Vector2 off = p2 - p1;

        const float step = 1f;      // your grid unit
        const float eps  = 0.15f;   // tolerance

        if (direction == Vector2.up)
            return Mathf.Abs(off.x) < eps && Mathf.Abs(off.y - step) < eps;
        if (direction == Vector2.down)
            return Mathf.Abs(off.x) < eps && Mathf.Abs(off.y + step) < eps;
        if (direction == Vector2.left)
            return Mathf.Abs(off.y) < eps && Mathf.Abs(off.x + step) < eps;
        // right
        return Mathf.Abs(off.y) < eps && Mathf.Abs(off.x - step) < eps;
    }

    void VisitShape(GameObject g)
    {
        visitedShapes[g] = true;
        heatMap[g] = heatMap[g] + 1;
        UpdateShapeColor(g);
    }

    void UpdateShapeColor(GameObject g)
    {
        int visits = heatMap[g];
        int idx = Mathf.Min(visits, heatColors.Length - 1);
        var sr = g.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = heatColors[idx];
    }

    void CheckWinCondition()
    {
        foreach (var kv in visitedShapes)
            if (!kv.Value) return;
        Win();
    }

    void Win()
    {
        isPlaying = false;
        Invoke(nameof(ReturnToMenu), 1.5f);
    }

    void ReturnToMenu()
    {
        ClearLevel();
        var mgr = FindObjectOfType<UserCreatedLevelsManager>(true);
        if (mgr != null && mgr.gameObject.activeInHierarchy)
            mgr.RefreshLevelList();
        Destroy(gameObject);
    }

    public void ClearLevel()
    {
        foreach (var g in allShapes) if (g != null) Destroy(g);
        allShapes.Clear();
        visitedShapes.Clear();
        heatMap.Clear();
        currentShape = null;
        isPlaying = false;
    }
}
