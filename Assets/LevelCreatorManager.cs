using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class LevelCreatorManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject trianglePrefab;
    public GameObject rightTrianglePrefab;
    public GameObject squarePrefab;

    [Header("UI References")]
    public GameObject levelCreatorPanel;
    public Canvas uiCanvas;

    // Shape palette buttons
    private Button triangleButton;
    private Button rightTriangleButton;
    private Button squareButton;

    // Color palette buttons
    private Button whiteColorButton;
    private Button redColorButton;
    private Button blueColorButton;
    private Button greenColorButton;

    // Action buttons
    private Button rotateButton;
    private Button clearAllButton;
    private Button saveLevelButton;
    private Button backButton;

    // Canvas workspace
    private GameObject canvasArea;
    private RectTransform canvasRect;
    private GameObject canvasWorkspaceTitle;
    private TextMeshProUGUI instructionsText;

    // Build-by-playing state
    private GameObject selectedShapePrefab;
    private Color selectedColor = Color.white;
    private float currentRotation = 0f;
    private List<GameObject> placedShapes = new List<GameObject>();
    private List<Vector2Int> shapeGridPositions = new List<Vector2Int>();
    private List<float> shapeRotations = new List<float>();
    
    // Current position tracking
    private int currentShapeIndex = -1;
    private GameObject currentPositionIndicator;
    private bool isPlaying = false;
    
    // Grid system
    private Dictionary<Vector2Int, GameObject> gridOccupancy = new Dictionary<Vector2Int, GameObject>();

    // Popup state
    private bool isPopupOpen = false;

    // Control flags
    private bool isLevelCreatorActive = false;

    void Start()
    {
        Debug.Log("LevelCreatorManager: Waiting for initialization...");
    }

    void OnEnable()
    {
        isLevelCreatorActive = true;
        DisableOtherGameSystems();
    }

    void OnDisable()
    {
        isLevelCreatorActive = false;
    }

    void DisableOtherGameSystems()
    {
        TeleportationLevelManager teleportManager = FindObjectOfType<TeleportationLevelManager>();
        if (teleportManager != null)
        {
            teleportManager.enabled = false;
            Debug.Log("Disabled TeleportationLevelManager during level creation");
        }

        CustomLevelPlayer levelPlayer = FindObjectOfType<CustomLevelPlayer>();
        if (levelPlayer != null)
        {
            levelPlayer.enabled = false;
            Debug.Log("Disabled CustomLevelPlayer during level creation");
        }
    }

    public void InitializeLevelCreator()
    {
        Debug.Log("INITIALIZING LEVEL CREATOR");
        
        isLevelCreatorActive = true;
        DisableOtherGameSystems();

        if (uiCanvas == null)
        {
            uiCanvas = FindObjectOfType<Canvas>();
            Debug.Log("Found UI Canvas: " + (uiCanvas != null));
        }

        CreateLevelCreatorUI();
        Debug.Log("LEVEL CREATOR READY - Place your first shape!");
    }

    void CreateLevelCreatorUI()
    {
        if (levelCreatorPanel == null)
        {
            Debug.LogError("Level Creator Panel not found!");
            return;
        }

        CreateCanvasWorkspace();
        CreateShapePalette();
        CreateColorPalette();
        CreateActionButtons();
        CreateInstructions();
        CreateCurrentPositionIndicator();

        Debug.Log("Level Creator UI created successfully");
    }

    // ========================================
    // CANVAS WORKSPACE CREATION
    // ========================================

    void CreateCanvasWorkspace()
    {
        Debug.Log("Creating canvas workspace...");

        canvasArea = new GameObject("CanvasWorkspace");
        canvasArea.transform.SetParent(levelCreatorPanel.transform, false);

        Image canvasBG = canvasArea.AddComponent<Image>();
        canvasBG.color = new Color(0.9f, 0.9f, 0.9f, 0.15f);
        canvasBG.raycastTarget = true;

        Outline canvasOutline = canvasArea.AddComponent<Outline>();
        canvasOutline.effectColor = Color.black;
        canvasOutline.effectDistance = new Vector2(3, -3);

        canvasRect = canvasArea.GetComponent<RectTransform>();
        canvasRect.anchorMin = new Vector2(0.22f, 0.05f);
        canvasRect.anchorMax = new Vector2(0.78f, 0.95f);
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        canvasWorkspaceTitle = new GameObject("CanvasTitle");
        canvasWorkspaceTitle.transform.SetParent(canvasArea.transform, false);

        TextMeshProUGUI canvasTitle = canvasWorkspaceTitle.AddComponent<TextMeshProUGUI>();
        canvasTitle.text = "Click to place your first shape!";
        canvasTitle.fontSize = 24;
        canvasTitle.color = new Color(0.3f, 0.3f, 0.3f, 0.9f);
        canvasTitle.alignment = TextAlignmentOptions.Center;
        canvasTitle.fontStyle = FontStyles.Bold;
        canvasTitle.raycastTarget = false;

        RectTransform titleRect = canvasWorkspaceTitle.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.5f);
        titleRect.anchorMax = new Vector2(1, 0.5f);
        titleRect.sizeDelta = new Vector2(-20, 50);
        titleRect.anchoredPosition = Vector2.zero;

        Debug.Log("Canvas workspace created");
    }

    void CreateInstructions()
    {
        GameObject instructionsObj = new GameObject("Instructions");
        instructionsObj.transform.SetParent(levelCreatorPanel.transform, false);

        instructionsText = instructionsObj.AddComponent<TextMeshProUGUI>();
        instructionsText.text = "Select a shape and color, then click to start!";
        instructionsText.fontSize = 18;
        instructionsText.color = Color.yellow;
        instructionsText.alignment = TextAlignmentOptions.Center;
        instructionsText.fontStyle = FontStyles.Bold;

        RectTransform instructionsRect = instructionsObj.GetComponent<RectTransform>();
        instructionsRect.anchorMin = new Vector2(0.22f, 0f);
        instructionsRect.anchorMax = new Vector2(0.78f, 0.05f);
        instructionsRect.offsetMin = Vector2.zero;
        instructionsRect.offsetMax = Vector2.zero;
    }

    void CreateCurrentPositionIndicator()
    {
        currentPositionIndicator = new GameObject("CurrentPositionIndicator");
        
        LineRenderer lr = currentPositionIndicator.AddComponent<LineRenderer>();
        lr.startWidth = 0.08f;  // Thinner line
        lr.endWidth = 0.08f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.yellow;
        lr.endColor = Color.yellow;
        lr.sortingOrder = 1001;
        lr.useWorldSpace = true;

        currentPositionIndicator.SetActive(false);
    }

    // ========================================
    // FIXED: OUTLINE WITH ACTUAL BOUNDS + ROTATION PREVIEW
    // ========================================

    void UpdateCurrentPositionIndicator()
    {
        if (currentShapeIndex < 0 || currentShapeIndex >= placedShapes.Count)
        {
            if (currentPositionIndicator != null)
            {
                currentPositionIndicator.SetActive(false);
            }
            return;
        }

        // Get current position
        GameObject currentShape = placedShapes[currentShapeIndex];
        Vector3 shapePos = currentShape.transform.position;

        // Show the NEXT shape at the NEXT rotation (preview)
        GameObject previewPrefab = selectedShapePrefab != null ? selectedShapePrefab : currentShape;
        string previewShapeType = GetShapeType(previewPrefab);
        float previewRotation = currentRotation;

        LineRenderer lr = currentPositionIndicator.GetComponent<LineRenderer>();
        if (lr == null) return;

        // Use actual sprite bounds from the preview prefab
        Vector3[] outlinePoints = GenerateShapeOutline(previewPrefab, previewShapeType, shapePos, previewRotation);
        
        lr.positionCount = outlinePoints.Length;
        lr.SetPositions(outlinePoints);

        currentPositionIndicator.SetActive(true);
    }

    Vector3[] GenerateShapeOutline(GameObject shapePrefab, string shapeType, Vector3 position, float rotation)
    {
        // FIXED: Get ACTUAL rendered bounds (with scale)
        Bounds spriteBounds = GetShapeSpriteBounds(shapePrefab);
        float padding = 0.06f; // Tight padding
        
        float halfWidth = spriteBounds.extents.x + padding;
        float halfHeight = spriteBounds.extents.y + padding;
        
        List<Vector3> points = new List<Vector3>();

        if (shapeType == "Square")
        {
            // Square outline (5 points to close)
            Vector3[] squarePoints = new Vector3[]
            {
                new Vector3(-halfWidth, -halfHeight, -0.3f),
                new Vector3(halfWidth, -halfHeight, -0.3f),
                new Vector3(halfWidth, halfHeight, -0.3f),
                new Vector3(-halfWidth, halfHeight, -0.3f),
                new Vector3(-halfWidth, -halfHeight, -0.3f)
            };

            foreach (Vector3 point in squarePoints)
            {
                Vector3 rotated = RotatePoint(point, rotation);
                points.Add(position + rotated);
            }
        }
        else if (shapeType == "Triangle")
        {
            // Equilateral triangle outline
            Vector3[] trianglePoints = new Vector3[]
            {
                new Vector3(0, halfHeight, -0.3f),              // Top
                new Vector3(-halfWidth, -halfHeight, -0.3f),    // Bottom left
                new Vector3(halfWidth, -halfHeight, -0.3f),     // Bottom right
                new Vector3(0, halfHeight, -0.3f)               // Back to top
            };

            foreach (Vector3 point in trianglePoints)
            {
                Vector3 rotated = RotatePoint(point, rotation);
                points.Add(position + rotated);
            }
        }
        else if (shapeType == "RightTriangle")
        {
            // Right triangle outline - right angle at bottom-left
            Vector3[] rightTrianglePoints = new Vector3[]
            {
                new Vector3(-halfWidth, -halfHeight, -0.3f),  // Bottom left (right angle)
                new Vector3(halfWidth, -halfHeight, -0.3f),   // Bottom right
                new Vector3(-halfWidth, halfHeight, -0.3f),   // Top left
                new Vector3(-halfWidth, -halfHeight, -0.3f)   // Close
            };

            foreach (Vector3 point in rightTrianglePoints)
            {
                Vector3 rotated = RotatePoint(point, rotation);
                points.Add(position + rotated);
            }
        }

        return points.ToArray();
    }

    Bounds GetShapeSpriteBounds(GameObject shapePrefab)
    {
        // FIXED: Get sprite bounds with scale applied
        SpriteRenderer sr = shapePrefab.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            Bounds spriteBounds = sr.sprite.bounds;
            
            // Apply prefab's local scale to get actual rendered size
            Vector3 scale = shapePrefab.transform.localScale;
            Vector3 scaledSize = new Vector3(
                spriteBounds.size.x * scale.x,
                spriteBounds.size.y * scale.y,
                spriteBounds.size.z * scale.z
            );
            
            Bounds scaledBounds = new Bounds(spriteBounds.center, scaledSize);
            
            Debug.Log($"[Bounds] {shapePrefab.name}: sprite={spriteBounds.size}, scale={scale}, final={scaledSize}");
            
            return scaledBounds;
        }

        // Fallback to grid-sized bounds
        Debug.LogWarning("Could not get sprite bounds for " + shapePrefab.name + ", using default");
        return new Bounds(Vector3.zero, new Vector3(0.5f, 0.5f, 0f));
    }

    Vector3 RotatePoint(Vector3 point, float angleDegrees)
    {
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angleRadians);
        float sin = Mathf.Sin(angleRadians);

        float newX = point.x * cos - point.y * sin;
        float newY = point.x * sin + point.y * cos;

        return new Vector3(newX, newY, point.z);
    }

    // ========================================
    // SHAPE PALETTE CREATION
    // ========================================

    void CreateShapePalette()
    {
        Debug.Log("Creating shape palette...");

        GameObject shapeTitleObj = new GameObject("ShapePaletteTitle");
        shapeTitleObj.transform.SetParent(levelCreatorPanel.transform, false);

        TextMeshProUGUI shapeTitle = shapeTitleObj.AddComponent<TextMeshProUGUI>();
        shapeTitle.text = "SHAPES";
        shapeTitle.fontSize = 24;
        shapeTitle.color = Color.yellow;
        shapeTitle.alignment = TextAlignmentOptions.Center;
        shapeTitle.fontStyle = FontStyles.Bold;

        RectTransform shapeTitleRect = shapeTitleObj.GetComponent<RectTransform>();
        shapeTitleRect.anchorMin = new Vector2(0, 0.85f);
        shapeTitleRect.anchorMax = new Vector2(0.2f, 0.95f);
        shapeTitleRect.offsetMin = new Vector2(10, 0);
        shapeTitleRect.offsetMax = new Vector2(-10, 0);

        triangleButton = CreateShapeButton("TriangleButton", trianglePrefab, new Vector2(0, 0.7f), new Vector2(0.2f, 0.82f));
        rightTriangleButton = CreateShapeButton("RightTriangleButton", rightTrianglePrefab, new Vector2(0, 0.55f), new Vector2(0.2f, 0.67f));
        squareButton = CreateShapeButton("SquareButton", squarePrefab, new Vector2(0, 0.4f), new Vector2(0.2f, 0.52f));

        Debug.Log("Shape palette created");
    }

    Button CreateShapeButton(string name, GameObject prefab, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(levelCreatorPanel.transform, false);

        Button button = buttonObj.AddComponent<Button>();
        Image buttonBG = buttonObj.AddComponent<Image>();
        buttonBG.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.offsetMin = new Vector2(10, 0);
        buttonRect.offsetMax = new Vector2(-10, 0);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = name.Replace("Button", "").Replace("Triangle", "Tri");
        buttonText.fontSize = 18;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        button.onClick.AddListener(() => SelectShape(prefab));

        return button;
    }

    // ========================================
    // COLOR PALETTE CREATION
    // ========================================

    void CreateColorPalette()
    {
        Debug.Log("Creating color palette...");

        GameObject colorTitleObj = new GameObject("ColorPaletteTitle");
        colorTitleObj.transform.SetParent(levelCreatorPanel.transform, false);

        TextMeshProUGUI colorTitle = colorTitleObj.AddComponent<TextMeshProUGUI>();
        colorTitle.text = "COLORS";
        colorTitle.fontSize = 24;
        colorTitle.color = Color.yellow;
        colorTitle.alignment = TextAlignmentOptions.Center;
        colorTitle.fontStyle = FontStyles.Bold;

        RectTransform colorTitleRect = colorTitleObj.GetComponent<RectTransform>();
        colorTitleRect.anchorMin = new Vector2(0, 0.25f);
        colorTitleRect.anchorMax = new Vector2(0.2f, 0.35f);
        colorTitleRect.offsetMin = new Vector2(10, 0);
        colorTitleRect.offsetMax = new Vector2(-10, 0);

        whiteColorButton = CreateColorButton("WhiteButton", Color.white, new Vector2(0, 0.15f), new Vector2(0.2f, 0.22f));
        redColorButton = CreateColorButton("RedButton", Color.red, new Vector2(0, 0.1f), new Vector2(0.2f, 0.17f));
        blueColorButton = CreateColorButton("BlueButton", Color.blue, new Vector2(0, 0.05f), new Vector2(0.2f, 0.12f));
        greenColorButton = CreateColorButton("GreenButton", Color.green, new Vector2(0, 0f), new Vector2(0.2f, 0.07f));

        Debug.Log("Color palette created");
    }

    Button CreateColorButton(string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(levelCreatorPanel.transform, false);

        Button button = buttonObj.AddComponent<Button>();
        Image buttonBG = buttonObj.AddComponent<Image>();
        buttonBG.color = color;

        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.offsetMin = new Vector2(10, 0);
        buttonRect.offsetMax = new Vector2(-10, 0);

        Outline outline = buttonObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);

        button.onClick.AddListener(() => SelectColor(color));

        return button;
    }

    // ========================================
    // ACTION BUTTONS CREATION
    // ========================================

    void CreateActionButtons()
    {
        Debug.Log("Creating action buttons...");

        rotateButton = CreateActionButton("RotateButton", "ROTATE (R)", new Vector2(0.8f, 0.9f), new Vector2(0.98f, 0.98f), RotateNextShape);
        clearAllButton = CreateActionButton("ClearAllButton", "CLEAR ALL", new Vector2(0.8f, 0.8f), new Vector2(0.98f, 0.88f), ClearAllShapes);
        saveLevelButton = CreateActionButton("SaveLevelButton", "SAVE LEVEL", new Vector2(0.8f, 0.7f), new Vector2(0.98f, 0.78f), OnSaveLevelClicked);
        backButton = CreateActionButton("BackButton", "BACK", new Vector2(0.8f, 0.02f), new Vector2(0.98f, 0.1f), OnBackClicked);

        Debug.Log("Action buttons created");
    }

    Button CreateActionButton(string name, string label, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(levelCreatorPanel.transform, false);

        Button button = buttonObj.AddComponent<Button>();
        Image buttonBG = buttonObj.AddComponent<Image>();
        buttonBG.color = new Color(0.2f, 0.6f, 0.8f, 1f);

        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.offsetMin = new Vector2(10, 0);
        buttonRect.offsetMax = new Vector2(-10, 0);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = label;
        buttonText.fontSize = 18;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.fontStyle = FontStyles.Bold;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        button.onClick.AddListener(onClick);

        return button;
    }

    // ========================================
    // SELECTION HANDLERS
    // ========================================

    void SelectShape(GameObject prefab)
    {
        selectedShapePrefab = prefab;
        currentRotation = 0f;
        UpdateCurrentPositionIndicator();
        Debug.Log("Selected shape: " + prefab.name);
    }

    void SelectColor(Color color)
    {
        selectedColor = color;
        Debug.Log("Selected color: " + color);
    }

    void RotateNextShape()
    {
        if (!isLevelCreatorActive) return;

        currentRotation += 90f;
        if (currentRotation >= 360f) currentRotation = 0f;
        
        UpdateCurrentPositionIndicator();
        
        Debug.Log("Next shape will be rotated: " + currentRotation + " degrees");
    }

    // ========================================
    // ACTION BUTTON HANDLERS
    // ========================================

    void ClearAllShapes()
    {
        int shapeCount = placedShapes.Count;

        foreach (GameObject shape in placedShapes)
        {
            if (shape != null)
            {
                Destroy(shape);
            }
        }

        placedShapes.Clear();
        shapeGridPositions.Clear();
        shapeRotations.Clear();
        gridOccupancy.Clear();
        currentShapeIndex = -1;
        isPlaying = false;
        currentRotation = 0f;

        if (currentPositionIndicator != null)
        {
            currentPositionIndicator.SetActive(false);
        }

        if (canvasWorkspaceTitle != null)
        {
            canvasWorkspaceTitle.SetActive(true);
        }

        if (instructionsText != null)
        {
            instructionsText.text = "Select a shape and color, then click to start!";
        }

        Debug.Log("Cleared " + shapeCount + " shape(s)");
    }

    void OnSaveLevelClicked()
    {
        if (placedShapes.Count == 0)
        {
            Debug.Log("Cannot save empty level!");
            return;
        }

        Debug.Log("Attempting to save level with " + placedShapes.Count + " shapes...");
        StartCoroutine(ShowLevelNamingPopup());
    }

    void OnBackClicked()
    {
        Debug.Log("Back button pressed - cleaning up and returning to main menu");

        ClearAllShapes();

        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            if (levelCreatorPanel != null)
            {
                levelCreatorPanel.SetActive(false);
            }

            if (gameManager.mainMenuPanel != null)
            {
                gameManager.mainMenuPanel.SetActive(true);
                Debug.Log("Returned to main menu");
            }
        }
    }

    // ========================================
    // INPUT HANDLING - BUILD BY PLAYING
    // ========================================

    void Update()
    {
        if (!isLevelCreatorActive || isPopupOpen)
        {
            return;
        }

        if (!isPlaying)
        {
            HandleInitialPlacement();
        }
        else
        {
            HandleWASDBuilding();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RotateNextShape();
        }

        if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
        {
            UndoLastShape();
        }
    }

    void HandleInitialPlacement()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (selectedShapePrefab == null)
            {
                Debug.Log("Please select a shape first!");
                return;
            }

            if (EventSystem.current.IsPointerOverGameObject())
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current);
                pointerData.position = Input.mousePosition;

                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                bool clickedCanvas = false;
                foreach (RaycastResult result in results)
                {
                    if (result.gameObject == canvasArea)
                    {
                        clickedCanvas = true;
                        break;
                    }
                }

                if (!clickedCanvas)
                {
                    return;
                }
            }

            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            if (IsWithinCanvasBounds(mousePos))
            {
                PlaceFirstShape(mousePos);
            }
        }
    }

    void PlaceFirstShape(Vector3 worldPos)
    {
        Vector3 snappedPos = SnapToGrid(worldPos);
        Vector2Int gridPos = WorldToGridPosition(snappedPos);

        GameObject newShape = Instantiate(selectedShapePrefab, snappedPos, Quaternion.Euler(0, 0, currentRotation));

        SpriteRenderer spriteRenderer = newShape.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = selectedColor;
            spriteRenderer.sortingOrder = 1000;
        }

        placedShapes.Add(newShape);
        shapeGridPositions.Add(gridPos);
        shapeRotations.Add(currentRotation);
        gridOccupancy[gridPos] = newShape;
        currentShapeIndex = 0;
        isPlaying = true;

        if (canvasWorkspaceTitle != null)
        {
            canvasWorkspaceTitle.SetActive(false);
        }

        if (instructionsText != null)
        {
            instructionsText.text = "Use WASD to build! R to rotate. Backspace to undo. Shapes: " + placedShapes.Count;
        }

        UpdateCurrentPositionIndicator();

        Debug.Log($"First shape placed at grid={gridPos}, world={snappedPos}");
    }

    void HandleWASDBuilding()
    {
        Vector2Int direction = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.W))
        {
            direction = Vector2Int.up;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            direction = Vector2Int.down;
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            direction = Vector2Int.left;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            direction = Vector2Int.right;
        }

        if (direction != Vector2Int.zero)
        {
            TryPlaceShapeInDirection(direction);
        }
    }

    void TryPlaceShapeInDirection(Vector2Int direction)
    {
        if (selectedShapePrefab == null)
        {
            Debug.Log("Select a shape first!");
            return;
        }

        if (currentShapeIndex < 0 || currentShapeIndex >= placedShapes.Count)
        {
            Debug.LogError("Invalid current shape index!");
            return;
        }

        Vector2Int currentGridPos = shapeGridPositions[currentShapeIndex];
        Vector2Int newGridPos = currentGridPos + direction;

        if (gridOccupancy.ContainsKey(newGridPos))
        {
            int existingIndex = shapeGridPositions.IndexOf(newGridPos);
            if (existingIndex >= 0)
            {
                currentShapeIndex = existingIndex;
                UpdateCurrentPositionIndicator();
                Debug.Log("Moved to existing shape at " + newGridPos);
                return;
            }
        }

        GameObject currentShape = placedShapes[currentShapeIndex];
        float currentShapeRotation = currentShape.transform.eulerAngles.z;
        
        // Edge compatibility checking
        if (!AreShapesCompatible(currentShape, currentShapeRotation, selectedShapePrefab, currentRotation, direction))
        {
            ShowInvalidMoveFeedback(GridToWorldPosition(newGridPos));
            Debug.Log("Invalid move - shapes not compatible! Edge types don't match.");
            return;
        }

        Vector3 worldPos = GridToWorldPosition(newGridPos);
        GameObject newShape = Instantiate(selectedShapePrefab, worldPos, Quaternion.Euler(0, 0, currentRotation));

        SpriteRenderer spriteRenderer = newShape.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = selectedColor;
            spriteRenderer.sortingOrder = 1000;
        }

        placedShapes.Add(newShape);
        shapeGridPositions.Add(newGridPos);
        shapeRotations.Add(currentRotation);
        gridOccupancy[newGridPos] = newShape;
        currentShapeIndex = placedShapes.Count - 1;

        UpdateCurrentPositionIndicator();

        if (instructionsText != null)
        {
            instructionsText.text = "Use WASD to build! R to rotate. Backspace to undo. Shapes: " + placedShapes.Count;
        }

        Debug.Log($"Placed at grid={newGridPos}, world={worldPos}. Total: {placedShapes.Count}");
    }

    // ========================================
    // EDGE COMPATIBILITY SYSTEM
    // ========================================

    bool AreShapesCompatible(GameObject shape1, float shape1Rotation, GameObject shape2Prefab, float shape2Rotation, Vector2Int direction)
    {
        string shape1Type = GetShapeType(shape1);
        string shape2Type = GetShapeType(shape2Prefab);

        // Square to Square: Always compatible
        if (shape1Type == "Square" && shape2Type == "Square")
        {
            return true;
        }

        // Triangle to Triangle: Always compatible
        if (shape1Type == "Triangle" && shape2Type == "Triangle")
        {
            return true;
        }

        // Square to Triangle or vice versa: Always compatible
        if ((shape1Type == "Square" && shape2Type == "Triangle") ||
            (shape1Type == "Triangle" && shape2Type == "Square"))
        {
            return true;
        }

        // Right Triangle requires edge checking
        if (shape1Type == "RightTriangle" || shape2Type == "RightTriangle")
        {
            bool shape1EdgeStraight = IsEdgeStraight(shape1Type, shape1Rotation, direction);
            bool shape2EdgeStraight = IsEdgeStraight(shape2Type, shape2Rotation, GetOppositeDirection(direction));

            bool compatible = shape1EdgeStraight && shape2EdgeStraight;

            if (!compatible)
            {
                Debug.Log($"Edge mismatch: {shape1Type}(rot:{shape1Rotation}°) edge {direction} is " +
                         $"{(shape1EdgeStraight ? "STRAIGHT" : "HYPOTENUSE")}, " +
                         $"{shape2Type}(rot:{shape2Rotation}°) edge {GetOppositeDirection(direction)} is " +
                         $"{(shape2EdgeStraight ? "STRAIGHT" : "HYPOTENUSE")}");
            }

            return compatible;
        }

        return true;
    }

    bool IsEdgeStraight(string shapeType, float rotation, Vector2Int direction)
    {
        if (shapeType == "Square" || shapeType == "Triangle")
        {
            return true;
        }

        if (shapeType == "RightTriangle")
        {
            float normalizedRotation = rotation % 360f;
            if (normalizedRotation < 0) normalizedRotation += 360f;

            int rotationAngle = Mathf.RoundToInt(normalizedRotation / 90f) * 90;
            rotationAngle = rotationAngle % 360;
            
            if (rotationAngle == 0)
            {
                return (direction == Vector2Int.down || direction == Vector2Int.left);
            }
            else if (rotationAngle == 90)
            {
                return (direction == Vector2Int.down || direction == Vector2Int.right);
            }
            else if (rotationAngle == 180)
            {
                return (direction == Vector2Int.up || direction == Vector2Int.right);
            }
            else if (rotationAngle == 270)
            {
                return (direction == Vector2Int.up || direction == Vector2Int.left);
            }
        }

        return true;
    }

    Vector2Int GetOppositeDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.up) return Vector2Int.down;
        if (direction == Vector2Int.down) return Vector2Int.up;
        if (direction == Vector2Int.left) return Vector2Int.right;
        if (direction == Vector2Int.right) return Vector2Int.left;
        return Vector2Int.zero;
    }

    string GetShapeType(GameObject shape)
    {
        if (shape.name.Contains("Square")) return "Square";
        if (shape.name.Contains("Right")) return "RightTriangle";
        if (shape.name.Contains("Triangle")) return "Triangle";
        return "Unknown";
    }

    void ShowInvalidMoveFeedback(Vector3 worldPos)
    {
        StartCoroutine(RedFlashEffect(worldPos));
    }

    IEnumerator RedFlashEffect(Vector3 position)
    {
        GameObject flash = new GameObject("InvalidFlash");
        flash.transform.position = new Vector3(position.x, position.y, -0.3f);

        SpriteRenderer sr = flash.AddComponent<SpriteRenderer>();
        sr.sprite = squarePrefab.GetComponent<SpriteRenderer>().sprite;
        sr.color = new Color(1f, 0f, 0f, 0.7f);
        sr.sortingOrder = 999;

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0.7f, 0f, elapsed / duration);
            sr.color = new Color(1f, 0f, 0f, alpha);
            yield return null;
        }

        Destroy(flash);
    }

    void UndoLastShape()
    {
        if (placedShapes.Count <= 1)
        {
            Debug.Log("Can't undo the first shape! Use Clear All instead.");
            return;
        }

        int lastIndex = placedShapes.Count - 1;
        GameObject lastShape = placedShapes[lastIndex];
        Vector2Int lastGridPos = shapeGridPositions[lastIndex];

        Destroy(lastShape);
        placedShapes.RemoveAt(lastIndex);
        shapeGridPositions.RemoveAt(lastIndex);
        shapeRotations.RemoveAt(lastIndex);
        gridOccupancy.Remove(lastGridPos);

        currentShapeIndex = placedShapes.Count - 1;
        UpdateCurrentPositionIndicator();

        if (instructionsText != null)
        {
            instructionsText.text = "Use WASD to build! R to rotate. Backspace to undo. Shapes: " + placedShapes.Count;
        }

        Debug.Log("Undid last shape - Shapes remaining: " + placedShapes.Count);
    }

    // ========================================
    // GRID UTILITIES (KEPT FROM WORKING VERSION)
    // ========================================

    Vector3 SnapToGrid(Vector3 position)
    {
        float gridSize = 0.5f;
        float x = Mathf.Round(position.x / gridSize) * gridSize;
        float y = Mathf.Round(position.y / gridSize) * gridSize;
        return new Vector3(x, y, 0);
    }

    Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        float gridSize = 0.5f;
        int x = Mathf.RoundToInt(worldPos.x / gridSize);
        int y = Mathf.RoundToInt(worldPos.y / gridSize);
        return new Vector2Int(x, y);
    }

    Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        float gridSize = 0.5f;
        return new Vector3(gridPos.x * gridSize, gridPos.y * gridSize, 0);
    }

    bool IsWithinCanvasBounds(Vector3 worldPos)
    {
        if (canvasRect == null) return true;

        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        return RectTransformUtility.RectangleContainsScreenPoint(canvasRect, screenPos, uiCanvas.worldCamera);
    }

    // ========================================
    // SAVE LEVEL POPUP
    // ========================================

    private IEnumerator ShowLevelNamingPopup()
    {
        isPopupOpen = true;
        Debug.Log("Opening level naming popup...");

        GameObject blocker = new GameObject("PopupBlocker");
        blocker.transform.SetParent(transform, false);

        Image blockerImage = blocker.AddComponent<Image>();
        blockerImage.color = new Color(0, 0, 0, 0.7f);

        RectTransform blockerRect = blocker.GetComponent<RectTransform>();
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.sizeDelta = Vector2.zero;
        blockerRect.anchoredPosition = Vector2.zero;

        GameObject popupPanel = new GameObject("LevelNamingPopup");
        popupPanel.transform.SetParent(blocker.transform, false);

        Image panelImage = popupPanel.AddComponent<Image>();
        panelImage.color = Color.white;

        Outline panelOutline = popupPanel.AddComponent<Outline>();
        panelOutline.effectColor = Color.yellow;
        panelOutline.effectDistance = new Vector2(3, -3);

        RectTransform popupRect = popupPanel.GetComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.sizeDelta = new Vector2(400, 300);
        popupRect.anchoredPosition = Vector2.zero;

        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(popupPanel.transform, false);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Name Your Level";
        titleText.fontSize = 28;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.black;

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(350, 50);
        titleRect.anchoredPosition = new Vector2(0, -40);

        GameObject inputObj = new GameObject("NameInputField");
        inputObj.transform.SetParent(popupPanel.transform, false);

        Image inputBg = inputObj.AddComponent<Image>();
        inputBg.color = Color.white;

        Outline inputOutline = inputObj.AddComponent<Outline>();
        inputOutline.effectColor = Color.black;
        inputOutline.effectDistance = new Vector2(2, -2);

        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
        inputField.textComponent = CreateInputTextComponent(inputObj);
        inputField.placeholder = CreatePlaceholderComponent(inputObj);

        RectTransform inputRect = inputObj.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.5f, 0.5f);
        inputRect.anchorMax = new Vector2(0.5f, 0.5f);
        inputRect.sizeDelta = new Vector2(350, 50);
        inputRect.anchoredPosition = new Vector2(0, 20);

        yield return null;

        GameObject saveButtonObj = new GameObject("SaveButton");
        saveButtonObj.transform.SetParent(popupPanel.transform, false);

        Image saveImage = saveButtonObj.AddComponent<Image>();
        saveImage.color = new Color(0.2f, 0.8f, 0.2f);

        Button saveButton = saveButtonObj.AddComponent<Button>();
        saveButton.targetGraphic = saveImage;

        RectTransform saveRect = saveButtonObj.GetComponent<RectTransform>();
        saveRect.anchorMin = new Vector2(0.5f, 0f);
        saveRect.anchorMax = new Vector2(0.5f, 0f);
        saveRect.pivot = new Vector2(0.5f, 0f);
        saveRect.sizeDelta = new Vector2(300, 50);
        saveRect.anchoredPosition = new Vector2(0, 80);

        GameObject saveTextObj = new GameObject("Text");
        saveTextObj.transform.SetParent(saveButtonObj.transform, false);

        TextMeshProUGUI saveText = saveTextObj.AddComponent<TextMeshProUGUI>();
        saveText.text = "SAVE LEVEL";
        saveText.fontSize = 24;
        saveText.fontStyle = FontStyles.Bold;
        saveText.alignment = TextAlignmentOptions.Center;
        saveText.color = Color.white;

        RectTransform saveTextRect = saveTextObj.GetComponent<RectTransform>();
        saveTextRect.anchorMin = Vector2.zero;
        saveTextRect.anchorMax = Vector2.one;
        saveTextRect.sizeDelta = Vector2.zero;
        saveTextRect.anchoredPosition = Vector2.zero;

        saveButton.onClick.AddListener(() =>
        {
            string levelName = inputField.text.Trim();
            if (!string.IsNullOrEmpty(levelName))
            {
                Debug.Log("Saving level as: " + levelName);
                SaveLevelToFile(levelName);
                Destroy(blocker);
                isPopupOpen = false;
            }
            else
            {
                Debug.Log("Level name cannot be empty!");
            }
        });

        GameObject cancelButtonObj = new GameObject("CancelButton");
        cancelButtonObj.transform.SetParent(popupPanel.transform, false);

        Image cancelImage = cancelButtonObj.AddComponent<Image>();
        cancelImage.color = new Color(0.8f, 0.2f, 0.2f);

        Button cancelButton = cancelButtonObj.AddComponent<Button>();
        cancelButton.targetGraphic = cancelImage;

        RectTransform cancelRect = cancelButtonObj.GetComponent<RectTransform>();
        cancelRect.anchorMin = new Vector2(0.5f, 0f);
        cancelRect.anchorMax = new Vector2(0.5f, 0f);
        cancelRect.pivot = new Vector2(0.5f, 0f);
        cancelRect.sizeDelta = new Vector2(300, 50);
        cancelRect.anchoredPosition = new Vector2(0, 20);

        GameObject cancelTextObj = new GameObject("Text");
        cancelTextObj.transform.SetParent(cancelButtonObj.transform, false);

        TextMeshProUGUI cancelText = cancelTextObj.AddComponent<TextMeshProUGUI>();
        cancelText.text = "CANCEL";
        cancelText.fontSize = 24;
        cancelText.fontStyle = FontStyles.Bold;
        cancelText.alignment = TextAlignmentOptions.Center;
        cancelText.color = Color.white;

        RectTransform cancelTextRect = cancelTextObj.GetComponent<RectTransform>();
        cancelTextRect.anchorMin = Vector2.zero;
        cancelTextRect.anchorMax = Vector2.one;
        cancelTextRect.sizeDelta = Vector2.zero;
        cancelTextRect.anchoredPosition = Vector2.zero;

        cancelButton.onClick.AddListener(() =>
        {
            Debug.Log("Save cancelled");
            Destroy(blocker);
            isPopupOpen = false;
        });

        saveButtonObj.transform.SetAsLastSibling();
        cancelButtonObj.transform.SetAsLastSibling();

        Debug.Log("Popup created");
    }

    private TextMeshProUGUI CreateInputTextComponent(GameObject parent)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent.transform, false);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 20;
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Left;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 5);
        textRect.offsetMax = new Vector2(-10, -5);

        return text;
    }

    private TextMeshProUGUI CreatePlaceholderComponent(GameObject parent)
    {
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(parent.transform, false);

        TextMeshProUGUI placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholder.text = "Enter level name...";
        placeholder.fontSize = 20;
        placeholder.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        placeholder.alignment = TextAlignmentOptions.Left;
        placeholder.fontStyle = FontStyles.Italic;

        RectTransform placeholderRect = placeholderObj.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(10, 5);
        placeholderRect.offsetMax = new Vector2(-10, 5);

        return placeholder;
    }

    void SaveLevelToFile(string levelName)
    {
        Debug.Log("SAVING LEVEL: " + levelName);

        CustomLevelData levelData = new CustomLevelData();
        levelData.levelName = levelName;
        levelData.shapes = new List<ShapeData>();

        foreach (GameObject shape in placedShapes)
        {
            ShapeData shapeData = new ShapeData();

            if (shape.name.Contains("Triangle"))
            {
                if (shape.name.Contains("Right"))
                {
                    shapeData.shapeType = "RightTriangle";
                }
                else
                {
                    shapeData.shapeType = "Triangle";
                }
            }
            else if (shape.name.Contains("Square"))
            {
                shapeData.shapeType = "Square";
            }

            SpriteRenderer spriteRenderer = shape.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                shapeData.colorR = color.r;
                shapeData.colorG = color.g;
                shapeData.colorB = color.b;
                shapeData.colorA = color.a;

                shapeData.isStartingPiece = !IsColorWhite(color);
            }

            shapeData.positionX = shape.transform.position.x;
            shapeData.positionY = shape.transform.position.y;
            shapeData.rotationZ = shape.transform.eulerAngles.z;

            levelData.shapes.Add(shapeData);
        }

        string json = JsonUtility.ToJson(levelData, true);

        string path = Application.persistentDataPath + "/CustomLevels/";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        string fileName = path + levelName + ".json";
        File.WriteAllText(fileName, json);

        Debug.Log("Level saved successfully! File: " + fileName);

        ClearAllShapes();
    }

    bool IsColorWhite(Color color)
    {
        return Mathf.Approximately(color.r, 1f) &&
               Mathf.Approximately(color.g, 1f) &&
               Mathf.Approximately(color.b, 1f);
    }
}