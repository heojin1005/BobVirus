// weaponData를 더 편리하게 편집하기 위한 커스텀 에디터 스크립트. 필요없으면 삭제해도 됨

using UnityEngine;
using UnityEditor; // 에디터 기능 사용을 위해 필수

[CustomEditor(typeof(WeaponData))]
public class WeaponDataEditor : Editor
{
    // 1. 프로퍼티 변수 선언
    SerializedProperty type, weaponName, projectilePrefab, targetLayers;
    SerializedProperty damage, fireRate, noiseRange, maxRange;
    SerializedProperty weaponSprite, spriteScale, holdPosOffset, holdAngleOffset, muzzleOffset;
    
    // Gun Specifics
    SerializedProperty isAutomatic, maxAmmo, reloadTime, projectileSpeed, bulletLifeTime;
    SerializedProperty baseSpread, maxSpread, spreadPerShot, spreadRecovery;
    
    // Melee Specifics
    SerializedProperty attackRadius, attackArc;
    
    // Throwable Specifics
    SerializedProperty throwForce, grenadeArcHeight, explosionRadius, explodeOnArrival, grenadeFuseTime;

    private void OnEnable()
    {
        // 2. WeaponData.cs의 실제 변수명과 완벽히 매칭
        type = serializedObject.FindProperty("type");
        weaponName = serializedObject.FindProperty("weaponName");
        projectilePrefab = serializedObject.FindProperty("projectilePrefab");
        targetLayers = serializedObject.FindProperty("targetLayers");

        damage = serializedObject.FindProperty("damage");
        fireRate = serializedObject.FindProperty("fireRate");
        noiseRange = serializedObject.FindProperty("noiseRange");
        maxRange = serializedObject.FindProperty("maxRange");

        weaponSprite = serializedObject.FindProperty("weaponSprite");
        spriteScale = serializedObject.FindProperty("spriteScale");
        holdPosOffset = serializedObject.FindProperty("holdPosOffset");
        holdAngleOffset = serializedObject.FindProperty("holdAngleOffset");
        muzzleOffset = serializedObject.FindProperty("muzzleOffset");

        isAutomatic = serializedObject.FindProperty("isAutomatic");
        maxAmmo = serializedObject.FindProperty("maxAmmo");
        reloadTime = serializedObject.FindProperty("reloadTime");
        projectileSpeed = serializedObject.FindProperty("projectileSpeed");
        bulletLifeTime = serializedObject.FindProperty("bulletLifeTime");

        baseSpread = serializedObject.FindProperty("baseSpread");
        maxSpread = serializedObject.FindProperty("maxSpread");
        spreadPerShot = serializedObject.FindProperty("spreadPerShot");
        spreadRecovery = serializedObject.FindProperty("spreadRecovery");

        attackRadius = serializedObject.FindProperty("attackRadius");
        attackArc = serializedObject.FindProperty("attackArc");

        throwForce = serializedObject.FindProperty("throwForce");
        grenadeArcHeight = serializedObject.FindProperty("grenadeArcHeight");
        explosionRadius = serializedObject.FindProperty("explosionRadius");
        explodeOnArrival = serializedObject.FindProperty("explodeOnArrival");
        grenadeFuseTime = serializedObject.FindProperty("grenadeFuseTime");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 헤더 스타일 설정
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 12;
        headerStyle.alignment = TextAnchor.MiddleCenter;

        // --- [공통 섹션] ---
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("=== BASIC INFO ===", headerStyle);
        EditorGUILayout.PropertyField(type);
        EditorGUILayout.PropertyField(weaponName);
        EditorGUILayout.PropertyField(projectilePrefab);
        EditorGUILayout.PropertyField(targetLayers);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("=== COMBAT STATS ===", headerStyle);
        EditorGUILayout.PropertyField(damage);
        EditorGUILayout.PropertyField(fireRate);
        EditorGUILayout.PropertyField(noiseRange);
        EditorGUILayout.PropertyField(maxRange);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("=== HOLDING & POSITIONING ===", headerStyle);
        EditorGUILayout.PropertyField(weaponSprite);
        EditorGUILayout.PropertyField(spriteScale);
        EditorGUILayout.PropertyField(holdPosOffset);
        EditorGUILayout.PropertyField(holdAngleOffset);
        EditorGUILayout.PropertyField(muzzleOffset);

        // --- [분기 섹션 (타입별)] ---
        WeaponType currentType = (WeaponType)type.enumValueIndex;
        EditorGUILayout.Space(15);

        switch (currentType)
        {
            case WeaponType.Gun:
                EditorGUILayout.HelpBox("GUN SETTINGS", MessageType.Info);
                EditorGUILayout.PropertyField(isAutomatic);
                EditorGUILayout.PropertyField(maxAmmo);
                EditorGUILayout.PropertyField(reloadTime);
                EditorGUILayout.PropertyField(projectileSpeed);
                EditorGUILayout.PropertyField(bulletLifeTime);
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Spread (Gun Only)", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(baseSpread);
                EditorGUILayout.PropertyField(maxSpread);
                EditorGUILayout.PropertyField(spreadPerShot);
                EditorGUILayout.PropertyField(spreadRecovery);
                break;

            case WeaponType.Melee:
                EditorGUILayout.HelpBox("MELEE SETTINGS", MessageType.Info);
                EditorGUILayout.PropertyField(attackRadius);
                EditorGUILayout.PropertyField(attackArc);
                break;

            case WeaponType.Throwable:
                EditorGUILayout.HelpBox("THROWABLE SETTINGS", MessageType.Info);
                EditorGUILayout.PropertyField(throwForce);
                EditorGUILayout.PropertyField(grenadeArcHeight);
                EditorGUILayout.PropertyField(explosionRadius);
                EditorGUILayout.PropertyField(explodeOnArrival);
                
                // 즉발(Explode On Arrival)이 아닐 때만 도화선 시간(Fuse Time)을 보여줌
                if (!explodeOnArrival.boolValue)
                {
                    EditorGUILayout.PropertyField(grenadeFuseTime);
                }
                break;
        }

        // 변경사항 적용
        serializedObject.ApplyModifiedProperties();
    }
}