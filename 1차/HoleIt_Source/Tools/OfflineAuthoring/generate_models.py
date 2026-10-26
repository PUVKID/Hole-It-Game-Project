from unity_assets import *
import trimesh, itertools
from PIL import Image,ImageDraw
SCRIPTS='Assets/Scripts/'
ROOT='Assets/HoleIt/'
PAL={'dark':(.055,.07,.11,1),'white':(.98,.98,1,1),'cream':(1,.87,.58,1),'brown':(.4,.17,.075,1),'glass':(.12,.66,.90,1),'green':(.15,.64,.27,1),'lightgreen':(.50,.90,.3,1),'red':(.94,.12,.14,1),'yellow':(1,.78,.14,1),'metal':(.64,.74,.8,1),'pink':(1,.43,.69,1),'blue':(.2,.54,.96,1)}
def col(c):return PAL.get(c,c) if isinstance(c,str) else c

def rotation(deg):
 return trimesh.transformations.euler_matrix(*np.radians(deg),axes='sxyz')

def chamfer():
 b=.38;points=[]
 for axis in range(3):
  for a,c,d in itertools.product([-1,1],repeat=3):
   v=[c*b,d*b];v.insert(axis,a*.5);points.append(v)
 return trimesh.convex.convex_hull(np.array(points))
BASE_BOX=chamfer();BASE_SPH=trimesh.creation.icosphere(subdivisions=2,radius=.5)

def cyl(bottom=.5,top=.5,n=16):
 v=[]
 for y,r in [(-.5,bottom),(.5,top)]:
  for i in range(n):v.append((math.cos(i*2*math.pi/n)*r,y,math.sin(i*2*math.pi/n)*r))
 v+= [(0,-.5,0),(0,.5,0)];f=[]
 for i in range(n):
  j=(i+1)%n;f += [(i,j,n+i),(j,n+j,n+i),(2*n,j,i),(2*n+1,n+i,n+j)]
 m=trimesh.Trimesh(v,f,process=False);trimesh.repair.fix_normals(m);return m

def torus(major=.35,tube=.12,n=24,k=8):
 v=[];f=[]
 for i in range(n):
  a=i*2*math.pi/n
  for j in range(k):
   b=j*2*math.pi/k;v.append(((major+tube*math.cos(b))*math.cos(a),tube*math.sin(b),(major+tube*math.cos(b))*math.sin(a)))
 for i in range(n):
  for j in range(k):
   a=i*k+j;b=((i+1)%n)*k+j;c=((i+1)%n)*k+(j+1)%k;d=i*k+(j+1)%k
   f.extend([(a,b,d),(b,c,d)])
 m=trimesh.Trimesh(v,f,process=False);trimesh.repair.fix_normals(m);return m

def prism(points,height=.15):
 n=len(points);v=[(x,y,z) for y in [0,height] for x,z in points];f=[]
 for i in range(1,n-1):f.extend([(0,i+1,i),(n,n+i,n+i+1)])
 for i in range(n):j=(i+1)%n;f.extend([(i,j,n+i),(j,n+j,n+i)])
 m=trimesh.Trimesh(v,f,process=False);trimesh.repair.fix_normals(m);return m

class Model:
 def __init__(self):self.parts=[]
 def part(self,mesh,c,pos=(0,0,0),scale=(1,1,1),rot=(0,0,0)):
  m=mesh.copy();m.apply_scale(scale);m.apply_transform(rotation(rot));m.apply_translation(pos);cc=col(c);cc=tuple(cc)+(1,) if len(cc)==3 else cc;self.parts.append((m,cc))
 def box(self,c,p,s,rot=(0,0,0)):self.part(BASE_BOX,c,p,s,rot)
 def sph(self,c,p,s):self.part(BASE_SPH,c,p,s)
 def cy(self,c,p,s,b=.5,t=.5,rot=(0,0,0),n=16):self.part(cyl(b,t,n),c,p,s,rot)
 def tor(self,c,p,s=(1,1,1),maj=.35,tube=.10,rot=(0,0,0)):self.part(torus(maj,tube),c,p,s,rot)
 def merged(self,scale=1,cap=.7):
  verts=[];faces=[];norm=[];colors=[];offset=0
  for m,c in self.parts:verts.extend(m.vertices);faces.extend(m.faces+offset);norm.extend(m.vertex_normals);colors.extend([c]*len(m.vertices));offset+=len(m.vertices)
  v=np.array(verts,dtype=np.float32)*scale;f=np.array(faces);n=np.array(norm,dtype=np.float32);c=np.array(colors,dtype=np.float32)
  mins,maxs=v.min(0),v.max(0);v[:,0]-=(mins[0]+maxs[0])/2;v[:,2]-=(mins[2]+maxs[2])/2;v[:,1]-=mins[1]
  radius=float(np.linalg.norm((maxs-mins)[[0,2]]/2));shrink=min(1,cap/max(radius,.01));v*=shrink
  return v,f,n,c,float(radius*shrink)

def make_model(sp):
 m=Model();b=sp['color'];shape=sp['shape'];key=sp['key']
 if shape=='Ball':
  m.sph(b,(0,.5,0),(1,1,1));
  for rot in [(0,0,0),(90,0,0),(0,0,90)]:m.tor('dark',(0,.5,0),maj=.494,tube=.018,rot=rot)
 elif shape=='Box':
  m.box(b,(0,.5,0),(1,1,1))
  for z in [-.51,.51]:
   for y in [.18,.48,.78]:m.box('cream',(0,y,z),(.84,.075,.03))
   for x in [-.4,.4]:m.box('metal',(x,.5,z),(.08,.95,.04))
 elif shape in ['Car','ToyCar']:
  bus=key=='Bus';ln=2.0 if bus else 1.3
  m.box(b,(0,.36,0),(ln,.42,.71));m.box(b,(-.08,.67,0),(ln*.66,.40,.64))
  for z in [-.329,.329]:
   if bus:
    for x in np.linspace(-.71,.68,6):m.box('glass',(x,.70,z),(.19,.26,.022))
   else:
    for x in [-.26,.17]:m.box('glass',(x,.71,z),(.32,.26,.025))
   for x in [-ln*.34,ln*.33]:
    m.cy('dark',(x,.23,z*1.14),(.39,.12,.39),rot=(90,0,0),n=12);m.cy('metal',(x,.23,z*1.34),(.21,.025,.21),rot=(90,0,0),n=12)
  for z in [-.24,.24]:m.box('cream',(ln*.5+.009,.43,z),(.035,.12,.17));m.box('red',(-ln*.5-.009,.43,z),(.035,.12,.15))
  m.box('glass',(ln*.305,.68,0),(.035,.25,.49));m.box('white',(ln*.502,.24,0),(.04,.08,.58))
 elif shape=='Donut':
  m.tor('cream',(0,.16,0),maj=.32,tube=.145);m.tor(b,(0,.225,0),(1,.57,1),maj=.32,tube=.143)
  for i in range(14):a=i*2*math.pi/14;r=.33+(.06 if i%2 else -.04);m.box(['white','yellow','pink','blue'][i%4],(math.cos(a)*r,.29,math.sin(a)*r),(.068,.029,.018),rot=(0,i*31,0))
 elif shape=='Waffle':
  m.box('brown',(0,.13,0),(.95,.25,.95));m.box('cream',(0,.24,0),(.92,.12,.92))
  for a in np.linspace(-.4,.4,5):m.box(b,(a,.33,0),(.07,.09,.9));m.box(b,(0,.33,a),(.9,.09,.07))
  m.box('yellow',(.06,.405,.02),(.24,.075,.22),rot=(0,12,0))
 elif shape=='Cookie':
  m.cy(b,(0,.13,0),(1,.25,1),n=20)
  for i in range(12):a=i*2.4;r=.08+.27*(i%4)/3;m.sph('brown',(math.cos(a)*r,.268,math.sin(a)*r),(.105,.045,.10))
 elif shape=='Cupcake':
  m.cy(b,(0,.26,0),(.85,.5,.85),b=.34,t=.50,n=14)
  for i in range(12):a=i*2*math.pi/12;m.box('white',(math.cos(a)*.31,.24,math.sin(a)*.31),(.025,.34,.03))
  for y,s in [(.51,.81),(.65,.63),(.79,.45),(.90,.24)]:m.sph('cream',(0,y,0),(s,s*.54,s))
  m.sph('red',(0,1.0,0),(.16,.18,.16))
 elif shape=='Cake':
  for y,c in [(.15,'cream'),(.30,b),(.45,'cream'),(.60,b)]:m.cy(c,(0,y,0),(1,.17,1),n=24)
  m.cy('white',(0,.70,0),(1.04,.09,1.04),n=24)
  for i in range(8):a=i*math.pi/4;m.sph('white',(math.cos(a)*.39,.78,math.sin(a)*.39),(.17,.18,.17))
  m.cy('yellow',(0,.91,0),(.05,.34,.05));m.sph('red',(0,1.13,0),(.08,.16,.08))
 elif shape=='IceCream':
  m.cy('cream',(0,.36,0),(.9,.72,.9),b=.04,t=.36,n=12)
  for y in [.36,.50,.64]:m.tor('brown',(0,y,0),(y*.92,.14,y*.92),maj=.46,tube=.025)
  m.sph(b,(0,.85,0),(.92,.80,.92));m.sph('white',(.08,1.07,0),(.36,.22,.34))
 elif shape=='Candy':
  m.sph(b,(0,.3,0),(.72,.53,.53))
  for x in [-.51,.51]:m.cy(b,(x,.3,0),(.37,.25,.37),b=.5,t=.08,rot=(0,0,90),n=8)
  m.tor('white',(0,.3,0),(.63,.40,.63),maj=.39,tube=.03,rot=(0,0,90))
 elif shape=='Soda':
  m.cy(b,(0,.51,0),(.63,1,.63),n=20);m.cy('metal',(0,1.02,0),(.63,.035,.63));m.cy('metal',(0,.02,0),(.63,.035,.63));m.tor('dark',(0,1.044,0),(.29,.3,.42),maj=.32,tube=.06);m.box('white',(0,.58,.32),(.39,.28,.018))
 elif shape=='Popsicle':
  m.box('cream',(0,.15,0),(.13,.3,.09));m.box(b,(0,.65,0),(.65,.86,.25));m.sph(b,(0,1.02,0),(.63,.31,.25))
  for x in [-.15,.15]:m.box('white',(x,.66,.13),(.05,.47,.025))
 elif shape=='MilkCarton':
  m.box('white',(0,.45,0),(.66,.86,.6));m.box('blue',(0,.49,.308),(.55,.35,.025));m.box('blue',(0,.49,-.308),(.55,.35,.025))
  roof=trimesh.Trimesh(vertices=[[-.33,0,-.3],[.33,0,-.3],[.33,0,.3],[-.33,0,.3],[-.33,.26,0],[.33,.26,0]],faces=[[0,1,4],[1,5,4],[1,2,5],[2,3,4],[2,4,5],[3,0,4],[0,3,2],[0,2,1]],process=True);trimesh.repair.fix_normals(roof);m.part(roof,b,(0,.88,0));m.box('blue',(0,1.13,0),(.68,.06,.045))
 elif shape=='Cheese':
  m.part(prism([(-.48,-.34),(.48,-.34),(0,.49)],.42),b)
  for x,z in [(-.19,-.14),(.19,-.19),(0,.2)]:m.cy('cream',(x,.427,z),(.15,.02,.15),n=12)
 elif shape=='Duck':
  m.sph(b,(0,.37,0),(.95,.64,.76));m.sph(b,(0,.74,.20),(.57,.56,.55));m.sph('yellow',(-.39,.42,0),(.2,.34,.53));m.sph('yellow',(.39,.42,0),(.2,.34,.53));m.box((1,.4,.07),(0,.65,.52),(.33,.12,.27))
  for x in [-.15,.15]:m.sph('dark',(x,.82,.423),(.055,.065,.04))
  m.sph(b,(0,.48,-.4),(.3,.35,.30))
 elif shape=='Scissors':
  for x,rot in [(-.23,-24),(.23,24)]:m.tor('pink',(x,.06,-.30),(.62,.4,.80),maj=.3,tube=.11);m.box('metal',(-x*.42,.08,.17),(.12,.055,.92),rot=(0,rot,0))
  m.cy('dark',(0,.115,.0),(.095,.025,.095),n=12)
 elif shape=='TimeBoost':
  m.cy(b,(0,.59,0),(1,.16,1),rot=(90,0,0),n=24);m.cy('white',(0,.59,.096),(.83,.028,.83),rot=(90,0,0),n=24)
  m.box('dark',(0,.73,.12),(.044,.30,.025));m.box('dark',(.11,.59,.12),(.25,.045,.025));m.sph('dark',(0,.59,.13),(.08,.08,.04));m.box('yellow',(0,1.17,0),(.27,.16,.15));m.box(b,(0,.06,0),(.35,.1,.25))
 elif shape=='Banana':
  for i in range(9):a=math.radians(-65+i*16);m.sph(b,(math.sin(a)*.48,.18+math.cos(a)*.32,0),(.22,.19,.22))
  m.box('brown',(-.46,.31,0),(.12,.13,.11),rot=(0,0,30));m.box('brown',(.47,.3,0),(.09,.12,.1),rot=(0,0,-30))
 elif shape=='Burger':
  for y,s,c in [(.15,1,'cream'),(.27,.95,'brown'),(.33,1.04,'green'),(.38,.98,'yellow'),(.44,.96,'brown'),(.51,1,'red')]:m.cy(c,(0,y,0),(s,.1,s),n=20)
  m.sph('cream',(0,.63,0),(1,.53,1))
  for i in range(12):a=i*2.3;r=.10+.22*(i%3)/2;m.box('white',(math.cos(a)*r,.86-r*.18,math.sin(a)*r),(.065,.025,.025),rot=(0,i*27,0))
 elif shape=='Coffee':
  m.cy(b,(0,.47,0),(.8,.9,.8),b=.34,t=.5,n=20);m.cy('white',(0,.48,0),(.86,.26,.86),b=.40,t=.45);m.cy('white',(0,.94,0),(.88,.1,.88));m.cy('dark',(0,1.01,0),(.75,.06,.75));m.box('cream',(.16,1.05,0),(.18,.022,.06))
 elif shape=='Pizza':
  tri=[(-.5,-.4),(.5,-.4),(0,.5)];m.part(prism(tri,.14),'cream');m.part(prism([(x*.94,z*.94) for x,z in tri],.04),'yellow',(0,.15,0));m.box('cream',(0,.19,-.4),(1,.16,.12))
  for x,z in [(-.2,-.19),(.17,-.19),(0,.2)]:m.cy('red',(x,.20,z),(.20,.026,.2),n=14)
 elif shape=='Fries':
  m.box(b,(0,.27,0),(.67,.49,.40));m.box('white',(0,.32,.205),(.32,.19,.025))
  for i in range(9):x=(i%5-2)*.118;z=(i//5-.5)*.15;y=.61+(i%3)*.055;m.box('yellow',(x,y,z),(.087,.6+(i%3)*.08,.083),rot=(0,0,(i%3-1)*6))
 elif shape in ['Tree','PlantPot','Flower']:
  if shape=='Tree':m.cy('brown',(0,.33,0),(.18,.64,.18),n=10);m.sph('green',(0,.90,0),(.97,1.2,.97));m.sph('lightgreen',(.17,1.18,.07),(.65,.65,.65))
  else:
   m.cy(b,(0,.2,0),(.62,.4,.62),b=.3,t=.5);m.cy('brown',(0,.41,0),(.57,.02,.57));m.cy('green',(0,.73,0),(.055,.67,.055),n=8)
   for x,y in [(-.17,.65),(.18,.76)]:m.sph('green',(x,y,0),(.40,.12,.19))
   if shape=='Flower':
    for i in range(6):a=i*math.pi/3;m.sph('pink',(math.cos(a)*.19,1.05+math.sin(a)*.19,.04),(.24,.24,.1))
    m.sph('yellow',(0,1.05,.10),(.20,.20,.13))
   else:m.sph('lightgreen',(0,1.01,0),(.41,.55,.4))
 elif shape=='TrafficLight':
  m.cy('dark',(0,.55,0),(.075,1.1,.075));m.cy('metal',(0,.04,0),(.36,.075,.36));m.box('dark',(0,1.15,0),(.34,.68,.25))
  for y,c in [(1.36,'red'),(1.15,'yellow'),(.94,'green')]:m.cy(c,(0,y,.143),(.16,.027,.16),rot=(90,0,0),n=12)
 elif shape=='StreetLamp':
  m.cy('dark',(0,.66,0),(.07,1.3,.07));m.cy('metal',(0,.04,0),(.33,.08,.33));m.box('cream',(0,1.38,0),(.3,.38,.3));m.box('dark',(0,1.18,0),(.39,.07,.39));m.cy('dark',(0,1.63,0),(.47,.20,.47),b=.5,t=.03,n=4)
 elif shape=='Bench':
  for x in [-.42,.42]:
   for z in [-.25,.25]:m.box('dark',(x,.21,z),(.095,.40,.095))
   m.box('dark',(x,.57,-.29),(.08,.6,.08),rot=(10,0,0))
  for z in [-.22,0,.22]:m.box(b,(0,.43,z),(1.15,.095,.18))
  for y in [.61,.80]:m.box(b,(0,y,-.34),(1.15,.16,.07))
 elif shape=='Hydrant':
  m.cy('red',(0,.31,0),(.4,.61,.4));m.cy('red',(0,.68,0),(.5,.19,.5),b=.5,t=.23);m.cy('metal',(0,.04,0),(.57,.08,.57));m.cy('red',(0,.4,0),(.22,.68,.22),rot=(0,0,90));m.cy('white',(.35,.4,0),(.24,.07,.24),rot=(0,0,90))
 elif shape=='TrafficCone':
  m.box('dark',(0,.04,0),(.85,.08,.85));m.cy(b,(0,.45,0),(.83,.80,.83),b=.42,t=.035,n=16);m.cy('white',(0,.45,0),(.8,.14,.8),b=.24,t=.18,n=16)
 elif shape=='Pillar':
  m.cy('dark',(0,.47,0),(.32,.90,.32));m.cy('yellow',(0,.57,0),(.335,.14,.335));m.cy('metal',(0,.04,0),(.6,.08,.6))
 elif shape=='Wall':
  m.box(b,(0,.34,0),(1.4,.66,.39))
  for x in [-.4,0,.4]:m.box('white',(x,.34,.207),(.13,.56,.03),rot=(0,0,-24))
 elif shape=='Building':
  tower=key=='BuildL';h=1.8 if tower else .8
  m.box(b,(0,h/2,0),(1,h,.85))
  if tower:m.box('white',(0,h+.07,0),(1.08,.14,.93))
  else:
   roof=trimesh.convex.convex_hull(np.array([[-.58,.76,-.5],[.58,.76,-.5],[-.58,.76,.5],[.58,.76,.5],[0,1.3,-.5],[0,1.3,.5]]));m.part(roof,'red')
  for z in [-.438,.438]:
   for y in np.arange(.39,h,.38):
    for x in [-.28,.28]:m.box('glass',(x,float(y),z),(.20,.23,.025))
  m.box('brown',(0,.22,.44),(.2,.43,.03))
 elif shape=='Fridge':
  m.box(b,(0,.85,0),(.9,1.7,.76));m.box('white',(0,1.3,.395),(.84,.65,.03));m.box('white',(0,.52,.395),(.84,.81,.03))
  for y in [.55,1.35]:m.box('metal',(.27,y,.44),(.045,.28,.04))
 elif shape=='Pan':
  m.cy('dark',(-.16,.13,0),(.83,.18,.83));m.cy('metal',(-.16,.21,0),(.79,.03,.79));m.cy('dark',(-.16,.23,0),(.66,.025,.66));m.box('dark',(.46,.14,0),(.67,.12,.18))
 elif shape=='Piano':
  m.box(b,(0,.34,0),(1.2,.46,.68));m.box('dark',(0,.62,-.21),(1.2,.22,.25))
  for i in range(10):m.box('white',((i-4.5)*.108,.60,.10),(.09,.11,.36))
  for i in [0,1,3,4,5,7,8]:m.box('dark',((i-4)*.108,.69,.025),(.06,.085,.18))
  for x in [-.42,.42]:m.box('dark',(x,.11,0),(.10,.25,.48))
 elif shape=='Block':
  m.box(b,(0,.24,0),(.95,.46,.63))
  for x in [-.26,.26]:
   for z in [-.16,.16]:m.cy(b,(x,.52,z),(.25,.16,.25),n=12)
 else:raise ValueError(('shape not implemented',shape,key))
 return m

def raster_icon(v,f,n,c,path,size=160):
 # An offline thumbnail renderer, not a claimed Unity screenshot.
 eye=np.array([2.5,2.7,3.7]);view=eye/np.linalg.norm(eye);right=np.cross([0,1,0],view);right/=np.linalg.norm(right);up=np.cross(view,right)
 ctr=(v.min(0)+v.max(0))/2;vv=v-ctr;xy=np.stack([vv@right,vv@up],axis=1);zz=vv@view
 span=max(np.ptp(xy,axis=0));xy=xy/max(span,.01)*(size*.84);xy[:,0]+=size/2;xy[:,1]=size/2-xy[:,1]
 img=np.zeros((size,size,4),np.float32);depth=np.full((size,size),-1e6)
 light=np.array([-.3,.8,.55]);light/=np.linalg.norm(light)
 for face in f:
  pts=xy[face];xmin=max(0,int(np.floor(pts[:,0].min())));xmax=min(size-1,int(np.ceil(pts[:,0].max())));ymin=max(0,int(np.floor(pts[:,1].min())));ymax=min(size-1,int(np.ceil(pts[:,1].max())))
  if xmax<xmin or ymax<ymin:continue
  a,b,d=pts;den=(b[1]-d[1])*(a[0]-d[0])+(d[0]-b[0])*(a[1]-d[1])
  if abs(den)<1e-6:continue
  xx,yy=np.meshgrid(np.arange(xmin,xmax+1)+.5,np.arange(ymin,ymax+1)+.5)
  w0=((b[1]-d[1])*(xx-d[0])+(d[0]-b[0])*(yy-d[1]))/den;w1=((d[1]-a[1])*(xx-d[0])+(a[0]-d[0])*(yy-d[1]))/den;w2=1-w0-w1
  z=w0*zz[face[0]]+w1*zz[face[1]]+w2*zz[face[2]];sub=depth[ymin:ymax+1,xmin:xmax+1];mask=(w0>=0)&(w1>=0)&(w2>=0)&(z>sub)
  if not mask.any():continue
  nn=w0[...,None]*n[face[0]]+w1[...,None]*n[face[1]]+w2[...,None]*n[face[2]];shade=.61+.39*np.maximum(0,nn@light)
  colors=(w0[...,None]*c[face[0],:3]+w1[...,None]*c[face[1],:3]+w2[...,None]*c[face[2],:3])*shade[...,None]
  img[ymin:ymax+1,xmin:xmax+1,:3][mask]=colors[mask];img[ymin:ymax+1,xmin:xmax+1,3][mask]=1;sub[mask]=z[mask]
 im=Image.fromarray(np.uint8(np.clip(img,0,1)*255)).resize((128,128),Image.Resampling.LANCZOS);p=R/path;p.parent.mkdir(parents=True,exist_ok=True);im.save(p)
 texture_meta(path)

def texture_meta(path,border=(0,0,0,0)):
 p=R/(path+'.meta');p.parent.mkdir(parents=True,exist_ok=True)
 b={'fileFormatVersion':2,'guid':guid(path),'TextureImporter':{'internalIDToNameTable':[],'externalObjects':{},'serializedVersion':13,'mipmaps':{'mipMapMode':0,'enableMipMap':0,'sRGBTexture':1,'linearTexture':0,'fadeOut':0,'borderMipMap':0,'mipMapsPreserveCoverage':0,'alphaTestReferenceValue':.5,'mipMapFadeDistanceStart':1,'mipMapFadeDistanceEnd':3},'isReadable':0,'streamingMipmaps':0,'textureFormat':1,'maxTextureSize':256,'textureSettings':{'serializedVersion':2,'filterMode':1,'aniso':1,'mipBias':0,'wrapU':1,'wrapV':1,'wrapW':1},'nPOTScale':0,'lightmap':0,'compressionQuality':50,'spriteMode':1,'spriteExtrude':1,'spriteMeshType':1,'alignment':0,'spritePivot':vec(.5,.5),'spritePixelsToUnits':100,'spriteBorder':vec(*border),'spriteGenerateFallbackPhysicsShape':0,'alphaUsage':1,'alphaIsTransparency':1,'textureType':8,'textureShape':1,'singleChannelComponent':0,'flipbookRows':1,'flipbookColumns':1,'platformSettings':[{'serializedVersion':3,'buildTarget':'DefaultTexturePlatform','maxTextureSize':256,'resizeAlgorithm':0,'textureFormat':-1,'textureCompression':0,'compressionQuality':50,'crunchedCompression':0,'allowsAlphaSplitting':0,'overridden':0,'ignorePlatformSupport':0,'androidETC2FallbackOverride':0,'forceMaximumCompressionQuality_BC6H_BC7':0}], 'spriteSheet':{'serializedVersion':2,'sprites':[],'outline':[],'physicsShape':[],'bones':[],'spriteID':guid(path+'sprite'),'internalID':0,'vertices':[],'indices':'','edges':[],'weights':[],'secondaryTextures':[],'nameFileIdTable':{}},'userData':'HoleItGenerated:v8','assetBundleName':'','assetBundleVariant':''}}
 p.write_text(dump(b))

def parse_specs():
 """Use the canonical catalog; do not append new variants again on a second run."""
 catalog=json.loads((Path(__file__).resolve().parent/'specs.json').read_text())
 assert len(catalog)==51 and len({s['key'] for s in catalog})==51
 return catalog

def main():
 sp=parse_specs();models=[]
 shader='Assets/Shaders/VertexLit.shader';(R/shader).write_text('''Shader "HoleIt/VertexLit" {
Properties { _Color ("Tint", Color)=(1,1,1,1) }
SubShader { Tags { "RenderType"="Opaque" "Queue"="Geometry+2" } LOD 150
CGPROGRAM
#pragma surface surf Lambert addshadow fullforwardshadows vertex:vert
#pragma target 3.0
#pragma multi_compile_instancing
struct Input { fixed4 color : COLOR; };
fixed4 _Color;
void vert(inout appdata_full v, out Input o) { UNITY_INITIALIZE_OUTPUT(Input,o); o.color=v.color; }
void surf(Input IN,inout SurfaceOutput o) { o.Albedo=IN.color.rgb*_Color.rgb; o.Alpha=1; }
ENDCG
} FallBack "Diffuse" }
''');meta(shader)
 material(ROOT+'Materials/VertexLit.mat',shader,{'_Color':(1,1,1,1)})
 catnames=['Ball','Box','Food','Tree','Car','Decoration','Obstacle','TimeBoost','Toy','Kitchen'];caps=[.70,1.05,1.40,1.83,2.37,2.99]
 for s in sp:
  mod=make_model(s);v,f,n,c,r=mod.merged(s['size'],caps[s['tier']-1]);s['radius']=round(r,6);key=s['key'];mp=ROOT+'Meshes/Completed_'+key+'.asset';pf=ROOT+'Prefabs/Edible_'+key+'.prefab';dp=ROOT+'Data/Edibles/Edible_'+key+'.asset';ip=ROOT+'Textures/Icon_'+key+'.png'
  info=mesh_asset(mp,v,f,n,c);info.update(key=key,parts=len(mod.parts));models.append(info);raster_icon(v,f,n,c,ip)
  sc=Scene();root=sc.go('Edible_'+key,layer=9);sc.body(root);mn=v.min(0);mx=v.max(0);sc.collider(root,size=mx-mn,center=(mx+mn)/2);sc.mono(root,SCRIPTS+'Objects/EdibleObject.cs',{'data':ref(dp,11400000)});sc.render(root,mp,ROOT+'Materials/VertexLit.mat');sc.save(pf);s['prefabFileId']=root['gid']
  so(dp,SCRIPTS+'Data/EdibleData.cs',{'displayName':s['name'],'objectId':key,'colorVariant':s['variant'],'footprintRadius':s['radius'],'prefab':ref(pf,root['gid'],3),'icon':ref(ip,21300000,3),'uiColor':color(s['color']),'objectType':catnames.index(s['category']),'sizeTier':s['tier'],'score':s['score'],'growthValue':s['growth'],'specialTimeBonus':8 if s['category']=='TimeBoost' else 0,'canBeSwallowed':1,'swallowPriority':0,'overrideWrongBehavior':0,'wrongBehavior':0})
  # Geometry is already persisted in Unity Mesh assets; no redundant binary cache is distributed.
 (R/'Documentation/ModelInventory.json').write_text(json.dumps(models,indent=2));(Path(__file__).resolve().parent/'specs.json').write_text(json.dumps(sp,indent=2))
 # Core geometric assets. Ground: 10 x 10 xz; mask/rim: horizontal 1 x 1; pit: radius .5, height 2.
 for key,v,f,uv in [('Floor',[[-5,0,-5],[5,0,-5],[5,0,5],[-5,0,5]],[[0,2,1],[0,3,2]],[[0,0],[1,0],[1,1],[0,1]]),('QuadXZ',[[-.5,0,-.5],[.5,0,-.5],[.5,0,.5],[-.5,0,.5]],[[0,2,1],[0,3,2]],[[0,0],[1,0],[1,1],[0,1]])]:mesh_asset(ROOT+'Meshes/'+key+'.asset',v,f,uv=uv)
 pit=cyl(n=40);pit.apply_scale((1,2,1));mesh_asset(ROOT+'Meshes/PitCylinder.asset',pit.vertices,pit.faces,pit.vertex_normals)
 mesh_asset(ROOT+'Meshes/Rail.asset',BASE_BOX.vertices,BASE_BOX.faces,BASE_BOX.vertex_normals)
 ring=torus(.5,.025,40,6);mesh_asset(ROOT+'Meshes/FXRing.asset',ring.vertices,ring.faces,ring.vertex_normals)
 # Original generated UI assets: no text or font pixels are baked into any sprite.
 for name,kind in [('Circle','circle'),('Ring','ring'),('Panel','panel'),('ButtonMint','button')]:
  im=Image.new('RGBA',(128,128));dr=ImageDraw.Draw(im)
  if kind=='circle':dr.ellipse((2,2,126,126),fill='white')
  elif kind=='ring':dr.ellipse((2,2,126,126),fill='white');dr.ellipse((14,14,114,114),fill=(0,0,0,0))
  else:dr.rounded_rectangle((0,0,127,127),radius=22,fill='white')
  path=ROOT+'Textures/'+name+'.png';im.save(R/path);texture_meta(path,(24,24,24,24) if kind in ['panel','button'] else (0,0,0,0))
 print('Generated',len(sp),'model prefabs, icons and edible data; total triangles',sum(x['triangles'] for x in models))
if __name__=='__main__':
 import os
 if os.environ.get('HOLEIT_ALLOW_ASSET_REGEN')!='1':
  raise SystemExit('Asset regeneration overwrites generated model/data assets. Set HOLEIT_ALLOW_ASSET_REGEN=1 only in a backed-up development copy.')
 main()
