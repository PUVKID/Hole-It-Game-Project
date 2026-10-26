using System.Collections.Generic;
using UnityEngine;
/// <summary>Data/placement checks and constructive growth simulation. This is not a timed human playthrough.</summary>
public static class LevelValidator
{
    public static List<string> Validate(LevelData level,HoleSizeTable sizes) {
        var errors=new List<string>();
        if(level==null){errors.Add("Level is null");return errors;}
        if(sizes==null||!sizes.IsValid()){errors.Add("Invalid growth table");return errors;}
        if(level.levelId<1)errors.Add("Invalid level ID");
        if(level.theme==null)errors.Add("Missing theme");
        if(!NumberGuard.Finite(level.timeLimit)||level.timeLimit<0||level.timeLimit>3600)errors.Add("Invalid time limit");
        if(!NumberGuard.Finite(level.growthMultiplier)||level.growthMultiplier<=0)errors.Add("Invalid growth multiplier");
        if(!NumberGuard.Finite(level.Size.x)||!NumberGuard.Finite(level.Size.y)||level.Size.x<8||level.Size.y<8)errors.Add("Invalid arena");
        if(level.initialSizeTier<1||level.initialSizeTier>sizes.TierCount)errors.Add("Invalid initial tier");
        if(!System.Enum.IsDefined(typeof(WrongTargetBehavior),level.defaultWrongBehavior))errors.Add("Invalid wrong-answer rule");
        if(level.missions==null||level.missions.Length==0||level.missions.Length>MissionManager.MaxMissions){errors.Add("Mission count must be 1..3");return errors;}
        if(level.placements==null||level.placements.Length==0){errors.Add("No saved placements; regenerate authored layout in Editor");return errors;}
        var ids=new HashSet<string>();
        for(int m=0;m<level.missions.Length;m++){
            var mission=level.missions[m];if(mission.requiredCount<=0)errors.Add("Invalid mission count");
            if(string.IsNullOrEmpty(mission.missionId)||!ids.Add(mission.missionId))errors.Add("Missing/duplicate mission ID");
            if(!System.Enum.IsDefined(typeof(MissionKind),mission.kind))errors.Add("Invalid mission kind");
            if(mission.kind==MissionKind.ExactObject&&mission.target==null)errors.Add("Missing exact mission target");
            if(mission.Icon==null)errors.Add("Missing mission icon");
            int available=0;for(int j=0;j<level.placements.Length;j++){var p=level.placements[j];if(!p.decorative&&p.edible!=null&&p.edible.canBeSwallowed&&mission.Matches(p.edible))available++;}
            if(available<mission.requiredCount)errors.Add("Insufficient objects for "+mission.Label);
        }
        var actual=new Dictionary<EdibleData,int>();
        Vector2 half=level.Size*0.5f;
        for(int i=0;i<level.placements.Length;i++){
            var p=level.placements[i];var d=p.edible;
            if(d==null||d.prefab==null){errors.Add("Missing data/prefab at placement "+i);continue;}
            if(string.IsNullOrEmpty(d.objectId))errors.Add("Missing object ID: "+d.name);
            if(!NumberGuard.Finite(d.footprintRadius)||d.footprintRadius<=0||!NumberGuard.Finite(d.growthValue)||d.growthValue<0)errors.Add("Invalid numeric object data: "+d.name);
            if(d.sizeTier<1||d.sizeTier>sizes.TierCount)errors.Add("Invalid object tier: "+d.name);
            if(!NumberGuard.Finite(p.position.x)||!NumberGuard.Finite(p.position.y))errors.Add("Nonfinite position");
            if(Mathf.Abs(p.position.x)+d.footprintRadius>half.x||Mathf.Abs(p.position.y)+d.footprintRadius>half.y)errors.Add("Object outside arena: "+i);
            if((p.position-level.holeStart).magnitude<level.startClearRadius+d.footprintRadius-0.001f)errors.Add("Start plaza obstructed: "+i);
            for(int j=0;j<i;j++){var q=level.placements[j];if(q.edible!=null&&(p.position-q.position).magnitude<d.footprintRadius+q.edible.footprintRadius+0.08f)errors.Add("Overlapping placements: "+j+","+i);}
            int count;actual.TryGetValue(d,out count);actual[d]=count+1;
        }
        if(level.spawnGroups==null){errors.Add("No spawn recipe");return errors;}
        var expected=new Dictionary<EdibleData,int>();
        foreach(var g in level.spawnGroups){if(g.edible==null||g.count<=0){errors.Add("Invalid spawn group");continue;}int n;expected.TryGetValue(g.edible,out n);expected[g.edible]=n+g.count;}
        foreach(var kv in expected){int n;actual.TryGetValue(kv.Key,out n);if(n!=kv.Value)errors.Add("Recipe/placement mismatch: "+kv.Key.name);}
        foreach(var kv in actual)if(!expected.ContainsKey(kv.Key))errors.Add("Placement not in recipe: "+kv.Key.name);
        if(errors.Count==0&&!HasGrowthPath(level,sizes))errors.Add("No constructive tier growth path to finish all missions");
        return errors;
    }
    public static bool HasGrowthPath(LevelData level,HoleSizeTable sizes){
        var used=new bool[level.placements.Length];var counts=new int[level.missions.Length];float growth=sizes.GetTier(level.initialSizeTier).growthRequired;
        for(int step=0;step<level.placements.Length;step++){
            int best=-1;int bestTier=int.MaxValue;
            for(int i=0;i<used.Length;i++){
                var p=level.placements[i];var d=p.edible;if(used[i]||p.decorative||d==null||!d.canBeSwallowed)continue;
                if(d.sizeTier>sizes.GetTierForGrowth(growth)||d.footprintRadius>sizes.GetRadiusForGrowth(growth)+0.001f)continue;
                bool activeTarget=false;for(int m=0;m<counts.Length;m++)if(counts[m]<level.missions[m].requiredCount&&level.missions[m].Matches(d)){activeTarget=true;break;}
                WrongTargetBehavior rule=d.overrideWrongBehavior?d.wrongBehavior:level.defaultWrongBehavior;
                if(rule==WrongTargetBehavior.Blocked&&!activeTarget)continue;
                if(d.sizeTier<bestTier){best=i;bestTier=d.sizeTier;}
            }
            if(best<0)break;used[best]=true;var edible=level.placements[best].edible;
            growth=Mathf.Min(sizes.GetTier(sizes.TierCount).growthRequired,growth+edible.growthValue*level.growthMultiplier);
            bool complete=true;for(int m=0;m<counts.Length;m++){if(level.missions[m].Matches(edible))counts[m]=Mathf.Min(counts[m]+1,level.missions[m].requiredCount);if(counts[m]<level.missions[m].requiredCount)complete=false;}
            if(complete)return true;
        }
        return false;
    }
}
