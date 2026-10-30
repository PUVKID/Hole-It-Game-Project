from pathlib import Path
import sys,json,struct
sys.path.insert(0,str(Path(__file__).parent/'ref-python'))
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler
root=Path(__file__).parent/'reference'
out=Path(__file__).parent/'AndroidFinal/Assets/Resources/ReferenceStyle'
out.mkdir(parents=True,exist_ok=True)
env=UnityPy.load(*[str(f) for f in root.rglob('*.unity3d')],*[str(f) for f in root.glob('*.bundle')])
names=['goal-card','timer-bg','timer-text-panel','pause-Icn','booster-Btn-blue','booster-btn-yellow','btn-big-green','btn-big-Blue','btn-mid-green-play','setting-Icn','LevelNumberBg','JoystickMarker','JoystickOutline','hole-sizeIncrease_icon-export','snow-flex','male-detective-icn','prebosster-toolkit-0','prebosster-toolkit-1','prebosster-toolkit-2','prebosster-toolkit-3','progress-green','progress-BG','holeinc','poison-bomb']
exported=[]
for o in env.objects:
    if o.type.name=='Sprite':
        d=o.read()
        if d.m_Name in names and d.m_Name not in exported:
            d.image.save(out/(d.m_Name+'.png'));exported.append(d.m_Name)
    if o.assets_file.name=='level2' and o.path_id==948:
        d=o.read().m_Mesh.read();h=MeshHandler(d);h.process()
        vs=h.m_Vertices;ns=h.m_Normals;uv=h.m_UV0;tris=h.get_triangles()
        with (out/'OriginalHole.bytes').open('wb') as f:
            f.write(struct.pack('<ii',len(vs),len(tris)))
            for v,n,u in zip(vs,ns,uv):f.write(struct.pack('<8f',*v[:3],*n[:3],*u[:2]))
            for sub in tris:
                flat=[i for t in sub for i in t];f.write(struct.pack('<i',len(flat)));f.write(struct.pack('<'+'i'*len(flat),*flat))
    if o.type.name=='Texture2D':
        d=o.read()
        if d.m_Name=='HoleGameTexture01_new':d.image.save(out/'OriginalHole.png')
print('Exported style assets:',exported)
