using System.Collections;
using UnityEngine;

public class Thrower : MonoBehaviour
{
    [Header("Projectile")]
    public GameObject Projectile;
    public Vector3 ProjectileLocalScale = new(0.5f, 0.5f, 1f);
    public bool OverrideProjectileColor;
    public Color ProjectileColor = Color.white;
    [Min(1)] public int ProjectileActorDamage = 1;
    [Header("Throwing")]
    public Vector3 ThrowingLocalOrigin = new(0f, 0f, 0f);
    public Vector3 ThrowingForce = new(1f, 0f, 0f);
    public bool UsePhysics2D = true;
    public bool AimAtPlayer;
    [Min(0.1f)] public float ProjectileSpeed = 4f;
    [Min(0f)] public float AutoThrowMaxDistance = 7f;
    public bool AutoThrowRequiresLineOfSight = true;
    public bool AutoThrowRequiresFacingTarget = true;
    [Header("Auto-Throw")]
    public bool AutoThrow;
    public float AutoThrowDelaySeconds = 1f;
    [Header("Audio")]
    public AudioClip ThrowingSoundFX;

    private AudioSource _audioSource;
    private bool _autoThrowing;

    public Vector3 ThrowingOrigin => transform.TransformPoint(ThrowingLocalOrigin);
    public Vector3 ScaledThrowingForce => ResolveThrowingForce();

    private void Awake() => _audioSource = GetComponent<AudioSource>();

    private void Update()
    {
        if (!_autoThrowing && AutoThrow) StartCoroutine(AutoThrowProjectile());
    }

    private IEnumerator MoveProjectile(GameObject projectile)
    {
        while (projectile != null)
        {
            projectile.transform.position += 0.01f * ScaledThrowingForce;
            yield return new WaitForFixedUpdate();
        }
    }

    public void MoveProjectileWithPhysics2D(GameObject projectile)
    {
        if (!projectile.TryGetComponent(out Rigidbody2D rigidBody2D))
        {
            Debug.LogError($"Could not find {nameof(Rigidbody2D)} component on projectile \"{projectile.name}\"");
            return;
        }
        rigidBody2D.gravityScale = 0f;
        rigidBody2D.linearVelocity = ScaledThrowingForce * ProjectileSpeed;
    }

    public IEnumerator AutoThrowProjectile()
    {
        _autoThrowing = true;
        while (AutoThrow)
        {
            yield return new WaitForSeconds(AutoThrowDelaySeconds);
            if (CanAutoThrow())
                ThrowProjectile();
        }
        _autoThrowing = false;
    }

    [ContextMenu("Throw Projectile")]
    public void ThrowProjectile()
    {
        if (Projectile == null)
        {
            Debug.LogWarning($"{nameof(Thrower)} on \"{name}\" has no projectile assigned.");
            return;
        }

        GameObject projectile = Instantiate(Projectile, ThrowingOrigin, Projectile.transform.rotation);
        projectile.transform.localScale = ProjectileLocalScale;
        ConfigureProjectile(projectile);
        if (UsePhysics2D) MoveProjectileWithPhysics2D(projectile);
        else StartCoroutine(MoveProjectile(projectile));
        if (ThrowingSoundFX)
        {
            if (AudioManager.Instance == null) return;
            if (_audioSource) AudioManager.Instance.PlaySoundFXLocally(_audioSource, ThrowingSoundFX);
            else AudioManager.Instance.PlaySoundFX(ThrowingSoundFX);
        }
    }

    private Vector3 ResolveThrowingForce()
    {
        if (AimAtPlayer)
        {
            GameObject player = PlayerLocator.FindMainPlayer();
            if (player != null)
            {
                Vector3 direction = player.transform.position + new Vector3(0f, 0.55f, 0f) - ThrowingOrigin;
                direction.z = 0f;
                if (direction.sqrMagnitude > 0.01f)
                    return direction.normalized;
            }
        }

        Vector3 force = ThrowingForce;
        if (transform.localScale.x < 0f)
            force.x *= -1f;
        return force.sqrMagnitude > 0.01f ? force.normalized : Vector3.right;
    }

    private bool CanAutoThrow()
    {
        if (!AimAtPlayer) return true;

        GameObject player = PlayerLocator.FindMainPlayer();
        if (player == null) return false;

        Vector3 target = player.transform.position + new Vector3(0f, 0.55f, 0f);
        Vector2 direction = target - ThrowingOrigin;
        float distance = direction.magnitude;
        if (AutoThrowMaxDistance > 0f && distance > AutoThrowMaxDistance)
            return false;
        if (AutoThrowRequiresFacingTarget && !IsTargetInFacingDirection(direction))
            return false;

        if (!AutoThrowRequiresLineOfSight || distance <= 0.01f)
            return true;

        RaycastHit2D[] hits = Physics2D.RaycastAll(ThrowingOrigin, direction / distance, distance);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger) continue;
            if (hit.collider.GetComponentInParent<Thrower>() == this) continue;
            if (hit.collider.GetComponentInParent<Platformer2DController>() != null) return true;
            return false;
        }

        return true;
    }

    private bool IsTargetInFacingDirection(Vector2 directionToTarget)
    {
        Vector3 facing = ThrowingForce;
        if (transform.localScale.x < 0f)
            facing.x *= -1f;
        if (facing.sqrMagnitude <= 0.01f || directionToTarget.sqrMagnitude <= 0.01f)
            return true;

        return Vector2.Dot(facing.normalized, directionToTarget.normalized) > 0.2f;
    }

    private void ConfigureProjectile(GameObject projectile)
    {
        if (OverrideProjectileColor)
        {
            SpriteRenderer renderer = projectile.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = ProjectileColor;
                renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 35);
            }
        }

        foreach (Collider2D collider in projectile.GetComponents<Collider2D>())
            collider.isTrigger = true;

        Projectile projectileBehaviour = projectile.GetComponent<Projectile>();
        if (projectileBehaviour != null)
        {
            projectileBehaviour.IgnoredTag = gameObject.tag;
            projectileBehaviour.ActorDamage = ProjectileActorDamage;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawCube(ThrowingOrigin, 0.1f * Vector3.one);
        Gizmos.DrawRay(ThrowingOrigin, ScaledThrowingForce);
    }
}
