using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverController : MonoBehaviour
{
    [Header("Audio")]
    public bool PlayGeneratedLoseTone = true;

    private static GameOverController _instance;
    private Canvas _canvas;
    private AudioSource _audioSource;
    private bool _shown;

    public static GameOverController Instance
    {
        get
        {
            if (_instance != null) return _instance;
            _instance = FindObjectOfType<GameOverController>();
            if (_instance != null) return _instance;

            GameObject go = new GameObject("KayipKristal_GameOverController");
            _instance = go.AddComponent<GameOverController>();
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.ignoreListenerPause = true;
    }

    private void Update()
    {
        if (!_shown) return;

        if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            Retry();
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.M))
            ReturnToMainMenu();
    }

    public void Show(string title = "TRY AGAIN", string message = "The danger caught you.")
    {
        if (_shown) return;

        _shown = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        EnsureEventSystem();
        BuildCanvas(title, message);
        if (PlayGeneratedLoseTone) PlayLoseTone();
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMainMenu()
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

    private void BuildCanvas(string title, string message)
    {
        GameObject canvasGo = new GameObject("KayipKristal_GameOverCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform root = canvasGo.GetComponent<RectTransform>();
        CreatePanel(root);
        CreateText(root, "Title", title, 84, new Vector2(0f, 104f), FontStyles.Bold);
        CreateText(root, "Message", message, 28, new Vector2(0f, 36f), FontStyles.Normal);

        Button retryButton = CreateButton(root, "Retry", new Vector2(-160f, -72f));
        retryButton.onClick.AddListener(Retry);

        Button menuButton = CreateButton(root, "Main Menu", new Vector2(160f, -72f));
        menuButton.onClick.AddListener(ReturnToMainMenu);
    }

    private void CreatePanel(RectTransform root)
    {
        GameObject panelGo = new GameObject("Dimmed_Background", typeof(Image));
        panelGo.transform.SetParent(root, false);
        RectTransform rect = panelGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panelGo.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.68f);
    }

    private TextMeshProUGUI CreateText(RectTransform root, string name, string text, int size, Vector2 anchoredPosition, FontStyles style)
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
        label.color = name == "Title" ? new Color(1f, 0.16f, 0.12f, 1f) : new Color(0.95f, 0.88f, 0.78f, 1f);
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
        image.color = new Color(0.14f, 0.12f, 0.1f, 0.96f);

        Button button = buttonGo.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.34f, 0.22f, 0.16f, 1f);
        colors.pressedColor = new Color(0.56f, 0.18f, 0.12f, 1f);
        button.colors = colors;

        TextMeshProUGUI label = CreateText(rect, "Label", text, 26, Vector2.zero, FontStyles.Bold);
        label.rectTransform.sizeDelta = rect.sizeDelta;
        label.color = new Color(1f, 0.9f, 0.78f, 1f);
        return button;
    }

    private void PlayLoseTone()
    {
        const int sampleRate = 44100;
        const float duration = 0.85f;
        int samples = Mathf.RoundToInt(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float frequency = Mathf.Lerp(180f, 62f, t / duration);
            float envelope = Mathf.Clamp01(1f - t / duration);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.32f;
        }

        AudioClip clip = AudioClip.Create("KayipKristal_LoseTone", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        _audioSource.PlayOneShot(clip);
    }
}
