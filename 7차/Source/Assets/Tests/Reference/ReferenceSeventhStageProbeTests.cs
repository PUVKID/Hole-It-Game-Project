using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ReferenceSeventhStageProbeTests {
    [UnityTest,Timeout(720000)] public IEnumerator UnresolvedStagesSupportFootprintsTrace(){
        yield return SceneManager.LoadSceneAsync("Main");yield return new WaitUntil(()=>ReferenceGame.Instance!=null&&ReferenceGame.Instance.Ready);
        var g=ReferenceGame.Instance;g.TestInput=true;Time.timeScale=4;var failures=new System.Collections.Generic.List<string>();var summary=new StringBuilder("Focused reachability diagnostics, not a human timing benchmark. No actor/physics mutations.\n");var total=System.Diagnostics.Stopwatch.StartNew();Directory.CreateDirectory("TestResults");
        try{foreach(int index in new[]{10,13,18,24,25,27}){
            g.LoadLevel(index);g.StartPlay();float lastProgress=0,next=0;int lastCount=0;var steps=new System.Collections.Generic.HashSet<int>();var report=new StringBuilder();string path="TestResults/SeventhFocusedStage_"+(index+1)+".txt";
            while(g.State==ReferenceGame.ScreenState.Playing&&g.SimulatedPhysicsTime<1200&&total.Elapsed.TotalSeconds<700){
                g.MoveHoleForTest(g.NearestCollectible());yield return null;
                if(g.Collected>lastCount){lastCount=g.Collected;lastProgress=g.SimulatedPhysicsTime;steps.Clear();}
                int step=g.VerificationInputStepForTest;if(step>=0&&steps.Add(step)){lastProgress=g.SimulatedPhysicsTime;report.AppendLine("New input attempt "+step+" at "+lastProgress.ToString("F2"));}if(g.VerificationFallingProgressForTest)lastProgress=g.SimulatedPhysicsTime;
                if(g.SimulatedPhysicsTime>=next){next=g.SimulatedPhysicsTime+2;report.AppendLine(g.VerificationDiagnosticForTest());File.WriteAllText(path,report.ToString());}
                if(g.SimulatedPhysicsTime-lastProgress>45)break;
            }
            string result="Stage "+(index+1)+": "+g.State+"; goals="+string.Join(",",g.GoalCounts)+"; "+g.VerificationDiagnosticForTest();summary.AppendLine(result);report.AppendLine(result);report.AppendLine("Remaining: "+g.RemainingDiagnostic());report.AppendLine("Bombs: "+string.Join(";",g.BombPositionsForTest()));report.Append(RemainingProbeDetails(g));File.WriteAllText(path,report.ToString());File.WriteAllText("TestResults/SeventhFocusedStages.txt",summary.ToString());if(g.State!=ReferenceGame.ScreenState.Won)failures.Add(result);
        }Assert.IsEmpty(failures,string.Join("\n",failures));}finally{Time.timeScale=1;}
    }
    [UnityTest,Timeout(340000)]public IEnumerator StageTwentySixCapsuleAxisTrace(){
        yield return SceneManager.LoadSceneAsync("Main");yield return new WaitUntil(()=>ReferenceGame.Instance!=null&&ReferenceGame.Instance.Ready);var g=ReferenceGame.Instance;g.TestInput=true;g.LoadLevel(25);g.StartPlay();Time.timeScale=4;var report=new StringBuilder("Stage26 sole-capsule axial input trace. Unchanged physics; not a human timing benchmark.\n");var clock=System.Diagnostics.Stopwatch.StartNew();float next=0,lastProgress=0;int lastCount=0;var steps=new System.Collections.Generic.HashSet<int>();Directory.CreateDirectory("TestResults");
        try{while(g.State==ReferenceGame.ScreenState.Playing&&g.SimulatedPhysicsTime<1200&&clock.Elapsed.TotalSeconds<320){g.MoveHoleForTest(g.NearestCollectible());yield return null;if(g.Collected>lastCount){lastCount=g.Collected;lastProgress=g.SimulatedPhysicsTime;steps.Clear();}int step=g.VerificationInputStepForTest;if(step>=0&&steps.Add(step))lastProgress=g.SimulatedPhysicsTime;if(g.VerificationFallingProgressForTest)lastProgress=g.SimulatedPhysicsTime;if(g.SimulatedPhysicsTime>=next){next=g.SimulatedPhysicsTime+2;report.AppendLine(g.VerificationDiagnosticForTest());File.WriteAllText("TestResults/SeventhStageTwentySixTrace.txt",report.ToString());}if(g.SimulatedPhysicsTime-lastProgress>45)break;}
            report.AppendLine("Result="+g.State+" goals="+string.Join(",",g.GoalCounts)+"; "+g.VerificationDiagnosticForTest());report.Append(RemainingProbeDetails(g));File.WriteAllText("TestResults/SeventhStageTwentySixTrace.txt",report.ToString());Assert.AreEqual(ReferenceGame.ScreenState.Won,g.State,g.VerificationDiagnosticForTest());
        }finally{Time.timeScale=1;}
    }
    static string RemainingProbeDetails(ReferenceGame g){
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var type=typeof(ReferenceGame);var items=(System.Collections.IList)type.GetField("actors",flags).GetValue(g);var report=new StringBuilder();
        for(int i=0;i<items.Count;i++){var a=items[i];if((int)a.GetType().GetField("state").GetValue(a)!=0)continue;var model=(ReferenceModel)a.GetType().GetField("model").GetValue(a);if(model.id<0)continue;var bounds=(Bounds)type.GetMethod("VerificationBounds",flags).Invoke(g,new object[]{a});float fit=(float)type.GetMethod("SourceFitRadius",flags).Invoke(g,new object[]{a});report.Append("RemainingActor ").Append(i).Append('/').Append(model.id).Append(" center=").Append(bounds.center).Append(" size=").Append(bounds.size).Append(" fit=").Append(fit);
            for(int probe=0;probe<=8;probe++){bool valid=g.TryProbeAimForTest(i,probe,out var aim);report.Append(" p").Append(probe).Append('=').Append(valid).Append(':').Append(aim);}report.AppendLine();}
        return report.ToString();
    }
    [UnityTest,Timeout(160000)] public IEnumerator StageFourteenLastSupportTrace(){
        yield return SceneManager.LoadSceneAsync("Main");yield return new WaitUntil(()=>ReferenceGame.Instance!=null&&ReferenceGame.Instance.Ready);
        var g=ReferenceGame.Instance;g.TestInput=true;g.LoadLevel(13);g.StartPlay();Time.timeScale=4;
        var report=new StringBuilder("Stage14 support probe with hole input only.\n");float deadline=Time.realtimeSinceStartup+140,next=0,lastProgress=0;int lastCount=0;
        Directory.CreateDirectory("TestResults");
        try{
            while(g.State==ReferenceGame.ScreenState.Playing&&Time.realtimeSinceStartup<deadline){
                g.MoveHoleForTest(g.NearestCollectible());yield return null;
                if(g.Collected>lastCount){lastCount=g.Collected;lastProgress=g.SimulatedPhysicsTime;}
                if(g.SimulatedPhysicsTime>=next){next=g.SimulatedPhysicsTime+1;report.AppendLine(g.VerificationDiagnosticForTest());File.WriteAllText("TestResults/SeventhStageFourteenTrace.txt",report.ToString());}
                if(g.SimulatedPhysicsTime-lastProgress>45)break;
            }
            report.AppendLine("Result="+g.State+" goals="+string.Join(",",g.GoalCounts)+"; "+g.VerificationDiagnosticForTest()+"; remaining="+g.RemainingDiagnostic());File.WriteAllText("TestResults/SeventhStageFourteenTrace.txt",report.ToString());
            Assert.AreEqual(ReferenceGame.ScreenState.Won,g.State,g.VerificationDiagnosticForTest());
        }finally{Time.timeScale=1;}
    }
    [UnityTest,Timeout(160000)] public IEnumerator StageElevenDiagonalSupportTrace(){
        yield return SceneManager.LoadSceneAsync("Main");yield return new WaitUntil(()=>ReferenceGame.Instance!=null&&ReferenceGame.Instance.Ready);
        var g=ReferenceGame.Instance;g.TestInput=true;g.LoadLevel(10);g.StartPlay();Time.timeScale=4;
        var report=new StringBuilder("Stage11 diagonal support probe with hole input only.\n");float deadline=Time.realtimeSinceStartup+140,next=0,lastProgress=0;int lastCount=0;
        Directory.CreateDirectory("TestResults");
        try{
            while(g.State==ReferenceGame.ScreenState.Playing&&Time.realtimeSinceStartup<deadline){
                g.MoveHoleForTest(g.NearestCollectible());yield return null;
                if(g.Collected>lastCount){lastCount=g.Collected;lastProgress=g.SimulatedPhysicsTime;}
                if(g.SimulatedPhysicsTime>=next){next=g.SimulatedPhysicsTime+1;var diagnostic=g.VerificationDiagnosticForTest();report.AppendLine(diagnostic);if(g.Collected>330&&diagnostic.EndsWith("target=none"))report.Append(RemainingProbeDetails(g));File.WriteAllText("TestResults/SeventhStageElevenTrace.txt",report.ToString());}
                if(g.SimulatedPhysicsTime-lastProgress>45)break;
            }
            report.AppendLine("Result="+g.State+" goals="+string.Join(",",g.GoalCounts)+"; "+g.VerificationDiagnosticForTest());
            report.AppendLine("Remaining: "+g.RemainingDiagnostic());report.AppendLine("Bombs: "+string.Join(";",g.BombPositionsForTest()));
            var items=(System.Collections.IList)typeof(ReferenceGame).GetField("actors",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(g);
            for(int i=0;i<items.Count;i++){var actor=items[i];if((int)actor.GetType().GetField("state").GetValue(actor)!=0)continue;var line="Item "+i+" center="+g.VerificationCenterForTest(i);for(int probe=0;probe<=8;probe++){bool valid=g.TryProbeAimForTest(i,probe,out var aim);line+=" p"+probe+"="+valid+":"+aim;}report.AppendLine(line);}
            File.WriteAllText("TestResults/SeventhStageElevenTrace.txt",report.ToString());
            Assert.AreEqual(ReferenceGame.ScreenState.Won,g.State,g.VerificationDiagnosticForTest());
        }finally{Time.timeScale=1;}
    }
    [UnityTest,Timeout(160000)] public IEnumerator StageNineSupportRemovalTrace(){
        yield return SceneManager.LoadSceneAsync("Main");yield return new WaitUntil(()=>ReferenceGame.Instance!=null&&ReferenceGame.Instance.Ready);
        var g=ReferenceGame.Instance;g.TestInput=true;g.LoadLevel(8);g.StartPlay();Time.timeScale=4;
        var report=new StringBuilder("Stage9 physical support-removal probe with hole input only.\\n");float deadline=Time.realtimeSinceStartup+140,next=0,lastProgress=0;int lastCount=0;
        Directory.CreateDirectory("TestResults");
        try{
            while(g.State==ReferenceGame.ScreenState.Playing&&Time.realtimeSinceStartup<deadline){
                g.MoveHoleForTest(g.NearestCollectible());yield return null;
                if(g.Collected>lastCount){lastCount=g.Collected;lastProgress=g.SimulatedPhysicsTime;}
                if(g.SimulatedPhysicsTime>=next){next=g.SimulatedPhysicsTime+1;report.AppendLine(g.VerificationDiagnosticForTest());File.WriteAllText("TestResults/SeventhStageNineTrace.txt",report.ToString());}
                if(g.SimulatedPhysicsTime-lastProgress>45)break;
            }
            report.AppendLine("Result="+g.State+" goals="+string.Join(",",g.GoalCounts)+"; "+g.VerificationDiagnosticForTest());File.WriteAllText("TestResults/SeventhStageNineTrace.txt",report.ToString());
            Assert.AreEqual(ReferenceGame.ScreenState.Won,g.State,g.VerificationDiagnosticForTest());
        }finally{Time.timeScale=1;}
    }
    [UnityTest,Timeout(160000)] public IEnumerator StageThreePhysicalProgressTrace(){
        yield return SceneManager.LoadSceneAsync("Main");yield return new WaitUntil(()=>ReferenceGame.Instance!=null&&ReferenceGame.Instance.Ready);
        var g=ReferenceGame.Instance;g.TestInput=true;g.LoadLevel(2);g.StartPlay();
        var report=new StringBuilder("Stage3 physical progress with test input only; maximum140 real seconds.\n");
        float deadline=Time.realtimeSinceStartup+140,next=0;
        Directory.CreateDirectory("TestResults");
        while(g.State==ReferenceGame.ScreenState.Playing&&Time.realtimeSinceStartup<deadline){
            g.MoveHoleForTest(g.NearestCollectible());yield return null;
            if(g.SimulatedPhysicsTime>=next){next=g.SimulatedPhysicsTime+1;report.AppendLine(g.VerificationDiagnosticForTest());File.WriteAllText("TestResults/SeventhStageThreeTrace.txt",report.ToString());}
        }
        report.AppendLine("Result="+g.State+" collected="+g.Collected+" remaining="+g.RemainingDiagnostic());File.WriteAllText("TestResults/SeventhStageThreeTrace.txt",report.ToString());
        Assert.AreEqual(ReferenceGame.ScreenState.Won,g.State,g.VerificationDiagnosticForTest());
    }
}
