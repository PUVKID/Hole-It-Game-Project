from unity_assets import *
import wave,os
if os.environ.get('HOLEIT_ALLOW_ASSET_REGEN')!='1':
 raise SystemExit('This resets authored campaign and audio defaults. Set HOLEIT_ALLOW_ASSET_REGEN=1 only in a backed-up development copy.')
S={s['key']:s for s in json.loads((Path(__file__).resolve().parent/'specs.json').read_text())}
D='Assets/HoleIt/Data'; A='Assets/HoleIt/Audio';L='Assets/HoleIt/Resources/Data/Levels'
def wav(name,data):
 p=f'{A}/{name}.wav';(R/p).parent.mkdir(parents=True,exist_ok=True)
 data=np.nan_to_num(data);data=np.clip(data,-.85,.85)
 with wave.open(str(R/p),'wb') as f:f.setnchannels(1);f.setsampwidth(2);f.setframerate(22050);f.writeframes((data*32767).astype('<i2').tobytes())
 imp={'fileFormatVersion':2,'guid':guid(p),'AudioImporter':{'externalObjects':{},'serializedVersion':7,'defaultSettings':{'serializedVersion':2,'loadType':0,'sampleRateSetting':0,'sampleRateOverride':22050,'compressionFormat':1,'quality':.7,'conversionMode':0,'preloadAudioData':1},'platformSettingOverrides':{},'forceToMono':1,'normalize':0,'ambisonic':0,'loadInBackground':0,'preloadAudioData':1,'3D':0,'userData':'Original procedural audio v8','assetBundleName':'','assetBundleVariant':''}}
 (R/(p+'.meta')).write_text(dump(imp));return p
specaudio={'Button':(.10,650,900),'Attract':(.14,180,360),'Swallow':(.25,320,85),'Growth':(.45,420,1100),'Mission':(.45,550,880),'Clear':(.75,420,1250),'Warning':(.12,620,620),'Fail':(.55,330,90),'Booster':(.35,600,1400),'LevelStart':(.35,360,760)}
for k,(dur,f0,f1) in specaudio.items():
 t=np.arange(round(22050*dur))/22050; env=np.minimum(t/.012,1)*np.maximum(0,1-t/dur)**1.8;phase=2*np.pi*(f0*t+(f1-f0)*t*t/(2*dur));wav(k,(np.sin(phase)+.18*np.sin(phase*2))*env*.22)
t=np.arange(22050)/22050;wav('MoveLoop',.035*np.sin(2*np.pi*90*t)*(0.8+.2*np.sin(2*np.pi*2*t)))
themes={
'Dessert':{'name':'Dessert Plaza','background':(.21,.14,.35),'floorA':(.77,.60,.90),'floorB':(.69,.50,.83),'rimColor':(.99,.61,.81),'accent':(.95,.43,.66)},
'City':{'name':'Toy Town','background':(.15,.24,.39),'floorA':(.57,.77,.89),'floorB':(.47,.66,.82),'rimColor':(.98,.79,.32),'accent':(.20,.70,.92)},
'Food':{'name':'Food Truck Court','background':(.27,.19,.28),'floorA':(.96,.73,.54),'floorB':(.89,.62,.42),'rimColor':(.49,.94,.71),'accent':(.97,.43,.37)},
'Park':{'name':'Pocket Park','background':(.12,.27,.29),'floorA':(.59,.85,.70),'floorB':(.48,.76,.61),'rimColor':(1,.85,.52),'accent':(.27,.75,.62)},
'Toy':{'name':'Toy Workshop','background':(.20,.20,.40),'floorA':(.65,.69,.95),'floorB':(.54,.57,.86),'rimColor':(1,.71,.37),'accent':(.60,.48,.96)},
'Candy':{'name':'Candy Factory','background':(.27,.13,.37),'floorA':(.86,.64,.92),'floorB':(.78,.52,.85),'rimColor':(.40,.97,.90),'accent':(.89,.42,.81)}}
for ti,(k,v) in enumerate(themes.items()):
 dur=8.;t=np.arange(22050*8)/22050;music=np.zeros_like(t)
 notes=[0,4,7,12,7,4,2,7,0,4,9,7,4,2,0,7];root=220*2**(ti/12)
 for j,note in enumerate(notes):
  start=int(j*.5*22050);nt=np.arange(11025)/22050;freq=root*2**(note/12);e=np.minimum(nt/.02,1)*np.exp(-nt*7);music[start:start+len(nt)]+=.075*np.sin(2*np.pi*freq*nt)*e
 # Fade both ends to remove a loop discontinuity.
 music*=np.minimum(t/.025,1)*np.minimum((8-t)/.06,1)
 mp=wav('Music_'+k,music)
 so(f'{D}/Themes/{k}.asset','Assets/Scripts/Data/ThemeData.cs',{'themeId':k,'displayName':v['name'],**{name:color(v[name]) for name in ['background','floorA','floorB','rimColor','accent']},'lightColor':color((1,.97,.91)),'music':ref(mp,8300000,3)})
# Fixed authored campaign. Goals, timings and grid flow follow the supplied design brief.
rows=[('Dessert',0,7,7,[('DonutS',6)]),('Dessert',0,8,7,[('CookieS',8)]),('Dessert',60,9,7,[('CupcakeS',6)]),('Candy',65,9,8,[('CandyS',10)]),('City',65,10,8,[('CarS',5)]),('Food',70,10,8,[('FriesS',8),('BurgerS',3)]),('Park',75,10,9,[('DuckS',8),('Flower',5)]),('Toy',75,11,9,[('BlockS',8),('BallS',3)]),('Dessert',70,11,9,[('DonutS',8)]),('City',75,11,10,[('CarS',6),('TimeBoost',2)]),('Candy',80,12,10,[('CandyS',12),('PopsicleS',4)]),('Park',80,12,10,[('Flower',8),('StreetLamp',3)]),('City',75,12,11,[('SodaS',10),('Hydrant',3)]),('Food',80,13,10,[('BurgerS',6),('CoffeeS',4)]),('Toy',80,13,11,[('DuckS',10),('TimeBoost',2)]),('Dessert',75,13,11,[('DonutS',10),('CakeM',2)]),('City',80,14,11,[('CarS',7),('TrafficLight',3)]),('Park',85,14,12,[('DuckS',10),('Bench',3)]),('Candy',85,14,12,[('CandyS',12),('PopsicleS',6)]),('City',80,15,12,[('CarS',8),('Bus',2)]),('Food',90,15,12,[('BurgerS',10),('FriesS',10),('CoffeeS',4)]),('Toy',90,15,13,[('BlockS',12),('ToyCar',5)]),('Park',90,15,13,[('DuckS',12),('TimeBoost',3)]),('City',85,16,13,[('CarS',8),('TrafficLight',4),('Hydrant',2)]),('Dessert',85,16,13,[('DonutS',12),('CookieS',8),('CakeM',3)]),('Candy',95,16,14,[('CandyS',12),('PopsicleS',8),('MilkCartonS',5)]),('City',90,17,14,[('CandyS',12),('CarS',8),('Bus',2)]),('Toy',95,17,14,[('DuckS',10),('BlockS',8),('TimeBoost',3)]),('Food',100,18,14,[('BurgerS',10),('FriesS',8),('CoffeeS',5)]),('Candy',100,18,15,[('CandyS',15),('CarS',10),('Bus',3)])]
themeextra={'Dessert':['CookieS','WaffleS','CupcakeS','CakeS'],'City':['SodaS','MilkCartonS','TrafficCone','CarBlue','CarYellow'],'Food':['PizzaS','BananaS','CheeseS','MilkCartonS'],'Park':['PlantPot','Flower','TreeS','BallS'],'Toy':['BallS','BoxS','ScissorsS','ToyCar'],'Candy':['CandyS','SodaS','MilkCartonS','PopsicleS']}
class RNG:
 def __init__(self,seed):self.state=seed or 1
 def next(self):
  self.state ^= (self.state<<13)&0xffffffff;self.state^=self.state>>17;self.state^=(self.state<<5)&0xffffffff;self.state&=0xffffffff;return (self.state&0xffffff)/16777216

def layout(lev):
 gx,gy=lev['grid'];sx,sy=lev['size'];half=np.array([sx,sy])/2;rng=RNG(lev['seed']);placed=[];items=[]
 for g in lev['groups']:
  for i in range(g['count']):items.append((g,len(items)))
 items.sort(key=lambda v:(-S[v[0]['key']]['radius'],v[1]))
 for g,idx in items:
  radius=S[g['key']]['radius'];cluster=np.array(g['center']);cells=[]
  for y in range(gy):
   for x in range(gx):
    p=np.array([(x+.5)*sx/gx-half[0],(y+.5)*sy/gy-half[1]]);d=float(sum((p-cluster)**2));d=abs(float(np.linalg.norm(p-cluster))-g.get('radius',4))*4 if g.get('ring',False) else d;cells.append((d+rng.next()*1.7,y*gx+x,p))
  cells.sort(key=lambda t:(t[0],t[1]));found=False
  for _,order,p in cells:
   if np.any(np.abs(p)+radius+.15>half):continue
   if np.linalg.norm(p-np.array(lev['start']))<lev['clearRadius']+radius:continue
   nearest=np.clip(p,-np.maximum(0,half-3.69),np.maximum(0,half-3.69))
   if np.linalg.norm(p-nearest)>3.35:continue
   if any(np.linalg.norm(p-np.array(q['position']))<radius+S[q['key']]['radius']+.10 for q in placed):continue
   placed.append({'key':g['key'],'position':[round(float(a),5) for a in p],'yaw':180,'decorative':False});found=True;break
  if not found:raise ValueError((lev['id'],g['key'],'no cell'))
 return placed
thresholds=[0,6,16,32,56,90]
def growthpath(lev):
 counts=[0]*len(lev['missions']);used=set();growth=0.;steps=[]
 for step in range(len(lev['placements'])):
  tier=sum(growth+1e-6>=a for a in thresholds);valid=[]
  for i,p in enumerate(lev['placements']):
   if i in used or S[p['key']]['tier']>tier:continue
   active=any(counts[m]<mis['count'] and p['key']==mis['key'] for m,mis in enumerate(lev['missions']))
   if lev['rule']==1 and not active:continue
   valid.append((S[p['key']]['tier'],i))
  if not valid:break
  _,i=min(valid);used.add(i);key=lev['placements'][i]['key'];growth=min(90,growth+S[key]['growth']*lev['growth']);steps.append(key)
  for m,mis in enumerate(lev['missions']):
   if mis['key']==key:counts[m]=min(counts[m]+1,mis['count'])
  if all(counts[m]>=mis['count'] for m,mis in enumerate(lev['missions'])):return True,steps,growth
 return False,steps,growth
campaign=[]
for idx,(theme,seconds,gx,gy,goals) in enumerate(rows,1):
 rule=1 if idx in (27,30) else 2 if idx>=17 else 0
 mult=1.5 if idx>=16 else 1.
 lev={'id':idx,'title':f'{idx:02d}  {themes[theme]["name"]}','theme':theme,'time':seconds,'grid':[gx,gy],'size':[gx*2.15,gy*2.15],'start':[0,0],'clearRadius':2.1,'seed':43801+idx*7919,'growth':mult,'rule':rule,'missions':[{'key':k,'count':n,'kind':0} for k,n in goals],'groups':[]}
 # First levels remain uncluttered; later clusters reward choosing a route rather than random sweeping.
 counts={k:n+(0 if rule==1 else max(1,n//5)) for k,n in goals}
 if rule!=1:
  if idx in (5,10,17,20,24):counts['SodaS']=max(counts.get('SodaS',0),14 if idx!=20 else 22)
  if idx==16:counts['CookieS']=14
  for k in themeextra[theme]:
   if k not in counts:counts[k]=2+(idx//8)
  if seconds and 'TimeBoost' not in counts:counts['TimeBoost']=1+idx//15
 else:
  # Distractors are visibly present but never become growth fodder in strict mode.
  counts['CarBlue']=3;counts['SodaS']=4
 for gi,(key,n) in enumerate(counts.items()):
  angle=(gi*2.39996323+idx*.43);cx=math.cos(angle)*lev['size'][0]*.28;cy=math.sin(angle)*lev['size'][1]*.28
  lev['groups'].append({'key':key,'count':n,'center':[round(cx,4),round(cy,4)],'radius':4+idx*.1,'ring':idx%5==0 and gi==0})
 lev['placements']=layout(lev)
 ok,steps,growth=growthpath(lev)
 if not ok:raise RuntimeError(('growth',idx,steps,growth))
 lev['growthPathCount']=len(steps);lev['growthPathFinalXP']=round(growth,5)
 # Level5 demonstrates explicit color+category matching rather than name/material inference.
 if idx==5:lev['missions'][0]['kind']=2
 if idx in (10,15,23,28):
  for m in lev['missions']:
   if m['key']=='TimeBoost':m['kind']=4
 campaign.append(lev)
 missions=[]
 for mi,m in enumerate(lev['missions']):
  k=m['key'];missions.append({'missionId':f'L{idx:02d}_M{mi+1}','kind':m['kind'],'target':ref(f'{D}/Edibles/Edible_{k}.asset',11400000),'category':4 if k=='CarS' else 7 if k=='TimeBoost' else 2,'color':1 if k=='CarS' else 0,'tier':S[k]['tier'],'requiredCount':m['count'],'icon':ref(f'Assets/HoleIt/Textures/Icon_{k}.png',21300000,3),'description':('RED CAR' if k=='CarS' else S[k]['name'])})
 groups=[{'edible':ref(f'{D}/Edibles/Edible_{g["key"]}.asset',11400000),'count':g['count'],'center':vec(*g['center']),'radius':g['radius'],'ring':int(g['ring']),'decorative':0} for g in lev['groups']]
 placements=[{'edible':ref(f'{D}/Edibles/Edible_{p["key"]}.asset',11400000),'position':vec(*p['position']),'yaw':p['yaw'],'decorative':0} for p in lev['placements']]
 so(f'{L}/Level_{idx:02d}.asset','Assets/Scripts/Data/LevelData.cs',{'schemaVersion':8,'levelId':idx,'levelTitle':lev['title'],'difficulty':min(5,(idx+5)//6),'timeLimit':seconds,'theme':ref(f'{D}/Themes/{theme}.asset',11400000),'mapSize':max(lev['size']),'arenaSize':vec(*lev['size']),'gridSize':Flow(x=gx,y=gy),'startClearRadius':2.1,'initialSizeTier':1,'growthMultiplier':mult,'movementSpeed':8.,'holeStart':vec(0,0),'cameraZoomMultiplier':1.,'missions':missions,'defaultWrongBehavior':rule,'penaltySeconds':2.,'penaltyScore':8,'randomSeed':lev['seed'],'spawnGroups':groups,'placements':placements,'missionBonus':100,'clearBonus':200,'timeBonusPerSecond':5,'nextLevelId':idx+1 if idx<30 else 0})
# Save one plain JSON recipe consumed by the Editor builder as well as the external validator.
(R/f'{D}/CampaignRecipe.json').write_text(json.dumps({'schemaVersion':8,'levels':campaign},indent=2));meta(f'{D}/CampaignRecipe.json')
(Path(__file__).resolve().parent/'campaign.json').write_text(json.dumps(campaign,indent=2))
so('Assets/HoleIt/Resources/GameSettings.asset','Assets/Scripts/Data/GameSettings.cs',{'joystickRadius':110,'joystickDeadZone':.08,'baseSpeed':8.,'dragSensitivity':1.,'maxSpeed':11.,'acceleration':60.,'deceleration':38.,'speedByRadius':{'serializedVersion':2,'m_Curve':[{'serializedVersion':3,'time':t,'value':v,'inSlope':.12,'outSlope':.12,'tangentMode':0,'weightedMode':0,'inWeight':.333333,'outWeight':.333333} for t,v in [(1,1),(2,1.18),(4,1.35)]],'m_PreInfinity':2,'m_PostInfinity':2,'m_RotationOrder':4},'radiusFollowSpeed':9,'growthPunch':.05,'growthPunchDuration':.25,'attractRadiusFactor':1.,'attractSpeed':5.,'attractSpin':540.,'swallowDuration':.45,'swallowStagger':.06,'fallGravity':22.,'swallowEdgeFactor':.5,'maxObjectToHoleRatio':1.,'cameraSmooth':7.,'cameraZoomSmooth':3.5,'cameraPitch':58.,'cameraYaw':0.,'cameraDistance':40.,'readyDelay':1.2,'clearDelay':.6,'lowTimeWarning':10.,'topScoreCount':3})
so('Assets/HoleIt/Resources/HoleSizeTable.asset','Assets/Scripts/Data/HoleSizeTable.cs',{'tiers':[{'growthRequired':g,'radius':r,'cameraSize':c} for g,r,c in zip(thresholds,[.9,1.2,1.6,2.1,2.7,3.4],[7,8,9.2,10.6,12,13.5])]})
print('Campaign:',len(campaign),'levels;',sum(len(l['placements']) for l in campaign),'saved placements; constructive growth paths',sum(growthpath(l)[0] for l in campaign))
