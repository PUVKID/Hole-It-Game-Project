using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
public class ReferenceTests {
    int oldUnlocked,oldSelected;bool hadUnlocked,hadSelected;
    [UnitySetUp]public IEnumerator Setup(){hadUnlocked=PlayerPrefs.HasKey("Gaeun.Ref.Unlocked");oldUnlocked=PlayerPrefs.GetInt("Gaeun.Ref.Unlocked");hadSelected=PlayerPrefs.HasKey("Gaeun.Ref.Selected");oldSelected=PlayerPrefs.GetInt("Gaeun.Ref.Selected");Time.timeScale=1;yield return SceneManager.LoadSceneAsync("Main");yield return new WaitUntil(()=>ReferenceGame.Instance!=null&&ReferenceGame.Instance.Ready);yield return new WaitForSecondsRealtime(.6f);ReferenceGame.Instance.TestInput=true;ReferenceGame.Instance.LoadLevel(0);}
    [UnityTearDown]public IEnumerator Cleanup(){var scene=SceneManager.GetSceneByName("Main");SceneManager.SetActiveScene(SceneManager.CreateScene("ReferenceCleanup"));yield return SceneManager.UnloadSceneAsync(scene);if(hadUnlocked)PlayerPrefs.SetInt("Gaeun.Ref.Unlocked",oldUnlocked);else PlayerPrefs.DeleteKey("Gaeun.Ref.Unlocked");if(hadSelected)PlayerPrefs.SetInt("Gaeun.Ref.Selected",oldSelected);else PlayerPrefs.DeleteKey("Gaeun.Ref.Selected");Time.timeScale=1;}
    [Test]public void GrowthConsumesWeightedPointsAndCapsPhysicalSize(){Assert.AreEqual(0,ReferenceGame.GrowthTier(9));Assert.AreEqual(1,ReferenceGame.GrowthTier(10));Assert.AreEqual(1,ReferenceGame.GrowthTier(29));Assert.AreEqual(2,ReferenceGame.GrowthTier(30));Assert.AreEqual(2,ReferenceGame.GrowthTier(129));Assert.AreEqual(3,ReferenceGame.GrowthTier(130));Assert.AreEqual(10,ReferenceGame.GrowthTier(2980));Assert.AreEqual(11,ReferenceGame.GrowthTier(3780));Assert.AreEqual(2.85f,ReferenceGame.GrowthRadius(2980),.001f);Assert.AreEqual(2.85f,ReferenceGame.GrowthRadius(10000),.001f);Assert.AreEqual(3.2f,ReferenceGame.GrowthVisualRadius(10000),.001f);Assert.AreEqual(3,ReferenceGame.GrowthRemainder(133));}
    [UnityTest]public IEnumerator AuthoredArenaStopsMovementAtBorder(){var g=ReferenceGame.Instance;g.LoadLevel(0);g.StartPlay();Assert.IsTrue(g.CanStandAt(new Vector3(0,0,-25)));Assert.IsFalse(g.CanStandAt(new Vector3(20,0,-25)));g.MoveHoleForTest(new Vector3(6.4f,0,-25));g.TestDirection=Vector2.right;yield return new WaitForSeconds(.4f);Assert.IsTrue(g.CanStandAt(g.HolePosition));Assert.Less(g.HolePosition.x,7);g.TestDirection=Vector2.zero;}
    [UnityTest]public IEnumerator GrowthOverlapAtBorderCanMoveBackInside(){var g=ReferenceGame.Instance;g.LoadLevel(0);var overlap=new Vector3(7.3f,0,-25);Assert.IsFalse(g.CanStandAt(overlap));var step=new Vector3(7.2f,0,-25);Assert.AreEqual(step,g.ConstrainMovement(overlap,step));var inside=new Vector3(6.7f,0,-25);Assert.AreEqual(inside,g.ConstrainMovement(step,inside));Assert.IsTrue(g.CanStandAt(inside));yield return null;}
    [Test]public void JoystickIsBoundedVelocityNotTravelDistance(){var s=new FloatingStick{Radius=50};s.Begin(new Vector2(100,100));Assert.AreEqual(Vector2.zero,s.Value);s.Move(new Vector2(150,100));Assert.AreEqual(Vector2.right,s.Value);s.Move(new Vector2(2000,100));Assert.AreEqual(Vector2.right,s.Value);s.Move(new Vector2(100,100));Assert.AreEqual(Vector2.zero,s.Value);s.Move(new Vector2(0,100));Assert.AreEqual(Vector2.left,s.Value);s.Release();Assert.IsFalse(s.Held);Assert.AreEqual(Vector2.zero,s.Value);}
    [Test]public void StageSevenEdgeBallsHavePlayableCatchPositionsWithoutRelaxingFit(){
        var g=ReferenceGame.Instance;g.LoadLevel(6);var radius=typeof(ReferenceGame).GetProperty("Radius");var visual=typeof(ReferenceGame).GetProperty("VisualRadius");float oldRadius=g.Radius,oldVisual=g.VisualRadius;
        try{
            radius.SetValue(g,1.958f);visual.SetValue(g,2.2f);
            foreach(float z in new[]{-22.93f,-15.71f}){
                var ball=new Vector3(16.92f,0,z);Assert.IsFalse(g.CanStandAt(ball));
                Assert.IsTrue(g.TryBoundaryAimForTest(ball,.27f,out var aim));Assert.IsTrue(g.CanStandAt(aim));
                Assert.LessOrEqual(Vector3.Distance(ball,aim),g.SourceApertureRadius-.27f-.01f);
                Assert.Less(aim.x,ball.x);
            }
        }finally{radius.SetValue(g,oldRadius);visual.SetValue(g,oldVisual);}
    }
    [UnityTest]public IEnumerator HoldingMovesAtSpeedLimitAndReleaseStops(){var g=ReferenceGame.Instance;g.StartPlay();g.MoveHoleForTest(new Vector3(-5,0,-27));Vector3 before=g.HolePosition;g.BeginStick(new Vector2(100,100));g.MoveStick(new Vector2(1000,100));g.TestDirection=g.Stick.Value;yield return new WaitForSeconds(.4f);float moved=Vector3.Distance(before,g.HolePosition);Assert.Greater(moved,.6f);Assert.Less(moved,g.MaximumSpeed*.55f);g.ReleaseStick();g.TestDirection=Vector2.zero;before=g.HolePosition;yield return new WaitForSeconds(.2f);Assert.Less(Vector3.Distance(before,g.HolePosition),.001f);g.BeginStick(new Vector2(400,300));Assert.AreEqual(Vector2.zero,g.Stick.Value);Assert.AreEqual(before,g.HolePosition);}
    [Test]public void StageNineLongBasketProbeExposesSupportUsingOnlyReachableHolePositions(){
        var g=ReferenceGame.Instance;g.LoadLevel(8);var radius=typeof(ReferenceGame).GetProperty("Radius");var visual=typeof(ReferenceGame).GetProperty("VisualRadius");float oldRadius=g.Radius,oldVisual=g.VisualRadius;
        try{radius.SetValue(g,2.181f);visual.SetValue(g,2.45f);var pivot=g.VerificationCenterForTest(641);
            Assert.IsTrue(g.TryProbeAimForTest(641,1,out var aim));Assert.IsTrue(g.CanStandAt(aim));Assert.Greater(Vector3.Distance(pivot,aim),.55f);Assert.AreEqual(g.SourceApertureRadius*.55f,Vector3.Distance(pivot,aim),.001f);
        }finally{radius.SetValue(g,oldRadius);visual.SetValue(g,oldVisual);}
    }
    [Test]public void RotatedSourceFloorPreservesWalkableInteriorAtGrownRadius(){
        var g=ReferenceGame.Instance;g.LoadLevel(27);Assert.AreEqual("00062",g.Catalog.levels[27].source);var radius=typeof(ReferenceGame).GetProperty("Radius");float previous=g.Radius;
        try{radius.SetValue(g,1.735f);foreach(var p in new[]{new Vector3(3.76f,0,-30.79f),new Vector3(2.52f,0,-30.54f),new Vector3(10.10f,0,-9.79f),new Vector3(12.23f,0,-11.91f),new Vector3(2.99f,0,-16.44f)})Assert.IsTrue(g.CanStandAt(p),"Authored45-degree floor and all12 character-radius samples contain "+p);}
        finally{radius.SetValue(g,previous);}
    }
    [Test]public void TightCapsuleTestProbesFollowItsActualAxisAndStartWithinClearance(){
        var obj=new GameObject("Capsule probe geometry");try{
            obj.transform.SetPositionAndRotation(new Vector3(3,1,-7),Quaternion.Euler(90,37,0));obj.transform.localScale=Vector3.one*4.61f;var capsule=obj.AddComponent<CapsuleCollider>();capsule.center=new Vector3(0,.63607645f,.006825f);capsule.radius=.33530998f;capsule.height=1.27587295f;
            float aperture=1.68937f,clearance=aperture-capsule.radius*4.61f;var method=typeof(ReferenceGame).GetMethod("CapsuleVerificationAim",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            var center=(Vector3)method.Invoke(null,new object[]{capsule,aperture,0});var axis=obj.transform.up;axis.y=0;axis.Normalize();
            for(int probe=1;probe<=10;probe++){var aim=(Vector3)method.Invoke(null,new object[]{capsule,aperture,probe});var offset=aim-center;Assert.Less((offset-Vector3.Project(offset,axis)).magnitude,.0001f);Assert.AreEqual(0,aim.y);if(probe<=4)Assert.LessOrEqual(offset.magnitude,clearance+.0001f);}
        }finally{Object.DestroyImmediate(obj);}
    }
    [UnityTest]public IEnumerator TimersPauseAndRetryUseOriginalBudget(){var g=ReferenceGame.Instance;Assert.AreEqual(300,g.Remaining);g.StartPlay();yield return new WaitForSeconds(.1f);g.Pause();float time=g.Remaining;yield return new WaitForSeconds(.1f);Assert.AreEqual(time,g.Remaining);g.Resume();g.SetCountdownForTest(.05f);yield return new WaitForSeconds(.15f);Assert.AreEqual(ReferenceGame.ScreenState.Lost,g.State);Assert.AreEqual("TIME UP",g.FailureReason);g.Retry();Assert.Greater(g.Remaining,299);Assert.AreEqual(0,g.Collected);}
    [UnityTest]public IEnumerator BombLosesAndRetryResets(){var g=ReferenceGame.Instance;g.LoadLevel(20);g.StartPlay();g.TriggerBombForTest();yield return new WaitForSeconds(1.5f);Assert.AreEqual(ReferenceGame.ScreenState.Lost,g.State);Assert.AreEqual("BOOM! TRY AGAIN",g.FailureReason);Assert.AreEqual(0,g.Collected);g.Retry();Assert.AreEqual(ReferenceGame.ScreenState.Playing,g.State);Assert.Greater(g.Remaining,79);}
    [UnityTest]public IEnumerator FirstStageCompletesUsingCappedMovement(){var g=ReferenceGame.Instance;g.StartPlay();float start=Time.realtimeSinceStartup,deadline=start+50;while(g.State==ReferenceGame.ScreenState.Playing&&Time.realtimeSinceStartup<deadline){Vector3 d=g.NearestCollectible()-g.HolePosition;g.TestDirection=d.magnitude>.12f?new Vector2(d.x,d.z).normalized:Vector2.zero;yield return null;}g.TestDirection=Vector2.zero;Assert.AreEqual(ReferenceGame.ScreenState.Won,g.State,"Collected "+g.Collected);Assert.AreEqual(1.066f,ReferenceGame.GrowthRadius(g.GrowthPoints),.001f);}
    [UnityTest]public IEnumerator AllLevelsHaveGoalsAndBoundedSceneObjects(){var g=ReferenceGame.Instance;var report=new System.Text.StringBuilder();for(int i=0;i<30;i++){g.LoadLevel(i);yield return null;var l=g.Catalog.levels[i];Assert.GreaterOrEqual(g.Remaining,80);Assert.AreEqual(l.points.Length+g.BombPositionsForTest().Length,g.ActorCount);var counts=l.points.GroupBy(p=>p.id).ToDictionary(x=>x.Key,x=>x.Count());foreach(var goal in l.goals)Assert.GreaterOrEqual(counts[goal.id],goal.amount);int objects=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;Assert.AreEqual(g.ActorCount,g.ActivePhysicsBodies,"원본처럼 모든 물체의 지지 충돌체와 강체가 존재해야 합니다.");foreach(var t in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include,FindObjectsSortMode.None)){Assert.AreEqual("Linik Sans",t.font.faceInfo.familyName);Assert.AreEqual("Black",t.font.faceInfo.styleName);Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(t.text,"[가-힣]"));}report.AppendLine("Level "+(i+1)+": "+g.ActorCount+" actors, "+objects+" scene transforms, timer "+l.time);}
        Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/ReferenceStructures.txt",report.ToString());}
    [UnityTest,Timeout(1800000)]public IEnumerator AllThirtyStagesCompleteThroughMouthAndStackSimulation(){
        var g=ReferenceGame.Instance;var total=System.Diagnostics.Stopwatch.StartNew();
        var report=new System.Text.StringBuilder("Physical reachability audit. Test teleports between targets at timeScale4 with the original0.02s physics step; this is NOT a human timing benchmark. Source timers are informational. Watchdogs:45 simulated seconds without collection, a new target/probe attempt, or active downward progress,1200 simulated seconds/stage,30 real minutes overall. Repeated attempts do not reset the progress clock.\n");
        Directory.CreateDirectory("TestResults");Time.timeScale=4;var failedStages=new List<string>();
        try {
            for(int l=0;l<30;l++){
                g.LoadLevel(l);g.StartPlay();var stageClock=System.Diagnostics.Stopwatch.StartNew();int lastCollected=0;float lastProgress=0,nextTrace=0;string stop="state";var attemptedSteps=new HashSet<int>();
                var trace=new System.Text.StringBuilder("Stage "+(l+1)+" test-only accelerated physical trace.\n");string tracePath="TestResults/ReferenceStage_"+(l+1).ToString("00")+".trace.txt";
                while(g.State==ReferenceGame.ScreenState.Playing){
                    g.MoveHoleForTest(g.NearestCollectible());yield return null;
                    if(g.Collected>lastCollected){lastCollected=g.Collected;lastProgress=g.SimulatedPhysicsTime;attemptedSteps.Clear();}
                    int inputStep=g.VerificationInputStepForTest;if(inputStep>=0&&attemptedSteps.Add(inputStep)){lastProgress=g.SimulatedPhysicsTime;trace.AppendLine("New input attempt "+inputStep+" at "+lastProgress.ToString("F2"));}
                    if(g.VerificationFallingProgressForTest)lastProgress=g.SimulatedPhysicsTime;
                    if(g.SimulatedPhysicsTime>=nextTrace){nextTrace=g.SimulatedPhysicsTime+5;trace.AppendLine(g.VerificationDiagnosticForTest());File.WriteAllText(tracePath,trace.ToString());}
                    if(g.SimulatedPhysicsTime-lastProgress>45){stop="45 simulated seconds without collection, a new input attempt, or active downward progress";break;}
                    if(g.SimulatedPhysicsTime>1200){stop="1200 simulated seconds stage limit";break;}
                    if(total.Elapsed.TotalSeconds>1790){stop="30 minute real-time safety limit";break;}
                }
                report.AppendLine("Level "+(l+1)+": "+g.State+"; collected="+g.Collected+"; radius="+g.Radius+"; simulated="+g.SimulatedPhysicsTime.ToString("F2")+"; realElapsed="+stageClock.Elapsed.TotalSeconds.ToString("F2")+"; sourceBudget="+g.Catalog.levels[l].time+"; goals="+string.Join(",",g.GoalCounts)+"; stop="+stop+(g.State==ReferenceGame.ScreenState.Won?"":"; diagnostic="+g.VerificationDiagnosticForTest()+"; remaining="+g.RemainingDiagnostic()));
                File.WriteAllText("TestResults/ReferenceCampaign.txt",report.ToString());
                if(g.State!=ReferenceGame.ScreenState.Won)failedStages.Add("Stage "+(l+1)+" "+stop+"; "+g.VerificationDiagnosticForTest());
                if(total.Elapsed.TotalSeconds>1790){if(l<29)failedStages.Add("Stages "+(l+2)+"-30 not run because overall safety limit was reached");break;}
            }
            Assert.IsEmpty(failedStages,string.Join("\n",failedStages));
        }finally{Time.timeScale=1;}
    }

}

