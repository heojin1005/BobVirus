using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용
using System.Collections;

public class CombatHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI ammoText; // 화면에 글씨 띄울 컴포넌트
    [SerializeField] private TextMeshProUGUI HPText;

    [Header("Rescue UI")]
    [SerializeField] private CanvasGroup rescuePanel;
    [SerializeField] private float fadeDuration = 1.0f;

    [SerializeField] private WeaponSystem weaponSystem; // 관찰할 무기
    [SerializeField] private PlayerHealth playerHealth;

    private void Start()
    {
        // 무기 시스템이 연결되어 있다면 이벤트를 구독(Subscribe)합니다.
        if (weaponSystem != null)
        {
            // "무기야, OnAmmoChanged 이벤트가 발생하면 내 UpdateAmmoText 함수를 실행해줘"
            weaponSystem.OnAmmoChanged += UpdateAmmoText;
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHPText;
            playerHealth.OnDie += HandlePlayerDeath;
        }

        if (rescuePanel != null)
        {
            rescuePanel.alpha = 0f; // 처음에는 투명하게 시작
            rescuePanel.blocksRaycasts = false; // 처음에는 클릭 막지 않음
        }
    }

    private void OnDestroy() // 오브젝트가 파괴될 때 (중요!)
    {
        // 구독을 해지하지 않으면 메모리 누수나 에러가 발생할 수 있습니다.
        if (weaponSystem != null)
        {
            weaponSystem.OnAmmoChanged -= UpdateAmmoText;
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHPText;
            playerHealth.OnDie -= HandlePlayerDeath;
        }
    }

    // 실제 텍스트를 바꾸는 함수
    private void UpdateAmmoText(int current, int max)
    {
        // 예: "30 / 30"
        ammoText.text = $"{current} / {max}";

        // 총알이 없으면 빨간색으로 표시
        if (current == 0) ammoText.color = Color.red;
        else ammoText.color = Color.white;
    }

    private void UpdateHPText(float current, float max)
    {
        if (HPText == null) return;
        HPText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";

        // 체력이 30% 이하이면 빨간색으로 표시
        if (current / max <= 0.3f) HPText.color = Color.red;
        else HPText.color = Color.white;
    }

    private void HandlePlayerDeath()
    {
        StartCoroutine(RescueSequence());
    }

    private IEnumerator RescueSequence()
    {
        if (rescuePanel != null)
        {
            rescuePanel.blocksRaycasts = true; // 클릭 막기
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                rescuePanel.alpha = timer / fadeDuration;
                yield return null;
            }
            rescuePanel.alpha = 1f; // 불투명하게
        }

        yield return new WaitForSeconds(1.0f); // 1초 대기 후

        SceneLoader.Load("Main"); // 허브 씬 제작 전까지 일단 메인씬으로 재시작
    }
}