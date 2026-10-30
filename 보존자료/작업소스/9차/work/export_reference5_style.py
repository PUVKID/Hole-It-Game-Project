from pathlib import Path
import sys,json,collections
w=Path(__file__).parent;sys.path.insert(0,str(w/'ref-python'))
import UnityPy
r=w/'reference';out=w/'AndroidFinal/Assets/Resources/ReferenceStyle'
env=UnityPy.load(*[str(f) for f in r.rglob('*.unity3d')],*[str(f) for f in r.glob('*.bundle')])
names=['btn-blue-settings','setting-btn-outter','setting-btn-inner','pause-Icn','setting-Icn','btn-mid-green-play','btn-mid2-green','btn-mid2-border','close-btn','lock-icon-gray','booster-btn-count','booster-free-panel','info-win-v3','heart-v2','Coin-UI','life-panel','spScreen-loading_BG','progress-green','prebosster-toolkit-0','prebosster-toolkit-1','prebosster-toolkit-2','prebosster-toolkit-3']
exported=set();shaders=collections.Counter();samples=[];transforms=[]
for o in env.objects:
    if o.type.name=='Sprite':
        d=o.read()
        if d.m_Name in names and d.m_Name not in exported:d.image.save(out/(d.m_Name+'.png'));exported.add(d.m_Name)
    elif o.type.name=='Material':
        try:
            d=o.read();shader=d.m_Shader.deref_parse_as_object().m_Name;shaders[shader]+=1
            if len(samples)<10:samples.append([d.m_Name,shader,o.read_typetree()['m_SavedProperties']])
        except:pass
    elif o.type.name=='Transform' and o.assets_file.name=='level2':
        d=o.read();name=d.m_GameObject.deref_parse_as_object().m_Name
        if any(n in name.lower() for n in ['light','camera','holeparent']):transforms.append([name,o.read_typetree()])
(w/'reference5-audit/render-details.json').write_text(json.dumps({'shaders':dict(shaders),'samples':samples,'transforms':transforms},indent=2,default=str))
print('EXPORTED',sorted(exported));print('SHADERS',shaders)
