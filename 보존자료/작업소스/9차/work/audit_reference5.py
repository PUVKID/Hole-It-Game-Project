from pathlib import Path
import sys,json,zipfile,io,collections
w=Path(__file__).parent;sys.path.insert(0,str(w/'ref-python'))
import UnityPy
r=w/'reference';out=w/'reference5-audit';out.mkdir(exist_ok=True)
report={}
with zipfile.ZipFile('C:/Users/PUVKID/Desktop/Hole+it!_1.31.17_APKPure.xapk') as z:
    for n in z.namelist():
        if not n.endswith('.apk'):continue
        a=zipfile.ZipFile(io.BytesIO(z.read(n)));groups=collections.Counter()
        for f in a.infolist():
            group='assets' if f.filename.startswith('assets/') else 'native libraries' if f.filename.startswith('lib/') else 'dex' if f.filename.endswith('.dex') else 'resources/metadata'
            groups[group]+=f.compress_size
        report[n]={'packageBytes':z.getinfo(n).file_size,'compressedContents':dict(groups),'largestFiles':sorted([(f.filename,f.file_size,f.compress_size) for f in a.infolist()],key=lambda v:-v[2])[:8]}
(out/'package-size.json').write_text(json.dumps(report,indent=2))
print('PACKAGES',[(k,round(v['packageBytes']/1048576,1),v['compressedContents']) for k,v in report.items()],flush=True)
items=json.loads((r/'all-items.json').read_text());levels=json.loads((r/'list_main_fall_back.txt').read_text())['level_list_info']
print('ITEMS',len(items),'MAIN LEVELS',len(levels),flush=True)
env=UnityPy.load(*[str(f) for f in r.rglob('*.unity3d')],*[str(f) for f in r.glob('*.bundle')])
names=[];settings=[];ui=[];physics=[];materials=[]
for o in env.objects:
    kind=o.type.name
    try:
        if kind in ['Sprite','Texture2D','AudioClip']:
            d=o.read();names.append([kind,d.m_Name,o.assets_file.name,o.path_id])
        elif kind in ['PhysicsManager','RenderSettings','Light','Camera']:
            settings.append([kind,o.assets_file.name,o.path_id,o.read_typetree()])
        elif kind in ['Rigidbody','PhysicMaterial','PhysicsMaterial']:
            d=o.read_typetree();physics.append([kind,o.assets_file.name,o.path_id,d])
        elif kind=='Material':
            d=o.read_typetree();materials.append([o.assets_file.name,o.path_id,d])
        elif kind in ['RectTransform','MonoBehaviour'] and o.assets_file.name in ['level0','level1','level2']:
            if kind=='RectTransform':
                d=o.read();name=d.m_GameObject.deref_parse_as_object().m_Name
                ui.append([kind,name,o.assets_file.name,o.path_id,o.read_typetree()])
            else:
                d=o.parse_monobehaviour_head();cl=d.m_Script.deref_parse_as_object().m_ClassName
                if any(t in cl.lower() for t in ['image','canvas','text','loading','splash','hole','camera','collect','gravity','movement','joystick']):
                    try:tree=o.read_typetree()
                    except:tree={'raw':o.get_raw_data().hex()}
                    name=d.m_GameObject.deref_parse_as_object().m_Name if d.m_GameObject.path_id else d.m_Name
                    ui.append([cl,name,o.assets_file.name,o.path_id,tree])
    except Exception:pass
for n,v in [('names',names),('scene-settings',settings),('ui',ui),('physics',physics),('materials',materials)]:
    (out/(n+'.json')).write_text(json.dumps(v,indent=2,default=str),encoding='utf-8')
print('COUNTS',len(names),len(settings),len(ui),len(physics),len(materials),flush=True)
print('LOADING ASSETS',[x[:2] for x in names if any(t in x[1].lower() for t in ['load','splash','logo','background','home','heart','coin'])],flush=True)
