
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
public class CampaignEditModeTests
{
    [Test] public void AllThirtyAuthoredLevelsPassDataAndGrowthValidation(){
        var sizes=AssetDatabase.LoadAssetAtPath<HoleSizeTable>("Assets/HoleIt/Resources/HoleSizeTable.asset");Assert.IsNotNull(sizes);Assert.IsTrue(sizes.IsValid());
        var guids=AssetDatabase.FindAssets("t:LevelData",new[]{"Assets/HoleIt/Resources/Data/Levels"});Assert.AreEqual(30,guids.Length);
        var ids=new HashSet<int>();foreach(var guid in guids){var level=AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));Assert.IsTrue(ids.Add(level.levelId));Assert.IsEmpty(LevelValidator.Validate(level,sizes),level.name);}
    }
    [Test] public void RebuiltGridIsDeterministic(){
        var level=AssetDatabase.LoadAssetAtPath<LevelData>("Assets/HoleIt/Resources/Data/Levels/Level_27.asset");var a=LevelLayout.Build(level);var b=LevelLayout.Build(level);Assert.AreEqual(a.Length,b.Length);
        for(int i=0;i<a.Length;i++){Assert.AreEqual(a[i].edible,b[i].edible);Assert.AreEqual(a[i].position,b[i].position);}
    }
    [Test] public void ImportedMeshesHaveFiniteData(){foreach(string guid in AssetDatabase.FindAssets("t:Mesh",new[]{"Assets/HoleIt/Meshes"}))HoleItProjectValidation.CheckMesh(AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid)));}
    [Test] public void ColourMatchingUsesMetadataAndCategory(){
        var a=ScriptableObject.CreateInstance<EdibleData>();var b=ScriptableObject.CreateInstance<EdibleData>();
        try {a.colorVariant=ColorVariant.Red;a.objectType=ObjectType.Car;b.colorVariant=ColorVariant.Blue;b.objectType=ObjectType.Car;
            var mission=new MissionData{kind=MissionKind.Color,color=ColorVariant.Red,category=ObjectType.Car};Assert.IsTrue(mission.Matches(a));Assert.IsFalse(mission.Matches(b));a.objectType=ObjectType.Food;Assert.IsFalse(mission.Matches(a));
            mission.kind=MissionKind.Tier;mission.tier=2;a.sizeTier=2;Assert.IsTrue(mission.Matches(a));
            mission.kind=MissionKind.TimeBooster;a.objectType=ObjectType.TimeBoost;Assert.IsTrue(mission.Matches(a));
        } finally {UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);}
    }
    [Test] public void MissionCompletionIsBoundedAndAnnouncedOnce(){
        var go=new GameObject("MissionTest");var manager=go.AddComponent<MissionManager>();var data=ScriptableObject.CreateInstance<EdibleData>();var other=ScriptableObject.CreateInstance<EdibleData>();var level=ScriptableObject.CreateInstance<LevelData>();
        try {level.missions=new[]{new MissionData{kind=MissionKind.ExactObject,target=data,requiredCount=2}};manager.LoadLevel(level);int completed=0;manager.OnAllCompleted+=()=>completed++;
            Assert.IsFalse(manager.Register(other));Assert.AreEqual(0,manager.GetCount(0));manager.Register(data);manager.Register(data);manager.Register(data);Assert.AreEqual(2,manager.GetCount(0));Assert.AreEqual(1,completed);Assert.IsTrue(manager.AllCompleted);
        }finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(level);UnityEngine.Object.DestroyImmediate(data);UnityEngine.Object.DestroyImmediate(other);}
    }
    [Test] public void ReservationsPreventBlockedQueueOverbooking(){
        var go=new GameObject("ReservationTest");var manager=go.AddComponent<MissionManager>();var data=ScriptableObject.CreateInstance<EdibleData>();var level=ScriptableObject.CreateInstance<LevelData>();
        try{level.missions=new[]{new MissionData{target=data,requiredCount=1}};manager.LoadLevel(level);Assert.IsTrue(manager.ReserveTarget(data));Assert.IsFalse(manager.ReserveTarget(data));manager.ReleaseReservation(data);manager.Register(data);Assert.IsFalse(manager.CanReserveTarget(data));}
        finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(level);UnityEngine.Object.DestroyImmediate(data);}
    }
    [Test] public void MissingAndUnreachableLevelDataAreRejected(){var size=ScriptableObject.CreateInstance<HoleSizeTable>();try{Assert.IsNotEmpty(LevelValidator.Validate(null,size));var level=ScriptableObject.CreateInstance<LevelData>();try{Assert.IsNotEmpty(LevelValidator.Validate(level,size));}finally{UnityEngine.Object.DestroyImmediate(level);}}finally{UnityEngine.Object.DestroyImmediate(size);}}
    [TestCase(-5L,0)][TestCase(2147483648L,int.MaxValue)][TestCase(42L,42)] public void ScoresAreSaturated(long input,int expected){Assert.AreEqual(expected,NumberGuard.Score(input));}
    [Test] public void GrowthThresholdsAreMonotonic(){var s=ScriptableObject.CreateInstance<HoleSizeTable>();try{Assert.IsTrue(s.IsValid());for(int i=1;i<=s.TierCount;i++)Assert.AreEqual(i,s.GetTierForGrowth(s.GetTier(i).growthRequired));Assert.IsFalse(NumberGuard.Finite(float.NaN));Assert.IsFalse(NumberGuard.Finite(float.PositiveInfinity));}finally{UnityEngine.Object.DestroyImmediate(s);}}
    [Test] public void PerLevelTopThreeSortAndPersistWithoutResettingUserData(){
        string[] keys={"HoleIt.V2.L30.Score0","HoleIt.V2.L30.Score1","HoleIt.V2.L30.Score2","HoleIt.SaveVersion"};bool[] exists=new bool[keys.Length];int[] old=new int[keys.Length];
        for(int i=0;i<keys.Length;i++){exists[i]=PlayerPrefs.HasKey(keys[i]);old[i]=PlayerPrefs.GetInt(keys[i]);PlayerPrefs.DeleteKey(keys[i]);}
        GameObject go=null;
        try{go=new GameObject("SaveTest");var h=go.AddComponent<HighScoreManager>();h.SelectLevel(30);h.Submit(100);h.Submit(300);h.Submit(200);h.Submit(50);h.Load();Assert.AreEqual(300,h.GetScore(0));Assert.AreEqual(200,h.GetScore(1));Assert.AreEqual(100,h.GetScore(2));}
        finally{if(go!=null)UnityEngine.Object.DestroyImmediate(go);for(int i=0;i<keys.Length;i++){if(exists[i])PlayerPrefs.SetInt(keys[i],old[i]);else PlayerPrefs.DeleteKey(keys[i]);}PlayerPrefs.Save();}
    }
    [Test] public void PoolReturnIsIdempotent(){
        var go=new GameObject("PoolTest");var pool=go.AddComponent<ObjectPooler>();var prefab=new GameObject("PooledTestObject");prefab.SetActive(false);GameObject a=null,b=null;
        try{pool.SendMessage("Awake");pool.Prewarm(prefab,1);a=pool.Get(prefab,Vector3.zero,Quaternion.identity);Assert.AreEqual(1,pool.ActiveCount);pool.Return(a);pool.Return(a);Assert.AreEqual(0,pool.ActiveCount);a=pool.Get(prefab,Vector3.zero,Quaternion.identity);b=pool.Get(prefab,Vector3.zero,Quaternion.identity);Assert.AreNotSame(a,b);Assert.AreEqual(2,pool.ActiveCount);pool.ReturnAllActive();Assert.AreEqual(0,pool.ActiveCount);}
        finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(prefab);}
    }
}
