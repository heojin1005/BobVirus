using System.Collections.Generic;
using UnityEngine;

public class PoisonPuddle : MonoBehaviour
{
    [Header("Defaults")]
    [SerializeField] private float radius = 1.1f;
    [SerializeField] private float lifetime = 6f;
    [SerializeField] private float damagePerTick = 3f;
    [SerializeField] private float tickInterval = 0.75f;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private Color poisonColor = new Color(0.25f, 0.75f, 0.12f, 0.62f);

    private readonly Dictionary<IDamageable, float> nextDamageTimes = new Dictionary<IDamageable, float>();
    private GameObject owner;
    private float expiresAt;
    private SpriteRenderer puddleRenderer;

    private void Awake()
    {
        puddleRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        expiresAt = Time.time + lifetime;
        ApplyVisual();
    }

    public void Initialize(float areaRadius, float duration, float tickDamage, float interval, LayerMask targets, GameObject source)
    {
        radius = areaRadius;
        lifetime = duration;
        damagePerTick = tickDamage;
        tickInterval = Mathf.Max(0.05f, interval);
        targetLayers = targets;
        owner = source;
        expiresAt = Time.time + lifetime;
        ApplyVisual();
    }

    private void Update()
    {
        if (Time.time >= expiresAt)
        {
            Destroy(gameObject);
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, targetLayers);
        HashSet<IDamageable> present = new HashSet<IDamageable>();
        foreach (Collider2D hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || !present.Add(target)) continue;
            if (nextDamageTimes.TryGetValue(target, out float nextTime) && Time.time < nextTime) continue;

            target.TakeDamage(damagePerTick, hit.ClosestPoint(transform.position), Vector2.zero, owner);
            nextDamageTimes[target] = Time.time + tickInterval;
        }
    }

    private void ApplyVisual()
    {
        if (puddleRenderer == null || puddleRenderer.sprite == null) return;
        puddleRenderer.color = poisonColor;
        float spriteDiameter = Mathf.Max(0.01f, puddleRenderer.sprite.bounds.size.x);
        puddleRenderer.transform.localScale = Vector3.one * ((radius * 2f) / spriteDiameter);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 0.1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
