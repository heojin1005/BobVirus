using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Settings")]
    [SerializeField] private float maxHp = 100f;

    // UI에 보낼 신호 (현재체력, 최대체력)
    public event Action<float, float> OnHealthChanged;
    // 사망 시 보낼 신호
    public event Action OnDie;

    private float currentHp;
    private bool isDead = false;

    private void Awake()
    {
        currentHp = maxHp;
    }

    private void Start()
    {
        // 시작하자마자 UI 갱신 (HUD가 켜진 후 보내기 위해 Start에서 실행)
        OnHealthChanged?.Invoke(currentHp, maxHp);
    }

    public void TakeDamage(float amount, Vector2 hitPoint, Vector2 hitNormal)
    {
        if (isDead) return;

        currentHp -= amount;
        //Debug.Log($"[플레이어 피격] 남은 체력: {currentHp}");

        // UI 갱신 알림
        OnHealthChanged?.Invoke(currentHp, maxHp);

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("--- GAME OVER ---");

        // 1. 사망 이벤트 전송
        OnDie?.Invoke();

        // 2. 플레이어 조작 끄기 (PlayerController 비활성화)
        GetComponent<PlayerController>().enabled = false;
        
        // 3. (선택) 그래픽 끄기 or 눕는 모션 재생
        // GetComponentInChildren<SpriteRenderer>().color = Color.gray; 
    }
}