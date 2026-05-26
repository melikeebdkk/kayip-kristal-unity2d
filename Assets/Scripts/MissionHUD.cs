using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MissionHUD : MonoBehaviour
{
    [Header("Text")]
    public TMP_Text CrystalText;
    public TMP_Text ScoreText;
    public TMP_Text HealthText;
    public TMP_Text ObjectiveText;
    public TMP_Text ExitHintText;
    public TMP_Text ToastText;

    [Header("Optional Bar")]
    public Slider CrystalProgress;
    private Actor _playerActor;
    private float _toastUntil;

    private void OnEnable()
    {
        if (MissionState.Instance == null) return;

        _playerActor = PlayerLocator.FindMainActor();
        MissionState.Instance.Changed += UpdateView;
        MissionState.Instance.Completed += ShowCompleted;
        UpdateView(MissionState.Instance.Crystals, MissionState.Instance.RequiredCrystals, MissionState.Instance.Score);
    }

    private void Update()
    {
        if (HealthText != null && _playerActor != null)
            HealthText.text = $"Health: {_playerActor.Health}";

        if (ToastText != null && ToastText.gameObject.activeSelf && Time.unscaledTime > _toastUntil)
            ToastText.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (MissionState.Instance == null) return;

        MissionState.Instance.Changed -= UpdateView;
        MissionState.Instance.Completed -= ShowCompleted;
    }

    private void UpdateView(int crystals, int requiredCrystals, int score)
    {
        if (CrystalText != null)
            CrystalText.text = $"Crystals: {crystals}/{requiredCrystals}";
        if (ScoreText != null)
            ScoreText.text = $"Score: {score}";
        if (HealthText != null && _playerActor != null)
            HealthText.text = $"Health: {_playerActor.Health}";
        if (ObjectiveText != null)
            ObjectiveText.text = crystals >= requiredCrystals
                ? "The castle gate is open. Enter it to continue."
                : ResolveObjectiveText(requiredCrystals);
        if (ExitHintText != null)
        {
            ExitHintText.text = crystals >= requiredCrystals ? "GATE OPEN ->" : "";
            ExitHintText.gameObject.SetActive(crystals >= requiredCrystals);
        }
        if (CrystalProgress != null)
        {
            CrystalProgress.maxValue = requiredCrystals;
            CrystalProgress.value = crystals;
        }
    }

    private void ShowCompleted()
    {
        if (ObjectiveText != null)
            ObjectiveText.text = "All crystals are collected. Enter the castle gate.";
        if (ExitHintText != null)
        {
            ExitHintText.text = "GATE OPEN ->";
            ExitHintText.gameObject.SetActive(true);
        }
        ShowToast("The castle gate is open. Step inside.");
    }

    private string ResolveObjectiveText(int requiredCrystals)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.Contains("Level2"))
            return $"BLUE RUINS: Collect {requiredCrystals} sapphires, then enter the castle gate.";
        if (sceneName.Contains("Level3"))
            return $"CRIMSON KEEP: Collect {requiredCrystals} rubies, then enter the castle gate.";
        return $"GREEN GROVE: Collect {requiredCrystals} emerald crystals, avoid danger, and reach the castle gate.";
    }

    public void ShowToast(string message, float seconds = 2.5f)
    {
        if (ToastText == null) return;

        ToastText.text = message;
        ToastText.gameObject.SetActive(true);
        _toastUntil = Time.unscaledTime + seconds;
    }
}
