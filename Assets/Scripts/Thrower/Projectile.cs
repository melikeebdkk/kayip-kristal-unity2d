using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Cleanup")]
    public bool DestroyGameObject = true;
    public float LifeSeconds = 2f;
    [Header("Cleanup / Fade Out")]
    public bool FadeOut = true;
    public float FadeOutSeconds = 3f;
    [Header("Hit")]
    public string[] DestroyOnTags = new[] { "Player", "Enemy", "Obstacle", "Damage" };
    public int ActorDamage = 3;
    public bool PushDynamicBlocks = true;
    public float DynamicBlockPushForce = 34f;
    public float DynamicBlockBurstRadius = 1.7f;
    public float DynamicBlockUpwardBoost = 0.9f;
    [HideInInspector] public string IgnoredTag = "";

    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        StartCoroutine(DisposeProjectile(_spriteRenderer));
    }

    private IEnumerator DisposeProjectile(SpriteRenderer spriteRenderer)
    {
        yield return new WaitForSeconds(LifeSeconds);
        if (FadeOut && spriteRenderer != null) SpriteFader.FadeOut(spriteRenderer, FadeOutSeconds, FinishProjectile);
        else FinishProjectile();
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        TryDestroyOnHit(collider.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDestroyOnHit(collision.gameObject);
    }

    private void TryDestroyOnHit(GameObject target)
    {
        if (!string.IsNullOrEmpty(IgnoredTag) && target.CompareTag(IgnoredTag))
            return;

        if (TryDamageActor(target))
        {
            FinishProjectile();
            return;
        }

        if (PushDynamicBlocks && TryPushDynamicBlock(target))
        {
            FinishProjectile();
            return;
        }

        foreach (string targetTag in DestroyOnTags)
        {
            if (!target.CompareTag(targetTag)) continue;
            FinishProjectile();
            return;
        }
    }

    private void FinishProjectile()
    {
        if (DestroyGameObject) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

    private bool TryDamageActor(GameObject target)
    {
        Actor actor = target.GetComponentInParent<Actor>();
        if (actor == null || (!string.IsNullOrEmpty(IgnoredTag) && actor.CompareTag(IgnoredTag)))
            return false;
        if (!actor.CanBeDamagedBy(gameObject))
            return false;

        actor.TakeDamage(gameObject, ActorDamage);
        return true;
    }

    private bool TryPushDynamicBlock(GameObject target)
    {
        if (target.GetComponentInParent<Actor>() != null) return false;
        if (target.GetComponentInParent<Collectable>() != null) return false;

        Rigidbody2D body = target.GetComponent<Rigidbody2D>() ?? target.GetComponentInParent<Rigidbody2D>();
        if (body == null || body.bodyType != RigidbodyType2D.Dynamic) return false;

        string lowerName = body.gameObject.name.ToLowerInvariant();
        bool pushableByName = lowerName.Contains("box") || lowerName.Contains("crate") || lowerName.Contains("block");
        if (!pushableByName)
            return false;

        PushBlockBody(body, 1f);
        PushNearbyDynamicBlocks(body);
        return true;
    }

    private void PushNearbyDynamicBlocks(Rigidbody2D primaryBody)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(primaryBody.position, DynamicBlockBurstRadius);
        foreach (Collider2D hit in hits)
        {
            if (!hit || hit.attachedRigidbody == null || hit.attachedRigidbody == primaryBody)
                continue;

            Rigidbody2D body = hit.attachedRigidbody;
            if (body.bodyType != RigidbodyType2D.Dynamic) continue;
            if (body.GetComponentInParent<Actor>() != null) continue;
            if (body.GetComponentInParent<Collectable>() != null) continue;
            string lowerName = body.gameObject.name.ToLowerInvariant();
            if (!lowerName.Contains("box") && !lowerName.Contains("crate") && !lowerName.Contains("block")) continue;

            PushBlockBody(body, 0.72f);
        }
    }

    private void PushBlockBody(Rigidbody2D body, float multiplier)
    {
        Vector2 pushDirection = body.position - (Vector2)transform.position;
        if (pushDirection.sqrMagnitude < 0.01f)
            pushDirection = Vector2.right;

        pushDirection = new Vector2(pushDirection.x, Mathf.Max(pushDirection.y, DynamicBlockUpwardBoost));
        body.WakeUp();
        body.AddForce(pushDirection.normalized * DynamicBlockPushForce * multiplier, ForceMode2D.Impulse);
        body.AddTorque(-Mathf.Sign(pushDirection.x) * DynamicBlockPushForce * 0.22f * multiplier, ForceMode2D.Impulse);
    }
}
