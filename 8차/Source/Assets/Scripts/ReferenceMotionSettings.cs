using UnityEngine;

/// <summary>
/// Verified values from the supplied 1.31.17 data and ARM GameInputManager /
/// HoleSizeManager methods. Character contact radius is separate from visible scale.
/// </summary>
public static class ReferenceMotionSettings
{
    // level2 GameInputManager (path ID 2409), serialized offsets 0x3c / 0x40.
    public const float StartingSpeed = 2.6f;
    public const float SpeedPerPhysicalTier = .442f;
    public const float SpeedBoosterMultiplier = 1.3f;
    public const float ArrowRotationSpeed = 40f;
    public const int MaximumPhysicalTier = 10;

    // SizeInfos TextAsset and HoleSizeManager's starting visual transform.
    public const float StartingVisualScale = .7f;
    public const float VisualScalePerTier = .25f;
    public const float ContactRadiusPerTier = .223f;

    // level2 HoleSizeManager path ID 2376, DG.Tweening.Ease.OutBack = 27.
    // Bundled DOTweenSettings (resources.assets:4589) supplies the overshoot.
    public const float GrowthExpandSeconds = .3f;
    public const float GrowthSettleSeconds = .15f;
    public const float GrowthWidthMultiplier = 1.125f;
    public const float GrowthEaseOvershoot = 1.70158f;

    public static int PhysicalTier(int growthTier) => Mathf.Clamp(growthTier, 0, MaximumPhysicalTier);

    // GameInputManager.SetNewSpeed, RVA 0x19722C8. The caller controls when the
    // source's growth-animation callback updates speed; this is the value formula.
    public static float SpeedForTier(int growthTier, bool speedBoosted = false)
    {
        float speed = StartingSpeed + PhysicalTier(growthTier) * SpeedPerPhysicalTier;
        return speedBoosted ? speed * SpeedBoosterMultiplier : speed;
    }

    // Native Update calls CharacterController.SimpleMove(direction * speed).
    // A manual position integrator must multiply this velocity by deltaTime once.
    public static Vector2 VelocityForInput(Vector2 input, int growthTier, bool speedBoosted = false)
    {
        return Vector2.ClampMagnitude(input, 1f) * SpeedForTier(growthTier, speedBoosted);
    }

    public static float VisualScaleForTier(int growthTier)
    {
        return StartingVisualScale + PhysicalTier(growthTier) * VisualScalePerTier;
    }

    // startRadius comes from EnvironmentVariantSo (.62 for the supplied campaign),
    // overriding the original constructor's .7 fallback.
    public static float ContactRadiusForTier(float startRadius, int growthTier)
    {
        return startRadius + PhysicalTier(growthTier) * ContactRadiusPerTier;
    }

    public static float GrowthOutBack(float normalizedTime)
    {
        float t = Mathf.Clamp01(normalizedTime) - 1f;
        return 1f + t * t * ((GrowthEaseOvershoot + 1f) * t + GrowthEaseOvershoot);
    }

    // HoleScaleUpWithAnimation RVA 0x197117C widens X, then its callback
    // RVA 0x19721A8 restores the uniform target with a .15-second linear tween.
    public static Vector3 GrowthAnimationScale(Vector3 fromScale, float targetScale, float elapsed)
    {
        if (elapsed <= 0) return fromScale;
        Vector3 target = Vector3.one * targetScale;
        Vector3 wideTarget = new Vector3(targetScale * GrowthWidthMultiplier, targetScale, targetScale);
        if (elapsed < GrowthExpandSeconds)
            return Vector3.LerpUnclamped(fromScale, wideTarget, GrowthOutBack(elapsed / GrowthExpandSeconds));
        return Vector3.Lerp(wideTarget, target, Mathf.Clamp01((elapsed - GrowthExpandSeconds) / GrowthSettleSeconds));
    }
}

/// <summary>Allocation-free state for the original growth width pulse and settle.</summary>
public struct ReferenceGrowthAnimation
{
    Vector3 fromScale;
    float targetScale, elapsed;
    public Vector3 Scale { get; private set; }
    public bool IsPlaying { get; private set; }

    public void Reset(float scale)
    {
        targetScale = scale;
        fromScale = Scale = Vector3.one * scale;
        elapsed = 0;
        IsPlaying = false;
    }

    public void Retarget(float scale)
    {
        if (Mathf.Approximately(targetScale, scale)) return;
        fromScale = Scale;
        targetScale = scale;
        elapsed = 0;
        IsPlaying = true;
    }

    // True once when the .3-second expansion completes and its source callback
    // starts the .15-second settle. This is when normal movement speed is updated.
    public bool Advance(float deltaTime)
    {
        if (!IsPlaying) return false;
        float previous = elapsed;
        elapsed += Mathf.Max(0, deltaTime);
        Scale = ReferenceMotionSettings.GrowthAnimationScale(fromScale, targetScale, elapsed);
        bool expansionCompleted = previous < ReferenceMotionSettings.GrowthExpandSeconds && elapsed >= ReferenceMotionSettings.GrowthExpandSeconds;
        if (elapsed >= ReferenceMotionSettings.GrowthExpandSeconds + ReferenceMotionSettings.GrowthSettleSeconds)
        {
            Scale = Vector3.one * targetScale;
            IsPlaying = false;
        }
        return expansionCompleted;
    }
}

/// <summary>Native speed-update timing, including the physical-cap callback guard.</summary>
public struct ReferenceGrowthSpeedState
{
    public int PhysicalTier { get; private set; }
    public bool Boosted { get; private set; }
    public float Speed { get; private set; }

    public void Reset()
    {
        PhysicalTier = 0;
        Boosted = false;
        Speed = ReferenceMotionSettings.StartingSpeed;
    }

    public void SetTier(int growthTier)
    {
        PhysicalTier = ReferenceMotionSettings.PhysicalTier(growthTier);
    }

    public void OnExpansionFinished()
    {
        // Native <HoleScaleUpWithAnimation>b__0 skips SetNewSpeed once the
        // actual size cap is reached. A later speed-booster transition still
        // recomputes speed from the actual tier, including tier 10.
        if (PhysicalTier < ReferenceMotionSettings.MaximumPhysicalTier)
            Speed = ReferenceMotionSettings.SpeedForTier(PhysicalTier, Boosted);
    }

    public void SetBooster(bool enabled)
    {
        if (Boosted == enabled) return;
        Boosted = enabled;
        Speed = ReferenceMotionSettings.SpeedForTier(PhysicalTier, Boosted);
    }
}
