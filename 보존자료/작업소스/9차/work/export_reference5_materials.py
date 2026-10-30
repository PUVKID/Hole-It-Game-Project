from pathlib import Path
import sys,json
w=Path(__file__).parent;sys.path.insert(0,str(w/'ref-python'))
import UnityPy
f=w/'AndroidFinal/Assets/ReferenceContent/Models.json';data=json.loads(f.read_text(encoding='utf-8'));mats={m['key']:m for m in data['materials']}
env=UnityPy.load(*[str(p) for p in (w/'reference').glob('*.bundle')])
for o in env.objects:
    key='M'+str(o.path_id)
    if o.type.name!='Material' or key not in mats:continue
    d=o.read_typetree();pr=d['m_SavedProperties'];colors=dict(pr['m_Colors']);floats=dict(pr['m_Floats']);m=mats[key]
    c=colors.get('_MainColor',colors.get('_BaseColor',colors.get('_Color')))
    if c:m['color']=[c[k] for k in 'rgba']
    sh=colors.get('_ShadowColor',{'r':.566,'g':.566,'b':.566,'a':1});m['shadow']=[sh[k] for k in 'rgba'];m['edgeStart']=floats.get('_ShadowEdgeStart',-1);m['edgeEnd']=floats.get('_ShadowEdgeEnd',2);m['gloss']=floats.get('_Glossiness',32)
f.write_text(json.dumps(data,separators=(',',':')),encoding='utf-8')
