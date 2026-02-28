using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance; // 어디서든 부를 수 있게 싱글톤 처리

    [Header("Target")]
    public Transform target;        // 플레이어
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f); // Z축 -10 필수 (2D)

    [Header("Settings")]
    [SerializeField] private float smoothSpeed = 5f; // 따라가는 속도 (높을수록 빠름)

    private float shakeDuration = 0f;
    private float shakeMagnitude = 0f;
    private Vector3 shakeOffset = Vector3.zero;

    private void Awake()
    {
        Instance = this; // 나 자신을 등록
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 1. 쉐이크 효과 계산
        if (shakeDuration > 0)
        {
            shakeOffset = Random.insideUnitSphere * shakeMagnitude;
            shakeOffset.z = 0; // 2D이므로 Z축 떨림 방지
            shakeDuration -= Time.deltaTime;
        }
        else
        {
            shakeOffset = Vector3.zero;
        }

        // 목표 위치 = 플레이어 위치 + 기본 거리 + 쉐이크 보정
        Vector3 desiredPosition = target.position + offset + shakeOffset;
        
        // 부드러운 이동 (Lerp)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        
        transform.position = smoothedPosition;

        
    }

    public void Shake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }
}