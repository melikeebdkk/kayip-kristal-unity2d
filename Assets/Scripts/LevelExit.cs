using UnityEngine;

public class LevelExit : MonoBehaviour
{
    public AudioClip LockedSoundFX;
    public AudioClip OpenSoundFX;

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (!collider.CompareTag("Player")) return;

        if (MissionState.Instance == null || !MissionState.Instance.IsComplete)
        {
            if (LockedSoundFX != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySoundFX(LockedSoundFX);
            return;
        }

        if (OpenSoundFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySoundFX(OpenSoundFX);

        MissionState.Instance.TryFinishLevel();
    }
}
