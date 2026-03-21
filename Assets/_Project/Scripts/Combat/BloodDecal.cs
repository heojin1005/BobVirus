using UnityEngine;
using System.Collections;

public class BloodDecal : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lifeTime = 10f; // 바닥에 남아있는 시간
    [SerializeField] private float fadeTime = 2f;  // 투명해지며 사라지는 데 걸리는 시간

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        StartCoroutine(FadeOutAndDestroy());
    }

    private IEnumerator FadeOutAndDestroy()
    {
        // 수명만큼 대기 (사라지기 시작할 때까지)
        yield return new WaitForSeconds(lifeTime - fadeTime);

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            float elapsed = 0f;

            // 서서히 투명(Alpha 0)하게 만들기
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                spriteRenderer.color = color;
                yield return null;
            }
        }

        // 완전히 투명해지면 오브젝트 파괴 (추후 풀링으로 교체 용이)
        Destroy(gameObject);
    }
}