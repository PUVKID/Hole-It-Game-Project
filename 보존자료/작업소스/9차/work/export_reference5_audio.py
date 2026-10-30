from pathlib import Path
import sys
w=Path(__file__).parent;sys.path.insert(0,str(w/'ref-python'))
import UnityPy
r=w/'reference';out=w/'reference5-audio';out.mkdir(exist_ok=True)
env=UnityPy.load(*[str(p) for p in r.rglob('*.unity3d')],*[str(p) for p in r.glob('*.bundle')])
names=['item-collect-bubble','hole_grow_01','GameFail','_win','BoosteCollect','Bomb','_button_click_new','goal-complete','Freeze','speed_booster']
for o in env.objects:
    if o.type.name!='AudioClip':continue
    d=o.read()
    if d.m_Name not in names:continue
    try:
        resource=r/'com.rocket.holeit.apk/assets/bin/Data'/Path(d.m_Resource.m_Source).name
        if resource.exists():
            import fmod_toolkit
            data=resource.read_bytes()[d.m_Resource.m_Offset:d.m_Resource.m_Offset+d.m_Resource.m_Size]
            samples=fmod_toolkit.raw_to_wav(data,d.m_Name,d.m_Channels,d.m_Frequency)
        else:samples=d.samples
        for name,blob in samples.items():
            path=out/(d.m_Name+Path(name).suffix);path.write_bytes(blob);print(path.name,len(blob))
    except Exception as e:print(d.m_Name,type(e).__name__,str(e)[:160])
