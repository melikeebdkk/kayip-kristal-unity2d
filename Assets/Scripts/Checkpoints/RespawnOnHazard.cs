using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RespawnOnHazard : MonoBehaviour
{
    [Header("Respawn")]
    public float RespawnYOffset = 0.25f;
    public string[] HazardTags = new[] { "Damage" };
    public bool ShowGameOverInsteadOfRespawn;
    public bool IgnoreHazardsWhileAscending = true;
    public float AscendingVelocityThreshold = 0.05f;
    [Header("Audio")]
    public AudioClip RespawnSoundFX;

    private Rigidbody2D _rigidbody2D;
    private Vector3 _startPosition;

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _startPosition = transform.position;
        PlayerRespawnPoint.Set(_startPosition);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsHazard(collision.gameObject)) Respawn();
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (IsHazard(collider.gameObject)) Respawn();
    }

    private bool IsHazard(GameObject target)
    {
        if (IgnoreHazardsWhileAscending && _rigidbody2D.linearVelocity.y > AscendingVelocityThreshold)
            return false;

        foreach (string hazardTag in HazardTags)
            if (target.CompareTag(hazardTag)) return true;

        return false;
    }

    private void Respawn()
    {
        if (ShowGameOverInsteadOfRespawn)
        {
            if (GameOverController.Instance != null)
                GameOverController.Instance.Show("TRY AGAIN", "Careful. The path is dangerous.");
            return;
        }

        if (RespawnSoundFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySoundFX(RespawnSoundFX);

        Vector3 targetPosition = PlayerRespawnPoint.Resolve(_startPosition);
        targetPosition.y += RespawnYOffset;
        transform.position = targetPosition;
        _rigidbody2D.linearVelocity = Vector2.zero;
        _rigidbody2D.angularVelocity = 0f;
    }
}
