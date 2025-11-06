using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CubeTransitionManager : MonoBehaviour
{
    [Header("3D Cube Settings")]
    public GameObject unitCubePrefab;
    public Transform cubeParent;
    public float cubeSize = 1f;
    public float animationSpeed = 1f;
    
    [Header("Camera")]
    public Camera mainCamera;
    
    [Header("Materials")]
    public Material wireframeMaterial;  // For showing the outline first
    
    [Header("UI Canvas (will find automatically if not assigned)")]
    public Canvas uiCanvas;
    
    // 3D cube structure - 3x3x3 = 27 cubes
    private GameObject[,,] cubeGrid = new GameObject[3, 3, 3];
    private GameObject[,,] wireframeGrid = new GameObject[3, 3, 3];
    private bool transitionActive = false;
    
    // UI Elements for educational content
    private GameObject educationalPanel;
    private TextMeshProUGUI mainText;
    private TextMeshProUGUI counterText;
    private Button continueButton;
    private int cubesFilled = 0;
    
    void Start()
    {
        // Create a parent object for organizing the 3D cubes
        if (cubeParent == null)
        {
            GameObject parent = new GameObject("3D_Cube_Structure");
            cubeParent = parent.transform;
        }
        
        // Find main camera if not assigned
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }
    
    public void StartCubeTransition()
    {
        if (transitionActive) return;
        
        transitionActive = true;
        Debug.Log("Starting 2D to 3D cube transition...");
        
        // Start the transition sequence
        StartCoroutine(TransitionSequence());
    }
    
    IEnumerator TransitionSequence()
    {
        // Step 1: Camera transition to 3D view
        yield return StartCoroutine(TransitionCameraTo3D());
        
        // Step 2: Show wireframe outline of the full 3x3x3 structure
        yield return StartCoroutine(ShowWireframeCube());
        
        // Step 3: Educational pause and volume prediction
        yield return StartCoroutine(EducationalPause_VolumePrediction());
        
        // Step 4: Fill the cube with animation and live counter
        yield return StartCoroutine(FillCubeWithEducation());
        
        // Step 5: Final educational summary
        yield return StartCoroutine(EducationalSummary());
        
        transitionActive = false;
    }
    
    IEnumerator TransitionCameraTo3D()
    {
        Debug.Log("Transitioning camera to 3D view...");
        
        // Store original camera settings
        Vector3 originalPosition = mainCamera.transform.position;
        Quaternion originalRotation = mainCamera.transform.rotation;
        bool wasOrthographic = mainCamera.orthographic;
        
        // Target 3D camera settings (positioned to show cube more to the right)
        Vector3 targetPosition = new Vector3(2f, 3f, -5f);
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(0.5f, 0, 0) - targetPosition, Vector3.up);
        
        float duration = 2f;
        float elapsed = 0f;
        
        // Animate camera transition
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Smooth camera movement
            mainCamera.transform.position = Vector3.Lerp(originalPosition, targetPosition, progress);
            mainCamera.transform.rotation = Quaternion.Lerp(originalRotation, targetRotation, progress);
            
            // Gradually switch from orthographic to perspective
            if (progress > 0.5f && wasOrthographic)
            {
                mainCamera.orthographic = false;
                mainCamera.fieldOfView = 60f;
            }
            
            yield return null;
        }
        
        // Ensure final position
        mainCamera.transform.position = targetPosition;
        mainCamera.transform.rotation = targetRotation;
        mainCamera.orthographic = false;
        
        Debug.Log("Camera transition complete!");
    }
    
    void CreateEmptyCubeStructure()
    {
        Debug.Log("Creating empty 3D cube structure...");
        
        // We'll create wireframe or transparent placeholder cubes first
        // For now, let's just prepare the positions without visible cubes
        // This step prepares the grid positions for the filling animation
        
        Debug.Log("3D structure positions prepared");
    }
    
    IEnumerator ShowWireframeCube()
    {
        Debug.Log("Showing wireframe cube structure...");
        
        // Create wireframe outline for all 27 positions
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                for (int z = 0; z < 3; z++)
                {
                    Vector3 cubePosition = new Vector3(
                        (x - 1) * cubeSize,
                        (y - 1) * cubeSize,
                        (z - 1) * cubeSize
                    );
                    
                    // Create a wireframe cube (just edges)
                    GameObject wireframeCube = CreateWireframeCube(cubePosition);
                    wireframeGrid[x, y, z] = wireframeCube;
                    
                    // Small delay for visual effect
                    yield return new WaitForSeconds(0.05f);
                }
            }
        }
        
        yield return new WaitForSeconds(1f); // Let user see the wireframe
        Debug.Log("Wireframe structure complete!");
    }
    
    GameObject CreateWireframeCube(Vector3 position)
    {
        // Create a wireframe version of your UnitCube prefab
        GameObject wireframe = Instantiate(unitCubePrefab, position, Quaternion.identity, cubeParent);
        wireframe.name = "WireframeCube";
        
        // Make it semi-transparent white for wireframe effect
        Renderer renderer = wireframe.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material wireframeMat = new Material(renderer.material);
            wireframeMat.color = new Color(1, 1, 1, 0.3f); // Semi-transparent white
            wireframeMat.SetFloat("_Mode", 3); // Transparent mode
            wireframeMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            wireframeMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            wireframeMat.SetInt("_ZWrite", 0);
            wireframeMat.DisableKeyword("_ALPHATEST_ON");
            wireframeMat.EnableKeyword("_ALPHABLEND_ON");
            wireframeMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            wireframeMat.renderQueue = 3000;
            renderer.material = wireframeMat;
        }
        
        // Make it slightly smaller
        wireframe.transform.localScale = Vector3.one * 0.8f;
        
        return wireframe;
    }
    
    void CreateEducationalUI()
    {
        // Find or get the Canvas
        if (uiCanvas == null)
        {
            uiCanvas = FindObjectOfType<Canvas>();
        }
        
        if (uiCanvas == null)
        {
            Debug.LogError("No Canvas found! Cannot create educational UI.");
            return;
        }
        
        // Create main educational panel
        educationalPanel = new GameObject("EducationalPanel");
        educationalPanel.transform.SetParent(uiCanvas.transform, false);
        
        // Add background panel
        Image panelBG = educationalPanel.AddComponent<Image>();
        panelBG.color = new Color(0, 0, 0, 0.8f); // Semi-transparent black
        
        RectTransform panelRect = educationalPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.65f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0.4f);
        panelRect.offsetMin = new Vector2(10, 10);
        panelRect.offsetMax = new Vector2(-10, -10);
        
        // Create main text (make it bigger)
        GameObject mainTextObj = new GameObject("MainText");
        mainTextObj.transform.SetParent(educationalPanel.transform, false);
        mainText = mainTextObj.AddComponent<TextMeshProUGUI>();
        mainText.text = "";
        mainText.fontSize = 24;
        mainText.color = Color.white;
        mainText.alignment = TextAlignmentOptions.TopLeft;
        mainText.fontStyle = FontStyles.Bold;
        
        RectTransform mainTextRect = mainTextObj.GetComponent<RectTransform>();
        mainTextRect.anchorMin = new Vector2(0, 0.3f);
        mainTextRect.anchorMax = new Vector2(1, 1f);
        mainTextRect.offsetMin = new Vector2(15, 10);
        mainTextRect.offsetMax = new Vector2(-15, -10);
        
        // Create counter text (make it bigger)
        GameObject counterTextObj = new GameObject("CounterText");
        counterTextObj.transform.SetParent(educationalPanel.transform, false);
        counterText = counterTextObj.AddComponent<TextMeshProUGUI>();
        counterText.text = "";
        counterText.fontSize = 20;
        counterText.color = Color.yellow;
        counterText.alignment = TextAlignmentOptions.TopLeft;
        counterText.fontStyle = FontStyles.Bold;
        
        RectTransform counterRect = counterTextObj.GetComponent<RectTransform>();
        counterRect.anchorMin = new Vector2(0, 0);
        counterRect.anchorMax = new Vector2(1, 0.4f);
        counterRect.offsetMin = new Vector2(15, 10);
        counterRect.offsetMax = new Vector2(-15, -10);
        
        // Create continue button (smaller and better positioned)
        GameObject buttonObj = new GameObject("ContinueButton");
        buttonObj.transform.SetParent(educationalPanel.transform, false);
        continueButton = buttonObj.AddComponent<Button>();
        
        // Button background
        Image buttonBG = buttonObj.AddComponent<Image>();
        buttonBG.color = new Color(0.2f, 0.6f, 1f, 0.9f); // Semi-transparent blue
        
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.2f, 0.05f);
        buttonRect.anchorMax = new Vector2(0.8f, 0.25f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;
        
        // Button text
        GameObject buttonTextObj = new GameObject("ButtonText");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Continue";
        buttonText.fontSize = 14;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.fontStyle = FontStyles.Bold;
        
        RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;
        
        // Hide initially
        educationalPanel.SetActive(false);
    }
    
    IEnumerator EducationalPause_VolumePrediction()
    {
        CreateEducationalUI();
        educationalPanel.SetActive(true);
        
        // Simple introduction without the math question
        mainText.text = "Here's your 3×3 square transformed into a 3D structure!\n\nLet's count the cubes as they fill in!";
        counterText.text = "";
        continueButton.gameObject.SetActive(true);
        
        // Wait for continue button click
        bool continueClicked = false;
        continueButton.onClick.AddListener(() => continueClicked = true);
        
        while (!continueClicked)
        {
            yield return null;
        }
        
        continueButton.onClick.RemoveAllListeners();
        continueButton.gameObject.SetActive(false);
    }
    
    IEnumerator FillCubeWithEducation()
    {
        Debug.Log("Starting educational cube fill animation...");
        cubesFilled = 0;
        
        // Show the UI panel on the left side
        mainText.text = "Watch the cubes fill up!";
        counterText.text = "Cubes Filled: 0";
        counterText.gameObject.SetActive(true);
        
        // Continuous filling without pauses between layers
        for (int z = 0; z < 3; z++)
        {
            // Update layer text without stopping
            mainText.text = $"Filling Layer {z + 1} of 3...";
            
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    Vector3 cubePosition = new Vector3(
                        (x - 1) * cubeSize,
                        (y - 1) * cubeSize,
                        (z - 1) * cubeSize
                    );
                    
                    // Remove wireframe
                    if (wireframeGrid[x, y, z] != null)
                    {
                        Destroy(wireframeGrid[x, y, z]);
                    }
                    
                    // Create solid cube with proper colors
                    GameObject newCube = CreateOutlinedCube(cubePosition);
                    cubeGrid[x, y, z] = newCube;
                    
                    // Update counter continuously
                    cubesFilled++;
                    counterText.text = $"Cubes Filled: {cubesFilled}";
                    
                    newCube.transform.localScale = Vector3.zero;
                    StartCoroutine(ScaleCube(newCube, Vector3.one * 0.9f));
                    
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }
        
        // Final completion message
        counterText.text = "Cubes Filled: 27 ✓";
        mainText.text = "Complete!\n\nVolume = 3×3×3 = 27 cubic units";
        yield return new WaitForSeconds(3f);
        
        educationalPanel.SetActive(false);
    }
    
    IEnumerator EducationalSummary()
    {
        educationalPanel.SetActive(true);
        counterText.gameObject.SetActive(false);
        continueButton.gameObject.SetActive(true);
        
        mainText.text = "From your simple 2D painting to 27 3D cubes!\n\nVolume = Length × Width × Height\nVolume = 3 × 3 × 3 = 27 cubic units";
        
        // Wait for final continue
        bool continueClicked = false;
        continueButton.onClick.AddListener(() => continueClicked = true);
        
        while (!continueClicked)
        {
            yield return null;
        }
        
        continueButton.onClick.RemoveAllListeners();
        educationalPanel.SetActive(false);
        
        Debug.Log("Educational sequence complete! Press R to reset and try again.");
    }
    
    IEnumerator FillCubeWithAnimation()
    {
        Debug.Log("Starting cube fill animation...");
        
        // Fill cube layer by layer (z = 0, then z = 1, then z = 2)
        for (int z = 0; z < 3; z++)
        {
            Debug.Log($"Filling layer {z + 1} of 3...");
            
            // Fill each position in this layer
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    // Create cube at this position
                    Vector3 cubePosition = new Vector3(
                        (x - 1) * cubeSize, // Center around 0: -1, 0, 1
                        (y - 1) * cubeSize,
                        (z - 1) * cubeSize
                    );
                    
                    // Remove the wireframe cube at this position
                    if (wireframeGrid[x, y, z] != null)
                    {
                        Destroy(wireframeGrid[x, y, z]);
                    }
                    
                    // Instantiate the solid cube with outline
                    GameObject newCube = CreateOutlinedCube(cubePosition);
                    cubeGrid[x, y, z] = newCube;
                    
                    // Start small and scale up for nice effect
                    newCube.transform.localScale = Vector3.zero;
                    StartCoroutine(ScaleCube(newCube, Vector3.one * 0.9f));
                    
                    // Small delay between each cube
                    yield return new WaitForSeconds(0.1f);
                }
            }
            
            // Pause between layers
            yield return new WaitForSeconds(0.5f);
        }
        
        Debug.Log("Cube filling complete! 27 cubes created.");
    }
    
    GameObject CreateOutlinedCube(Vector3 position)
    {
        // Simply instantiate your UnitCube prefab - clean and simple
        GameObject newCube = Instantiate(unitCubePrefab, position, Quaternion.identity, cubeParent);
        newCube.name = "UnitCube";
        
        return newCube;
    }
    
    IEnumerator ScaleCube(GameObject cube, Vector3 targetScale)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Smooth scale animation with slight bounce
            float bounceProgress = Mathf.Sin(progress * Mathf.PI);
            cube.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, bounceProgress);
            
            yield return null;
        }
        
        cube.transform.localScale = targetScale;
    }
    
    void ShowVolumeEducation()
    {
        Debug.Log("=== VOLUME LESSON ===");
        Debug.Log("You just saw a 3x3x3 cube being built!");
        Debug.Log("Volume = 3 × 3 × 3 = 27 cubic units");
        Debug.Log("Surface Area = 6 faces × (3 × 3) = 54 square units");
        Debug.Log("The 2D square you painted was just ONE face of this cube!");
        
        // Here we'll later add actual UI text overlays
    }
    
    public void ResetCubeStructure()
    {
        // Clear all existing cubes
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                for (int z = 0; z < 3; z++)
                {
                    if (cubeGrid[x, y, z] != null)
                    {
                        Destroy(cubeGrid[x, y, z]);
                        cubeGrid[x, y, z] = null;
                    }
                    
                    if (wireframeGrid[x, y, z] != null)
                    {
                        Destroy(wireframeGrid[x, y, z]);
                        wireframeGrid[x, y, z] = null;
                    }
                }
            }
        }
        
        // Reset camera to 2D view
        if (mainCamera != null)
        {
            mainCamera.transform.position = new Vector3(0, 0, -10);
            mainCamera.transform.rotation = Quaternion.identity;
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 5;
        }
        
        transitionActive = false;
        Debug.Log("Cube structure reset to 2D view");
    }
}