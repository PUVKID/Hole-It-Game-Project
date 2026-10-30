from pathlib import Path
import sys,json,collections
sys.path.insert(0,str(Path(__file__).parent/'ref-python'))
import UnityPy
root=Path(__file__).parent/'reference'
stats=collections.Counter(); names=[];scripts={};trees=[]
for f in root.rglob('*.unity3d'):
    env=UnityPy.load(str(f))
    for o in env.objects:
        stats[o.type.name]+=1
        if o.type.name in ['MonoScript','TextAsset','GameObject','MonoBehaviour','Camera','Light']:
            try:
                tree=o.read_typetree()
                name=tree.get('m_Name','')
                names.append((o.type.name,name,o.path_id))
                if o.type.name=='MonoScript':scripts[o.path_id]=tree
                if o.type.name in ['MonoBehaviour','Camera','Light'] and len(str(tree))<150000:trees.append({'type':o.type.name,'id':o.path_id,'data':tree})
                if o.type.name=='TextAsset' and any(w in name.lower() for w in ['level','config','setting']):
                    (root/('text_'+str(o.path_id)+'.json')).write_text(json.dumps(tree,ensure_ascii=False,default=str),encoding='utf-8')
            except Exception as e:
                names.append((o.type.name,str(e)[:100],o.path_id))
(root/'objects.json').write_text(json.dumps(names,ensure_ascii=False,indent=2),encoding='utf-8')
(root/'scripts.json').write_text(json.dumps(scripts,ensure_ascii=False,indent=2),encoding='utf-8')
(root/'trees.json').write_text(json.dumps(trees,ensure_ascii=False,indent=2),encoding='utf-8')
print(stats);print('Scriptable component trees:',len(trees));print('Scripts:',len(scripts))
print('\n'.join(str(x) for x in names if any(w in x[1].lower() for w in ['level','camera','joystick','hole','setting']) )[:10000])
