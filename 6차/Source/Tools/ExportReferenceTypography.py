"""Recover static typography from the supplied Hole+it! 1.31.17 Unity data.

Usage: python ExportReferenceTypography.py --reference-dir <extracted bundles>
       --unitypy-path <directory containing UnityPy>
The extracted reference directory is an input, never modified by this tool.
"""
import argparse
import json
import pathlib
import sys

parser = argparse.ArgumentParser()
parser.add_argument('--reference-dir', required=True)
parser.add_argument('--unitypy-path')
args = parser.parse_args()
if args.unitypy_path:
    sys.path.insert(0, args.unitypy_path)
import UnityPy

reference = pathlib.Path(args.reference_dir)
project = pathlib.Path(__file__).resolve().parents[1]
out = project / 'Assets/Resources/ReferenceTypography'
out.mkdir(parents=True, exist_ok=True)
env = UnityPy.load(*[str(p) for p in reference.rglob('*.unity3d')],
                   *[str(p) for p in reference.glob('*.bundle')])
objects = {(o.assets_file.name, o.path_id): o for o in env.objects}
template = next(o for o in env.objects if o.assets_file.name.startswith('CAB-1fe4')
                and o.path_id == -6208889883279558476)
font = objects[('sharedassets0.assets', 636)]
data = font.read_typetree(nodes=template.serialized_type.nodes)
atlas_id = data['m_AtlasTextures'][0]['m_PathID']
atlas = objects[('sharedassets0.assets', atlas_id)].read()
atlas.image.save(out / 'LinikSansBlackAtlas.png')
# Object references are restored by the editor importer, not copied as stale PPtrs.
for key in ['m_GameObject', 'm_Script', 'm_Material', 'm_SourceFontFile',
            'm_AtlasTextures', 'm_FallbackFontAssetTable', 'm_FontWeightTable',
            'fontWeights', 'fallbackFontAssets', 'atlas']:
    data.pop(key, None)
(out / 'FontSource.json').write_text(json.dumps(data, ensure_ascii=False), encoding='utf-8')

materials = []
seen = set()
for obj in env.objects:
    if obj.type.name != 'Material' or not obj.assets_file.name.startswith('sharedassets'):
        continue
    material = obj.read_typetree()
    name = material['m_Name']
    if not name.startswith('linik-sans ') or name in seen:
        continue
    seen.add(name)
    key = name[len('linik-sans '):]
    if key == 'Material':
        key = 'Base'
    props = material['m_SavedProperties']
    materials.append({'key': key, 'name': name, 'sourceFile': obj.assets_file.name,
                      'sourceId': str(obj.path_id), 'keywords': material.get('m_ValidKeywords', []),
                      'floats': [{'name': n, 'value': v} for n, v in props['m_Floats']],
                      'colors': [{'name': n, 'value': v} for n, v in props['m_Colors']]})
(out / 'MaterialsSource.json').write_text(json.dumps({'materials': materials}, ensure_ascii=False), encoding='utf-8')
(out / 'Provenance.txt').write_text(
    'Source: user-supplied Hole+it! 1.31.17 XAPK.\n'
    'Primary font: sharedassets0.assets:636, linik-sans, Linik Sans Black.\n'
    'Static SDF atlas: sharedassets0.assets:130, 512x512, padding5.\n'
    '99 original glyphs and character mappings. Source TTF is not embedded.\n'
    'Font and material values are recovered without substituting another typeface.\n'
    'Original shader: TextMeshPro/Distance Field, provided by Unity TMP essential resources.\n'
    'The custom Coton loading and home artwork is intentionally retained.\n', encoding='utf-8')
print('Exported original SDF font, atlas and', len(materials), 'material variants to', out)
