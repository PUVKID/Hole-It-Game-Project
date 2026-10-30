from pathlib import Path
import io
import json
import struct
import sys
import zipfile

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / '분석도구'))
import UnityPy
from capstone import Cs, CS_ARCH_ARM, CS_MODE_ARM

INPUT = Path(r'C:\Users\PUVKID\Desktop\Hole+it!_1.31.17_APKPure.xapk')
DUMP = Path(r'C:\Users\PUVKID\Documents\Codex\2026-10-13\new-chat\work\il2cpp-analysis\dump')
methods = json.loads((DUMP / 'script.json').read_text(encoding='utf-8-sig'))['ScriptMethod']
names = {m['Address']: m['Name'] for m in methods}
addresses = sorted(names)
binary = (ROOT / '원본임시/libil2cpp.so').read_bytes()
decoder = Cs(CS_ARCH_ARM, CS_MODE_ARM)
targets = [
    'CollectableManager$$OnCollected', 'CollectableManager$$MakeItemPhysics',
    'CollectableManager$$MakeAboveItemPhysics', 'SmoothObjectDetection$$OnTriggerEnter',
    'SmoothObjectDetection$$OnTriggerExit', 'HoleSizeManager$$OnObjectEatenResponse',
    'HoleSizeManager$$UpdateHoleProgressValue', 'HoleSizeManager$$GetRequiredSizeAmount',
    'HoleProgressBar$$DrawProgressBar', 'HoleSizeManager$$UpdateHoleSizeLevelText',
    'CollectionCollisionManager$$OnTriggerEnter',
    'HoleSizeManager$$SetNextHoleSizeLevel', 'HoleSizeManager$$IncreaseHoleSize',
    'HoleSizeManager$$GetNextHoleSizeIncreaseInfo',
    'HoleSizeManager$$<UpdateHoleProgressValue>b__42_0',
    'HoleProgressBar$$.ctor',
]
lines = []
for index, address in enumerate(addresses):
    if not any(names[address].endswith(t) for t in targets):
        continue
    end = addresses[index + 1]
    lines.append(f'\n함수: {names[address]} / 주소: {address:#x}')
    for ins in decoder.disasm(binary[address:end], address):
        extra = ''
        if ins.mnemonic in ('bl', 'b') and ins.op_str.startswith('#'):
            target = int(ins.op_str[1:], 16)
            extra = ' ' + names.get(target, '')
        if ins.mnemonic in ('ldr', 'vldr') and '[pc,' in ins.op_str:
            import re
            m = re.search(r'\[pc, #(-?0x[0-9a-f]+|-?\d+)\]', ins.op_str)
            if m:
                p = ins.address + 8 + int(m.group(1), 0)
                if 0 <= p <= len(binary)-4:
                    extra += f' 값:{binary[p:p+4].hex()} 실수:{struct.unpack_from("<f",binary,p)[0]:.8g}'
        lines.append(f'{ins.address:x}: {ins.mnemonic} {ins.op_str}{extra}')
(ROOT / '원본_수집과_성장_실행코드.txt').write_text('\n'.join(lines), encoding='utf-8')

with zipfile.ZipFile(INPUT) as x:
    with zipfile.ZipFile(io.BytesIO(x.read('UnityDataAssetPack.config.astc.apk'))) as a:
        data = a.read('assets/bin/Data/datapack.unity3d')
    with zipfile.ZipFile(io.BytesIO(x.read('UnityTextureCompressionsAssetPack.config.astc.apk'))) as a:
        base_data = a.read('assets/bin/Data/data.unity3d')
env = UnityPy.load(base_data, data)
found = []
for obj in env.objects:
    if obj.type.name == 'TextAsset':
        asset = obj.read()
        if 'size' in asset.m_Name.lower():
            found.append({'파일': obj.assets_file.name, '경로번호': obj.path_id,
                          '이름': asset.m_Name, '내용': asset.m_Script})
    elif obj.type.name == 'MonoBehaviour':
        try:
            asset = obj.parse_monobehaviour_head()
            script = asset.m_Script.deref().read()
            if script.m_ClassName not in ('HoleProgressBar', 'SmoothPhysicsSo', 'HoleSizeManager', 'LevelManager', 'HoleCapsuleCollider', 'CollectionCollisionManager', 'SmoothObjectDetection', 'Disc'):
                continue
            tree = obj.read_typetree(check_read=False)
            tree['raw'] = obj.get_raw_data().hex()
            found.append({'파일': obj.assets_file.name, '경로번호': obj.path_id,
                          '클래스': script.m_ClassName, '값': tree})
        except (ValueError, AttributeError, KeyError, FileNotFoundError):
            continue
(ROOT / '원본_물리와_성장_설정.json').write_text(json.dumps(found, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({'실행코드_줄수':len(lines),'설정_항목수':len(found),
                  '설정_목록':[(f.get('클래스',f.get('이름')), f['파일'],f['경로번호']) for f in found]},ensure_ascii=False))
