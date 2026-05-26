using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MissionState : Singleton<MissionState>
{
    [Header("Mission")]
    [Min(1)] public int RequiredCrystals = 5;
    [Min(0)] public int ScorePerCrystal = 10;
    public string WinSceneName = "";

    public int Crystals { get; private set; }
    public int Score { get; private set; }
    public bool IsComplete => Crystals >= RequiredCrystals;
    private bool _finished;
    private int _pendingNextSceneIndex = -1;

    public event Action<int, int, int> Changed;
    public event Action Completed;

    internal override void Awake()
    {
        base.Awake();
        ResetMission();
    }

    public void ResetMission()
    {
        _finished = false;
        Time.timeScale = 1f;
        Crystals = 0;
        Score = 0;
        Changed?.Invoke(Crystals, RequiredCrystals, Score);
    }

    public void AddCrystal(int value = 1)
    {
        if (value <= 0) value = 1;

        bool wasComplete = IsComplete;
        Crystals += value;
        Score += ScorePerCrystal * value;
        Changed?.Invoke(Crystals, RequiredCrystals, Score);

        if (!wasComplete && IsComplete)
            Completed?.Invoke();
    }

    public void TryFinishLevel()
    {
        if (_finished) return;
        if (!IsComplete) return;
        _finished = true;

        if (!string.IsNullOrWhiteSpace(WinSceneName))
        {
            SceneManager.LoadScene(WinSceneName);
            return;
        }

        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            ShowTransitionScreen(nextSceneIndex);
            return;
        }

        ShowChampionScreen();
    }

    private void Update()
    {
        if (_pendingNextSceneIndex < 0) return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            LoadPendingLevel();
    }

    private void ShowTransitionScreen(int nextSceneIndex)
    {
        _pendingNextSceneIndex = nextSceneIndex;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        EnsureEventSystem();

        RectTransform panelRect = CreateOverlayRoot("LevelTransitionPanel", new Color(0f, 0f, 0f, 0.82f));
        int nextLevel = Mathf.Max(1, nextSceneIndex);
        string title = nextLevel == 2 ? "Blue Ruins Unlocked" : "Crimson Keep Unlocked";
        string body = nextLevel == 2
            ? "The next castle gate opens into the blue ruins.\nStep through to begin Level 2."
            : "The final castle gate burns red ahead.\nStep through to begin Level 3.";

        CreateText(panelRect, "TransitionTitle", title, 50, new Vector2(0f, 140f), FontStyles.Bold, new Color(1f, 0.82f, 0.28f, 1f));
        CreateText(panelRect, "TransitionBody", body, 28, new Vector2(0f, -22f), FontStyles.Normal, new Color(0.94f, 0.9f, 0.82f, 1f));
        Button enterButton = CreateButton(panelRect, "Enter", new Vector2(0f, -142f));
        enterButton.onClick.AddListener(LoadPendingLevel);
    }

    private void ShowChampionScreen()
    {
        Time.timeScale = 0f;
        AudioListener.pause = true;
        EnsureEventSystem();

        RectTransform panelRect = CreateOverlayRoot("WinPanel", new Color(0f, 0f, 0f, 0.82f));

        CreateText(panelRect, "WinTitle", "VICTORY!\nThe Lost Crystal is restored", 52, new Vector2(0f, 150f), FontStyles.Bold, new Color(1f, 0.82f, 0.28f, 1f));
        CreateText(panelRect, "WinSummary", $"All 3 levels cleared.\nFinal score: {Score}", 30, new Vector2(0f, 2f), FontStyles.Normal, new Color(0.95f, 0.9f, 0.82f, 1f));
        StartCoroutine(PlayFireworks(panelRect));

        Button retryButton = CreateButton(panelRect, "Play Again", new Vector2(-160f, -120f));
        retryButton.onClick.AddListener(RestartLevel);

        Button menuButton = CreateButton(panelRect, "Main Menu", new Vector2(160f, -120f));
        menuButton.onClick.AddListener(ReturnToMainMenu);
    }

    private void RestartLevel()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(0);
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.hideFlags = HideFlags.DontSaveInBuild;
    }

    private void LoadPendingLevel()
    {
        if (_pendingNextSceneIndex < 0) return;

        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(_pendingNextSceneIndex);
    }

    private RectTransform CreateOverlayRoot(string panelName, Color background)
    {
        Canvas existingCanvas = FindObjectOfType<Canvas>();
        GameObject canvasGo = existingCanvas != null
            ? existingCanvas.gameObject
            : new GameObject("KayipKristal_WinCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        Transform existingPanel = canvasGo.transform.Find(panelName);
        if (existingPanel != null)
            Destroy(existingPanel.gameObject);

        GameObject panel = new GameObject(panelName, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasGo.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = background;
        return panelRect;
    }

    private Image CreateImage(RectTransform root, string name, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(Image));
        go.transform.SetParent(root, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private TextMeshProUGUI CreateText(RectTransform root, string name, string text, int size, Vector2 anchoredPosition, FontStyles style, Color color)
    {
        GameObject textGo = new GameObject(name, typeof(TextMeshProUGUI));
        textGo.transform.SetParent(root, false);
        RectTransform rect = textGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(900f, 110f);
        rect.anchoredPosition = anchoredPosition;

        TextMeshProUGUI label = textGo.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.enableWordWrapping = true;
        return label;
    }

    private Button CreateButton(RectTransform root, string text, Vector2 anchoredPosition)
    {
        GameObject buttonGo = new GameObject(text, typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(root, false);
        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(250f, 58f);
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonGo.GetComponent<Image>();
        image.color = new Color(0.16f, 0.14f, 0.12f, 0.96f);

        Button button = buttonGo.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.34f, 0.25f, 0.15f, 1f);
        colors.pressedColor = new Color(0.52f, 0.34f, 0.12f, 1f);
        button.colors = colors;

        TextMeshProUGUI label = CreateText(rect, "Label", text, 24, Vector2.zero, FontStyles.Bold, new Color(1f, 0.92f, 0.78f, 1f));
        label.rectTransform.sizeDelta = rect.sizeDelta;
        return button;
    }

    private IEnumerator PlayFireworks(RectTransform root)
    {
        Vector2[] burstPositions =
        {
            new(-420f, 210f),
            new(360f, 240f),
            new(0f, 290f),
            new(-220f, 90f),
            new(260f, 80f)
        };

        int burst = 0;
        while (burst < 12)
        {
            SpawnFireworkBurst(root, burstPositions[burst % burstPositions.Length]);
            burst++;
            yield return new WaitForSecondsRealtime(0.38f);
        }
    }

    private void SpawnFireworkBurst(RectTransform root, Vector2 center)
    {
        Color[] colors =
        {
            new(1f, 0.76f, 0.24f, 1f),
            new(0.36f, 0.88f, 1f, 1f),
            new(0.72f, 1f, 0.46f, 1f),
            new(1f, 0.42f, 0.34f, 1f)
        };

        for (int i = 0; i < 24; i++)
        {
            float angle = i * Mathf.PI * 2f / 24f;
            Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
            float distance = UnityEngine.Random.Range(70f, 150f);
            Color color = colors[UnityEngine.Random.Range(0, colors.Length)];
            StartCoroutine(AnimateFireworkParticle(root, center, direction * distance, color));
        }
    }

    private IEnumerator AnimateFireworkParticle(RectTransform root, Vector2 center, Vector2 offset, Color color)
    {
        GameObject go = new("FireworkSpark", typeof(Image));
        go.transform.SetParent(root, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(10f, 10f);
        rect.anchoredPosition = center;
        rect.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 45f));

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        const float duration = 0.82f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rect.anchoredPosition = Vector2.Lerp(center, center + offset, 1f - Mathf.Pow(1f - t, 2f));
            rect.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.2f, t);
            image.color = new Color(color.r, color.g, color.b, 1f - t);
            yield return null;
        }

        Destroy(go);
    }
}
