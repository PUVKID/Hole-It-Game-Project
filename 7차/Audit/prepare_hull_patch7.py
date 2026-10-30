from pathlib import Path
import sys,json,struct,re
w=Path(r'C:\Users\PUVKID\Documents\Codex\2026-10-13\new-chat\work')
sys.path.insert(0,str(w/'ref-python'))
import numpy as np
from scipy.spatial import ConvexHull
root=Path(r'C:\Users\PUVKID\Desktop\Hole It\7차')
source=root/'Source/Assets/ReferenceContent'
dest=root/'Audit/PendingCollisionHulls'
dest.mkdir(exist_ok=True)
d=json.loads((source/'Models.json').read_text(encoding='utf8'))
keys=sorted(set(re.findall(r'Source mesh name: (G-?\d+)',(root/'Audit/Tests.log').read_text(encoding='utf8'))))
report=[]
for key in keys:
    raw=(source/(key+'.bytes')).read_bytes();n=struct.unpack_from('<i',raw)[0]
    vertices=np.unique(np.frombuffer(raw,dtype='<f4',count=n*8,offset=8).reshape(n,8)[:,:3].astype(float),axis=0)
    full=ConvexHull(vertices);points=vertices[full.vertices]
    # All six extreme source coordinates are kept exactly. Add the source point
    # most outside the current hull until the native contact budget is reached.
    selected=set(np.argmin(points,axis=0))|set(np.argmax(points,axis=0))
    while True:
        indices=sorted(selected);hull=ConvexHull(points[indices]);equations=hull.equations
        violations=(points@equations[:,:3].T+equations[:,3]).max(axis=1)
        if len(selected)>=120 or violations.max()<1e-7:break
        selected.add(int(np.argmax(violations)))
    picked=points[indices];hull=ConvexHull(picked)
    triangles=hull.simplices.copy()
    # Qhull simplices have arbitrary winding; explicitly orient away from the volume.
    for i,tri in enumerate(triangles):
        a,b,c=picked[tri]
        if np.dot(np.cross(b-a,c-a),hull.equations[i,:3])<0:triangles[i,[1,2]]=triangles[i,[2,1]]
    with (dest/(key+'.bytes')).open('wb') as f:
        f.write(struct.pack('<ii',len(picked),1))
        for v in picked:f.write(struct.pack('<8f',*v,0,1,0,0,0))
        flat=triangles.ravel();f.write(struct.pack('<i',len(flat)));f.write(flat.astype('<i4').tobytes())
    assert len(picked)<=120 and len(triangles)<=240
    assert np.array_equal(picked.min(axis=0),points.min(axis=0)) and np.array_equal(picked.max(axis=0),points.max(axis=0))
    modelids=[m['id'] for m in d['models'] if any(c.get('mesh')==key for node in m['nodes'] for c in node.get('colliders',[]))]
    report.append(dict(mesh=key,models=modelids,sourceHullVertices=len(full.vertices),sourceHullTriangles=len(full.simplices),vertices=len(picked),triangles=len(triangles),boundsPreserved=True,maxOutsidePlaneDistance=float(violations.max()),volumeRetainedFraction=float(hull.volume/full.volume),method='Greedy maximum supporting-plane violation; original vertices only, with all axis extrema preserved.'))
(dest/'report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
print(json.dumps(report,ensure_ascii=False,indent=2))
