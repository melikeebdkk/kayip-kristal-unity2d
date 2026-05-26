using UnityEngine;

public class Collectable : MonoBehaviour
{
    [Min(1)] public int CrystalValue = 1;
    public float AttractDistance = 1.6f;
    public float AttractSpeed = 8f;
    public AudioClip CollectingSoundFX;

    private Transform _player;

    private void Update()
    {
        if (_player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                _player = playerObject.transform;
        }

        if (_player == null) return;

        float distance = Vector2.Distance(transform.position, _player.position);
        if (distance <= AttractDistance)
            transform.position = Vector2.MoveTowards(transform.position, _player.position, AttractSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySoundFX(CollectingSoundFX);
            if (MissionState.Instance != null)
                MissionState.Instance.AddCrystal(CrystalValue);
            Destroy(gameObject);
        }
    }
}
