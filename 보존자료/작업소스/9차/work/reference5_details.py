from pathlib import Path
import json,sys,collections,gzip,base64
w=Path(__file__).parent;r=w/'reference';a=w/'reference5-audit'
ui=json.loads((a/'ui.json').read_text())
print('UI classes',collections.Counter(x[0] for x in ui))
print('SCENE NAMES',[(x[2],x[1]) for x in ui if x[0]=='RectTransform' and any(t in x[1].lower() for t in ['load','splash','home','main','logo','booster','start','level','setting'])][:150])
for x in json.loads((a/'scene-settings.json').read_text()):
    print('SETTING',x[0],x[1],str(x[3])[:3500])
physics=json.loads((a/'physics.json').read_text())
print('PHYSICS',physics[:2])
print('MATERIALS',[x for x in physics if x[0] in ['PhysicMaterial','PhysicsMaterial']][:12])
items={d['item']['id']:d['item'] for d in json.loads((r/'all-items.json').read_text())}
texts={d['m_Name']:json.loads(d['m_Script']) for f in r.glob('text*json') if (d:=json.loads(f.read_text())).get('m_Name','').startswith('Level_')}
containers=json.loads((r/'containers.json').read_text());prefabs={x[0].rsplit('/',1)[-1].lower() for x in containers if x[0].endswith('.prefab')}
levels=json.loads((r/'list_main_fall_back.txt').read_text())['level_list_info'];missing=[];available=[];ids=set()
for i,l in enumerate(levels):
    d=texts.get('Level_'+l['i'])
    if not d:missing.append([i+1,'layout']);continue
    c=json.loads(gzip.decompress(base64.b64decode(d['c'])));li={int(v.split('|')[0]) for v in c['cisd']}
    mi=[v for v in li if v not in items or (items[v]['ItemName']+'_Prefab.prefab').lower() not in prefabs]
    if mi:missing.append([i+1,mi]);continue
    ids.update(li);available.append(i+1)
print('AVAILABLE LEVELS',len(available),'required models',len(ids),'first missing',missing[:12],'last',available[-10:])
(a/'availability.json').write_text(json.dumps({'available':available,'missing':missing,'itemIds':sorted(ids)},indent=2))
sys.path.insert(0,str(w/'ref-python'));import UnityPy
env=UnityPy.load(*[str(f) for f in r.rglob('*.unity3d')],*[str(f) for f in r.glob('*.bundle')])
selected=['splash-image','spScreen-loading_BG','Home-screen','HoleGameTexture01HomeScreen','btn-green-coinBuy','progress-green','loading-circle','Coin-UI','heart-v2','home-Icn','play-Btn','btn-mid-green-play','setting-Icn']
for o in env.objects:
    if o.type.name not in ['Sprite','Texture2D']:continue
    d=o.read()
    if d.m_Name in selected:
        f=a/(d.m_Name+'.png')
        if not f.exists():d.image.save(f)
print('IMAGES EXPORTED')
