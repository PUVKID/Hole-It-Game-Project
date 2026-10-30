from pathlib import Path
import sys,json,struct
sys.path.insert(0,str(Path(__file__).parent/'ref-python'))
import UnityPy
root=Path(__file__).parent/'reference'
targets=['SizeInfos','list_main_fall_back','list_helpers_fall_back','list_loop_fall_back','StageMetadata.StageResource']
for f in root.rglob('*.unity3d'):
    env=UnityPy.load(str(f))
    for o in env.objects:
        if o.type.name=='TextAsset':
            d=o.read()
            if d.m_Name in targets:
                (root/(d.m_Name+'.txt')).write_text(d.m_Script,encoding='utf-8');print(d.m_Name,d.m_Script[:10000])
        if o.type.name=='MonoBehaviour':
            try:
                d=o.parse_monobehaviour_head();script=d.m_Script.deref_parse_as_object();name=script.m_ClassName
                if name in ['VariableJoystick','HoleSizeManager','HoleCharacterController','GameSettingsManager','CollectablesSo']:
                    raw=o.get_raw_data();(root/(name+'_'+str(o.path_id)+'.bin')).write_bytes(raw)
                    print(name,o.path_id,len(raw),raw[:140].hex())
            except Exception:pass
