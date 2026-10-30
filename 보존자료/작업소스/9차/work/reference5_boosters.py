from pathlib import Path
import sys,json,struct
w=Path(__file__).parent;sys.path.insert(0,str(w/'ref-python'));import UnityPy
r=w/'reference';out=w/'reference5-audit'
e=UnityPy.load(*[str(p) for p in r.rglob('*.unity3d')],*[str(p) for p in r.glob('*.bundle')])
results=[]
for o in e.objects:
    if o.type.name!='MonoBehaviour':continue
    try:
        h=o.parse_monobehaviour_head();name=h.m_Script.deref_parse_as_object().m_ClassName
        if name in ['BoosterSo','BoosterSettings','BoosterInfo','GameSettingsManager','EnvironmentVariantSo']:
            try:d=o.read_typetree()
            except:d={'raw':o.get_raw_data().hex()}
            results.append([name,h.m_Name,o.assets_file.name,o.path_id,d])
    except:pass
(out/'booster-settings.json').write_text(json.dumps(results,indent=2,default=str),encoding='utf-8')
print([(a,b,c,d,list(t)[:8]) for a,b,c,d,t in results])
