using UnityEngine;

/// <summary>Small sprite sequencer tailored to the Brute's few prototype frames.</summary>
public class BruteSpriteAnimator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private Sprite[] slamFrames;
    [SerializeField] private Sprite[] rageFrames;
    [SerializeField] private Sprite chargePreparationSprite;
    [SerializeField] private Sprite[] chargeFrames;
    [SerializeField, Min(1f)] private float walkFramesPerSecond = 5f;
    [SerializeField, Min(1f)] private float chargeFramesPerSecond = 7f;

    private Sprite[] activeSequence;
    private float activeFramesPerSecond;
    private float sequenceTimer;
    private bool loopSequence;
    private bool abilityOverride;
    private bool wasMoving;

    public bool HasValidSprites => targetRenderer != null && idleSprite != null;

    private void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponentInChildren<SpriteRenderer>();
        Show(idleSprite);
    }

    private void Update()
    {
        if (activeSequence == null || activeSequence.Length == 0) return;

        sequenceTimer += Time.deltaTime;
        int frameIndex = Mathf.FloorToInt(sequenceTimer * activeFramesPerSecond);
        if (loopSequence) frameIndex %= activeSequence.Length;
        else frameIndex = Mathf.Min(frameIndex, activeSequence.Length - 1);
        Show(activeSequence[frameIndex]);
    }

    public void SetLocomotion(bool moving)
    {
        if (abilityOverride) return;
        if (moving == wasMoving && activeSequence != null) return;

        wasMoving = moving;
        if (moving && walkFrames != null && walkFrames.Length > 0)
            PlaySequence(walkFrames, walkFramesPerSecond, true, false);
        else
            ShowIdle();
    }

    public void PlaySlamWindup()
    {
        abilityOverride = true;
        StopSequence();
        Show(GetFrame(slamFrames, 0));
    }

    public void PlaySlamImpact()
    {
        abilityOverride = true;
        StopSequence();
        Show(GetFrame(slamFrames, 1));
    }

    public void PlayRage(float duration)
    {
        abilityOverride = true;
        float fps = rageFrames != null && rageFrames.Length > 0
            ? rageFrames.Length / Mathf.Max(0.01f, duration)
            : 1f;
        PlaySequence(rageFrames, fps, false, true);
    }

    public void PlayChargePreparation()
    {
        abilityOverride = true;
        StopSequence();
        Show(chargePreparationSprite);
    }

    public void PlayChargeLoop()
    {
        abilityOverride = true;
        PlaySequence(chargeFrames, chargeFramesPerSecond, true, true);
    }

    public void ClearAbility()
    {
        abilityOverride = false;
        wasMoving = false;
        ShowIdle();
    }

    private void PlaySequence(Sprite[] frames, float framesPerSecond, bool loop, bool keepAbilityOverride)
    {
        abilityOverride |= keepAbilityOverride;
        activeSequence = frames;
        activeFramesPerSecond = Mathf.Max(1f, framesPerSecond);
        sequenceTimer = 0f;
        loopSequence = loop;
        if (frames != null && frames.Length > 0) Show(frames[0]);
        else ShowIdle();
    }

    private void ShowIdle()
    {
        StopSequence();
        Show(idleSprite);
    }

    private void StopSequence()
    {
        activeSequence = null;
        sequenceTimer = 0f;
    }

    private void Show(Sprite sprite)
    {
        if (targetRenderer != null && sprite != null) targetRenderer.sprite = sprite;
    }

    private static Sprite GetFrame(Sprite[] frames, int index)
    {
        if (frames == null || frames.Length == 0) return null;
        return frames[Mathf.Clamp(index, 0, frames.Length - 1)];
    }
}
