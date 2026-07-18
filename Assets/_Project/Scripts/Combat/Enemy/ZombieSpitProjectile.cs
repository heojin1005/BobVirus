using UnityEngine;

public class ZombieSpitProjectile : MonoBehaviour
{
    [SerializeField] private float hitRadius = 0.25f;
    [SerializeField] private Color spitColor = new Color(0.55f, 0.9f, 0.15f, 1f);

    private Vector2 direction;
    private float speed;
    private float damage;
    private float expiresAt;
    private LayerMask targetLayers;
    private GameObject owner;
    private bool initialized;

    private void Awake()
    {
        SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>();
        if (renderer != null) renderer.color = spitColor;
    }

    public void Initialize(Vector2 travelDirection, float moveSpeed, float hitDamage, float lifetime, LayerMask targets, GameObject source)
    {
        direction = travelDirection.normalized;
        speed = moveSpeed;
        damage = hitDamage;
        expiresAt = Time.time + lifetime;
        targetLayers = targets;
        owner = source;
        initialized = true;
        transform.right = direction;
    }

    private void Update()
    {
        if (!initialized) return;
        if (Time.time >= expiresAt) { Destroy(gameObject); return; }

        Vector2 nextPosition = (Vector2)transform.position + direction * (speed * Time.deltaTime);
        Collider2D[] hits = Physics2D.OverlapCircleAll(nextPosition, hitRadius, targetLayers);
        foreach (Collider2D hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null) continue;
            target.TakeDamage(damage, hit.ClosestPoint(nextPosition), -direction, owner);
            Destroy(gameObject);
            return;
        }

        transform.position = nextPosition;
    }
}
