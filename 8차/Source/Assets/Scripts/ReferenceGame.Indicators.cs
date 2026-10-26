using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// PositionIndicatorManager.UpdateTargetIconPosition, original RVA 0x196D794.
// The source uses viewport visibility, independent axis clamps, a 115-unit horizontal
// buffer and vertical buffers (580 top, 420 bottom), all multiplied by Canvas.scaleFactor.
public static class ReferenceIndicatorLayout {
    public const int MaximumPerItem=5;
    public static bool IsOnScreen(Vector3 viewport)=>viewport.z>=0&&viewport.x>=0&&viewport.x<=1&&viewport.y>=0&&viewport.y<=1;
    public static Vector2 ClampOffscreen(Vector3 viewport,float width,float height,float canvasScale){
        if(viewport.z<0){
            float x=1-viewport.x,y=1-viewport.y;
            float divisor=Mathf.Max(x,y,1e-6f);
            viewport.x=x/divisor;viewport.y=y/divisor;
        }
        float left=Mathf.Min(115*canvasScale,width*.49f),bottom=Mathf.Min(420*canvasScale,height*.49f);
        float top=Mathf.Max(bottom,height-580*canvasScale);
        return new Vector2(Mathf.Clamp(viewport.x*width,left,width-left),Mathf.Clamp(viewport.y*height,bottom,top));
    }
    public static float ArrowAngle(Vector2 target,Vector2 indicator){var delta=target-indicator;return Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg+180;}
}

public partial class ReferenceGame {
    [Serializable] sealed class ReferenceIndicatorMidpoint {public int id;public Vector3 position;}
    [Serializable] sealed class ReferenceIndicatorMidpointCollection {public ReferenceIndicatorMidpoint[] models;}
    sealed class ReferenceIndicatorGroup {
        public int id,limit,count;
        public readonly Actor[] nearest=new Actor[ReferenceIndicatorLayout.MaximumPerItem];
        public readonly float[] distances=new float[ReferenceIndicatorLayout.MaximumPerItem];
        public readonly ReferenceIndicatorMarker[] markers=new ReferenceIndicatorMarker[ReferenceIndicatorLayout.MaximumPerItem];
    }
    sealed class ReferenceIndicatorMarker {public RectTransform root,imageParent,rotationParent,targetParent;public Image icon;}
    readonly List<ReferenceIndicatorGroup> referenceIndicatorGroups=new List<ReferenceIndicatorGroup>();
    readonly Dictionary<int,Vector3> referenceIndicatorMidpoints=new Dictionary<int,Vector3>();
    RectTransform referenceIndicatorRoot;ReferenceLevel referenceIndicatorLevel;float referenceIndicatorRefresh;

    void CreateReferenceIndicators(){
        DisposeReferenceIndicators();referenceIndicatorLevel=level;
        referenceIndicatorRoot=IndicatorRect("Original Finder Indicators",hud.transform,Vector2.zero,Vector2.zero);
        referenceIndicatorRoot.anchorMin=Vector2.zero;referenceIndicatorRoot.anchorMax=Vector2.one;
        referenceIndicatorRoot.offsetMin=referenceIndicatorRoot.offsetMax=Vector2.zero;
        // Render beneath timer/goals/controls, with every graphic excluded from touch raycasts.
        referenceIndicatorRoot.SetAsFirstSibling();
        if(referenceIndicatorMidpoints.Count==0){
            var source=Resources.Load<TextAsset>("ReferenceStyle/IndicatorMidpoints");
            if(source!=null){var data=JsonUtility.FromJson<ReferenceIndicatorMidpointCollection>(source.text);if(data.models!=null)foreach(var entry in data.models)referenceIndicatorMidpoints[entry.id]=entry.position;}
        }
        if(level.goals.Length==0)referenceIndicatorGroups.Add(new ReferenceIndicatorGroup{id=int.MinValue});
        else foreach(var goal in level.goals){
            bool present=false;foreach(var group in referenceIndicatorGroups)if(group.id==goal.id){present=true;break;}
            if(!present&&goal.id>=0)referenceIndicatorGroups.Add(new ReferenceIndicatorGroup{id=goal.id});
        }
        referenceIndicatorRefresh=-1;
    }
    static RectTransform IndicatorRect(string name,Transform parent,Vector2 size,Vector2 position){
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
        rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
    }
    Image IndicatorImage(string name,Transform parent,string sprite,Vector2 size,Vector2 position){
        var rect=IndicatorRect(name,parent,size,position);var graphic=rect.gameObject.AddComponent<Image>();
        graphic.sprite=string.IsNullOrEmpty(sprite)?null:Style(sprite);graphic.type=Image.Type.Simple;graphic.preserveAspect=true;graphic.raycastTarget=false;return graphic;
    }
    ReferenceIndicatorMarker CreateReferenceIndicatorMarker(int id,int slot){
        var marker=new ReferenceIndicatorMarker();
        marker.root=IndicatorRect("Finder "+id+" "+slot,referenceIndicatorRoot,new Vector2(120,120),Vector2.zero);
        marker.imageParent=IndicatorRect("Image-Parent",marker.root,new Vector2(120,120),Vector2.zero);marker.imageParent.localScale=Vector3.one*3.5f;
        marker.rotationParent=IndicatorRect("RotationParent",marker.imageParent,new Vector2(120,120),Vector2.zero);
        var pointer=IndicatorImage("Image-Indicator",marker.rotationParent,"indicator-pointer_2",new Vector2(10.7084f,21.4877f),new Vector2(-18.13f,0));pointer.transform.localScale=Vector3.one*3.27f;
        var background=IndicatorImage("Image-iconBg",marker.imageParent,"indicator-circle_2",new Vector2(40,40),Vector2.zero);
        marker.icon=IndicatorImage("Image-icon",background.transform,"",new Vector2(27,27),Vector2.zero);
        marker.targetParent=IndicatorRect("Image-TargetParent",marker.root,new Vector2(120,120),Vector2.zero);marker.targetParent.localScale=Vector3.one*4.5f;
        var target=IndicatorImage("Image-Target",marker.targetParent,"terget",new Vector2(20,20),Vector2.zero);target.color=new Color(1,1,1,179f/255);
        return marker;
    }
    void SelectReferenceIndicatorTargets(){
        foreach(var group in referenceIndicatorGroups){
            int remaining=0;
            if(group.id==int.MinValue)remaining=CollectionTarget-Collected;
            else for(int i=0;i<level.goals.Length;i++)if(level.goals[i].id==group.id)remaining=Mathf.Max(remaining,level.goals[i].amount-goalCounts[i]);
            group.limit=Mathf.Clamp(remaining,0,ReferenceIndicatorLayout.MaximumPerItem);group.count=0;
            Array.Clear(group.nearest,0,group.nearest.Length);
        }
        foreach(var actor in actors){
            if(actor.state!=0||actor.model.id<0)continue;
            foreach(var group in referenceIndicatorGroups){
                if(group.limit==0||(group.id!=int.MinValue&&group.id!=actor.model.id))continue;
                float distance=(actor.pos-HolePosition).sqrMagnitude;int insertion=group.count;
                for(int i=0;i<group.count;i++)if(distance<group.distances[i]){insertion=i;break;}
                if(insertion>=group.limit)continue;
                int last=Mathf.Min(group.count,group.limit-1);
                for(int i=last;i>insertion;i--){group.nearest[i]=group.nearest[i-1];group.distances[i]=group.distances[i-1];}
                group.nearest[insertion]=actor;group.distances[insertion]=distance;group.count=Mathf.Min(group.count+1,group.limit);
            }
        }
        referenceIndicatorRefresh=playTime+.1f;
    }
    void UpdateReferenceIndicators(){
        if(hud==null||cam==null||level==null)return;
        if(playTime>=finderUntil||(State!=ScreenState.Playing&&State!=ScreenState.Paused)){DisposeReferenceIndicators();return;}
        if(referenceIndicatorRoot==null||referenceIndicatorLevel!=level)CreateReferenceIndicators();
        referenceIndicatorRoot.gameObject.SetActive(State==ScreenState.Playing);
        if(State!=ScreenState.Playing)return;
        bool refresh=playTime>=referenceIndicatorRefresh;
        if(!refresh)foreach(var group in referenceIndicatorGroups)for(int i=0;i<group.count;i++){var actor=group.nearest[i];if(actor==null||actor.state!=0||!Needed(actor.model.id)){refresh=true;break;}}
        if(refresh)SelectReferenceIndicatorTargets();
        foreach(var group in referenceIndicatorGroups)for(int slot=0;slot<ReferenceIndicatorLayout.MaximumPerItem;slot++){
            var marker=group.markers[slot];
            if(slot>=group.count){if(marker!=null)marker.root.gameObject.SetActive(false);continue;}
            if(marker==null)group.markers[slot]=marker=CreateReferenceIndicatorMarker(group.id,slot);
            marker.root.gameObject.SetActive(true);var actor=group.nearest[slot];
            Vector3 rootPosition=RenderPosition(actor),viewport=cam.WorldToViewportPoint(rootPosition);
            bool onScreen=ReferenceIndicatorLayout.IsOnScreen(viewport);Vector2 screenPosition;
            if(onScreen){
                Vector3 midpoint=rootPosition;
                if(referenceIndicatorMidpoints.TryGetValue(actor.model.id,out var local))midpoint+=RenderRotation(actor)*(local*actor.scale);
                screenPosition=cam.WorldToScreenPoint(midpoint);
            }else screenPosition=ReferenceIndicatorLayout.ClampOffscreen(viewport,Screen.width,Screen.height,canvas.scaleFactor);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(referenceIndicatorRoot,screenPosition,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var position);marker.root.anchoredPosition=position;
            marker.imageParent.gameObject.SetActive(!onScreen);marker.targetParent.gameObject.SetActive(onScreen);
            if(!onScreen){marker.icon.sprite=actor.model.icon;marker.rotationParent.localRotation=Quaternion.Euler(0,0,ReferenceIndicatorLayout.ArrowAngle(cam.WorldToScreenPoint(rootPosition),screenPosition));}
        }
    }
    void DisposeReferenceIndicators(){
        if(referenceIndicatorRoot!=null){referenceIndicatorRoot.gameObject.SetActive(false);Destroy(referenceIndicatorRoot.gameObject);}
        referenceIndicatorRoot=null;referenceIndicatorLevel=null;referenceIndicatorGroups.Clear();
    }
}