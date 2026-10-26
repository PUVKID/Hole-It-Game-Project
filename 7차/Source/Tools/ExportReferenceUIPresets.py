import argparse, json, pathlib, sys, re, uuid
p=argparse.ArgumentParser();p.add_argument('--audit',required=True);p.add_argument('--reference-dir',required=True);p.add_argument('--unitypy-path',required=True);a=p.parse_args()
root=pathlib.Path(__file__).resolve().parents[1]; audit=json.loads(pathlib.Path(a.audit).read_text(encoding='utf-8-sig'))
ids={'Loading':('level1',1457),'HomeLevel':('level1',1426),'CoinCount':('level1',1448),'HeartCount':('level1',1476),'HeartTimer':('level1',1454),'HeartFull':('level1',1504),'SettingsTitle':('level1',1472),'SoundTitle':('level1',1456),'ShopTitle':('level1',1429),'Collect':('level1',1443),'Timer':('level2',2355),'HUDLevel':('level2',2333),'PauseTitle':('level2',2286),'Continue':('level2',2239),'Next':('level2',2248),'Retry':('level2',2343),'TimeUp':('level2',2347),'LevelFailed':('level2',2329),'BombFailed':('level2',2274),'LevelTitle':('level2',2284),'MoveHole':('level2',2341),'Exit':('level2',2298),'WinCoins':('level2',2321),'Buy':('level1',1488),'Refill':('level1',1460)}
records=[]
for key,(scene,pid) in ids.items():
 t=next(x for x in audit['texts'] if x['scene']==scene and x['id']==pid)
 mat=t['material'].removeprefix('linik-sans ');mat='Base' if mat=='Material' else mat
 rec={'key':key,'scene':scene,'sourceId':pid,'hierarchy':t['hierarchy'],'text':t['m_text'],'material':mat,'size':t['m_fontSize'],'min':t['m_fontSizeMin'],'max':t['m_fontSizeMax'],'autoSize':bool(t['m_enableAutoSizing']),'style':t['m_fontStyle'],'horizontal':t['m_HorizontalAlignment'],'vertical':t['m_VerticalAlignment'],'spacing':t['m_characterSpacing'],'wordSpacing':t['m_wordSpacing'],'lineSpacing':t['m_lineSpacing'],'paragraphSpacing':t['m_paragraphSpacing'],'wrapping':t['m_TextWrappingMode'],'overflow':t['m_overflowMode'],'margin':t['m_margin'],'extraPadding':bool(t['m_enableExtraPadding']),'color':t['m_fontColor']}
 records.append(rec)
(root/'Assets/Resources/ReferenceTypography/TextPresets.json').write_text(json.dumps({'presets':records},ensure_ascii=False,indent=2),encoding='utf-8')
sys.path.insert(0,a.unitypy_path);import UnityPy
ref=pathlib.Path(a.reference_dir); env=UnityPy.load(*[str(q) for q in ref.rglob('*.unity3d')],*[str(q) for q in ref.glob('*.bundle')])
objects={(o.assets_file.name,o.path_id):o for o in env.objects}
template=objects[('CAB-1fe4b43663f71a311a59b149b4422a07',-8828100010140662438)]
extra_sources=[('GoalCount','sharedassets2.assets',571,'Outline_InCanvas'),('StarGoalCount','sharedassets2.assets',569,'Outline_InCanvas'),('WorldHoleSize','level2',2234,'Outline_InGame')]
raw=[]
for key,file,pid,material in extra_sources:
 o=objects[(file,pid)];t=o.read_typetree(nodes=template.serialized_type.nodes)
 if key=='WorldHoleSize':
  # The common TMP base fields are shared; the UGUI-only tail is not a world-text field.
  t.pop('m_maskOffset',None);t.pop('m_baseMaterial',None)
 raw.append({'file':file,'id':pid,'componentType':'TextMeshPro' if key=='WorldHoleSize' else 'TextMeshProUGUI','data':t})
 rec={'key':key,'scene':file,'sourceId':pid,'hierarchy':objects[(file,t['m_GameObject']['m_PathID'])].read().m_Name,'text':t['m_text'],'material':material,'size':t['m_fontSize'],'min':t['m_fontSizeMin'],'max':t['m_fontSizeMax'],'autoSize':bool(t['m_enableAutoSizing']),'style':t['m_fontStyle'],'horizontal':t['m_HorizontalAlignment'],'vertical':t['m_VerticalAlignment'],'spacing':t['m_characterSpacing'],'wordSpacing':t['m_wordSpacing'],'lineSpacing':t['m_lineSpacing'],'paragraphSpacing':t['m_paragraphSpacing'],'wrapping':t['m_TextWrappingMode'],'overflow':t['m_overflowMode'],'margin':t['m_margin'],'extraPadding':bool(t['m_enableExtraPadding']),'color':t['m_fontColor']}
 records.append(rec)
(root/'Assets/Resources/ReferenceTypography/TextPresets.json').write_text(json.dumps({'presets':records},ensure_ascii=False,indent=2),encoding='utf-8')
(root/'Assets/Resources/ReferenceTypography/GoalGrowthSource.json').write_text(json.dumps(raw,indent=2),encoding='utf-8')
names={'point-panel','Heart','heart-loop','sound-icon','cross','btn-small-green','btn-small-yellow','blue-info-card','SpScreen-progress','clock-body','clock-white','clock-dial','b-BG','r-BG','a-BG','v-BG','o-BG','!-BG','b','r','a','v','o','!','OK'}
style=root/'Assets/Resources/ReferenceStyle';meta=(style/'Coin-UI.png.meta').read_text(encoding='utf-8');provenance=[]
for obj in env.objects:
 if obj.type.name!='Sprite':continue
 data=obj.read()
 if data.m_Name not in names:continue
 name=data.m_Name;data.image.save(style/(name+'.png'));m=meta; m=re.sub(r'^guid: .*$', 'guid: '+uuid.uuid4().hex,m,flags=re.M);m=re.sub(r'^  spriteID: .*$', '  spriteID: '+uuid.uuid4().hex,m,flags=re.M)
 meta_path=style/(name+'.png.meta'); meta_path.write_text(meta_path.read_text(encoding='utf-8') if meta_path.exists() else m,encoding='utf-8');provenance.append({'name':name,'asset':obj.assets_file.name,'id':obj.path_id});names.remove(name)
 if not names:break
if names:raise RuntimeError('Missing source sprites '+str(names))
(root/'Assets/Resources/ReferenceTypography/Edition7UIProvenance.json').write_text(json.dumps({'source':'User-supplied Hole+it! 1.31.17 XAPK','sprites':provenance,'textAudit':a.audit},indent=2),encoding='utf-8')
print('Exported',len(records),'exact text presets and',len(provenance),'source sprites')