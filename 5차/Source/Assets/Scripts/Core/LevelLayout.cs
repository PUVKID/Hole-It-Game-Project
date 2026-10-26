using System.Collections.Generic;
using UnityEngine;
/// <summary>Deterministic grid clustering with exact circle separation; generation only during loading/editor use.</summary>
public static class LevelLayout
{
    private struct Candidate { public Vector2 p; public float weight; public int order; }
    private struct Item { public SpawnGroup group; public int order; }
    private struct Random32 {
        private uint state;
        public Random32(int seed){state=(uint)seed; if(state==0)state=1;}
        public float Next(){state^=state<<13;state^=state>>17;state^=state<<5;return (state & 0xFFFFFF)/16777216f;}
    }
    public static SpawnPlacement[] Build(LevelData level) {
        if(level==null||level.spawnGroups==null)throw new System.ArgumentException("Missing level/spawn groups.");
        var items=new List<Item>();
        for(int g=0;g<level.spawnGroups.Length;g++)for(int n=0;n<level.spawnGroups[g].count;n++)items.Add(new Item{group=level.spawnGroups[g],order=items.Count});
        items.Sort((a,b)=> {int cmp=b.group.edible.footprintRadius.CompareTo(a.group.edible.footprintRadius);return cmp!=0?cmp:a.order.CompareTo(b.order);});
        var placed=new List<SpawnPlacement>(items.Count); var rng=new Random32(level.randomSeed);
        Vector2 half=level.Size*0.5f;int cols=Mathf.Max(1,level.gridSize.x),rows=Mathf.Max(1,level.gridSize.y);
        for(int itemIndex=0;itemIndex<items.Count;itemIndex++) {
            Item item=items[itemIndex];float radius=item.group.edible.footprintRadius;
            Vector2 cluster=item.group.center;
            var cells=new List<Candidate>(cols*rows);
            for(int y=0;y<rows;y++)for(int x=0;x<cols;x++) {
                Vector2 p=new Vector2((x+0.5f)*level.Size.x/cols-half.x,(y+0.5f)*level.Size.y/rows-half.y);
                float distance=(p-cluster).sqrMagnitude;
                if(item.group.ring)distance=Mathf.Abs((p-cluster).magnitude-item.group.radius)*4;
                cells.Add(new Candidate{p=p,weight=distance+rng.Next()*1.7f,order=y*cols+x});
            }
            cells.Sort((a,b)=>{int c=a.weight.CompareTo(b.weight);return c!=0?c:a.order.CompareTo(b.order);});
            bool found=false;
            for(int c=0;c<cells.Count;c++) {
                Vector2 p=cells[c].p;
                if(Mathf.Abs(p.x)+radius+0.15f>half.x||Mathf.Abs(p.y)+radius+0.15f>half.y)continue;
                if((p-level.holeStart).magnitude<level.startClearRadius+radius)continue;
                // Rounded corner exclusion keeps every placement reachable after maximum growth.
                float largest=3.4f,margin=largest*1.05f+0.12f;
                Vector2 nearest=new Vector2(Mathf.Clamp(p.x,-Mathf.Max(0,half.x-margin),Mathf.Max(0,half.x-margin)),Mathf.Clamp(p.y,-Mathf.Max(0,half.y-margin),Mathf.Max(0,half.y-margin)));
                if((p-nearest).magnitude>largest-0.05f)continue;
                bool free=true;
                for(int q=0;q<placed.Count;q++)if((p-placed[q].position).magnitude<radius+placed[q].edible.footprintRadius+0.10f){free=false;break;}
                if(!free)continue;
                placed.Add(new SpawnPlacement{edible=item.group.edible,position=p,yaw=180,decorative=item.group.decorative});found=true;break;
            }
            if(!found)throw new System.InvalidOperationException("Level "+level.levelId+": no non-overlapping grid cell for "+item.group.edible.name);
        }
        return placed.ToArray();
    }
}
