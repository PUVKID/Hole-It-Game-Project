from pathlib import Path
import sys,json,struct,math
w=Path(__file__).parent;sys.path.insert(0,str(w/'ref-python'))
import numpy as np
from scipy.spatial import ConvexHull
p=w/'AndroidFinal/Assets/ReferenceContent';out=w/'fifth-colliders';out.mkdir(exist_ok=True)
j=json.loads((p/'Models.json').read_text());keys=set(c['mesh'] for m in j['models'] for n in m['nodes'] for c in n.get('colliders',[]) if c.get('mesh'))
report=[]
for key in sorted(keys):
 raw=(p/(key+'.bytes')).read_bytes();n,subs=struct.unpack_from('<ii',raw);vs=np.frombuffer(raw,dtype='<f4',count=n*8,offset=8).reshape(n,8)[:,:3];vs=np.unique(vs,axis=0)
 if len(vs)<4:continue
 try:hull=ConvexHull(vs)
 except:continue
 if len(hull.simplices)<=240:continue
 original=len(hull.simplices);vs=vs[hull.vertices];chosen=set()
 directions=list(np.eye(3))+list(-np.eye(3))
 for i in range(112):
  y=1-2*(i+.5)/112;r=math.sqrt(1-y*y);a=i*math.pi*(3-math.sqrt(5));directions.append([math.cos(a)*r,y,math.sin(a)*r])
 for d in directions:chosen.add(int(np.argmax(vs@d)))
 vs=vs[sorted(chosen)];hull=ConvexHull(vs);tri=hull.simplices.copy()
 for i,t in enumerate(tri):
  if np.dot(np.cross(vs[t[1]]-vs[t[0]],vs[t[2]]-vs[t[0]]),hull.equations[i,:3])<0:tri[i]=t[::-1]
 assert len(tri)<=240
 data=bytearray(struct.pack('<ii',len(vs),1))
 for v in vs:data+=struct.pack('<8f',*v,0,1,0,0,0)
 flat=tri.flatten();data+=struct.pack('<i',len(flat))+flat.astype('<i4').tobytes();(out/(key+'.bytes')).write_bytes(data)
 report.append({'mesh':key,'originalHullFaces':original,'collisionHullFaces':len(tri),'vertices':len(vs)})
(out/'report.json').write_text(json.dumps(report,indent=2));print('Optimized',len(report),'of',len(keys),'mesh colliders')
