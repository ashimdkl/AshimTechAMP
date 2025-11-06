using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TeleportationLevelManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public float cellSize = 1f;
    
    [Header("Prefabs")]
    public GameObject gridCellPrefab;  // White box prefab
    public GameObject playerPiecePrefab;  // Red square prefab
    public GameObject teleportationPadPrefab;  // Golden arc indicator
    
    [Header("UI")]
    public TextMeshProUGUI movesText;
    public Canvas uiCanvas;
    public GameObject educationalPopupPrefab;  // We'll create this next
    
    [Header("Menu")]
    public GameObject mainMenuPanel;
    
    // Game state
    private int[,] visitCountGrid1;  // 2x2 grid (top-left)
    private int[,] visitCountGrid2;  // 2x2 grid (bottom-right)
    private GameObject playerPiece;
    private Vector2Int playerGridPosition;  // Position within current grid
    private int currentGrid = 1;  // Which grid player is in (1 or 2)
    private int movesCount = 0;
    private bool gameCompleted = false;
    private bool hasSeenEducationalPopup = false;
    
    // Grid visual objects
    private GameObject[,] gridCells1;  // Visual cells for grid 1
    private GameObject[,] gridCells2;  // Visual cells for grid 2
    private GameObject teleportPad1;  // Pad on grid 1 (bottom-right)
    private GameObject teleportPad2;  // Pad on grid 2 (top-left)
    
    // Educational popup
    private GameObject educationalPanel;
    private bool waitingForContinue = false;
    
    // World positions for the two grids - NOW TOUCHING EACH OTHER
    private Vector3 grid1Offset = new Vector3(-1f, 1f, 0);   // Top-left grid
    private Vector3 grid2Offset = new Vector3(1f, -1f, 0);   // Bottom-right grid (touching diagonally)
    
    void Start()
    {
        // Don't auto-start, wait for button click
    }
    
    public void StartTeleportationLevel()
    {
        Debug.Log("Starting Teleportation Level!");
        
        // Hide menu
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }
        
        // Initialize the level
        InitializeGrids();
        CreateTeleportationPads();
        SpawnPlayerPiece();
        UpdateUI();
    }
    
    void Update()
    {
        // Don't allow movement if game is completed or waiting for popup
        if (gameCompleted || waitingForContinue) return;
        
        // Handle WASD and Arrow Key input
        Vector2Int moveDirection = Vector2Int.zero;
        
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            moveDirection = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            moveDirection = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            moveDirection = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            moveDirection = Vector2Int.right;
        
        if (moveDirection != Vector2Int.zero)
        {
            TryMovePlayer(moveDirection);
        }
        
        // Reset with R key
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetLevel();
        }
    }
    
    void InitializeGrids()
    {
        // Initialize visit counts for both 2x2 grids
        visitCountGrid1 = new int[2, 2];
        visitCountGrid2 = new int[2, 2];
        
        gridCells1 = new GameObject[2, 2];
        gridCells2 = new GameObject[2, 2];
        
        // Create Grid 1 (top-left area)
        GameObject grid1Parent = new GameObject("Grid1_TopLeft");
        CreateGridVisual(grid1Parent, gridCells1, visitCountGrid1, grid1Offset, "Grid1");
        
        // Create Grid 2 (bottom-right area)
        GameObject grid2Parent = new GameObject("Grid2_BottomRight");
        CreateGridVisual(grid2Parent, gridCells2, visitCountGrid2, grid2Offset, "Grid2");
    }
    
    void CreateGridVisual(GameObject parent, GameObject[,] cellArray, int[,] visitArray, Vector3 offset, string gridName)
    {
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                // Calculate world position for this grid cell
                Vector3 worldPos = new Vector3(
                    offset.x + x * cellSize,
                    offset.y + y * cellSize,
                    0
                );
                
                // Create grid cell visual
                GameObject cell = Instantiate(gridCellPrefab, worldPos, Quaternion.identity, parent.transform);
                cell.name = $"{gridName}_Cell_{x}_{y}";
                cellArray[x, y] = cell;
                
                // Set initial state (unvisited)
                visitArray[x, y] = 0;
                UpdateCellVisual(cell, 0);
            }
        }
    }
    
    void CreateTeleportationPads()
    {
        // Grid 1 bottom-right corner position
        Vector3 grid1CornerPos = new Vector3(
            grid1Offset.x + 1 * cellSize,  // Right edge of grid 1
            grid1Offset.y + 0 * cellSize,  // Bottom edge of grid 1
            -0.2f
        );
        
        // Position the arc at the actual corner (bottom-right of that cell)
        Vector3 pad1Position = new Vector3(
            grid1CornerPos.x + cellSize * 0.5f,  // Right edge of the cell
            grid1CornerPos.y - cellSize * 0.5f,  // Bottom edge of the cell
            -0.2f
        );
        
        teleportPad1 = CreateTeleportArc(pad1Position, "TeleportPad_Grid1", 180f); // Arc pointing down-right
        
        // Grid 2 top-left corner position
        Vector3 grid2CornerPos = new Vector3(
            grid2Offset.x + 0 * cellSize,  // Left edge of grid 2
            grid2Offset.y + 1 * cellSize,  // Top edge of grid 2
            -0.2f
        );
        
        // Position the arc at the actual corner (top-left of that cell)
        Vector3 pad2Position = new Vector3(
            grid2CornerPos.x - cellSize * 0.5f,  // Left edge of the cell
            grid2CornerPos.y + cellSize * 0.5f,  // Top edge of the cell
            -0.2f
        );
        
        teleportPad2 = CreateTeleportArc(pad2Position, "TeleportPad_Grid2", 0f); // Arc pointing up-left
    }
    
    GameObject CreateTeleportArc(Vector3 position, string padName, float rotationOffset)
    {
        GameObject pad = new GameObject(padName);
        pad.transform.position = position;
        
        // Create LineRenderer for arc shape
        LineRenderer lr = pad.AddComponent<LineRenderer>();
        
        // Arc settings - BIGGER and more visible
        int segments = 25;
        float radius = 0.35f;  // Increased radius
        float startAngle = 0f + rotationOffset;
        float endAngle = 90f + rotationOffset;
        
        lr.positionCount = segments;
        lr.startWidth = 0.1f;  // Thicker line
        lr.endWidth = 0.1f;
        
        // Set material and color - BLACK
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.black;
        lr.endColor = Color.black;
        
        lr.sortingOrder = 10;
        lr.useWorldSpace = false; // Use local space
        
        // Generate arc points
        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.Lerp(startAngle, endAngle, i / (float)(segments - 1));
            float rad = angle * Mathf.Deg2Rad;
            
            float x = Mathf.Cos(rad) * radius;
            float y = Mathf.Sin(rad) * radius;
            
            lr.SetPosition(i, new Vector3(x, y, 0));
        }
        
        return pad;
    }
    
    void SpawnPlayerPiece()
    {
        // Start in Grid 1, top-left position (0, 1)
        currentGrid = 1;
        playerGridPosition = new Vector2Int(0, 1);
        
        Vector3 worldPos = GetWorldPosition(currentGrid, playerGridPosition);
        playerPiece = Instantiate(playerPiecePrefab, worldPos, Quaternion.identity);
        playerPiece.name = "PlayerPiece";
        
        // Visit the starting position
        visitCountGrid1[playerGridPosition.x, playerGridPosition.y]++;
        UpdateCellVisual(gridCells1[playerGridPosition.x, playerGridPosition.y], 
                        visitCountGrid1[playerGridPosition.x, playerGridPosition.y]);
    }
    
    void TryMovePlayer(Vector2Int direction)
    {
        Vector2Int newPosition = playerGridPosition + direction;
        
        // Check if new position is valid within current grid
        if (IsValidPositionInGrid(newPosition))
        {
            // Normal move within the grid
            MovePlayer(newPosition);
        }
        else
        {
            // Check if we're at a teleportation point
            if (IsAtTeleportationPoint(playerGridPosition, direction))
            {
                PerformTeleportation(direction);
            }
            else
            {
                Debug.Log("Can't move outside the grid boundaries!");
            }
        }
    }
    
    bool IsValidPositionInGrid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < 2 && pos.y >= 0 && pos.y < 2;
    }
    
    bool IsAtTeleportationPoint(Vector2Int pos, Vector2Int direction)
    {
        // Grid 1: bottom-right corner (1, 0), moving right or down
        if (currentGrid == 1 && pos == new Vector2Int(1, 0))
        {
            if (direction == Vector2Int.right || direction == Vector2Int.down)
                return true;
        }
        
        // Grid 2: top-left corner (0, 1), moving left or up
        if (currentGrid == 2 && pos == new Vector2Int(0, 1))
        {
            if (direction == Vector2Int.left || direction == Vector2Int.up)
                return true;
        }
        
        return false;
    }
    
    void PerformTeleportation(Vector2Int direction)
    {
        Debug.Log($"Teleporting from Grid {currentGrid}!");
        
        // Animate teleportation first
        StartCoroutine(TeleportAnimation());
        
        // Show educational popup on first teleportation AFTER animation
        if (!hasSeenEducationalPopup)
        {
            hasSeenEducationalPopup = true;
            StartCoroutine(ShowEducationalPopupAfterDelay());
        }
    }
    
    IEnumerator ShowEducationalPopupAfterDelay()
    {
        // Wait for teleportation animation to finish
        yield return new WaitForSeconds(0.6f);
        
        // Create and show the popup
        CreateEducationalPopup();
    }
    
    void CreateEducationalPopup()
    {
        // Find or get the Canvas
        if (uiCanvas == null)
        {
            uiCanvas = FindObjectOfType<Canvas>();
        }
        
        if (uiCanvas == null)
        {
            Debug.LogError("No Canvas found! Cannot create educational popup.");
            return;
        }
        
        // Create main panel
        educationalPanel = new GameObject("EducationalPopup");
        educationalPanel.transform.SetParent(uiCanvas.transform, false);
        
        // Add Image component for background
        Image panelBG = educationalPanel.AddComponent<Image>();
        panelBG.color = new Color(0, 0, 0, 0.9f); // Dark semi-transparent
        
        // Set up RectTransform - POSITIONED TO THE LEFT
        RectTransform panelRect = educationalPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0.5f);
        panelRect.anchorMax = new Vector2(0f, 0.5f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = new Vector2(20, 0); // 20 pixels from left edge
        panelRect.sizeDelta = new Vector2(400, 320);
        
        // Create title text
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(educationalPanel.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "TELEPORTATION!";
        titleText.fontSize = 28;
        titleText.color = new Color(1f, 0.84f, 0f); // Golden
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.75f);
        titleRect.anchorMax = new Vector2(1, 0.95f);
        titleRect.offsetMin = new Vector2(15, 0);
        titleRect.offsetMax = new Vector2(-15, 0);
        
        // Create main educational text
        GameObject mainTextObj = new GameObject("MainText");
        mainTextObj.transform.SetParent(educationalPanel.transform, false);
        TextMeshProUGUI mainText = mainTextObj.AddComponent<TextMeshProUGUI>();
        mainText.text = "You just experienced a wild teleportation!\n\n" +
                       "<b>Geometry Fact:</b>\n" +
                       "When shapes flip across a gap like this,\n" +
                       "<color=#FFD700>OPPOSING ANGLES\nARE EQUAL!</color>\n\n" +
                       "The angles at the teleportation points stay the same on both sides.";
        mainText.fontSize = 18;
        mainText.color = Color.white;
        mainText.alignment = TextAlignmentOptions.Center;
        mainText.lineSpacing = 1.1f;
        
        RectTransform mainTextRect = mainTextObj.GetComponent<RectTransform>();
        mainTextRect.anchorMin = new Vector2(0, 0.25f);
        mainTextRect.anchorMax = new Vector2(1, 0.75f);
        mainTextRect.offsetMin = new Vector2(15, 0);
        mainTextRect.offsetMax = new Vector2(-15, 0);
        
        // Create continue button
        GameObject buttonObj = new GameObject("ContinueButton");
        buttonObj.transform.SetParent(educationalPanel.transform, false);
        Button continueButton = buttonObj.AddComponent<Button>();
        
        // Button background
        Image buttonBG = buttonObj.AddComponent<Image>();
        buttonBG.color = new Color(0.2f, 0.7f, 1f, 1f); // Blue
        
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.15f, 0.05f);
        buttonRect.anchorMax = new Vector2(0.85f, 0.2f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;
        
        // Button text
        GameObject buttonTextObj = new GameObject("ButtonText");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Continue Playing!";
        buttonText.fontSize = 18;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.fontStyle = FontStyles.Bold;
        
        RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;
        
        // Set up button click
        continueButton.onClick.AddListener(CloseEducationalPopup);
        
        // Pause the game
        waitingForContinue = true;
        
        Debug.Log("Educational popup shown!");
    }
    
    void CloseEducationalPopup()
    {
        if (educationalPanel != null)
        {
            Destroy(educationalPanel);
            educationalPanel = null;
        }
        
        waitingForContinue = false;
        Debug.Log("Educational popup closed! Continue playing.");
    }
    
    IEnumerator TeleportAnimation()
    {
        Vector3 startPos = playerPiece.transform.position;
        Vector3 targetPos;
        
        // Determine target position based on current grid
        if (currentGrid == 1)
        {
            // Teleport to Grid 2, top-left (0, 1)
            currentGrid = 2;
            playerGridPosition = new Vector2Int(0, 1);
            targetPos = GetWorldPosition(2, playerGridPosition);
        }
        else
        {
            // Teleport to Grid 1, bottom-right (1, 0)
            currentGrid = 1;
            playerGridPosition = new Vector2Int(1, 0);
            targetPos = GetWorldPosition(1, playerGridPosition);
        }
        
        // Animate with a spinning effect
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Move player
            playerPiece.transform.position = Vector3.Lerp(startPos, targetPos, progress);
            
            // Spin player
            playerPiece.transform.rotation = Quaternion.Euler(0, 0, progress * 360f);
            
            yield return null;
        }
        
        playerPiece.transform.position = targetPos;
        playerPiece.transform.rotation = Quaternion.identity;
        
        // Update visit count for new position
        movesCount++;
        UpdateVisitCount();
        UpdateUI();
        CheckWinCondition();
    }
    
    void MovePlayer(Vector2Int newPosition)
    {
        movesCount++;
        playerGridPosition = newPosition;
        
        Vector3 targetWorldPos = GetWorldPosition(currentGrid, playerGridPosition);
        StartCoroutine(AnimateMove(targetWorldPos));
        
        UpdateVisitCount();
        UpdateUI();
        CheckWinCondition();
    }
    
    IEnumerator AnimateMove(Vector3 targetPosition)
    {
        Vector3 startPos = playerPiece.transform.position;
        float duration = 0.2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            playerPiece.transform.position = Vector3.Lerp(startPos, targetPosition, progress);
            yield return null;
        }
        
        playerPiece.transform.position = targetPosition;
    }
    
    void UpdateVisitCount()
    {
        if (currentGrid == 1)
        {
            visitCountGrid1[playerGridPosition.x, playerGridPosition.y]++;
            UpdateCellVisual(gridCells1[playerGridPosition.x, playerGridPosition.y], 
                            visitCountGrid1[playerGridPosition.x, playerGridPosition.y]);
        }
        else
        {
            visitCountGrid2[playerGridPosition.x, playerGridPosition.y]++;
            UpdateCellVisual(gridCells2[playerGridPosition.x, playerGridPosition.y], 
                            visitCountGrid2[playerGridPosition.x, playerGridPosition.y]);
        }
    }
    
    Vector3 GetWorldPosition(int gridNumber, Vector2Int gridPos)
    {
        Vector3 offset = (gridNumber == 1) ? grid1Offset : grid2Offset;
        
        return new Vector3(
            offset.x + gridPos.x * cellSize,
            offset.y + gridPos.y * cellSize,
            -0.1f  // Slightly in front of grid
        );
    }
    
    void UpdateCellVisual(GameObject cell, int visits)
    {
        if (cell != null)
        {
            SpriteRenderer spriteRenderer = cell.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = GetHeatMapColor(visits);
            }
        }
    }
    
    Color GetHeatMapColor(int visits)
    {
        if (visits == 0)
            return Color.white;
        else if (visits == 1)
            return Color.yellow;
        else if (visits == 2)
            return new Color(1f, 0.5f, 0f, 1f);  // Orange
        else if (visits == 3)
            return new Color(1f, 0.2f, 0f, 1f);  // Red-orange
        else
            return Color.red;
    }
    
    void UpdateUI()
    {
        if (movesText != null)
            movesText.text = "MOVES: " + movesCount;
    }
    
    void CheckWinCondition()
    {
        bool allVisited = true;
        
        // Check Grid 1
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                if (visitCountGrid1[x, y] == 0)
                {
                    allVisited = false;
                    break;
                }
            }
            if (!allVisited) break;
        }
        
        // Check Grid 2
        if (allVisited)
        {
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    if (visitCountGrid2[x, y] == 0)
                    {
                        allVisited = false;
                        break;
                    }
                }
                if (!allVisited) break;
            }
        }
        
        if (allVisited)
        {
            WinLevel();
        }
    }
    
    void WinLevel()
    {
        gameCompleted = true;
        Debug.Log("TELEPORTATION LEVEL COMPLETE!");
        Debug.Log($"You filled both grids in {movesCount} moves!");
        Debug.Log("Press R to try again, or return to menu.");
        
        // Show completion message
        StartCoroutine(ShowCompletionMessage());
    }
    
    IEnumerator ShowCompletionMessage()
    {
        yield return new WaitForSeconds(0.5f);
        
        // Create completion popup
        GameObject completionPanel = new GameObject("CompletionPanel");
        completionPanel.transform.SetParent(uiCanvas.transform, false);
        
        Image panelBG = completionPanel.AddComponent<Image>();
        panelBG.color = new Color(0, 0.5f, 0, 0.9f); // Green
        
        RectTransform panelRect = completionPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(500, 250);
        
        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(completionPanel.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "🎉 LEVEL COMPLETE! 🎉";
        titleText.fontSize = 32;
        titleText.color = Color.yellow;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.6f);
        titleRect.anchorMax = new Vector2(1, 0.9f);
        titleRect.offsetMin = new Vector2(20, 0);
        titleRect.offsetMax = new Vector2(-20, 0);
        
        // Stats text
        GameObject statsObj = new GameObject("Stats");
        statsObj.transform.SetParent(completionPanel.transform, false);
        TextMeshProUGUI statsText = statsObj.AddComponent<TextMeshProUGUI>();
        statsText.text = $"You filled both grids in {movesCount} moves!\n\nPress R to play again!";
        statsText.fontSize = 20;
        statsText.color = Color.white;
        statsText.alignment = TextAlignmentOptions.Center;
        
        RectTransform statsRect = statsObj.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0, 0.2f);
        statsRect.anchorMax = new Vector2(1, 0.6f);
        statsRect.offsetMin = new Vector2(20, 0);
        statsRect.offsetMax = new Vector2(-20, 0);
    }
    
    public void ResetLevel()
    {
        Debug.Log("Resetting Teleportation Level...");
        
        // Close any popups
        if (educationalPanel != null)
        {
            Destroy(educationalPanel);
            educationalPanel = null;
        }
        
        // Destroy all existing objects
        GameObject grid1 = GameObject.Find("Grid1_TopLeft");
        GameObject grid2 = GameObject.Find("Grid2_BottomRight");
        GameObject completionPanel = GameObject.Find("CompletionPanel");
        
        if (grid1 != null) Destroy(grid1);
        if (grid2 != null) Destroy(grid2);
        if (playerPiece != null) Destroy(playerPiece);
        if (teleportPad1 != null) Destroy(teleportPad1);
        if (teleportPad2 != null) Destroy(teleportPad2);
        if (completionPanel != null) Destroy(completionPanel);
        
        // Reset state
        movesCount = 0;
        gameCompleted = false;
        hasSeenEducationalPopup = false;
        waitingForContinue = false;
        
        // Restart level
        StartTeleportationLevel();
    }
}