using UnityEngine;

public interface IDamageable
{
    // 데미지, 맞은 위치(혈흔용), 맞은 각도(혈흔 회전용), 공격자 정보
    void TakeDamage(float amount, Vector2 hitPoint, Vector2 hitNormal, GameObject attacker);
}