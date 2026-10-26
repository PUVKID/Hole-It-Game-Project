using NUnit.Framework;
using UnityEngine;

public class ReferenceMotionSettingsTests
{
    [Test]
    public void SpeedUsesActualPhysicalGrowthAndBoosterRestoresBaseline()
    {
        Assert.AreEqual(2.6f, ReferenceMotionSettings.SpeedForTier(0), .00001f);
        Assert.AreEqual(3.042f, ReferenceMotionSettings.SpeedForTier(1), .00001f);
        Assert.AreEqual(6.578f, ReferenceMotionSettings.SpeedForTier(9), .00001f);
        Assert.AreEqual(7.02f, ReferenceMotionSettings.SpeedForTier(10), .00001f);
        Assert.AreEqual(7.02f, ReferenceMotionSettings.SpeedForTier(100), .00001f);
        Assert.AreEqual(3.38f, ReferenceMotionSettings.SpeedForTier(0, true), .00001f);
        Assert.AreEqual(9.126f, ReferenceMotionSettings.SpeedForTier(10, true), .00001f);
        Assert.AreEqual(7.02f, ReferenceMotionSettings.SpeedForTier(10, false), .00001f);
    }

    [Test]
    public void LongAndDiagonalDragsStayBoundedWhilePartialInputRemainsAnalog()
    {
        Vector2 longDrag = ReferenceMotionSettings.VelocityForInput(new Vector2(1200, 1600), 0);
        Assert.AreEqual(2.6f, longDrag.magnitude, .00001f);
        Assert.AreEqual(.75f, longDrag.x / longDrag.y, .00001f);
        Assert.AreEqual(2.6f, ReferenceMotionSettings.VelocityForInput(Vector2.one, 0).magnitude, .00001f);
        Assert.AreEqual(new Vector2(1.3f, 0), ReferenceMotionSettings.VelocityForInput(Vector2.right * .5f, 0));
        Assert.AreEqual(Vector2.zero, ReferenceMotionSettings.VelocityForInput(Vector2.zero, 10, true));
        Assert.AreEqual(Vector2.left * 2.6f, ReferenceMotionSettings.VelocityForInput(Vector2.left, 0),
            "An immediate direction reversal must not retain velocity from the previous frame.");
    }

    [Test]
    public void VelocityIntegratesToSameTravelDistanceAtThirtySixtyAndOneTwentyFps()
    {
        foreach (int fps in new[] { 30, 60, 120 })
        {
            Vector2 position = Vector2.zero;
            for (int frame = 0; frame < fps * 2; frame++)
                position += ReferenceMotionSettings.VelocityForInput(Vector2.right, 3) / fps;
            Assert.AreEqual(7.852f, position.x, .0001f);
            Assert.AreEqual(0, position.y);
        }
    }

    [Test]
    public void VisibleScaleAndContactRadiusRetainTheirDifferentSourceIncrements()
    {
        Assert.AreEqual(.7f, ReferenceMotionSettings.VisualScaleForTier(0), .00001f);
        Assert.AreEqual(.62f, ReferenceMotionSettings.ContactRadiusForTier(.62f, 0), .00001f);
        Assert.AreEqual(.95f, ReferenceMotionSettings.VisualScaleForTier(1), .00001f);
        Assert.AreEqual(.843f, ReferenceMotionSettings.ContactRadiusForTier(.62f, 1), .00001f);
        Assert.AreEqual(3.2f, ReferenceMotionSettings.VisualScaleForTier(10), .00001f);
        Assert.AreEqual(2.85f, ReferenceMotionSettings.ContactRadiusForTier(.62f, 10), .00001f);
        Assert.AreEqual(3.2f, ReferenceMotionSettings.VisualScaleForTier(30), .00001f);
        Assert.AreEqual(2.85f, ReferenceMotionSettings.ContactRadiusForTier(.62f, 30), .00001f);
        Assert.AreEqual(.503f, ReferenceMotionSettings.ContactRadiusForTier(.28f, 1), .00001f,
            "Alternate environment contact radii must remain supported.");
    }

    [Test]
    public void GrowthHasBoundedDurationAndRestoresUniformScaleAfterSourceWidthPunch()
    {
        Vector3 start = Vector3.one * .7f;
        Assert.AreEqual(start, ReferenceMotionSettings.GrowthAnimationScale(start, .95f, 0));
        Vector3 atExpandEnd = ReferenceMotionSettings.GrowthAnimationScale(start, .95f, .3f);
        Assert.AreEqual(1.06875f, atExpandEnd.x, .00001f);
        Assert.AreEqual(.95f, atExpandEnd.y, .00001f);
        Assert.AreEqual(.95f, atExpandEnd.z, .00001f);
        Vector3 halfwaySettle = ReferenceMotionSettings.GrowthAnimationScale(start, .95f, .375f);
        Assert.AreEqual(1.009375f, halfwaySettle.x, .00001f);
        Assert.AreEqual(.95f, halfwaySettle.z, .00001f);
        Assert.That(ReferenceMotionSettings.GrowthAnimationScale(start, .95f, .45f).x, Is.EqualTo(.95f).Within(.00001f));
        Assert.AreEqual(Vector3.one * .95f, ReferenceMotionSettings.GrowthAnimationScale(start, .95f, 20));
        Assert.AreEqual(0, ReferenceMotionSettings.GrowthOutBack(0), .00001f);
        Assert.AreEqual(1, ReferenceMotionSettings.GrowthOutBack(1), .00001f);
        Assert.Greater(ReferenceMotionSettings.GrowthOutBack(.7f), 1,
            "OutBack must retain the source's visible overshoot rather than exponential easing.");
    }

    [Test]
    public void GrowthAnimationEmitsOneExpansionCallbackAndRetargetsWithoutSnapping()
    {
        var animation = new ReferenceGrowthAnimation();
        animation.Reset(.7f);
        animation.Retarget(.95f);
        Assert.IsFalse(animation.Advance(.1f));
        Vector3 visibleBeforeRetarget = animation.Scale;
        animation.Retarget(1.2f);
        Assert.AreEqual(visibleBeforeRetarget, animation.Scale);
        Assert.IsFalse(animation.Advance(.2f));
        animation.Retarget(1.2f); // Repeated target assignment must not restart it.
        Assert.IsTrue(animation.Advance(.1f));
        Assert.IsFalse(animation.Advance(.15f));
        Assert.IsFalse(animation.IsPlaying);
        Assert.AreEqual(Vector3.one * 1.2f, animation.Scale);
        Assert.IsFalse(animation.Advance(1f));
    }

    [Test]
    public void SpeedChangesAtExpansionCallbackAndCapMatchesNativeGuard()
    {
        var speed = new ReferenceGrowthSpeedState();
        speed.Reset();
        speed.SetTier(1);
        Assert.AreEqual(2.6f, speed.Speed, .00001f);
        speed.OnExpansionFinished();
        Assert.AreEqual(3.042f, speed.Speed, .00001f);
        speed.SetTier(9);
        speed.OnExpansionFinished();
        Assert.AreEqual(6.578f, speed.Speed, .00001f);
        speed.SetTier(10);
        speed.OnExpansionFinished();
        Assert.AreEqual(6.578f, speed.Speed, .00001f, "The native cap guard skips the ordinary speed update.");
        speed.SetBooster(false);
        Assert.AreEqual(6.578f, speed.Speed, .00001f, "Polling an unchanged booster must not bypass that guard.");
        speed.SetBooster(true);
        Assert.AreEqual(9.126f, speed.Speed, .00001f);
        speed.SetBooster(false);
        Assert.AreEqual(7.02f, speed.Speed, .00001f);
    }
}
