using UnityEngine;

public class Checkpoint2D : MonoBehaviour
{
    public AudioClip ActivationSoundFX;

    private bool _activated;

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (_activated || !collider.CompareTag("Player")) return;

        _activated = true;
        PlayerRespawnPoint.Set(collider.transform.position);

        if (ActivationSoundFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySoundFX(ActivationSoundFX);

        MissionHUD hud = FindObjectOfType<MissionHUD>();
        if (hud != null)
            hud.ShowToast("Checkpoint kaydedildi.");
    }
}
