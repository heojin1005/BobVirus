using System.Collections.Generic;
using UnityEngine;

public static class ZombieCombatUtility
{
    public static int DamageCircle(Vector2 center, float radius, LayerMask mask, float damage, GameObject attacker)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, mask);
        HashSet<IDamageable> damaged = new HashSet<IDamageable>();

        foreach (Collider2D hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || !damaged.Add(target)) continue;
            target.TakeDamage(damage, hit.ClosestPoint(center), Vector2.zero, attacker);
        }

        return damaged.Count;
    }

    public static bool ContainsDamageable(Vector2 center, float radius, LayerMask mask)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, mask);
        foreach (Collider2D hit in hits)
            if (hit.GetComponentInParent<IDamageable>() != null) return true;
        return false;
    }
}
