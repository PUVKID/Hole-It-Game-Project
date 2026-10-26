using NUnit.Framework;
using UnityEngine;

public class ReferenceIndicatorTests {
    [Test] public void OffscreenMarkersKeepOriginalAsymmetricBuffersAndNeverCrossScreenEdges(){
        Assert.AreEqual(new Vector2(115,1340),ReferenceIndicatorLayout.ClampOffscreen(new Vector3(-2,2,1),1080,1920,1));
        Assert.AreEqual(new Vector2(965,420),ReferenceIndicatorLayout.ClampOffscreen(new Vector3(2,-2,1),1080,1920,1));
        // Native Start scales horizontal buffer; Update scales vertical buffers by Canvas.scaleFactor.
        var small=ReferenceIndicatorLayout.ClampOffscreen(new Vector3(-2,2,1),540,960,.5f);
        Assert.AreEqual(57.5f,small.x);Assert.AreEqual(670,small.y);
    }
    [Test] public void BehindCameraTargetsRemainFiniteAndVisibleAfterClamping(){
        foreach(var viewport in new[]{new Vector3(.5f,.5f,-1),new Vector3(1,1,-1),new Vector3(2,-1,-1)}){
            Assert.IsFalse(ReferenceIndicatorLayout.IsOnScreen(viewport));
            var point=ReferenceIndicatorLayout.ClampOffscreen(viewport,1080,1920,1);
            Assert.IsFalse(float.IsNaN(point.x)||float.IsNaN(point.y)||float.IsInfinity(point.x)||float.IsInfinity(point.y));
            Assert.That(point.x,Is.InRange(115,965));Assert.That(point.y,Is.InRange(420,1340));
        }
        Assert.IsTrue(ReferenceIndicatorLayout.IsOnScreen(new Vector3(0,1,1)));
        Assert.IsFalse(ReferenceIndicatorLayout.IsOnScreen(new Vector3(1.001f,.5f,1)));
    }
    [Test] public void RestoredLeftFacingArrowRotatesTowardTargetFromItsClampedPosition(){
        Assert.AreEqual(0,Mathf.DeltaAngle(0,ReferenceIndicatorLayout.ArrowAngle(Vector2.left,Vector2.zero)),.001f);
        Assert.AreEqual(0,Mathf.DeltaAngle(180,ReferenceIndicatorLayout.ArrowAngle(Vector2.right,Vector2.zero)),.001f);
        Assert.AreEqual(0,Mathf.DeltaAngle(270,ReferenceIndicatorLayout.ArrowAngle(Vector2.up,Vector2.zero)),.001f);
        Assert.AreEqual(0,Mathf.DeltaAngle(90,ReferenceIndicatorLayout.ArrowAngle(Vector2.down,Vector2.zero)),.001f);
    }
}