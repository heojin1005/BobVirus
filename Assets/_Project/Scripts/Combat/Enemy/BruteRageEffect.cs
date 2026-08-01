using UnityEngine;

/// <summary>Creates a persistent ember aura with Unity's built-in ParticleSystem.</summary>
public class BruteRageEffect : MonoBehaviour
{
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Color emberColor = new Color(1f, 0.08f, 0.01f, 1f);
    [SerializeField] private Color smokeColor = new Color(0.35f, 0.02f, 0.01f, 0.25f);
    [SerializeField] private float emissionRate = 14f;

    private ParticleSystem rageParticles;

    private void Awake()
    {
        if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();
        CreateParticleSystem();
    }

    public void Activate()
    {
        if (rageParticles == null) CreateParticleSystem();
        if (rageParticles != null && !rageParticles.isPlaying) rageParticles.Play();
    }

    public void Deactivate()
    {
        if (rageParticles != null)
            rageParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void CreateParticleSystem()
    {
        if (rageParticles != null) return;

        Transform child = transform.Find("RageAura");
        GameObject aura = child != null ? child.gameObject : new GameObject("RageAura");
        if (child == null)
        {
            aura.transform.SetParent(transform, false);
            aura.transform.localPosition = new Vector3(0f, 0.055f, 0f);
        }

        rageParticles = aura.GetComponent<ParticleSystem>();
        if (rageParticles == null) rageParticles = aura.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = rageParticles.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.075f);
        main.startColor = new ParticleSystem.MinMaxGradient(emberColor, smokeColor);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.maxParticles = 48;

        ParticleSystem.EmissionModule emission = rageParticles.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate;

        ParticleSystem.ShapeModule shape = rageParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.055f;
        shape.radiusThickness = 1f;

        ParticleSystem.VelocityOverLifetimeModule velocity = rageParticles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = rageParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(emberColor, 0f), new GradientColorKey(smokeColor, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = fade;

        ParticleSystemRenderer particleRenderer = aura.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sortingLayerID = bodyRenderer != null ? bodyRenderer.sortingLayerID : 0;
        particleRenderer.sortingOrder = bodyRenderer != null ? bodyRenderer.sortingOrder + 1 : 1;
        rageParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
