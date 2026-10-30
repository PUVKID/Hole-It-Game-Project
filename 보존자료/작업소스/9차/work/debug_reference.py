from pathlib import Path
import sys,json,collections
sys.path.insert(0,str(Path(__file__).parent/'ref-python'))
import UnityPy
root=Path(__file__).parent/'reference'
env=UnityPy.load(*[str(f) for f in root.rglob('*.unity3d')],*[str(f) for f in root.glob('*.bundle')])
errors=[];found=[]
for o in env.objects:
    if o.type.name!='MonoBehaviour':continue
    try:
        d=o.parse_monobehaviour_head();sc=d.m_Script.deref_parse_as_object();name=sc.m_ClassName
        if any(s in name.lower() for s in ['joystick','holecharacter','holecontroller','iteminfo','collectable','cameramanager']):
            found.append([o.assets_file.name,o.path_id,name,d.m_Name])
            if name in ['VariableJoystick','HoleSizeManager','HoleCharacterController','ItemInfo','CollectableItemInfo','CollectablesSo']:
                raw=o.get_raw_data();(root/(name+'_'+str(o.path_id)+'.bin')).write_bytes(raw)
    except Exception as e:
        if len(errors)<3:errors.append(str(e))
(root/'component-index.json').write_text(json.dumps(found,indent=2),encoding='utf-8')
print('errors',errors);print('found',len(found));print(found[:60])
