from pathlib import Path
import json,hashlib,re,xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parent.parent
A=ROOT/'Audit'
manifest=json.loads((A/'FinalRegressionSourceHashes.json').read_text(encoding='utf-8'))
changed=[]
for relative,expected in manifest['files'].items():
    file=ROOT/'Source'/relative
    if not file.exists() or hashlib.sha256(file.read_bytes()).hexdigest()!=expected:changed.append(relative)
if changed:raise RuntimeError('C# snapshot changed during final verification: '+str(changed))
raw=(A/'Tests-final.xml').read_bytes();root=ET.fromstring(raw)
if not root.get('end-time'):raise RuntimeError('Final run is not complete')
cases=list(root.iter('test-case'))
if any('StageProbeTests' in c.get('fullname','') for c in cases):raise RuntimeError('Diagnostic fixtures included')
if int(root.get('total'))!=len(cases):raise RuntimeError('Case count mismatch')
failures=[{'name':c.get('fullname'),'result':c.get('result'),'message':c.findtext('failure/message')} for c in cases if c.get('result')!='Passed']
lines=(ROOT/'Source'/'TestResults'/'ReferenceCampaign.txt').read_text(encoding='utf-8').splitlines()
stages=[]
for line in lines:
    m=re.match(r'Level (\d+): (\w+); collected=(\d+); radius=([^;]+); simulated=([^;]+); realElapsed=([^;]+); sourceBudget=([^;]+); goals=([^;]*); stop=([^;]+)',line)
    if m:stages.append({'stage':int(m[1]),'state':m[2],'collected':int(m[3]),'radius':float(m[4]),'simulatedSeconds':float(m[5]),'realSeconds':float(m[6]),'sourceBudgetInformational':float(m[7]),'goals':[int(x) for x in m[8].split(',') if x],'stop':m[9]})
summary={'source':'Tests-final.xml','sha256':hashlib.sha256(raw).hexdigest(),'result':root.get('result'),'total':len(cases),'passed':sum(c.get('result')=='Passed' for c in cases),'failures':failures,'start':root.get('start-time'),'end':root.get('end-time'),'sourceSnapshotUnchanged':True,'campaign':{'stagesAttempted':len(stages),'stagesCompleted':[s['stage'] for s in stages if s['state']=='Won'],'unresolvedStages':[s['stage'] for s in stages if s['state']!='Won'],'stages':stages,'notHumanTimingBenchmark':True}}
(A/'FinalVerificationSummary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
d=json.loads((A/'PhysicsFidelityAudit7.json').read_text(encoding='utf-8'));d['finalRegression']={k:v for k,v in summary.items() if k!='campaign'};d['finalCampaign']=summary['campaign'];d['campaignVerificationLimitations']['status']='Final bounded suite completed; see finalRegression/finalCampaign for exact unresolved cases';(A/'PhysicsFidelityAudit7.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
p=A/'PhysicsFidelity.txt';s=p.read_text(encoding='utf-8');old='Final verification status: the frozen-source full regression suite is running as Tests-final.xml/log. Do not treat earlier focused passes as a final all-stage result. FinalRegressionSourceHashes.json records the C# snapshot used for this run.'
new='Final verification: '+str(summary['passed'])+'/'+str(summary['total'])+' NUnit cases passed in the fresh14-fixture run (Tests-final.xml/log). The campaign completed '+str(len(summary['campaign']['stagesCompleted']))+'/'+str(len(stages))+' attempted stages.'
if summary['campaign']['unresolvedStages']:new+=' Unresolved automated stages: '+', '.join(map(str,summary['campaign']['unresolvedStages']))+'. These failures are preserved; no all-stage or error-free claim is made.'
new+=' FinalVerificationSummary.json records exact outcomes. All107 C# files matched FinalRegressionSourceHashes.json after the run.'
if old not in s:raise RuntimeError('Final summary paragraph already replaced or missing')
p.write_text(s.replace(old,new),encoding='utf-8')
print(json.dumps({k:v for k,v in summary.items() if k not in ['failures','campaign']},indent=2));print('Unresolved campaign stages',summary['campaign']['unresolvedStages']);print('Failures:',[f['name'] for f in failures])
