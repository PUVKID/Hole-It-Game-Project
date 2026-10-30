from pathlib import Path
p=Path(__file__).parent/'AndroidFinal/Assets/Scripts/ReferenceGame.cs'
s=p.read_text()
s=s.replace('readonly List<EnvironmentDraw> environmentDraws=', 'readonly List<Rect> playground=new List<Rect>();\n    readonly List<EnvironmentDraw> environmentDraws=')
s=s.replace('environmentDraws.Clear();environmentBatches.Clear();','environmentDraws.Clear();environmentBatches.Clear();playground.Clear();')
s=s.replace('if(model==null)continue;var q=', '''if(model==null)continue;if(p.kind==5){float x=p.position[0],z=p.position[2],rx=model.bounds.extents.x*p.scale[0],rz=model.bounds.extents.z*p.scale[2];playground.Add(Rect.MinMaxRect(x-rx,z-rz,x+rx,z+rz));}var q=''')
s=s.replace('HolePosition+=new Vector3(Velocity.x,0,Velocity.y)*dt;', 'HolePosition=ConstrainMovement(HolePosition,HolePosition+new Vector3(Velocity.x,0,Velocity.y)*dt);')
s=s.replace('public void StartPlay(){', '''bool OnPlayground(Vector2 p){if(playground.Count==0)return true;foreach(var tile in playground)if(tile.Contains(p))return true;return false;}
    public bool CanStandAt(Vector3 p){if(!OnPlayground(new Vector2(p.x,p.z)))return false;for(int i=0;i<12;i++){float a=i*Mathf.PI/6;if(!OnPlayground(new Vector2(p.x+Mathf.Cos(a)*Radius,p.z+Mathf.Sin(a)*Radius)))return false;}return true;}
    public Vector3 ConstrainMovement(Vector3 from,Vector3 target){if(CanStandAt(target))return target;var alongX=new Vector3(target.x,0,from.z);if(CanStandAt(alongX))return alongX;var alongZ=new Vector3(from.x,0,target.z);return CanStandAt(alongZ)?alongZ:from;}
    public void StartPlay(){''')
p.write_text(s)
p=Path(__file__).parent/'AndroidFinal/Assets/Tests/Reference/ReferenceTests.cs';s=p.read_text(encoding='utf-8')
s=s.replace('    [Test]public void Joystick', '''    [UnityTest]public IEnumerator AuthoredArenaStopsMovementAtBorder(){var g=ReferenceGame.Instance;g.LoadLevel(0);g.StartPlay();Assert.IsTrue(g.CanStandAt(new Vector3(0,0,-25)));Assert.IsFalse(g.CanStandAt(new Vector3(20,0,-25)));g.MoveHoleForTest(new Vector3(6.4f,0,-25));g.TestDirection=Vector2.right;yield return new WaitForSeconds(.4f);Assert.IsTrue(g.CanStandAt(g.HolePosition));Assert.Less(g.HolePosition.x,7);g.TestDirection=Vector2.zero;}
    [Test]public void Joystick''')
p.write_text(s,encoding='utf-8')
