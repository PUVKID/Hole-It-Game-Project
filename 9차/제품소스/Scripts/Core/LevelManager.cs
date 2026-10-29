using System.Collections.Generic;
using UnityEngine;
public class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelData[] levels;
    [SerializeField] private ObjectPooler pooler;
    [SerializeField] private HoleController hole;
    [SerializeField] private HoleSwallow holeSwallow;
    [SerializeField] private MissionManager missionManager;
    [SerializeField] private Transform ground;
    [SerializeField] private Transform consumeZone;
    [SerializeField] private ArenaPresenter arena;
    // Old serialized field names retained; LevelData now owns per-level placement margins.
#pragma warning disable CS0414
    [SerializeField] private float startClearRadius=2.1f;
    [SerializeField] private float edgeMargin=0.15f;
#pragma warning restore CS0414
    private readonly Dictionary<GameObject,int> prewarmCounts=new Dictionary<GameObject,int>(64);
    public int LevelCount{get{return levels!=null?levels.Length:0;}}
    public LevelData Current{get;private set;}
    public int CurrentIndex{get;private set;}
    public LevelData GetLevel(int index){return levels!=null&&index>=0&&index<levels.Length?levels[index]:null;}
    public void StopSwallows(){if(holeSwallow!=null)holeSwallow.ClearAll();}
    public bool Load(int index){
        Current=null;
        if(index<0||index>=LevelCount||pooler==null||hole==null||holeSwallow==null||missionManager==null){Debug.LogError("[HoleIt] Invalid level index or missing serialized LevelManager references.");return false;}
        LevelData level=levels[index];var problems=LevelValidator.Validate(level,hole.SizeTable);
        if(problems.Count>0){Debug.LogError("[HoleIt] Level "+(index+1)+": "+string.Join("; ",problems));return false;}
        StopSwallows();pooler.ReturnAllActive();CurrentIndex=index;Current=level;
        if(ground!=null)ground.localScale=new Vector3(level.Size.x/10f,1f,level.Size.y/10f);
        if(consumeZone!=null)consumeZone.localScale=new Vector3(level.Size.x+8,1,level.Size.y+8);
        missionManager.LoadLevel(level);hole.ResetForLevel(level);if(arena!=null)arena.Apply(level);
        prewarmCounts.Clear();foreach(var p in level.placements){int n;prewarmCounts.TryGetValue(p.edible.prefab,out n);prewarmCounts[p.edible.prefab]=n+1;}
        foreach(var kv in prewarmCounts)pooler.Prewarm(kv.Key,kv.Value);
        foreach(var p in level.placements){
            GameObject go=pooler.Get(p.edible.prefab,new Vector3(p.position.x,0.02f+p.height,p.position.y),Quaternion.Euler(0,p.yaw,0));
            EdibleObject edible=go!=null?go.GetComponent<EdibleObject>():null;
            if(edible==null){Debug.LogError("[HoleIt] Invalid pooled edible prefab: "+p.edible.name);pooler.ReturnAllActive();Current=null;return false;}
            edible.Init(p.edible,p.decorative);
            if(p.stacked)edible.EnableStackPhysics();
        }
        Physics.SyncTransforms();return true;
    }
}
