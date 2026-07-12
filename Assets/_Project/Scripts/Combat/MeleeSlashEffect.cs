using UnityEngine;

public class MeleeSlashEffect : MonoBehaviour
{
    [SerializeField] private float lifeTime = 0.15f; 
    private float timer = 0f;

    private void Update()
    {
        // Time.deltaTime 대신 unscaledDeltaTime을 쓰면 타임스케일(역경직)을 무시합니다.
        timer += Time.unscaledDeltaTime; 

        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
}