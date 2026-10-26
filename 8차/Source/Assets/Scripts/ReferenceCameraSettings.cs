using UnityEngine;

/// <summary>Clean-launch camera settings verified in the supplied 1.31.17 data.</summary>
public static class ReferenceCameraSettings
{
    // ApplyHeightCamera (RVA 0x18AF50C) selects Height mode and FOV 44 on the
    // first launch, overriding the scene's older mode 0 / FOV 46 values.
    public const float StartingFov = 25f;
    public const float PlayingFov = 44f;
    public const float FovAnimationSeconds = .3f;

    // CameraManager level2:2392. Its Start method replaces the serialized
    // CinemachineTransposer offset with these values.
    public const float StartingHeight = 12.5f;
    public const float HeightPerPhysicalTier = 1.5f;
    public const float OffsetZDivisor = -1.73f;
    public const float PitchDegrees = 60f;
    public const float HeightAnimationSeconds = .5f;
    public const float SizeBoosterExtraHeight = 1f;

    // CinemachineTransposer level2:2384: X/Y/Z damping .3. Cinemachine's
    // Damper uses a 1% residual after one damping interval, not exp(-dt/time).
    public const float FollowDampingSeconds = .3f;
    public const float FollowDecayConstant = 4.6051702f;

    public static float HeightForTier(int growthTier, bool sizeBoosted = false)
    {
        return StartingHeight + ReferenceMotionSettings.PhysicalTier(growthTier) * HeightPerPhysicalTier
            + (sizeBoosted ? SizeBoosterExtraHeight : 0f);
    }

    public static Vector3 OffsetForHeight(float height) => new Vector3(0, height, height / OffsetZDivisor);

    public static float HeightOutCubic(float normalizedTime)
    {
        float remaining = 1f - Mathf.Clamp01(normalizedTime);
        return 1f - remaining * remaining * remaining;
    }

    public static float FollowBlend(float deltaTime)
    {
        return 1f - Mathf.Exp(-FollowDecayConstant * Mathf.Max(0, deltaTime) / FollowDampingSeconds);
    }

    public static Vector3 Follow(Vector3 current, Vector3 target, float deltaTime)
    {
        return Vector3.LerpUnclamped(current, target, FollowBlend(deltaTime));
    }
}

/// <summary>Allocation-free FOV and height tweens for the original default Height camera.</summary>
public struct ReferenceCameraState
{
    float heightFrom, heightTarget, heightElapsed, fovFrom, fovElapsed;
    bool changingHeight, changingFov, playStarted;
    public int PhysicalTier { get; private set; }
    public bool SizeBoosted { get; private set; }
    public float Height { get; private set; }
    public float Fov { get; private set; }
    public Vector3 Offset => ReferenceCameraSettings.OffsetForHeight(Height);

    public void Reset()
    {
        PhysicalTier = 0;
        SizeBoosted = false;
        Height = heightFrom = heightTarget = ReferenceCameraSettings.StartingHeight;
        Fov = fovFrom = ReferenceCameraSettings.StartingFov;
        heightElapsed = fovElapsed = 0;
        changingHeight = changingFov = playStarted = false;
    }

    // CameraManager.OnPlayButtonClickedResponse -> IncreaseCameraFov.
    public void BeginPlay()
    {
        if (playStarted) return;
        playStarted = true;
        fovFrom = Fov;
        fovElapsed = 0;
        changingFov = true;
    }

    public void SetGrowthTier(int growthTier)
    {
        int tier = ReferenceMotionSettings.PhysicalTier(growthTier);
        if (tier == PhysicalTier) return;
        PhysicalTier = tier;
        // SetNextSizeLevel (RVA 0x1970ED4) returns before the ordinary camera
        // event at the physical cap. A booster transition can still use tier 10.
        if (tier < ReferenceMotionSettings.MaximumPhysicalTier)
            RetargetHeight(ReferenceCameraSettings.HeightForTier(tier, SizeBoosted));
    }

    public void SetSizeBooster(bool enabled)
    {
        if (SizeBoosted == enabled) return;
        SizeBoosted = enabled;
        // AnimateCameraHeightForBooster / ResetHeightForBooster both call
        // SetCameraHeightOffsetForBooster: the same .5-second OutCubic tween.
        RetargetHeight(ReferenceCameraSettings.HeightForTier(PhysicalTier, enabled));
    }

    void RetargetHeight(float height)
    {
        if (Mathf.Approximately(heightTarget, height)) return;
        heightFrom = Height;
        heightTarget = height;
        heightElapsed = 0;
        changingHeight = true;
    }

    public void Advance(float deltaTime)
    {
        float dt = Mathf.Max(0, deltaTime);
        if (changingFov)
        {
            fovElapsed += dt;
            Fov = Mathf.Lerp(fovFrom, ReferenceCameraSettings.PlayingFov,
                fovElapsed / ReferenceCameraSettings.FovAnimationSeconds);
            if (fovElapsed >= ReferenceCameraSettings.FovAnimationSeconds)
            {
                Fov = ReferenceCameraSettings.PlayingFov;
                changingFov = false;
            }
        }
        if (changingHeight)
        {
            heightElapsed += dt;
            Height = Mathf.LerpUnclamped(heightFrom, heightTarget,
                ReferenceCameraSettings.HeightOutCubic(heightElapsed / ReferenceCameraSettings.HeightAnimationSeconds));
            if (heightElapsed >= ReferenceCameraSettings.HeightAnimationSeconds)
            {
                Height = heightTarget;
                changingHeight = false;
            }
        }
    }
}
