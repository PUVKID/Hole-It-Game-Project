from pathlib import Path
import json,sys
w=Path(__file__).parent;sys.path.insert(0,str(w/'ref-python'));import UnityPy
r=w/'reference';env=UnityPy.load(*[str(f) for f in r.rglob('*.unity3d')],*[str(f) for f in r.glob('*.bundle')])
lookup={(o.assets_file.name,o.path_id):o for o in env.objects}
for row in json.loads((w/'reference5-audit/booster-settings.json').read_text()):
 if 'boosterAssetInfo' not in row[4]:continue
 info=row[4]['boosterAssetInfo'];obj=lookup[row[2],info['boosterIcon']['m_PathID']];d=obj.read();d.image.save(w/f"AndroidFinal/Assets/Resources/ReferenceStyle/Booster{info['boosterType']}.png");print(info['boosterType'],d.m_Name)
j=json.loads((w/'AndroidFinal/Assets/ReferenceContent/Models.json').read_text());print(type(j),list(j)[:6])
for m in j['models']:
 if 'G-3075635067240017007' in json.dumps(m):print('CONVEX MODEL',m['id'],m.get('name'))
