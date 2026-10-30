from pathlib import Path
import sys,json,zipfile,io
sys.path.insert(0,str(Path(__file__).parent/'ref-python'))
import UnityPy
root=Path(__file__).parent/'reference'
with zipfile.ZipFile('C:/Users/PUVKID/Desktop/Hole+it!_1.31.17_APKPure.xapk') as z:
    a=zipfile.ZipFile(io.BytesIO(z.read('UnityStreamingAssetsPack.apk')))
    for n in a.namelist():
        if n.endswith('.bundle') and not (root/Path(n).name).exists():(root/Path(n).name).write_bytes(a.read(n))
env=UnityPy.load(*[str(f) for f in root.rglob('*.unity3d')],*[str(f) for f in root.glob('*.bundle')])
template=None
for o in env.objects:
    if o.type.name=='MonoBehaviour':
        try:
            d=o.read_typetree()
            if d.get('m_Name','').startswith('_ItemInfo') and 'item' in d:template=o.serialized_type.node;break
        except:pass
assert template
items=[]
for o in env.objects:
    if o.type.name=='MonoBehaviour':
        try:
            head=o.parse_monobehaviour_head()
            if head.m_Name.startswith('_ItemInfo'):
                d=o.read_typetree(nodes=template);items.append(d)
        except:pass
(root/'all-items.json').write_text(json.dumps(items,indent=2),encoding='utf-8')
print('items',len(items));print([(d['item']['id'],d['item']['ItemName']) for d in items if d['item']['id'] in [7,11,93]])
for o in env.objects:
    if o.assets_file.name=='level2' and o.type.name in ['MonoBehaviour','Transform']:
        try:
            if o.type.name=='Transform':
                d=o.read();name=d.m_GameObject.deref_parse_as_object().m_Name
                if name in ['Main Camera','HoleParent','Variable Joystick']:
                    print(name,o.read_typetree())
            else:
                d=o.parse_monobehaviour_head();sc=d.m_Script.deref_parse_as_object()
                if sc.m_ClassName=='VariableJoystick':
                    print('Joystick bytes',o.get_raw_data().hex())
        except:pass
