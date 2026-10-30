from pathlib import Path
import zipfile,io,importlib.util,json
root=Path(__file__).parent/'reference';root.mkdir(exist_ok=True)
with zipfile.ZipFile('C:/Users/PUVKID/Desktop/Hole+it!_1.31.17_APKPure.xapk') as z:
    report={}
    for n in z.namelist():
        if not n.endswith('.apk'):continue
        a=zipfile.ZipFile(io.BytesIO(z.read(n)))
        entries=[(f.filename,f.file_size) for f in a.infolist() if f.filename.startswith('assets/')]
        report[n]=entries
        for f,size in entries:
            if f.startswith('assets/bin/Data/') or f.endswith(('catalog.json','settings.json')):
                target=root/n/f;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(a.read(f))
    (root/'inventory.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    for n,entries in report.items():
        print(n,len(entries),'assets');print('\n'.join(str(x) for x in entries[:18]))
print('UnityPy available',bool(importlib.util.find_spec('UnityPy')))
