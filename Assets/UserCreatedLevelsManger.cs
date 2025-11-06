using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;

public class UserCreatedLevelsManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject userCreatedLevelsPanel;
    public GameObject mainMenuPanel;
    public Canvas uiCanvas;

    [Header("Level Player")]
    // Optional: if you later make a prefab for the runtime player UI, you can assign it here.
    public GameObject customLevelPlayerPrefab;

    // UI Elements we either find or create at runtime
    private GameObject levelListPanel;
    private GameObject scrollViewContent;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI noLevelsText;
    private GameObject backButton;
    private readonly List<GameObject> levelButtons = new List<GameObject>();

    // Gameplay UI
    private GameObject gameplayPanel;
    private GameObject goBackButton;
    private CustomLevelPlayer activeLevelPlayer;

    void Start()
    {
        // Intentionally empty. We initialize when the panel opens.
    }

    public void InitializeUserLevelsMenu()
    {
        Debug.Log("Initializing User Created Levels Menu...");

        if (gameplayPanel != null) gameplayPanel.SetActive(false);

        if (scrollViewContent == null) FindOrCreateUIElements();

        LoadAllLevels();
    }

    // ————————————————— UI creation / hookup —————————————————
    void FindOrCreateUIElements()
    {
        if (uiCanvas == null) uiCanvas = FindObjectOfType<Canvas>();

        if (userCreatedLevelsPanel == null)
        {
            Debug.LogError("User Created Levels Panel not found!");
            return;
        }

        Transform panel = userCreatedLevelsPanel.transform;

        Transform titleTr = panel.Find("TitleText");
        Transform scrollTr = panel.Find("LevelScrollView");
        Transform backTr  = panel.Find("BackButton");

        if (titleTr == null || scrollTr == null)
        {
            CreateCompleteUI();
        }
        else
        {
            ConnectToExistingUI(titleTr, scrollTr, backTr);
        }
    }

    void ConnectToExistingUI(Transform titleTr, Transform scrollTr, Transform backTr)
    {
        if (titleTr != null) titleText = titleTr.GetComponent<TextMeshProUGUI>();

        if (scrollTr != null)
        {
            levelListPanel = scrollTr.gameObject;
            Transform viewport = scrollTr.Find("Viewport");
            if (viewport != null)
            {
                Transform content = viewport.Find("Content");
                if (content != null) scrollViewContent = content.gameObject;
            }
        }

        if (backTr != null)
        {
            backButton = backTr.gameObject;
            Button b = backButton.GetComponent<Button>();
            if (b != null)
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(ReturnToMainMenu);
            }
        }

        Transform noLevelsTr = userCreatedLevelsPanel.transform.Find("NoLevelsMessage");
        if (noLevelsTr != null) noLevelsText = noLevelsTr.GetComponent<TextMeshProUGUI>();
        else CreateNoLevelsMessage();
    }

    void CreateCompleteUI()
    {
        CreateTitleText();
        CreateScrollView();
        CreateNoLevelsMessage();
        CreateBackButton();
    }

    void CreateTitleText()
    {
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(userCreatedLevelsPanel.transform, false);
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "YOUR CUSTOM LEVELS";
        titleText.fontSize = 32;
        titleText.color = Color.yellow;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;
        RectTransform r = titleObj.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0, 0.9f);
        r.anchorMax = new Vector2(1, 0.98f);
        r.offsetMin = new Vector2(20, 0);
        r.offsetMax = new Vector2(-20, 0);
    }

    void CreateScrollView()
    {
        GameObject scroll = new GameObject("LevelScrollView");
        scroll.transform.SetParent(userCreatedLevelsPanel.transform, false);
        Image bg = scroll.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        RectTransform sr = scroll.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0.15f, 0.2f);
        sr.anchorMax = new Vector2(0.85f, 0.85f);

        ScrollRect sc = scroll.AddComponent<ScrollRect>();
        sc.horizontal = false;

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scroll.transform, false);
        RectTransform vpr = viewport.AddComponent<RectTransform>();
        vpr.anchorMin = Vector2.zero; vpr.anchorMax = Vector2.one;
        vpr.offsetMin = new Vector2(10, 10); vpr.offsetMax = new Vector2(-10, -10);
        Image vImg = viewport.AddComponent<Image>();
        vImg.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        Mask mask = viewport.AddComponent<Mask>(); mask.showMaskGraphic = false;

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform cr = content.AddComponent<RectTransform>();
        cr.anchorMin = new Vector2(0, 1); cr.anchorMax = new Vector2(1, 1);
        cr.pivot = new Vector2(0.5f, 1); cr.sizeDelta = new Vector2(0, 600);

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true; vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        ContentSizeFitter fit = content.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sc.viewport = vpr;
        sc.content  = cr;

        scrollViewContent = content;
        levelListPanel = scroll;
    }

    void CreateNoLevelsMessage()
    {
        GameObject nl = new GameObject("NoLevelsMessage");
        nl.transform.SetParent(userCreatedLevelsPanel.transform, false);
        noLevelsText = nl.AddComponent<TextMeshProUGUI>();
        noLevelsText.text = "No custom levels yet!\n\nCreate your first level in the Level Creator!";
        noLevelsText.fontSize = 24;
        noLevelsText.color = new Color(1, 1, 1, 0.6f);
        noLevelsText.alignment = TextAlignmentOptions.Center;
        noLevelsText.fontStyle = FontStyles.Italic;
        RectTransform r = nl.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.2f, 0.4f);
        r.anchorMax = new Vector2(0.8f, 0.6f);
        noLevelsText.gameObject.SetActive(false);
    }

    void CreateBackButton()
    {
        GameObject back = new GameObject("BackButton");
        back.transform.SetParent(userCreatedLevelsPanel.transform, false);
        Button b = back.AddComponent<Button>();
        Image bg = back.AddComponent<Image>();
        bg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        RectTransform r = back.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.35f, 0.05f);
        r.anchorMax = new Vector2(0.65f, 0.15f);
        GameObject txt = new GameObject("Text");
        txt.transform.SetParent(back.transform, false);
        TextMeshProUGUI t = txt.AddComponent<TextMeshProUGUI>();
        t.text = "BACK TO MENU";
        t.fontSize = 24; t.fontStyle = FontStyles.Bold; t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        RectTransform tr = txt.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.sizeDelta = Vector2.zero;
        b.onClick.AddListener(ReturnToMainMenu);
        backButton = back;
    }

    void ReturnToMainMenu()
    {
        if (userCreatedLevelsPanel != null) userCreatedLevelsPanel.SetActive(false);
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null && gm.mainMenuPanel != null) gm.mainMenuPanel.SetActive(true);
    }

    // ————————————————— list population —————————————————
    void LoadAllLevels()
    {
        if (scrollViewContent == null)
        {
            Debug.LogError("ScrollViewContent missing.");
            return;
        }

        ClearLevelButtons();

        string path = Application.persistentDataPath + "/CustomLevels/";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            ShowNoLevelsMessage();
            return;
        }

        string[] files = Directory.GetFiles(path, "*.json");
        if (files.Length == 0)
        {
            ShowNoLevelsMessage();
            return;
        }

        if (noLevelsText != null) noLevelsText.gameObject.SetActive(false);

        foreach (var f in files) CreateLevelButton(f);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollViewContent.GetComponent<RectTransform>());
    }

    void CreateLevelButton(string filePath)
    {
        string json = File.ReadAllText(filePath);
        CustomLevelData levelData = JsonUtility.FromJson<CustomLevelData>(json);
        if (levelData == null) return;
        if (string.IsNullOrEmpty(levelData.levelName))
            levelData.levelName = Path.GetFileNameWithoutExtension(filePath);

        GameObject buttonObj = new GameObject("LevelButton_" + levelData.levelName);
        buttonObj.transform.SetParent(scrollViewContent.transform, false);

        Image bg = buttonObj.AddComponent<Image>(); 
        bg.color = new Color(0.2f, 0.5f, 0.8f, 1f);

        Button btn = buttonObj.AddComponent<Button>(); 
        btn.targetGraphic = bg;

        LayoutElement le = buttonObj.AddComponent<LayoutElement>();
        le.preferredHeight = 70; 
        le.minHeight = 70;

        // wrapper
        GameObject textWrap = new GameObject("TextContainer");
        textWrap.transform.SetParent(buttonObj.transform, false);
        RectTransform twr = textWrap.AddComponent<RectTransform>();
        twr.anchorMin = Vector2.zero; 
        twr.anchorMax = Vector2.one;
        twr.offsetMin = new Vector2(15, 5); 
        twr.offsetMax = new Vector2(-15, -5);

        // Level name
        GameObject nameObj = new GameObject("LevelName");
        nameObj.transform.SetParent(textWrap.transform, false);
        TextMeshProUGUI name = nameObj.AddComponent<TextMeshProUGUI>();
        name.text = levelData.levelName; 
        name.fontSize = 24; 
        name.color = Color.white;
        name.alignment = TextAlignmentOptions.Left; 
        name.fontStyle = FontStyles.Bold; 
        name.raycastTarget = false;
        RectTransform nr = nameObj.GetComponent<RectTransform>();   // <-- GET, don't add
        nr.anchorMin = new Vector2(0, 0.4f); 
        nr.anchorMax = new Vector2(1, 1f); 
        nr.offsetMin = Vector2.zero; 
        nr.offsetMax = Vector2.zero;

        // Shape count
        GameObject infoObj = new GameObject("ShapeCount");
        infoObj.transform.SetParent(textWrap.transform, false);
        TextMeshProUGUI info = infoObj.AddComponent<TextMeshProUGUI>();
        info.text = levelData.shapes != null ? $"{levelData.shapes.Count} shapes" : "0 shapes";
        info.fontSize = 16; 
        info.color = new Color(1, 1, 1, 0.7f); 
        info.alignment = TextAlignmentOptions.Left; 
        info.raycastTarget = false;
        RectTransform ir = infoObj.GetComponent<RectTransform>();   // <-- GET, don't add
        ir.anchorMin = new Vector2(0, 0); 
        ir.anchorMax = new Vector2(1, 0.4f); 
        ir.offsetMin = Vector2.zero; 
        ir.offsetMax = Vector2.zero;

        string capturedPath = filePath;
        btn.onClick.AddListener(() => PlayLevel(capturedPath));

        levelButtons.Add(buttonObj);
    }

    // ————————————————— play level —————————————————
    void PlayLevel(string filePath)
    {
        string json = File.ReadAllText(filePath);
        CustomLevelData levelData = JsonUtility.FromJson<CustomLevelData>(json);
        if (levelData == null) { Debug.LogError("Level JSON parse failed."); return; }

        if (userCreatedLevelsPanel != null) userCreatedLevelsPanel.SetActive(false);
        if (gameplayPanel == null) CreateGameplayUI();
        gameplayPanel.SetActive(true);

        if (activeLevelPlayer != null) { activeLevelPlayer.ClearLevel(); Destroy(activeLevelPlayer.gameObject); }

        GameObject playerGO = new GameObject("CustomLevelPlayer");
        activeLevelPlayer = playerGO.AddComponent<CustomLevelPlayer>();

        // Create a spawn root so shapes have a parent (local positions match your creator grid)
        GameObject spawnRoot = new GameObject("SpawnRoot");
        spawnRoot.transform.SetParent(playerGO.transform, false);
        activeLevelPlayer.InitializeRuntime(spawnRoot.transform);

        // Try to pick prefabs off the LevelCreatorManager EVEN IF IT IS INACTIVE
        LevelCreatorManager creator = FindObjectOfType<LevelCreatorManager>(true);  // includeInactive = true
        if (creator != null)
        {
            activeLevelPlayer.trianglePrefab      = creator.trianglePrefab;
            activeLevelPlayer.rightTrianglePrefab = creator.rightTrianglePrefab;
            activeLevelPlayer.squarePrefab        = creator.squarePrefab;
        }

        // Also try Resources fallback if still nulls (optional)
        if (activeLevelPlayer.trianglePrefab == null)      activeLevelPlayer.trianglePrefab      = Resources.Load<GameObject>("TrianglePrefab");
        if (activeLevelPlayer.rightTrianglePrefab == null) activeLevelPlayer.rightTrianglePrefab = Resources.Load<GameObject>("RightTrianglePrefab");
        if (activeLevelPlayer.squarePrefab == null)        activeLevelPlayer.squarePrefab        = Resources.Load<GameObject>("SquarePrefab");

        activeLevelPlayer.LoadAndStartLevel(levelData);
    }

    void CreateGameplayUI()
    {
        gameplayPanel = new GameObject("GameplayPanel");
        gameplayPanel.transform.SetParent(uiCanvas.transform, false);
        RectTransform pr = gameplayPanel.AddComponent<RectTransform>();
        pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one; pr.sizeDelta = Vector2.zero;

        Image bg = gameplayPanel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.3f);
        bg.raycastTarget = false;

        // Go Back button
        GameObject back = new GameObject("GoBackButton");
        back.transform.SetParent(gameplayPanel.transform, false);
        Button b = back.AddComponent<Button>();
        Image bbg = back.AddComponent<Image>(); bbg.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
        RectTransform br = back.GetComponent<RectTransform>();
        br.anchorMin = new Vector2(0.8f, 0.02f); br.anchorMax = new Vector2(0.98f, 0.12f);
        GameObject txt = new GameObject("Text"); txt.transform.SetParent(back.transform, false);
        TextMeshProUGUI t = txt.AddComponent<TextMeshProUGUI>();
        t.text = "GO BACK"; t.fontSize = 20; t.fontStyle = FontStyles.Bold; t.color = Color.white; t.alignment = TextAlignmentOptions.Center;
        RectTransform tr = txt.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.sizeDelta = Vector2.zero;
        b.onClick.AddListener(ExitLevel);

        goBackButton = back;
        gameplayPanel.SetActive(false);
    }

    void ExitLevel()
    {
        if (activeLevelPlayer != null)
        {
            activeLevelPlayer.ClearLevel();
            Destroy(activeLevelPlayer.gameObject);
            activeLevelPlayer = null;
        }
        if (gameplayPanel != null) gameplayPanel.SetActive(false);
        if (userCreatedLevelsPanel != null) userCreatedLevelsPanel.SetActive(true);
    }

    void ShowNoLevelsMessage()
    {
        if (noLevelsText != null) noLevelsText.gameObject.SetActive(true);
    }

    void ClearLevelButtons()
    {
        foreach (var b in levelButtons) if (b != null) Destroy(b);
        levelButtons.Clear();

        if (scrollViewContent != null)
        {
            // cleanup orphans
            var toDestroy = new List<GameObject>();
            foreach (Transform child in scrollViewContent.transform)
                if (child.name.StartsWith("LevelButton_")) toDestroy.Add(child.gameObject);
            toDestroy.ForEach(Destroy);
        }
    }

    public void RefreshLevelList() => LoadAllLevels();
}
