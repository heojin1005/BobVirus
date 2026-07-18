using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

[InitializeOnLoad]
public static class SpecialZombiePrefabGenerator
{
    private const string EnemyFolder = "Assets/_Project/Prefabs/Enemy";
    private const string BaseZombiePath = EnemyFolder + "/Zombie.prefab";
    private const string CircleSpritePath = "Assets/_Project/Art/Sprite/\uC6D0.png";
    private const string BruteArtFolder = "Assets/_Project/Art/Sprite/Test/";

    static SpecialZombiePrefabGenerator()
    {
        EditorApplication.delayCall += GenerateIfMissing;
    }

    [MenuItem("Tools/BobVirus/Regenerate Special Zombie Prefabs")]
    public static void Regenerate()
    {
        Generate(force: true);
    }

    private static void GenerateIfMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        CleanupInterruptedPrototype();
        bool missing = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyFolder + "/BruteZombie.prefab") == null
            || AssetDatabase.LoadAssetAtPath<GameObject>(EnemyFolder + "/ScreamerBoss.prefab") == null
            || AssetDatabase.LoadAssetAtPath<GameObject>(EnemyFolder + "/CorruptBoss.prefab") == null;
        if (missing) Generate(force: false);
    }

    private static void Generate(bool force)
    {
        GameObject baseZombie = AssetDatabase.LoadAssetAtPath<GameObject>(BaseZombiePath);
        Sprite circle = AssetDatabase.LoadAllAssetsAtPath(CircleSpritePath).OfType<Sprite>().FirstOrDefault();
        if (baseZombie == null || circle == null)
        {
            Debug.LogWarning("Special zombie prefabs were not generated: base Zombie or circle prototype sprite is missing.");
            return;
        }

        ZombieSpitProjectile spit = CreateSpitProjectile(circle, force);
        PoisonPuddle puddle = CreatePoisonPuddle(circle, force);
        CreateBrute(baseZombie, force);
        CreateScreamer(baseZombie, spit, force);
        CreateCorrupt(baseZombie, puddle, force);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Generated BruteZombie, ScreamerBoss, and CorruptBoss prototype prefabs.");
    }

    private static ZombieSpitProjectile CreateSpitProjectile(Sprite circle, bool force)
    {
        string path = EnemyFolder + "/ZombieSpitProjectile.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null && !force) return existing.GetComponent<ZombieSpitProjectile>();

        GameObject root = new GameObject("ZombieSpitProjectile");
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = circle;
        renderer.color = new Color(0.55f, 0.9f, 0.15f, 1f);
        renderer.sortingOrder = 2;
        root.transform.localScale = Vector3.one * 0.08f;
        root.AddComponent<ZombieSpitProjectile>();
        GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return asset.GetComponent<ZombieSpitProjectile>();
    }

    private static PoisonPuddle CreatePoisonPuddle(Sprite circle, bool force)
    {
        string path = EnemyFolder + "/PoisonPuddle.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null && !force) return existing.GetComponent<PoisonPuddle>();

        GameObject root = new GameObject("PoisonPuddle");
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = circle;
        renderer.color = new Color(0.25f, 0.75f, 0.12f, 0.62f);
        renderer.sortingOrder = -1;
        PoisonPuddle puddle = root.AddComponent<PoisonPuddle>();
        SetLayerMask(puddle, "targetLayers", PlayerMask);
        GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return asset.GetComponent<PoisonPuddle>();
    }

    private static void CreateBrute(GameObject baseZombie, bool force)
    {
        string path = EnemyFolder + "/BruteZombie.prefab";
        if (!force && AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
        GameObject root = CreateFromBase(baseZombie, "BruteZombie", new Vector3(13f, 13f, 1f), new Color(0.7f, 0.48f, 0.38f));
        SetFloat(root.GetComponent<EnemyHealth>(), "maxHp", 320f);
        BruteZombieAI brain = root.AddComponent<BruteZombieAI>();
        BruteSpriteAnimator animator = root.AddComponent<BruteSpriteAnimator>();
        BruteRageEffect rageEffect = root.AddComponent<BruteRageEffect>();
        ConfigureBruteVisuals(root, animator, rageEffect);
        SetObject(brain, "spriteAnimator", animator);
        SetObject(brain, "rageEffect", rageEffect);
        SetLayerMask(brain, "playerLayers", PlayerMask);
        ConfigureAgent(root.GetComponent<NavMeshAgent>(), 0.14f, 1.4f);
        SaveAndDestroy(root, path);
    }

    private static void ConfigureBruteVisuals(GameObject root, BruteSpriteAnimator animator, BruteRageEffect rageEffect)
    {
        Sprite[] locomotion = LoadSprites(BruteArtFolder + "brute-idle,walking.png");
        Sprite[] slam = LoadSprites(BruteArtFolder + "brute-attack 1.png");
        Sprite[] rage = LoadSprites(BruteArtFolder + "brute-rage.png");
        Sprite[] running = LoadSprites(BruteArtFolder + "brute-running.png");
        if (locomotion.Length < 3 || slam.Length < 2 || rage.Length < 2 || running.Length < 3)
        {
            Debug.LogWarning("Brute sprites are missing or do not have the expected frame counts.");
            return;
        }

        SpriteRenderer renderer = root.GetComponentInChildren<SpriteRenderer>();
        renderer.sprite = locomotion[0];
        renderer.color = Color.white;
        SetObject(animator, "targetRenderer", renderer);
        SetObject(animator, "idleSprite", locomotion[0]);
        SetObjectArray(animator, "walkFrames", locomotion.Skip(1).ToArray());
        SetObjectArray(animator, "slamFrames", slam);
        SetObjectArray(animator, "rageFrames", rage);
        SetObject(animator, "chargePreparationSprite", running[0]);
        SetObjectArray(animator, "chargeFrames", running.Skip(1).ToArray());
        SetObject(rageEffect, "bodyRenderer", renderer);
    }

    private static Sprite[] LoadSprites(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name)
            .ToArray();
    }

    private static void CreateScreamer(GameObject baseZombie, ZombieSpitProjectile spit, bool force)
    {
        string path = EnemyFolder + "/ScreamerBoss.prefab";
        if (!force && AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
        GameObject root = CreateFromBase(baseZombie, "ScreamerBoss", new Vector3(12f, 12f, 1f), new Color(0.72f, 0.42f, 0.82f));
        SetFloat(root.GetComponent<EnemyHealth>(), "maxHp", 450f);
        ScreamerBossAI brain = root.AddComponent<ScreamerBossAI>();
        Transform origin = new GameObject("SpitOrigin").transform;
        origin.SetParent(root.transform, false);
        origin.localPosition = new Vector3(0.08f, 0.06f, 0f);
        SetObject(brain, "spitOrigin", origin);
        SetObject(brain, "spitProjectilePrefab", spit);
        SetLayerMask(brain, "playerLayers", PlayerMask);
        SetLayerMask(brain, "zombieLayers", ZombieMask);
        ConfigureAgent(root.GetComponent<NavMeshAgent>(), 0.12f, 1.1f);
        SaveAndDestroy(root, path);
    }

    private static void CreateCorrupt(GameObject baseZombie, PoisonPuddle puddle, bool force)
    {
        string path = EnemyFolder + "/CorruptBoss.prefab";
        if (!force && AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
        GameObject root = CreateFromBase(baseZombie, "CorruptBoss", new Vector3(12f, 12f, 1f), new Color(0.45f, 0.72f, 0.22f));
        SetFloat(root.GetComponent<EnemyHealth>(), "maxHp", 380f);
        CorruptExplosionController explosion = root.AddComponent<CorruptExplosionController>();
        CorruptBossAI brain = root.AddComponent<CorruptBossAI>();
        SetObject(explosion, "poisonPuddlePrefab", puddle);
        SetLayerMask(explosion, "playerLayers", PlayerMask);
        SetObject(brain, "poisonPuddlePrefab", puddle);
        SetObject(brain, "explosionController", explosion);
        SetLayerMask(brain, "playerLayers", PlayerMask);
        ConfigureAgent(root.GetComponent<NavMeshAgent>(), 0.13f, 0.75f);
        SaveAndDestroy(root, path);
    }

    private static GameObject CreateFromBase(GameObject baseZombie, string name, Vector3 scale, Color tint)
    {
        GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(baseZombie);
        PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        root.name = name;
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = scale;
        EnemyAI oldBrain = root.GetComponent<EnemyAI>();
        if (oldBrain != null) Object.DestroyImmediate(oldBrain);
        TestDummy prototypeHealth = root.GetComponent<TestDummy>();
        if (prototypeHealth != null) Object.DestroyImmediate(prototypeHealth);
        if (root.GetComponent<EnemyHealth>() == null) root.AddComponent<EnemyHealth>();
        SpriteRenderer renderer = root.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null) renderer.color = tint;
        return root;
    }

    private static void CleanupInterruptedPrototype()
    {
        foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (candidate.name != "BruteZombie" || !candidate.scene.IsValid()) continue;
            if (candidate.GetComponent<EnemyAI>() == null && candidate.GetComponent<BruteZombieAI>() == null)
                Object.DestroyImmediate(candidate);
        }
    }

    private static void ConfigureAgent(NavMeshAgent agent, float radius, float stoppingDistance)
    {
        agent.radius = radius;
        agent.stoppingDistance = stoppingDistance;
        agent.acceleration = 40f;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    private static void SaveAndDestroy(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void SetFloat(Object target, string name, float value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(name).floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetLayerMask(Object target, string name, int value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(name).intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObject(Object target, string name, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(name).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObjectArray(Object target, string name, Object[] values)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(name);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private const int PlayerMask = (1 << 6) | (1 << 10);
    private const int ZombieMask = (1 << 7) | (1 << 11);
}
