using NUnit.Framework;
using UnityEngine;

public class ReferenceCameraSettingsTests
{
    [Test]
    public void PlayFovUsesRuntimeFirstLaunchOverrideAndLinearPointThreeSecondTween()
    {
        var camera = new ReferenceCameraState();
        camera.Reset();
        camera.Advance(10);
        Assert.AreEqual(25, camera.Fov);
        Assert.AreEqual(12.5f, camera.Height);
        camera.BeginPlay();
        camera.Advance(.15f);
        Assert.AreEqual(34.5f, camera.Fov, .00001f);
        camera.BeginPlay(); // Repeated UI dispatch must not reset the zoom.
        camera.Advance(.15f);
        Assert.AreEqual(44, camera.Fov);
        camera.Advance(20);
        Assert.AreEqual(44, camera.Fov);
        camera.Reset();
        Assert.AreEqual(25, camera.Fov);
    }

    [Test]
    public void GrowthHeightUsesPointFiveSecondOutCubicAndSourceDepthRatio()
    {
        var camera = new ReferenceCameraState();
        camera.Reset();
        camera.SetGrowthTier(1);
        Assert.AreEqual(12.5f, camera.Height);
        camera.Advance(.25f);
        Assert.AreEqual(13.8125f, camera.Height, .00001f);
        Assert.AreEqual(camera.Height / -1.73f, camera.Offset.z, .00001f);
        Assert.AreEqual(0, camera.Offset.x);
        camera.SetGrowthTier(1); // Frame polling does not restart the tween.
        camera.Advance(.25f);
        Assert.AreEqual(14f, camera.Height);
        Assert.AreEqual(14f, camera.Offset.y);
    }

    [Test]
    public void InterruptedGrowthKeepsCurrentViewAndInterpolatesTowardNewHeight()
    {
        var camera = new ReferenceCameraState();
        camera.Reset();
        camera.SetGrowthTier(1);
        camera.Advance(.25f);
        float previous = camera.Height;
        camera.SetGrowthTier(2);
        Assert.AreEqual(previous, camera.Height);
        camera.Advance(.25f);
        Assert.AreEqual(Mathf.Lerp(previous, 15.5f, .875f), camera.Height, .00001f);
        camera.Advance(.25f);
        Assert.AreEqual(15.5f, camera.Height);
    }

    [Test]
    public void NormalCapSkipsCameraEventButSizeBoosterRecomputesActualTier()
    {
        var camera = new ReferenceCameraState();
        camera.Reset();
        camera.SetGrowthTier(9);
        camera.Advance(.5f);
        Assert.AreEqual(26f, camera.Height);
        camera.SetGrowthTier(10);
        camera.Advance(2);
        Assert.AreEqual(26f, camera.Height);
        camera.SetGrowthTier(20);
        camera.SetSizeBooster(false);
        camera.Advance(2);
        Assert.AreEqual(26f, camera.Height, "Polling unchanged state cannot bypass the source cap guard.");
        camera.SetSizeBooster(true);
        camera.Advance(.5f);
        Assert.AreEqual(28.5f, camera.Height);
        camera.SetSizeBooster(false);
        camera.Advance(.5f);
        Assert.AreEqual(27.5f, camera.Height);
    }

    [Test]
    public void GrowthDuringSizeBoosterPreservesExtraHeightWithoutFovInflation()
    {
        var camera = new ReferenceCameraState();
        camera.Reset();
        camera.BeginPlay();
        camera.SetSizeBooster(true);
        camera.Advance(.5f);
        Assert.AreEqual(13.5f, camera.Height);
        camera.SetGrowthTier(1);
        camera.Advance(.5f);
        Assert.AreEqual(15f, camera.Height);
        Assert.AreEqual(44, camera.Fov, "The clean-launch Height mode does not use the legacy FOV booster.");
    }

    [Test]
    public void CinemachineDampingLeavesOnePercentResidualAtAllSupportedFrameRates()
    {
        Vector3 target = new Vector3(10, 20, -30);
        foreach (int fps in new[] { 30, 60, 120 })
        {
            Vector3 position = Vector3.zero;
            for (int frame = 0; frame < fps * 3 / 10; frame++)
                position = ReferenceCameraSettings.Follow(position, target, 1f / fps);
            Assert.AreEqual(.01f, (target - position).magnitude / target.magnitude, .00001f);
        }
        Assert.AreEqual(Vector3.one, ReferenceCameraSettings.Follow(Vector3.one, target, 0));
        Assert.AreEqual(Vector3.one, ReferenceCameraSettings.Follow(Vector3.one, target, -.1f));
    }

    [Test]
    public void CameraTweenStateIsFrameRateIndependentAndClampsHitchesToTweenEndpoints()
    {
        foreach (int fps in new[] { 30, 60, 120 })
        {
            var camera = new ReferenceCameraState();
            camera.Reset();
            camera.BeginPlay();
            camera.SetGrowthTier(3);
            for (int frame = 0; frame < fps; frame++) camera.Advance(1f / fps);
            Assert.AreEqual(44f, camera.Fov);
            Assert.AreEqual(17f, camera.Height);
        }
        var stalled = new ReferenceCameraState();
        stalled.Reset();
        stalled.BeginPlay();
        stalled.SetGrowthTier(3);
        stalled.Advance(-1);
        Assert.AreEqual(25, stalled.Fov);
        Assert.AreEqual(12.5f, stalled.Height);
        stalled.Advance(5);
        Assert.AreEqual(44, stalled.Fov);
        Assert.AreEqual(17f, stalled.Height);
    }
}
