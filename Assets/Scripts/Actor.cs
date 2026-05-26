using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(Exploder2D))]
public class Actor : MonoBehaviour
{
    [Header("Stats")]
    [Range(0, 100)] public int Health = 3;
    [Header("Battle")]
    public string[] DamageSourcesTags = new[] { "Damage" };
    [Min(0)] public float DamageInvincibilitySeconds = 0.5f;
    public bool IgnoreEnemyContactDamageWhileAscending;
    public float AscendingDamageIgnoreVelocity = 0.05f;
    [Header("Animator")]
    public string DamagedAnimStateName = "Actor_Damaged";
    [Header("Audio")]
    public AudioClip HitSoundFX;

    private Animator _animator;
    private Exploder2D _exploder;
    private Rigidbody2D _rigidbody2D;
    private bool _invincible;


    private void Awake()
    {   
        _animator = GetComponent<Animator>();
        _exploder = GetComponent<Exploder2D>();
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (CanBeDamagedBy(collision.gameObject))
            TakeDamage(collision.gameObject);
    }

    private void OnTriggerStay2D(Collider2D collider)
    {
        if (CanBeDamagedBy(collider.gameObject))
            TakeDamage(collider.gameObject);
    }

    public bool CanBeDamagedBy(GameObject source)
    {
        if (source == null) return false;

        foreach (string damageSourceTag in DamageSourcesTags)
        {
            if (source.CompareTag(damageSourceTag) && CanTakeDamageFrom(source))
                return true;
        }

        return false;
    }

    private bool CanTakeDamageFrom(GameObject source)
    {
        if (!IgnoreEnemyContactDamageWhileAscending) return true;
        if (_rigidbody2D == null || _rigidbody2D.linearVelocity.y <= AscendingDamageIgnoreVelocity) return true;
        return !source.CompareTag("Enemy");
    }

    public void TakeDamage(GameObject source, int amount = 1)
    {
        if (_invincible || Health <= 0) return;

        if (PlayerLocator.IsMainPlayer(gameObject) && source != null)
            Debug.LogWarning($"Player damaged by \"{source.name}\" with tag \"{source.tag}\" at {source.transform.position}.");

        _invincible = true;
        StartCoroutine(Damage(source, amount));
    }

    private IEnumerator Damage(GameObject _, int amount = 1)
    {
        Health -= Mathf.Max(1, amount);
        if (HitSoundFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySoundFX(HitSoundFX);
        if (_animator != null && !string.IsNullOrEmpty(DamagedAnimStateName))
            _animator.Play(DamagedAnimStateName);
        if (Health <= 0)
        {
            Health = 0;
            if (PlayerLocator.IsMainPlayer(gameObject))
            {
                if (GameOverController.Instance != null)
                    GameOverController.Instance.Show("TRY AGAIN", "The crystal is still waiting for you.");
                yield break;
            }
            if (_exploder != null)
                _exploder.Explode();
            yield return null;
        }
        yield return new WaitForSeconds(DamageInvincibilitySeconds);
        _invincible = false;
    }
}
