using UnityEngine;
using UnityEngine.InputSystem;

public class RotateToMouse : MonoBehaviour
{
    private void Update()
    {
        // 마우스가 없으면 실행 안 함 (에러 방지)
        if (Mouse.current == null) return;

        // 1. 마우스 화면 좌표 가져오기 (New Input System 방식)
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        // 2. 월드 좌표로 변환 (Z축 깊이 문제 방지를 위해 z값 강제 설정)
        Vector3 screenPosWithDepth = new Vector3(mouseScreenPos.x, mouseScreenPos.y, 10f); // 카메라 앞 10거리에
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(screenPosWithDepth);
        mouseWorldPos.z = 0f; // 2D 게임이므로 z는 0으로 고정

        // 3. 방향 계산 및 회전
        Vector2 dir = (Vector2)mouseWorldPos - (Vector2)transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        
        // FOV 알고리즘 기준(Y축=0도)에 맞추기 위해 -90도 보정
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
}