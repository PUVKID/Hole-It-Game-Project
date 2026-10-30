using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ReferenceSeventhStageProbeTests {
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
