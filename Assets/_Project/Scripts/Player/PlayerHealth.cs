using System;
using System.Collections; // [추가]
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Settings")]
    [SerializeField] private float maxHp = 100f;

    // [추가] 피격 시각 효과용
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer bodySprite; 
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.1f;
    private Color originalColor = Color.white;
    private Coroutine hitFlashRoutine;

    // UI
    public event Action<float, float> OnHealthChanged;
    //
    public event Action OnDie;

    private float currentHp;
    private bool isDead = false;

    private void Awake()
    {
        currentHp = maxHp;
        // [추가] 원래 색상 기억
        if (bodySprite != null) originalColor = bodySprite.color;
    }

    private void Start()
    {
        // UI (HUD Start
        OnHealthChanged?.Invoke(currentHp, maxHp);
    }

    public void TakeDamage(float amount, Vector2 hitPoint, Vector2 hitNormal)
    {
        if (isDead) return;
        currentHp -= amount;
        //Debug.Log($"[ : {currentHp}");

        // [추가] 피격 깜빡임 효과 실행
        if (bodySprite != null)
        {
            if (hitFlashRoutine != null) StopCoroutine(hitFlashRoutine);
            hitFlashRoutine = StartCoroutine(HitFlashRoutine());
        }

        // UI 
        OnHealthChanged?.Invoke(currentHp, maxHp);

        if (currentHp <= 0)
        {
            Die();
        }
    }

    // [추가] 몸이 빨갛게 변했다가 0.1초 뒤 원래 색으로 돌아오는 코루틴
    private IEnumerator HitFlashRoutine()
    {
        bodySprite.color = hitColor;
        yield return new WaitForSeconds(hitFlashDuration);
        bodySprite.color = originalColor;
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