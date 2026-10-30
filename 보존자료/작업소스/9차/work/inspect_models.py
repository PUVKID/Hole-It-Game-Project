from pathlib import Path
import sys,json,collections
sys.path.insert(0,str(Path(__file__).parent/'ref-python'))
import UnityPy
root=Path(__file__).parent/'reference'
env=UnityPy.load(str(next(root.glob('defaultlocalgroup*.bundle'))))
print(collections.Counter(o.type.name for o in env.objects))
for attempt in range(5):
    try: containers=dict(env.container);break
    except RuntimeError:pass
print('CONTAINERS',len(containers))
items=[]
for key,obj in containers.items():
    items.append([key,obj.path_id,obj.type.name])
(root/'containers.json').write_text(json.dumps(items,indent=2),encoding='utf-8')
print('\n'.join(str(x) for x in items[:50]))
names=[]
for o in env.objects:
    if o.type.name in ['MonoBehaviour','GameObject','Mesh','Material']:
        try:
            d=o.read_typetree();names.append([o.type.name,d.get('m_Name',''),o.path_id]);
            if o.type.name=='MonoBehaviour' and any(k in str(d).lower() for k in ['collectable','hole','size']):
                (root/('model-component-'+str(o.path_id)+'.json')).write_text(json.dumps(d,indent=2,default=str),encoding='utf-8')
        except:pass
(root/'model-names.json').write_text(json.dumps(names,indent=2),encoding='utf-8')
print('NAMED',len(names));print('\n'.join(str(x) for x in names[:40]))
