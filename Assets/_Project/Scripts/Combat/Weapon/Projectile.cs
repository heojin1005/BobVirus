using UnityEngine;
using System.Collections;

public class Projectile : MonoBehaviour
{

    private float damage;
    private float speed;
    private float lifeTime;
    private LayerMask targetLayer;
    private TrailRenderer trail;
    private SpriteRenderer spriteRenderer; 
    private GameObject shooter; 

    // 무기 시스템에서 생성할 때 호출하여 정보를 넘겨주는 함수
    public void Initialize(float damageAmount, LayerMask layer, float bulletSpeed, float lifeTime, GameObject shooterObj = null)
    {
        this.damage = damageAmount;
        this.lifeTime = lifeTime;
        this.speed = bulletSpeed;
        this.targetLayer = layer;
        this.shooter = shooterObj;

        trail = GetComponent<TrailRenderer>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (trail != null)
        {
            trail.Clear(); // 트레일 초기화 (이전 총알의 잔상이 남는 것을 방지)
            trail.emitting = true; // 트레일이 발사와 함께 생성되도록 설정
        }

        Destroy(gameObject, lifeTime); // 일정 시간 후 자동 삭제 (안전장치)
    }

    private void Update()
    {
        // 이번 프레임에 이동할 거리 계산
        float moveDistance = speed * Time.deltaTime;

        // 이동하기 전에, 이동할 경로에 무언가 있는지 레이캐스트로 검사 (터널링 방지)
        if (CheckCollision(moveDistance))
        {
            return; // 충돌이 발생하면 더 이상 이동하지 않음
        }

        // 실제로 이동
        transform.Translate(Vector3.right * moveDistance);
    }

    private bool CheckCollision(float moveDistance)
    {
        // 현재 위치에서 앞쪽으로 moveDistance만큼 레이를 쏜다
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.right, moveDistance, targetLayer);

        if (hit.collider != null)
        {
            // 충돌 발생!
            StartCoroutine(OnHitRoutine(hit.collider, hit.point, hit.normal));
            return true;
        }
        return false; // 충돌 없음
    }

    private IEnumerator OnHitRoutine(Collider2D collider, Vector2 hitPoint, Vector2 hitNormal)
    {
        transform.position = hitPoint; // 총알을 충돌 지점으로 이동 (맞기 전에 사라지는거 방지)
        
        // 데미지 처리
        IDamageable damageable = collider.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(damage, hitPoint, hitNormal, shooter);
        }

        EnemyAI ai = collider.GetComponentInParent<EnemyAI>();
        if (ai != null)
        {
            ai.OnAttacked(shooter); // 여기서 비로소 패닉 조건이 검사됩니다!
        }
        
        EnemyShooterAI shooterAI = collider.GetComponentInParent<EnemyShooterAI>();
        if (shooterAI != null)
        {
            shooterAI.OnAttacked(shooter);
        }
        

        // 여기에 피격 이펙트(Particle) 생성 로직 추가 가능
        // Instantiate(hitEffect, hitPoint, Quaternion.identity);
        
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        
        // 이동 로직이 더 이상 돌지 않도록 컴포넌트 비활성화 (선택사항)
        enabled = false; 

        // Trail이 벽까지 쭉 늘어날 수 있도록 프레임 끝까지 대기
        // 유니티가 모든 렌더링 계산을 마칠 때까지 기다립니다.
        yield return new WaitForEndOfFrame();

        // 이제 Trail 분리
        if (trail != null)
        {
            trail.transform.parent = null; 
            trail.autodestruct = true; 
        }

        // 총알 본체 삭제
        Destroy(gameObject);
    }
}